using System.Collections.Concurrent;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;

namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

internal sealed record ResultadoCadenaCertificadoSri(
    EstadoCadenaCertificadoSri EstadoCadena,
    EstadoRevocacionCertificadoSri EstadoRevocacion,
    IReadOnlyList<string> Errores,
    IReadOnlyList<string> Advertencias,
    IReadOnlyList<string> Detalles,
    DateTimeOffset? FechaRevocacion = null);

public sealed partial class ValidadorCertificadoSri : IValidadorCertificadoSri
{
    private const int DiasProximoACaducar = 30;
    private static readonly TimeSpan VigenciaCacheConfirmada =
        TimeSpan.FromHours(6);
    private static readonly TimeSpan VigenciaCacheDesconocida =
        TimeSpan.FromMinutes(5);

    private readonly TimeProvider timeProvider;
    private readonly Func<X509Certificate2, X509Certificate2Collection,
        ResultadoCadenaCertificadoSri>
        evaluarCadena;
    private readonly ConcurrentDictionary<string, CacheCadena> cache = new();

    public ValidadorCertificadoSri(TimeProvider timeProvider)
        : this(timeProvider, EvaluarCadenaSistema)
    {
    }

    internal ValidadorCertificadoSri(
        TimeProvider timeProvider,
        Func<X509Certificate2, ResultadoCadenaCertificadoSri> evaluarCadena)
        : this(timeProvider, (certificate, _) => evaluarCadena(certificate))
    {
    }

    private ValidadorCertificadoSri(
        TimeProvider timeProvider,
        Func<X509Certificate2, X509Certificate2Collection,
            ResultadoCadenaCertificadoSri> evaluarCadena)
    {
        this.timeProvider = timeProvider;
        this.evaluarCadena = evaluarCadena;
    }

    internal ValidadorCertificadoSri(
        TimeProvider timeProvider,
        Func<X509Certificate2, bool> validarCadena)
        : this(timeProvider, certificate => validarCadena(certificate)
            ? new(
                EstadoCadenaCertificadoSri.Valida,
                EstadoRevocacionCertificadoSri.NoRevocado,
                [], [], [])
            : new(
                EstadoCadenaCertificadoSri.NoValida,
                EstadoRevocacionCertificadoSri.NoComprobado,
                ["La cadena de certificación no es confiable."],
                [], []))
    {
    }

    public ResultadoValidacionCertificadoSri Validar(
        ReadOnlyMemory<byte> pkcs12,
        ReadOnlySpan<char> password,
        string? rucEsperado = null) =>
        ValidarCompleto(pkcs12, password, rucEsperado,
            forzarConsultaRevocacion: false);

    public ResultadoValidacionCertificadoSri ValidarLocal(
        ReadOnlyMemory<byte> pkcs12,
        ReadOnlySpan<char> password,
        string? rucEsperado = null) =>
        ValidarInterno(pkcs12, password, rucEsperado, null);

    public Task<ResultadoValidacionCertificadoSri> ValidarAsync(
        ReadOnlyMemory<byte> pkcs12,
        string password,
        string? rucEsperado = null,
        bool forzarConsultaRevocacion = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(password);
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValidarCompleto(pkcs12, password.AsSpan(), rucEsperado,
                forzarConsultaRevocacion);
        }, cancellationToken);
    }

    private ResultadoValidacionCertificadoSri ValidarCompleto(
        ReadOnlyMemory<byte> pkcs12,
        ReadOnlySpan<char> password,
        string? rucEsperado,
        bool forzarConsultaRevocacion)
    {
        CertificadosPkcs12 certificates;
        try
        {
            certificates = Cargar(pkcs12, password);
        }
        catch (CryptographicException)
        {
            return ArchivoInvalido();
        }

        using (certificates)
        {
            var certificate = certificates.Firma;
            var key = certificate.Thumbprint;
            ResultadoCadenaCertificadoSri chainResult;
            if (!forzarConsultaRevocacion &&
                cache.TryGetValue(key, out var cached) &&
                cached.ExpiraAt > timeProvider.GetUtcNow())
            {
                chainResult = cached.Resultado;
            }
            else
            {
                chainResult = evaluarCadena(certificate,
                    certificates.Adicionales);
                var duration = chainResult.EstadoRevocacion ==
                               EstadoRevocacionCertificadoSri.Desconocido
                    ? VigenciaCacheDesconocida
                    : VigenciaCacheConfirmada;
                cache[key] = new(chainResult,
                    timeProvider.GetUtcNow().Add(duration));
            }
            return ValidarCertificadoCargado(certificate, rucEsperado,
                chainResult);
        }
    }

    private ResultadoValidacionCertificadoSri ValidarInterno(
        ReadOnlyMemory<byte> pkcs12,
        ReadOnlySpan<char> password,
        string? rucEsperado,
        ResultadoCadenaCertificadoSri? chainResult)
    {
        CertificadosPkcs12 certificates;
        try
        {
            certificates = Cargar(pkcs12, password);
        }
        catch (CryptographicException)
        {
            return ArchivoInvalido();
        }
        using (certificates)
            return ValidarCertificadoCargado(certificates.Firma, rucEsperado,
                chainResult);
    }

    private ResultadoValidacionCertificadoSri ValidarCertificadoCargado(
        X509Certificate2 certificate,
        string? rucEsperado,
        ResultadoCadenaCertificadoSri? chainResult)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var now = timeProvider.GetLocalNow();
        var from = new DateTimeOffset(certificate.NotBefore);
        var until = new DateTimeOffset(certificate.NotAfter);
        var hasRsa = false;
        using (var rsa = certificate.GetRSAPrivateKey())
        {
            if (!certificate.HasPrivateKey)
                errors.Add("El certificado no contiene una clave privada.");
            if (rsa is null)
                errors.Add("La clave privada debe ser RSA.");
            else
            {
                hasRsa = true;
                if (rsa.KeySize < 2048)
                    errors.Add("La clave RSA debe tener al menos 2048 bits.");
            }
        }
        if (now < from)
            errors.Add("El certificado todavía no está vigente.");
        if (now > until)
            errors.Add("El certificado está caducado.");
        if (!PermiteFirmaDigital(certificate))
            errors.Add("El certificado no está habilitado para firma digital.");

        var identification = ObtenerIdentificacion(certificate);
        if (!string.IsNullOrWhiteSpace(rucEsperado))
        {
            if (identification is null)
                errors.Add(
                    "No fue posible acreditar el RUC del titular en el certificado.");
            else if (!string.Equals(identification, rucEsperado,
                         StringComparison.Ordinal))
                errors.Add("El certificado corresponde a otra identificación.");
        }

        if (chainResult is not null)
        {
            errors.AddRange(chainResult.Errores.Where(x =>
                chainResult.EstadoRevocacion ==
                EstadoRevocacionCertificadoSri.Revocado));
            warnings.AddRange(chainResult.Advertencias);
        }

        var days = (int)Math.Floor((until - now).TotalDays);
        var info = new CertificadoFirmaSriInfo(
            certificate.GetNameInfo(X509NameType.SimpleName, false),
            certificate.GetNameInfo(X509NameType.SimpleName, true),
            certificate.SerialNumber,
            identification,
            from,
            until,
            certificate.HasPrivateKey,
            hasRsa,
            now >= from && now <= until,
            days)
        {
            Thumbprint = certificate.Thumbprint
        };

        var chainState = chainResult?.EstadoCadena ??
                         EstadoCadenaCertificadoSri.NoComprobada;
        var revocationState = chainResult?.EstadoRevocacion ??
                              EstadoRevocacionCertificadoSri.NoComprobado;
        var valid = errors.Count == 0 &&
                    revocationState != EstadoRevocacionCertificadoSri.Revocado;
        return new(valid, info, errors)
        {
            Estado = DeterminarEstado(certificate, now, until, days,
                chainState, revocationState, errors),
            EstadoCadena = chainState,
            EstadoRevocacion = revocationState,
            FechaRevocacion = chainResult?.FechaRevocacion,
            Advertencias = warnings,
            DetallesCadena = chainResult?.Detalles ?? []
        };
    }

    private static EstadoCertificadoSri DeterminarEstado(
        X509Certificate2 certificate,
        DateTimeOffset now,
        DateTimeOffset until,
        int days,
        EstadoCadenaCertificadoSri chainState,
        EstadoRevocacionCertificadoSri revocationState,
        IReadOnlyCollection<string> errors)
    {
        if (!certificate.HasPrivateKey)
            return EstadoCertificadoSri.SinClavePrivada;
        if (now < new DateTimeOffset(certificate.NotBefore))
            return EstadoCertificadoSri.AunNoVigente;
        if (now > until) return EstadoCertificadoSri.Caducado;
        if (revocationState == EstadoRevocacionCertificadoSri.Revocado)
            return EstadoCertificadoSri.Revocado;
        if (errors.Count > 0) return EstadoCertificadoSri.Error;
        if (revocationState is EstadoRevocacionCertificadoSri.Desconocido or
            EstadoRevocacionCertificadoSri.NoComprobado)
            return EstadoCertificadoSri.EstadoRevocacionDesconocido;
        if (days <= DiasProximoACaducar)
            return EstadoCertificadoSri.ProximoACaducar;
        return EstadoCertificadoSri.Valido;
    }

    private static CertificadosPkcs12 Cargar(
        ReadOnlyMemory<byte> pkcs12,
        ReadOnlySpan<char> password)
    {
        if (pkcs12.IsEmpty) throw new CryptographicException();
        var collection = X509CertificateLoader.LoadPkcs12Collection(
            pkcs12.Span, password, X509KeyStorageFlags.EphemeralKeySet);
        if (collection.Count == 0) throw new CryptographicException();
        var signingCertificate = collection.Cast<X509Certificate2>()
            .FirstOrDefault(x => x.HasPrivateKey) ?? collection[0];
        var additionalCertificates = new X509Certificate2Collection();
        foreach (var certificate in collection)
            if (!ReferenceEquals(certificate, signingCertificate))
                additionalCertificates.Add(certificate);
        return new(signingCertificate, additionalCertificates, collection);
    }

    private static ResultadoValidacionCertificadoSri ArchivoInvalido() =>
        new(false, null,
            ["El archivo no es un PKCS#12 válido o la contraseña es incorrecta."])
        {
            Estado = EstadoCertificadoSri.ArchivoOContrasenaInvalida
        };

    internal static ResultadoCadenaCertificadoSri InterpretarEstadosCadena(
        bool buildSucceeded,
        IEnumerable<X509ChainStatus> statuses)
    {
        var flags = statuses
            .Where(x => x.Status != X509ChainStatusFlags.NoError)
            .GroupBy(x => x.Status)
            .Select(x => x.First())
            .ToArray();
        var revoked = flags.Any(x =>
            x.Status.HasFlag(X509ChainStatusFlags.Revoked));
        var unknownRevocation = flags.Any(x =>
            x.Status.HasFlag(X509ChainStatusFlags.RevocationStatusUnknown) ||
            x.Status.HasFlag(X509ChainStatusFlags.OfflineRevocation));
        var blocking = flags.Where(x =>
                !SoloEstadoRevocacion(x.Status))
            .ToArray();
        var chainState = blocking.Length == 0 &&
                         (buildSucceeded || unknownRevocation || revoked)
            ? EstadoCadenaCertificadoSri.Valida
            : EstadoCadenaCertificadoSri.NoValida;
        var onlyUntrustedRoot = blocking.Length > 0 && blocking.All(x =>
            (x.Status & ~X509ChainStatusFlags.UntrustedRoot) == 0);
        var revocationState = revoked
            ? EstadoRevocacionCertificadoSri.Revocado
            : unknownRevocation
                ? EstadoRevocacionCertificadoSri.Desconocido
                : chainState == EstadoCadenaCertificadoSri.Valida ||
                  onlyUntrustedRoot
                    ? EstadoRevocacionCertificadoSri.NoRevocado
                    : EstadoRevocacionCertificadoSri.NoComprobado;
        var details = flags.Select(DetalleEstadoCadena).ToArray();
        var errors = new List<string>();
        var warnings = new List<string>();
        if (revoked)
            errors.Add("El certificado está revocado y no puede utilizarse.");
        if (unknownRevocation && !revoked)
            warnings.Add(
                "El certificado está vigente, pero no fue posible comprobar su estado de revocación. Revisa la conexión con CRL/OCSP e inténtalo nuevamente.");
        else if (revocationState ==
                 EstadoRevocacionCertificadoSri.NoComprobado)
            warnings.Add(
                "El certificado está vigente, pero su estado de revocación no pudo comprobarse en este momento.");
        return new(chainState, revocationState, errors, warnings, details);
    }

    private static ResultadoCadenaCertificadoSri EvaluarCadenaSistema(
        X509Certificate2 certificate,
        X509Certificate2Collection additionalCertificates)
    {
        try
        {
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
            chain.ChainPolicy.RevocationFlag =
                X509RevocationFlag.EndCertificateOnly;
            chain.ChainPolicy.VerificationFlags =
                X509VerificationFlags.NoFlag;
            chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(15);
            chain.ChainPolicy.DisableCertificateDownloads = false;
            chain.ChainPolicy.ExtraStore.AddRange(additionalCertificates);
            var built = chain.Build(certificate);
            var statuses = chain.ChainStatus
                .Concat(chain.ChainElements.Cast<X509ChainElement>()
                    .SelectMany(x => x.ChainElementStatus))
                .ToArray();
            return InterpretarEstadosCadena(built, statuses);
        }
        catch (CryptographicException exception)
        {
            return new(
                EstadoCadenaCertificadoSri.NoValida,
                EstadoRevocacionCertificadoSri.NoComprobado,
                [],
                ["No fue posible comprobar el estado de revocación del certificado."],
                [$"Error criptográfico: {exception.HResult:X8}"]);
        }
    }

    private static bool SoloEstadoRevocacion(X509ChainStatusFlags status)
    {
        var allowed = X509ChainStatusFlags.Revoked |
                      X509ChainStatusFlags.RevocationStatusUnknown |
                      X509ChainStatusFlags.OfflineRevocation;
        return (status & ~allowed) == 0;
    }

    private static string DetalleEstadoCadena(X509ChainStatus status) =>
        status.Status switch
        {
            X509ChainStatusFlags.UntrustedRoot =>
                "la autoridad raíz no es confiable en Windows",
            X509ChainStatusFlags.PartialChain =>
                "falta un certificado intermedio o raíz",
            X509ChainStatusFlags.NotTimeValid =>
                "un certificado de la cadena no está vigente",
            X509ChainStatusFlags.Revoked => "revocación confirmada",
            X509ChainStatusFlags.RevocationStatusUnknown =>
                "el estado de revocación es desconocido",
            X509ChainStatusFlags.OfflineRevocation =>
                "el servicio CRL/OCSP no estuvo disponible",
            X509ChainStatusFlags.NotValidForUsage =>
                "la cadena no permite el uso solicitado",
            X509ChainStatusFlags.HasWeakSignature =>
                "la cadena contiene una firma criptográfica débil",
            X509ChainStatusFlags.ExplicitDistrust =>
                "Windows desconfía explícitamente del certificado",
            _ => string.IsNullOrWhiteSpace(status.StatusInformation)
                ? status.Status.ToString()
                : status.StatusInformation.Trim()
        };

    private static bool PermiteFirmaDigital(X509Certificate2 certificate)
    {
        var extension = certificate.Extensions
            .OfType<X509KeyUsageExtension>()
            .FirstOrDefault();
        return extension is null ||
            extension.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature) ||
            extension.KeyUsages.HasFlag(X509KeyUsageFlags.NonRepudiation);
    }

    private static string? ObtenerIdentificacion(X509Certificate2 certificate)
    {
        var candidates = new[]
        {
            certificate.Subject,
            certificate.GetNameInfo(X509NameType.SimpleName, false),
            ObtenerSerialSubject(certificate)
        };
        foreach (var candidate in candidates.Where(x =>
                     !string.IsNullOrWhiteSpace(x)))
        {
            var match = RucRegex().Match(candidate!);
            if (match.Success) return match.Value;
        }
        return null;
    }

    private static string? ObtenerSerialSubject(X509Certificate2 certificate)
    {
        try
        {
            foreach (var item in certificate.SubjectName
                         .EnumerateRelativeDistinguishedNames())
                if (item.GetSingleElementType().Value == "2.5.4.5")
                    return item.GetSingleElementValue();
        }
        catch (AsnContentException)
        {
        }
        return null;
    }

    private sealed record CacheCadena(
        ResultadoCadenaCertificadoSri Resultado,
        DateTimeOffset ExpiraAt);

    private sealed class CertificadosPkcs12(
        X509Certificate2 firma,
        X509Certificate2Collection adicionales,
        X509Certificate2Collection todos) : IDisposable
    {
        public X509Certificate2 Firma { get; } = firma;
        public X509Certificate2Collection Adicionales { get; } = adicionales;

        public void Dispose()
        {
            foreach (var certificate in todos) certificate.Dispose();
        }
    }

    [GeneratedRegex(@"(?<!\d)\d{13}(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex RucRegex();
}
