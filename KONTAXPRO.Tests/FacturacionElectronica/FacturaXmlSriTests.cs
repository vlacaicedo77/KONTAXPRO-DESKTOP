using System.Xml;
using KONTAXPRO.Application.FacturacionElectronica;
using KONTAXPRO.Application.Models.FacturacionElectronica;
using KONTAXPRO.Infrastructure.FacturacionElectronica;

namespace KONTAXPRO.Tests.FacturacionElectronica;

public sealed class FacturaXmlSriTests
{
    [Fact]
    public void Genera_factura_21_valida_con_caracteres_especiales_y_decimales_invariantes()
    {
        var factura = CrearFactura();
        var documento = new GeneradorXmlFacturaSri().Generar(factura);
        var resultado = new ValidadorXmlSri().ValidarFactura(documento);

        Assert.True(resultado.EsValido, string.Join(Environment.NewLine, resultado.Errores));
        Assert.Equal("comprobante", documento.DocumentElement!.GetAttribute("id"));
        Assert.Equal("2.1.0", documento.DocumentElement.GetAttribute("version"));
        Assert.Equal("SERVICIO & ASESORÍA", documento.SelectSingleNode("/factura/detalles/detalle/descripcion")!.InnerText);
        Assert.Equal("1.5", documento.SelectSingleNode("/factura/detalles/detalle/cantidad")!.InnerText);
        Assert.Equal("15.00", documento.SelectSingleNode("/factura/infoFactura/importeTotal")!.InnerText);
    }

    [Fact]
    public void Conserva_multiples_detalles_impuestos_pagos_e_informacion_adicional()
    {
        var baseFactura = CrearFactura();
        var segundo = baseFactura.Detalles[0] with
        {
            CodigoPrincipal = "P002",
            Descripcion = "SEGUNDO PRODUCTO",
            Cantidad = 1,
            PrecioUnitario = 0,
            PrecioTotalSinImpuesto = 0,
            Impuestos = [new("2", "0", 0, 0, 0)]
        };
        var factura = baseFactura with
        {
            Detalles = [baseFactura.Detalles[0], segundo],
            Pagos = [new("01", 10), new("20", 5, 30, "dias")],
            InformacionAdicional = [
                new("Email", "cliente@ejemplo.com"),
                new("RUC Proveedor", "1790012345001")]
        };

        var documento = new GeneradorXmlFacturaSri().Generar(factura);
        var resultado = new ValidadorXmlSri().ValidarFactura(documento);

        Assert.True(resultado.EsValido, string.Join(Environment.NewLine, resultado.Errores));
        Assert.Equal(2, documento.SelectNodes("/factura/detalles/detalle")!.Count);
        Assert.Equal(2, documento.SelectNodes("/factura/infoFactura/pagos/pago")!.Count);
        Assert.Equal(2, documento.SelectNodes("/factura/infoAdicional/campoAdicional")!.Count);
    }

    [Theory]
    [InlineData("RIMPE", "CONTRIBUYENTE RÉGIMEN RIMPE")]
    [InlineData("RIMPE_NEGOCIO_POPULAR", "CONTRIBUYENTE NEGOCIO POPULAR - RÉGIMEN RIMPE")]
    [InlineData("GENERAL", null)]
    public void Resuelve_leyenda_segun_regimen_de_la_empresa(
        string codigoRegimen,
        string? leyendaEsperada)
    {
        Assert.Equal(
            leyendaEsperada,
            GeneradorXmlComprobanteElectronico.ObtenerLeyendaRegimen(codigoRegimen));
    }

    [Theory]
    [InlineData("CONTRIBUYENTE RÉGIMEN RIMPE")]
    [InlineData("CONTRIBUYENTE NEGOCIO POPULAR - RÉGIMEN RIMPE")]
    public void Genera_leyenda_rimpe_valida_en_info_tributaria(string leyenda)
    {
        var documento = new GeneradorXmlFacturaSri().Generar(
            CrearFactura() with { LeyendaRegimen = leyenda });
        var resultado = new ValidadorXmlSri().ValidarFactura(documento);

        Assert.True(resultado.EsValido, string.Join(Environment.NewLine, resultado.Errores));
        Assert.Equal(
            leyenda,
            documento.SelectSingleNode("/factura/infoTributaria/contribuyenteRimpe")!.InnerText);
    }

    [Fact]
    public void Xsd_detecta_campo_obligatorio_ausente()
    {
        var documento = new GeneradorXmlFacturaSri().Generar(CrearFactura());
        documento.SelectSingleNode("/factura/infoFactura/importeTotal")!.ParentNode!
            .RemoveChild(documento.SelectSingleNode("/factura/infoFactura/importeTotal")!);

        var resultado = new ValidadorXmlSri().ValidarFactura(documento);

        Assert.False(resultado.EsValido);
        Assert.NotEmpty(resultado.Errores);
    }

    [Fact]
    public void Xsd_detecta_orden_incorrecto()
    {
        var documento = new GeneradorXmlFacturaSri().Generar(CrearFactura());
        var info = documento.SelectSingleNode("/factura/infoFactura")!;
        var fecha = info.SelectSingleNode("fechaEmision")!;
        info.RemoveChild(fecha);
        info.AppendChild(fecha);

        var resultado = new ValidadorXmlSri().ValidarFactura(documento);

        Assert.False(resultado.EsValido);
    }

    [Fact]
    public void Rechaza_dtd_y_entidades_externas()
    {
        const string xml = "<!DOCTYPE factura [<!ENTITY xxe SYSTEM 'file:///etc/passwd'>]><factura>&xxe;</factura>";
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };
        using var textReader = new StringReader(xml);
        using var reader = XmlReader.Create(textReader, settings);
        Assert.ThrowsAny<XmlException>(() => { while (reader.Read()) { } });
    }

    private static FacturaElectronicaSri CrearFactura()
    {
        var generador = new GeneradorClaveAccesoSri();
        var clave = generador.Generar(new(
            new DateOnly(2026, 8, 16), "01", "1790012345001", 1,
            "001", "001", 1, "12345678"));

        return new(
            clave, 1, 1, "1790012345001", "EMPRESA DE PRUEBAS S.A.",
            "KONTAXPRO DEMO", "AV. PRINCIPAL 123", "001", "001", 1,
            new DateOnly(2026, 8, 16), "AV. PRINCIPAL 123", null, true,
            null, "05", "CLIENTE DE PRUEBA", "2300257942",
            "CALLE SECUNDARIA", 13.04m, 0, 15m,
            [new("2", "4", 13.04m, 15m, 1.96m)],
            [new("SERVICIO & ASESORÍA", 1.5m, 8.693333m, 0, 13.04m,
                [new("2", "4", 15m, 13.04m, 1.96m)], "P001")],
            [new("01", 15m)],
            [new("Email", "cliente@ejemplo.com")]);
    }
}
