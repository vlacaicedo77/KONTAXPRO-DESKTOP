using System.Collections.Concurrent;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Interoperabilidad;

namespace KONTAXPRO.Infrastructure.Interoperabilidad;

public sealed class ConstanciaVerificacionIdentificacionStore(
    TimeProvider timeProvider)
    : IConstanciaVerificacionIdentificacionStore
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<Guid, ConstanciaVerificacionIdentificacion>
        _proofs = new();
    private readonly ConcurrentDictionary<AttemptKey, AttemptState> _attempts =
        new();

    internal ConsultaIdentificacionResult RegisterResult(
        ConsultaIdentificacionRequest request,
        ConsultaIdentificacionResult result,
        long usuarioId)
    {
        if (usuarioId <= 0)
            return result;

        var type = request.TipoIdentificacionCodigo.Trim().ToUpperInvariant();
        var number = result.NumeroNormalizado.Trim();
        if (string.IsNullOrEmpty(number))
            return result;

        if (result.Encontrado)
        {
            RemoveAttempt(request, usuarioId, type, number);
            return result with
            {
                ConstanciaVerificacionId = Issue(
                    usuarioId,
                    type,
                    number,
                    request.Proposito,
                    TipoConstanciaVerificacion.Verificada,
                    result)
            };
        }

        if (result.Estado != EstadoConsultaIdentificacion.FuentesNoDisponibles ||
            request.FlujoId == Guid.Empty)
        {
            RemoveAttempt(request, usuarioId, type, number);
            return result;
        }

        var key = CreateAttemptKey(request, usuarioId, type, number);
        var now = timeProvider.GetUtcNow();
        PurgeExpired(now);
        var attempt = _attempts.AddOrUpdate(
            key,
            new AttemptState(1, now),
            (_, current) => current.UpdatedAt.Add(Lifetime) <= now
                ? new AttemptState(1, now)
                : new AttemptState(current.Count + 1, now));
        if (attempt.Count < 3)
            return result;

        _attempts.TryRemove(key, out _);
        return result with
        {
            ConstanciaVerificacionId = Issue(
                usuarioId,
                type,
                number,
                request.Proposito,
                TipoConstanciaVerificacion.OfflineAutorizada,
                result)
        };
    }

    public bool TryTake(
        Guid constanciaId,
        long usuarioId,
        string tipoIdentificacionCodigo,
        string numeroNormalizado,
        PropositoConsultaIdentificacion proposito,
        out ConstanciaVerificacionIdentificacion? constancia)
    {
        constancia = null;
        if (!_proofs.TryGetValue(constanciaId, out var stored))
            return false;

        var now = timeProvider.GetUtcNow();
        if (stored.ExpiraAt <= now)
        {
            _proofs.TryRemove(constanciaId, out _);
            return false;
        }

        if (stored.UsuarioId != usuarioId ||
            stored.Proposito != proposito ||
            !string.Equals(
                stored.TipoIdentificacionCodigo,
                tipoIdentificacionCodigo.Trim(),
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                stored.NumeroNormalizado,
                numeroNormalizado.Trim(),
                StringComparison.Ordinal))
            return false;

        if (!_proofs.TryRemove(
                new KeyValuePair<Guid, ConstanciaVerificacionIdentificacion>(
                    constanciaId,
                    stored)))
            return false;

        constancia = stored;
        return true;
    }

    private Guid Issue(
        long usuarioId,
        string type,
        string number,
        PropositoConsultaIdentificacion purpose,
        TipoConstanciaVerificacion proofType,
        ConsultaIdentificacionResult result)
    {
        var now = timeProvider.GetUtcNow();
        PurgeExpired(now);
        var id = Guid.NewGuid();
        _proofs[id] = new ConstanciaVerificacionIdentificacion(
            id,
            usuarioId,
            type,
            number,
            purpose,
            proofType,
            proofType == TipoConstanciaVerificacion.Verificada
                ? result.RazonSocial?.Trim()
                : null,
            proofType == TipoConstanciaVerificacion.Verificada
                ? NullIfWhiteSpace(result.NombreComercial)
                : null,
            proofType == TipoConstanciaVerificacion.Verificada
                ? NullIfWhiteSpace(result.Fuente)
                : "MANUAL",
            now.Add(Lifetime));
        return id;
    }

    private void PurgeExpired(DateTimeOffset now)
    {
        foreach (var proof in _proofs)
        {
            if (proof.Value.ExpiraAt <= now)
                _proofs.TryRemove(proof.Key, out _);
        }
        foreach (var attempt in _attempts)
        {
            if (attempt.Value.UpdatedAt.Add(Lifetime) <= now)
                _attempts.TryRemove(attempt.Key, out _);
        }
    }

    private void RemoveAttempt(
        ConsultaIdentificacionRequest request,
        long usuarioId,
        string type,
        string number)
    {
        if (request.FlujoId != Guid.Empty)
            _attempts.TryRemove(
                CreateAttemptKey(request, usuarioId, type, number),
                out _);
    }

    private static AttemptKey CreateAttemptKey(
        ConsultaIdentificacionRequest request,
        long usuarioId,
        string type,
        string number) =>
        new(usuarioId, request.FlujoId, type, number, request.Proposito);

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private readonly record struct AttemptKey(
        long UsuarioId,
        Guid FlujoId,
        string Tipo,
        string Numero,
        PropositoConsultaIdentificacion Proposito);

    private readonly record struct AttemptState(
        int Count,
        DateTimeOffset UpdatedAt);
}
