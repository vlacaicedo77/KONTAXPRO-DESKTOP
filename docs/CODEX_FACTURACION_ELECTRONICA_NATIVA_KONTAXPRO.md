# KONTAXPRO Desktop — Motor nativo de Facturación Electrónica SRI (.NET 10)

> **Documento de ejecución para Codex**
>
> Ubicación recomendada dentro del repositorio: `docs/FACTURACION_ELECTRONICA_NATIVA.md`
>
> Objetivo: implementar en KONTAXPRO Desktop un motor de comprobantes electrónicos del SRI **100 % nativo en .NET 10**, reemplazando la dependencia histórica de librerías Java/JAR para generación, firma, envío, consulta de autorización y, en una fase separada, generación de RIDE.

---

## 1. Regla principal

Implementa esta funcionalidad **sin Java en tiempo de ejecución**.

No usar:

- JRE/JDK instalados como requisito del sistema.
- `java.exe`, `Process.Start()` ni ejecución de comandos Java.
- IKVM.
- Bridges .NET ↔ Java.
- Los `.jar` históricos como dependencia de producción.
- Código Java descompilado copiado dentro de C#.
- Claves privadas, contraseñas de certificados o certificados reales dentro del repositorio.
- Certificados SSL del SRI fijados o “quemados” en código.

Los JAR analizados sirven únicamente como **referencia funcional del sistema legado**. La implementación nueva debe basarse en:

1. La **Ficha Técnica de Comprobantes Electrónicos – Esquema Off-line** vigente del SRI.
2. Los **XSD/XML oficiales** publicados por el SRI.
3. Estándares XMLDSIG/XAdES requeridos por el SRI.
4. APIs criptográficas y XML nativas de .NET.
5. Pruebas automatizadas que reproduzcan los contratos y comportamientos requeridos.

Si existe cualquier diferencia entre este documento, los JAR históricos y la documentación vigente del SRI, **manda la documentación vigente del SRI**.

Al momento de redactar este documento, la página oficial del SRI publica como esquema actual la **Ficha Técnica Off-line versión 2.34, actualizada a julio de 2026**. Verifica nuevamente la versión al iniciar el trabajo.

---

# 2. Antes de escribir código

## 2.1. Inspección obligatoria del repositorio

Antes de modificar nada:

1. Lee el `AGENTS.md` de la raíz.
2. Busca y lee cualquier `AGENTS.md` adicional que afecte las carpetas que vas a tocar.
3. Lee los documentos relevantes de `/docs`.
4. Revisa la solución completa y confirma los proyectos reales y sus dependencias.
5. Localiza:
   - entidades de empresa;
   - establecimientos;
   - puntos de emisión;
   - secuenciales;
   - clientes;
   - productos;
   - impuestos;
   - ventas;
   - configuración;
   - almacenamiento de secretos;
   - logging;
   - servicios HTTP;
   - pruebas existentes;
   - patrones de DI;
   - patrones de persistencia;
   - convenciones de nombres y namespaces.
6. Reutiliza abstractions, DTOs, servicios y patrones existentes cuando ya resuelvan parte del problema.
7. **No dupliques entidades ni servicios existentes.**
8. No cambies arquitectura, UI global ni convenciones del proyecto salvo que sea estrictamente necesario.

## 2.2. Construcción inicial

Ejecuta primero:

```powershell
dotnet build
dotnet test
```

Registra el estado inicial. Si ya existen fallos ajenos a este desarrollo, documenta cuáles son y no los confundas con regresiones nuevas.

---

# 3. Resultado esperado

Al terminar debe existir un flujo nativo similar a:

```text
Datos de venta
    ↓
Generar clave de acceso
    ↓
Construir XML según XSD del SRI
    ↓
Validar XML
    ↓
Firmar XML con certificado .p12/.pfx
    ↓
Verificar criptográficamente la firma generada
    ↓
Enviar XML firmado a Recepción SRI
    ↓
RECIBIDA / DEVUELTA
    ↓
Si RECIBIDA:
Consultar Autorización
    ↓
PPR / AUTORIZADO / NO AUTORIZADO
    ↓
Persistir resultado
    ↓
Disponibilizar XML autorizado
    ↓
Generar RIDE en fase posterior
```

Todo debe ser testeable sin depender de la interfaz gráfica.

---

# 4. Hallazgos confirmados en las librerías Java históricas

Se analizaron las librerías que utiliza actualmente el sistema Java:

| JAR legado | Responsabilidad observada | Reemplazo nativo propuesto |
|---|---|---|
| `MITyCLibXADES-1.1.7.jar` | XAdES-BES | `System.Security.Cryptography.Xml` + construcción controlada de nodos XAdES |
| `MITyCLibAPI-1.1.7.jar` | Infraestructura de firma | Servicios C# propios |
| `MITyCLibCert-1.1.7.jar` | Certificados | `X509CertificateLoader`, `X509Certificate2`, RSA |
| `xmlsec-1.4.2-ADSI-1.1.jar` | XMLDSIG/canonicalización | `SignedXml`, transforms XMLDSIG de .NET |
| `bcmail-jdk16-1.43.jar` | Criptografía/certificados auxiliar | Criptografía nativa de .NET; no introducir BouncyCastle salvo necesidad demostrada |
| `FacturaElectronicaSRIOffline.jar` | Orquestación, clave, firma | Servicios Application/Infrastructure |
| `cliente-ws-offline.jar` | Cliente JAX-WS generado | Cliente SOAP nativo con `HttpClient` |
| `sysconmEnlaceSRIOffline.jar` | Recepción/autorización SRI | `ISriReceptionClient` / `ISriAuthorizationClient` |
| `RideSri.jar` | Generación de RIDE | `IRideGenerator` + solución PDF .NET |
| `Fpdf.jar` | PDF | Reemplazar por stack PDF .NET existente o uno aprobado |
| `EnSystemsFact.jar` | UI/orquestación Swing | WPF + MVVM actual de KONTAXPRO |
| `EnSystems.utils.jar` | Configuración/utilidades | Configuración/servicios existentes de KONTAXPRO |

También se verificó en el legado:

- El certificado se carga como **PKCS#12**.
- Se busca un certificado con clave privada apta para firma.
- Se usa XAdES-BES.
- La firma es enveloped.
- El nodo objetivo es `id="comprobante"`.
- Se usa UTF-8.
- El proceso hace primero recepción y luego autorización.
- Los Web Services históricos coinciden con los endpoints Offline del SRI.
- La clave de acceso usa Módulo 11 con ponderadores repetidos 2..7 desde la derecha.

No copies el código descompilado. Reproduce únicamente el comportamiento exigido por el protocolo oficial.

---

# 5. Arquitectura deseada

Adapta nombres y carpetas después de inspeccionar el repositorio. No fuerces estas rutas si el proyecto ya tiene una convención equivalente.

La separación conceptual debe quedar aproximadamente así:

```text
KONTAXPRO.Application
└── FacturacionElectronica/
    ├── Abstractions/
    │   ├── IAccessKeyGenerator.cs
    │   ├── IElectronicDocumentXmlBuilder.cs
    │   ├── IXmlSchemaValidator.cs
    │   ├── IXadesBesSigner.cs
    │   ├── ISriReceptionClient.cs
    │   ├── ISriAuthorizationClient.cs
    │   └── IElectronicInvoicingService.cs
    ├── Models/
    └── Services/

KONTAXPRO.Infrastructure
└── FacturacionElectronica/
    └── Sri/
        ├── AccessKey/
        ├── Xml/
        ├── Schemas/
        ├── Signing/
        ├── Soap/
        └── Persistence/

Tests
└── FacturacionElectronica/
```

### Responsabilidades por capa

**Application**

- Contratos.
- Casos de uso.
- Modelos de resultado.
- Orquestación del flujo.
- Sin detalles de `HttpClient`, XMLDSIG, archivos P12 ni EF/SQLite/PostgreSQL.

**Infrastructure**

- Implementación de firma.
- Lectura de certificados.
- XML/XSD.
- SOAP.
- almacenamiento/persistencia.
- configuración técnica.

**Desktop**

- Solo integración con ViewModels y presentación.
- No colocar criptografía ni SOAP en ViewModels/code-behind.

---

# 6. Implementar primero el núcleo sin UI

No empieces por pantallas.

Primero entrega un motor funcional con pruebas. La UI de Ventas se conecta después.

---

# 7. Modelos base

Crea o adapta modelos similares a estos conceptos:

```text
SriEnvironment
- Test
- Production

ElectronicDocumentType
- Invoice              01
- PurchaseLiquidation  03
- CreditNote           04
- DebitNote            05
- RemissionGuide       06
- Withholding          07

SriReceptionStatus
- Received
- Returned
- Unknown

SriAuthorizationStatus
- Processing
- Authorized
- NotAuthorized
- Unknown

SriMessage
- Identifier
- Message
- AdditionalInformation
- Type
```

No mezclar el estado de **recepción** con el estado de **autorización**.

Crear resultados explícitos, por ejemplo:

```text
SriReceptionResult
SriAuthorizationResult
ElectronicSubmissionResult
```

Nunca usar `bool` para representar todo el proceso.

---

# 8. Clave de acceso SRI

Implementa `IAccessKeyGenerator`.

La clave contiene 49 dígitos:

| Campo | Longitud |
|---|---:|
| Fecha de emisión `ddMMyyyy` | 8 |
| Tipo de comprobante | 2 |
| RUC | 13 |
| Ambiente | 1 |
| Serie: establecimiento + punto de emisión | 6 |
| Secuencial | 9 |
| Código numérico | 8 |
| Tipo de emisión | 1 |
| Dígito verificador Módulo 11 | 1 |

Los primeros 48 dígitos se someten a Módulo 11.

## 8.1. Módulo 11

Implementar exactamente:

1. Recorrer los 48 dígitos desde la derecha hacia la izquierda.
2. Multiplicar sucesivamente por factores `2,3,4,5,6,7`.
3. Reiniciar el factor en `2` después de `7`.
4. Sumar los productos.
5. Calcular:

```text
resultado = 11 - (suma % 11)
```

6. Si el resultado es `11`, dígito = `0`.
7. Si el resultado es `10`, dígito = `1`.
8. En otro caso, usar el resultado.

No aceptar claves mal formadas silenciosamente.

Validar:

- RUC exactamente 13 caracteres numéricos.
- establecimiento: 3 dígitos.
- punto de emisión: 3 dígitos.
- secuencial: 9 dígitos con `PadLeft`.
- código numérico: 8 dígitos.
- ambiente permitido.
- tipo de emisión válido.
- longitud final: 49.
- únicamente caracteres `0-9`.

## 8.2. Código numérico

El SRI permite que el emisor defina el algoritmo del código numérico.

Implementa un generador de 8 dígitos usando una fuente segura como:

```csharp
RandomNumberGenerator
```

No usar `Random` compartido para esta finalidad.

La generación de la clave debe admitir además recibir un código numérico explícito para pruebas deterministas.

---

# 9. Generación de XML

Crear una abstracción de construcción de XML por tipo de documento.

No concatenar XML con strings.

Usar:

- `XmlWriter`;
- clases/records DTO específicos;
- serialización controlada;
- `CultureInfo.InvariantCulture`;
- formatos decimales exactos;
- UTF-8.

El elemento raíz del comprobante debe conservar:

```xml
id="comprobante"
```

y su atributo de versión correspondiente.

## 9.1. Autoridad de esquemas

Los XSD oficiales deben estar disponibles localmente en el proyecto/aplicación.

No descargar XSD en cada emisión.

Crear un `SriSchemaCatalog` o equivalente que resuelva:

```text
tipo de comprobante + versión -> XSD
```

Al momento de este documento, la página oficial lista:

- Factura: 1.0.0, 1.1.0, 2.0.0, 2.1.0.
- Nota de Crédito: 1.0.0, 1.1.0.
- Nota de Débito: 1.0.0.
- Liquidación de Compra: 1.0.0, 1.1.0.
- Guía de Remisión: 1.0.0, 1.1.0.
- Comprobante de Retención: 1.0.0, 2.0.0.

Antes de implementar cada documento verifica la ficha/XSD vigente.

## 9.2. Alcance inicial

Completa primero una **Factura end-to-end**.

No intentes construir todos los comprobantes al mismo tiempo.

Orden de extensión recomendado después de factura:

1. Nota de crédito.
2. Nota de débito.
3. Guía de remisión.
4. Los demás solo cuando el alcance funcional de KONTAXPRO los requiera.

La infraestructura de clave, firma, XSD, SOAP y estados debe ser reutilizable por todos.

---

# 10. Validación XSD

Implementa `IXmlSchemaValidator` usando `XmlSchemaSet`.

Reglas:

- `DtdProcessing = Prohibit`.
- `XmlResolver = null` cuando corresponda.
- No resolver entidades externas.
- Recoger **todos** los errores y advertencias relevantes.
- Devolver un resultado estructurado.
- No enviar al SRI un XML que ya falla contra su XSD local.

Crear pruebas con:

- XML válido.
- tag obligatorio ausente.
- longitud incorrecta.
- decimal inválido.
- enum/código inválido.

---

# 11. Firma electrónica nativa XAdES-BES

Esta es la parte crítica.

Crear `IXadesBesSigner`.

Usar como primera opción:

```text
System.Security.Cryptography
System.Security.Cryptography.X509Certificates
System.Security.Cryptography.Xml
System.Xml
```

Si `System.Security.Cryptography.Xml` no está referenciado, agregar el paquete/assembly oficial de Microsoft correspondiente a la versión objetivo.

No agregar una librería criptográfica de terceros salvo que una prueba concreta demuestre que .NET no puede cumplir un requisito del SRI. Si eso ocurre, documenta la limitación y la justificación antes de introducirla.

---

# 12. Carga del certificado PKCS#12

Para .NET moderno usar:

```csharp
X509CertificateLoader.LoadPkcs12FromFile(...)
```

o la variante de colección cuando sea necesario inspeccionar múltiples certificados.

El `.p12`/`.pfx` debe:

- cargarse con contraseña;
- contener un certificado con clave privada;
- ser RSA para este protocolo;
- estar dentro de su período de validez;
- tener uso de clave compatible con firma digital cuando la extensión Key Usage esté presente.

Si el P12 contiene más de un certificado, seleccionar de forma explícita el que tenga la clave privada apta para firmar.

No persistir innecesariamente la clave privada en el almacén de Windows. Preferir una carga efímera cuando sea compatible con el certificado del cliente.

Manejar claramente:

- archivo inexistente;
- contraseña incorrecta;
- P12 corrupto;
- ausencia de clave privada;
- certificado vencido;
- algoritmo no RSA;
- certificado sin uso permitido para firma.

Nunca incluir la contraseña en excepciones, logs o telemetría.

---

# 13. Perfil de firma exigido por SRI

La implementación debe construir la firma que exige el SRI, tomando la ficha técnica vigente como autoridad.

Base verificada:

```text
Formato: XAdES-BES
Esquema XAdES: 1.3.2
Encoding: UTF-8
Tipo: ENVELOPED
Algoritmo de firma: RSA-SHA1
Digest: SHA1
Canonicalización: XML C14N 1.0 sin comentarios
PKCS#12: .p12/.pfx
```

Namespaces:

```text
XMLDSIG:
http://www.w3.org/2000/09/xmldsig#

XAdES 1.3.2:
http://uri.etsi.org/01903/v1.3.2#

SignedProperties Type:
http://uri.etsi.org/01903#SignedProperties
```

**Importante:** SHA-1 no debe convertirse en una elección criptográfica general de KONTAXPRO. Se usa únicamente dentro de este protocolo porque forma parte de la especificación de interoperabilidad del SRI. No reutilizarlo para contraseñas, tokens, integridad interna ni otros fines.

---

# 14. Anatomía obligatoria de la firma

El comprobante debe terminar conceptualmente con una estructura equivalente a:

```xml
<factura id="comprobante" version="...">
    ...
    <ds:Signature Id="Signature...">
        <ds:SignedInfo Id="Signature-SignedInfo...">
            <ds:CanonicalizationMethod ... />
            <ds:SignatureMethod ... />

            <ds:Reference
                Type="http://uri.etsi.org/01903#SignedProperties"
                URI="#...SignedProperties...">
                ...
            </ds:Reference>

            <ds:Reference URI="#...Certificate...">
                ...
            </ds:Reference>

            <ds:Reference Id="Reference-ID-..." URI="#comprobante">
                <ds:Transforms>
                    <ds:Transform
                      Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature" />
                </ds:Transforms>
                ...
            </ds:Reference>
        </ds:SignedInfo>

        <ds:SignatureValue Id="SignatureValue..." />

        <ds:KeyInfo Id="Certificate...">
            <ds:X509Data>
                <ds:X509Certificate>...</ds:X509Certificate>
            </ds:X509Data>
            <ds:KeyValue>
                <ds:RSAKeyValue>
                    <ds:Modulus>...</ds:Modulus>
                    <ds:Exponent>...</ds:Exponent>
                </ds:RSAKeyValue>
            </ds:KeyValue>
        </ds:KeyInfo>

        <ds:Object Id="...Object...">
            <etsi:QualifyingProperties Target="#Signature...">
                <etsi:SignedProperties Id="...SignedProperties...">
                    <etsi:SignedSignatureProperties>
                        <etsi:SigningTime>...</etsi:SigningTime>
                        <etsi:SigningCertificate>
                            ...
                        </etsi:SigningCertificate>
                    </etsi:SignedSignatureProperties>

                    <etsi:SignedDataObjectProperties>
                        <etsi:DataObjectFormat
                           ObjectReference="#Reference-ID-...">
                            <etsi:Description>
                                contenido comprobante
                            </etsi:Description>
                            <etsi:MimeType>text/xml</etsi:MimeType>
                        </etsi:DataObjectFormat>
                    </etsi:SignedDataObjectProperties>
                </etsi:SignedProperties>
            </etsi:QualifyingProperties>
        </ds:Object>
    </ds:Signature>
</factura>
```

No usar este ejemplo como reemplazo de la ficha técnica; es únicamente la guía estructural.

---

# 15. Referencias que deben firmarse

La firma debe proteger como mínimo:

1. `SignedProperties`.
2. `KeyInfo` / certificado.
3. Todo el comprobante identificado por:

```xml
id="comprobante"
```

La referencia del comprobante debe llevar el transform:

```text
http://www.w3.org/2000/09/xmldsig#enveloped-signature
```

Usar SHA1 como `DigestMethod` donde lo exige la ficha.

---

# 16. `SignedProperties`

Construir XAdES explícitamente.

Incluir:

```text
QualifyingProperties
SignedProperties
SignedSignatureProperties
SigningTime
SigningCertificate
Cert
CertDigest
IssuerSerial
SignedDataObjectProperties
DataObjectFormat
Description
MimeType
```

## 16.1. SigningTime

Usar `DateTimeOffset`, no `DateTime` ambiguo.

Mantener zona horaria explícita, por ejemplo:

```text
yyyy-MM-ddTHH:mm:sszzz
```

Inyectar `TimeProvider` para que sea testeable.

No depender de `DateTime.Now` dentro del servicio de firma.

## 16.2. CertDigest

El digest del certificado debe calcularse sobre los bytes DER del certificado según el algoritmo requerido por el SRI.

## 16.3. IssuerSerial

Generar correctamente:

- `X509IssuerName`.
- `X509SerialNumber` en representación decimal.

Cuidado: el orden de bytes/serial de `X509Certificate2` puede resultar confuso. Implementa conversión explícita y crea pruebas con un certificado cuyo serial sea conocido.

---

# 17. IDs XML

Generar IDs únicos válidos para cada firma:

```text
Signature...
Signature-SignedInfo...
SignatureValue...
Certificate...
Reference-ID-...
...Object...
...SignedProperties...
```

No reutilizar IDs entre documentos.

Pueden derivarse de un generador seguro/UUID transformado a un identificador XML válido.

Para pruebas, permitir inyectar un generador determinista.

---

# 18. Punto crítico: resolución de IDs con `SignedXml`

El SRI usa:

```xml
id="comprobante"
```

en minúscula, y XAdES agrega otros `Id`.

`SignedXml` puede no resolver automáticamente todos los IDs ubicados en nodos personalizados/XAdES.

Implementa una subclase como concepto:

```text
SriSignedXml : SignedXml
```

y sobrescribe `GetIdElement(...)` de manera segura para resolver:

- `Id`;
- `ID`;
- `id`;

dentro del documento y de los objetos que forman parte de la firma.

No resolver IDs de forma ambigua.

Si existen dos elementos con el mismo ID, la firma debe fallar; no elegir uno arbitrariamente. Esto evita vulnerabilidades de wrapping.

Crear pruebas específicas para:

- `#comprobante`;
- `#SignedProperties...`;
- `#Certificate...`;
- ID duplicado;
- ID inexistente.

---

# 19. Orden seguro de construcción de firma

Codex debe determinar el orden exacto que permita a `SignedXml` resolver las referencias, pero el algoritmo debe ser equivalente a:

1. Cargar XML con `PreserveWhitespace = true`.
2. Confirmar raíz y `id="comprobante"`.
3. Cargar certificado P12.
4. Obtener RSA privada.
5. Crear `SriSignedXml`.
6. Establecer algoritmo de canonicalización.
7. Establecer RSA-SHA1.
8. Crear `KeyInfo` con:
   - X509Certificate;
   - RSAKeyValue.
9. Crear `ds:Object` con XAdES `QualifyingProperties`.
10. Crear `SignedProperties`.
11. Añadir las tres referencias.
12. Resolver correctamente todos los IDs.
13. Ejecutar `ComputeSignature()`.
14. Insertar `ds:Signature` como hijo del comprobante.
15. Volver a cargar/verificar la firma completa.
16. Serializar directamente el DOM firmado.

No reconstruir ni “embellecer” el XML después de firmarlo.

---

# 20. Seguridad XML

En toda lectura XML no confiable:

- `DtdProcessing = Prohibit`.
- `XmlResolver = null`.
- límites razonables cuando aplique.
- no expandir entidades externas.
- no aceptar referencias externas en la firma.
- las referencias de XMLDSIG deben ser internas al documento para este caso.

No permitir que el verificador descargue recursos por HTTP/archivo.

---

# 21. Verificación local de la firma

Después de firmar, **la propia aplicación debe verificar lo generado** antes de enviarlo.

Implementa `Verify(...)` o equivalente.

Debe comprobar:

- firma matemática;
- referencias/digests;
- certificado esperado;
- estructura XAdES requerida;
- que la firma pertenezca al `comprobante`;
- que las URIs sean internas.

Si la verificación local falla:

```text
NO ENVIAR AL SRI
```

La operación debe terminar con un error técnico entendible.

---

# 22. Pruebas de firma sin secretos reales

No necesitas un certificado comercial para las pruebas unitarias.

Crear durante el test:

1. RSA de 2048 bits.
2. `CertificateRequest`.
3. certificado X509 autofirmado.
4. exportarlo temporalmente a PKCS#12 con una contraseña conocida solo por el test.
5. firmar XML.
6. verificar XML.

Eliminar archivos temporales al finalizar.

No versionar P12 reales.

Pruebas mínimas:

- firma válida;
- XML modificado después de firmar → inválida;
- `SignedProperties` modificado → inválida;
- `KeyInfo` modificado → inválida;
- contraseña P12 incorrecta;
- P12 sin clave privada;
- certificado vencido;
- certificado no RSA;
- serial decimal correcto;
- SigningTime con offset;
- IDs únicos;
- ID duplicado rechazado.

---

# 23. SOAP nativo del SRI

No reutilizar JAX-WS.

Implementar clientes usando:

```text
HttpClient
IHttpClientFactory
CancellationToken
```

Preferir SOAP 1.1 explícito y DTO/parsers propios en lugar de introducir dependencias WCF antiguas.

Separar:

```text
ISriReceptionClient
ISriAuthorizationClient
```

Los endpoints deben vivir en configuración, no dispersos en código.

---

# 24. Endpoints Offline

Valores base actuales/históricamente confirmados en SRI:

## Pruebas

```text
Recepción:
https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl

Autorización:
https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl
```

## Producción

```text
Recepción:
https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl

Autorización:
https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl
```

Verifica la ficha vigente antes de cerrar la implementación.

No fijar el certificado TLS del servidor. Confiar en el almacén de certificados del sistema operativo y validación TLS estándar.

---

# 25. Contratos SOAP observados

## 25.1. Recepción

Namespace:

```text
http://ec.gob.sri.ws.recepcion
```

Operación:

```text
validarComprobante
```

Entrada:

```text
xml : base64Binary
```

La entrada es el XML firmado convertido a bytes y enviado como Base64 dentro del SOAP.

Respuesta principal:

```text
RespuestaRecepcionComprobante
```

Estados relevantes:

```text
RECIBIDA
DEVUELTA
```

Extraer todos los mensajes devueltos por el SRI:

```text
identificador
mensaje
informacionAdicional
tipo
```

No perder mensajes secundarios.

## 25.2. Autorización

Namespace:

```text
http://ec.gob.sri.ws.autorizacion
```

Operación principal:

```text
autorizacionComprobante
```

Entrada:

```text
claveAccesoComprobante
```

Respuesta:

```text
RespuestaAutorizacionComprobante
```

Extraer:

- clave consultada;
- número de comprobantes;
- lista de autorizaciones;
- estado;
- número de autorización;
- fecha de autorización;
- ambiente;
- contenido del comprobante;
- mensajes.

El contenido del comprobante puede venir como texto XML/CDATA. Presérvalo sin destruir su contenido.

---

# 26. SOAP: implementación robusta

Crear envelopes con `XmlWriter` o `XDocument`.

No interpolar valores sin escapar.

Configurar:

```text
Content-Type: text/xml; charset=utf-8
```

Confirma `SOAPAction` contra el WSDL vigente. No inventarlo.

Crear pruebas con `HttpMessageHandler` falso o servidor HTTP local para comprobar exactamente:

- envelope enviado;
- Base64 del XML;
- namespaces;
- operación;
- parser de respuesta;
- errores HTTP;
- timeout;
- cancelación.

No probar la lógica principal únicamente contra internet.

---

# 27. Flujo de recepción y autorización

Implementar el orquestador `IElectronicInvoicingService` aproximadamente así:

```text
1. Validar datos de negocio.
2. Generar clave.
3. Construir XML.
4. Validar XSD.
5. Firmar.
6. Verificar firma local.
7. Guardar el estado SIGNED.
8. Enviar a Recepción.
9. Persistir respuesta de Recepción.
10. Si DEVUELTA:
      terminar y devolver mensajes.
11. Si RECIBIDA:
      consultar Autorización.
12. Si PPR / EN PROCESO:
      devolver estado pendiente.
13. Si AUTORIZADO:
      persistir autorización y XML autorizado.
14. Si NO AUTORIZADO:
      persistir mensajes.
```

No ocultar un estado pendiente como error.

---

# 28. Idempotencia

La clave de acceso identifica el comprobante.

Evita emisiones duplicadas por doble clic, timeout o reintento.

Antes de reenviar un comprobante cuya recepción quedó incierta:

1. Consulta primero la autorización por clave de acceso.
2. Si ya existe/autorizó, actualiza el estado local.
3. Solo reenvía cuando la lógica del protocolo demuestre que es seguro.

No reintentar ciegamente una operación de emisión.

Agregar índice/constraint único por clave de acceso si el modelo de datos actual no lo tiene y si corresponde dentro de la arquitectura existente.

---

# 29. Reintentos y resiliencia

Separar:

## Reintentable

- timeout de red;
- conexión interrumpida;
- errores HTTP temporales;
- fallos transitorios de DNS.

## No reintentable automáticamente

- XML inválido;
- firma inválida;
- RUC inválido;
- clave mal formada;
- comprobante DEVUELTO por reglas de negocio;
- certificado inválido;
- contraseña errónea.

Usar backoff acotado para red.

No mantener la UI bloqueada durante largos períodos.

La consulta de autorización puede quedar en estado pendiente para reconciliación posterior.

---

# 30. Configuración

Crear opciones fuertemente tipadas, por ejemplo:

```text
SriElectronicInvoicingOptions
```

Debe contemplar:

```text
Environment
ReceptionTestUrl
AuthorizationTestUrl
ReceptionProductionUrl
AuthorizationProductionUrl
HttpTimeout
AuthorizationPollingInitialDelay
AuthorizationPollingMaxAttempts
```

Los endpoints deben poder cambiarse por configuración sin recompilar.

---

# 31. Certificado por empresa

KONTAXPRO es multiempresa.

La configuración de firma debe asociarse a la empresa emisora correcta.

Nunca seleccionar “el primer certificado del sistema” sin relación con la empresa.

Configurar como mínimo:

```text
EmpresaId
CertificatePath
CertificatePasswordProtected
```

o adaptar al modelo existente.

Antes de firmar validar que el certificado corresponde razonablemente al emisor cuando sea posible obtener el identificador/RUC del certificado. Si los proveedores de certificados codifican esa información de maneras distintas, encapsula la extracción y no bloquees certificados válidos con una regla frágil.

---

# 32. Protección de la contraseña del certificado

No guardar la contraseña P12 en texto plano.

Primero inspecciona si KONTAXPRO ya tiene un mecanismo de secretos.

Si ya existe, reutilízalo.

Si no existe y la aplicación sigue siendo Windows-only, crear una abstracción:

```text
ISecretProtector
```

con una implementación segura apropiada para Windows, por ejemplo protección vinculada al usuario/equipo mediante APIs del sistema operativo.

La capa Application no debe conocer DPAPI ni detalles de Windows.

No incluir secretos en:

- logs;
- stack traces mostrados al usuario;
- archivos de configuración versionados;
- backups exportados sin protección.

---

# 33. Persistencia

Inspecciona primero las tablas actuales de ventas/comprobantes.

No crear un segundo modelo paralelo si ya existe uno.

La solución debe poder conservar al menos:

```text
EmpresaId
TipoDocumento
VersionXml
Ambiente
ClaveAcceso
FechaEmision
Establecimiento
PuntoEmision
Secuencial
EstadoLocal
EstadoRecepcionSri
EstadoAutorizacionSri
NumeroAutorizacion
FechaAutorizacion
XmlGenerado
XmlFirmado
XmlAutorizado
MensajesSri
FechaEnvio
FechaUltimaConsulta
NumeroIntentos
```

Adapta estos conceptos al modelo real.

Si el proyecto almacena XML como archivos y solo guarda rutas, respeta ese patrón; no dupliques grandes XML en base de datos sin necesidad.

---

# 34. Transacciones y secuenciales

La asignación del secuencial debe ser atómica.

Evitar:

```text
leer último + 1
```

sin protección concurrente.

Reutiliza el mecanismo de `secuenciales_comprobantes` ya existente si está implementado.

La reserva del secuencial y creación del comprobante debe quedar consistente incluso si:

- la firma falla;
- no hay internet;
- el SRI está caído;
- el usuario cierra la aplicación.

No reutilices una clave/secuencial ya emitido sin analizar primero el estado real.

---

# 35. Logging

Registrar información suficiente para soporte técnico sin filtrar información sensible.

Sí registrar:

```text
EmpresaId
TipoDocumento
ClaveAcceso
Ambiente
EstadoRecepcion
EstadoAutorizacion
Identificador de mensaje SRI
Duración de llamada
Código HTTP
CorrelationId
```

No registrar por defecto:

- contraseña P12;
- clave privada;
- P12 completo;
- datos secretos;
- XML íntegro con datos personales en logs generales.

Si existe un modo diagnóstico para guardar XML, debe hacerlo en un almacenamiento controlado y explícito, no en logs de texto indiscriminados.

---

# 36. Excepciones

Crear errores específicos o resultados tipados para diferenciar:

```text
ElectronicDocumentValidationException
CertificateLoadException
ElectronicSignatureException
SriCommunicationException
SriProtocolException
```

No convertir todos los problemas en:

```text
"Error al facturar"
```

La UI podrá presentar un mensaje amigable mientras el log técnico conserva el detalle necesario.

---

# 37. RIDE — fase independiente

No mezclar la autorización con la generación del PDF.

Definir:

```text
IRideGenerator
```

El RIDE debe construirse preferentemente desde el comprobante autorizado/datos definitivos.

Primero busca si KONTAXPRO ya usa una librería PDF.

Si existe, reutilízala si es adecuada.

Si no existe:

- no metas una dependencia PDF al azar;
- evalúa licencia, mantenimiento y compatibilidad .NET 10;
- documenta la decisión;
- mantén el RIDE como fase posterior si escoger el motor PDF excede el alcance del núcleo.

El motor de autorización SRI debe funcionar aunque todavía no exista RIDE.

No portar `Fpdf.jar` ni `RideSri.jar`.

---

# 38. Integración con Ventas / WPF

Solo después de que el motor y sus pruebas estén estables.

Mantener:

- WPF;
- MVVM;
- CommunityToolkit.Mvvm;
- DI existente;
- estilos Light/Dark;
- convenciones de KONTAXPRO.

No ejecutar SOAP ni firma en code-behind.

No bloquear el hilo UI.

El ViewModel debe invocar un caso de uso Application y mostrar estados como:

```text
Generando
Firmando
Enviando
Recibida
Procesando en SRI
Autorizada
Devuelta
No autorizada
Sin conexión / pendiente de envío
```

No agregar UI si el módulo de Ventas todavía no está preparado para esta integración. En ese caso deja el motor listo y documenta el punto exacto de conexión futura.

---

# 39. Pruebas obligatorias

Crear una suite específica para Facturación Electrónica.

## 39.1. Clave de acceso

- longitud 49;
- composición correcta;
- Módulo 11;
- resultado 10 → 1;
- resultado 11 → 0;
- entradas inválidas;
- código numérico determinista en test.

Usa también el ejemplo oficial de Módulo 11 publicado por el SRI como vector de verificación.

## 39.2. XML

- factura válida contra XSD;
- campo obligatorio omitido;
- orden de nodos;
- formato de fechas;
- decimales con punto;
- caracteres especiales;
- UTF-8;
- `id="comprobante"`.

## 39.3. Firma

- PKCS12 válido;
- firma criptográficamente válida;
- RSA-SHA1;
- SHA1 references;
- canonicalización correcta;
- enveloped transform;
- referencia `#comprobante`;
- referencia SignedProperties;
- referencia KeyInfo;
- X509Data;
- RSAKeyValue;
- SigningCertificate;
- IssuerSerial;
- DataObjectFormat;
- certificado alterado;
- comprobante alterado;
- SignedProperties alterado;
- IDs duplicados;
- certificado vencido;
- contraseña incorrecta.

## 39.4. SOAP

Fixtures para:

```text
RECIBIDA
DEVUELTA
PPR
AUTORIZADO
NO AUTORIZADO
múltiples mensajes
mensaje con informacionAdicional
respuesta vacía
SOAP Fault
HTTP 500
timeout
cancelación
```

## 39.5. Flujo completo simulado

Con un servidor HTTP falso:

```text
Venta
→ XML
→ XSD
→ Firma
→ Recepción RECIBIDA
→ Autorización AUTORIZADO
→ Persistencia
```

y también:

```text
Venta
→ Firma
→ Recepción DEVUELTA
→ no consultar autorización
```

---

# 40. Prueba real contra ambiente de certificación

Crear una categoría de tests/integración **deshabilitada por defecto**.

Nunca meter credenciales reales en código.

Puede usar variables de entorno locales, por ejemplo:

```text
KONTAXPRO_SRI_TEST_P12
KONTAXPRO_SRI_TEST_P12_PASSWORD
```

Si se necesita un RUC/empresa de prueba, obtenerlo de configuración local no versionada.

La prueba real debe:

1. generar un XML válido;
2. firmarlo;
3. verificar firma local;
4. enviar a `celcer`;
5. registrar la respuesta completa estructurada;
6. consultar autorización;
7. guardar el XML de diagnóstico en carpeta temporal si falla.

No ejecutar pruebas contra **Producción** automáticamente.

---

# 41. Comparación con el legado

Cuando el motor nativo ya firme correctamente, usa las librerías Java antiguas solo como **oráculo temporal de comparación**, fuera del runtime de la aplicación, si resulta útil.

Para el mismo XML y certificado las firmas no tienen que ser byte-a-byte idénticas porque pueden variar:

- IDs;
- SigningTime;
- valores derivados de esos datos.

Pero sí deben coincidir en:

- namespaces;
- algoritmos;
- referencias;
- transforms;
- XAdES;
- certificado;
- estructura exigida;
- validación criptográfica;
- aceptación por el SRI.

No persigas una igualdad textual si ambas firmas son protocolariamente correctas.

---

# 42. Manejo de XML autorizados

Conservar la respuesta del SRI sin alterar el comprobante.

Distinguir:

```text
XML generado
XML firmado
XML autorizado / respuesta de autorización
```

No volver a firmar un XML que ya fue firmado.

No modificar el comprobante firmado después de enviarlo.

La clave de acceso debe permanecer idéntica durante todo el ciclo.

---

# 43. Compatibilidad con cambios futuros del SRI

Evitar constantes dispersas.

Centralizar:

- endpoints;
- namespaces;
- versiones XSD;
- códigos de documento;
- códigos de ambiente;
- algoritmos exigidos por el protocolo.

No crear un “mega servicio” con cientos de condiciones.

Cada documento debe tener su builder/mapping específico, compartiendo el núcleo común.

---

# 44. Qué no hacer

No:

- portar las clases Java línea por línea;
- meter toda la facturación electrónica en un único archivo C#;
- construir XML con `$"<tag>{valor}</tag>"`;
- desactivar validaciones de certificados globalmente;
- aceptar cualquier certificado TLS;
- guardar la contraseña del P12 en SQLite en texto plano;
- usar `SHA1` fuera del signer SRI;
- reintentar emisiones indiscriminadamente;
- tragarse mensajes del SRI;
- depender de internet para pruebas unitarias;
- transformar el XML firmado con LINQ to XML después de firmarlo;
- cambiar el formato del XML por “presentación” después de generar la firma;
- agregar paquetes NuGet innecesarios;
- introducir cambios masivos no relacionados.

---

# 45. Estrategia de implementación por entregas

Realiza el trabajo en este orden.

## Entrega 1 — Fundación

Implementar:

- modelos;
- configuración;
- clave de acceso;
- Módulo 11;
- pruebas.

Ejecutar:

```powershell
dotnet build
dotnet test
```

## Entrega 2 — XML Factura

Implementar:

- builder de factura;
- catálogo XSD;
- validador XSD;
- fixtures;
- pruebas.

Build/test.

## Entrega 3 — Certificado y XAdES-BES

Implementar:

- loader PKCS12;
- selector certificado;
- `SriSignedXml`;
- constructor XAdES;
- firma;
- verificación;
- suite criptográfica.

Build/test.

**No continúes a SOAP si la verificación local de firma no es 100 % estable.**

## Entrega 4 — SOAP SRI

Implementar:

- recepción;
- autorización;
- parsers;
- fixtures;
- resiliencia;
- tests con HTTP simulado.

Build/test.

## Entrega 5 — Orquestación y persistencia

Implementar:

- flujo end-to-end;
- idempotencia;
- estados;
- persistencia;
- recuperación ante fallos;
- tests.

Build/test.

## Entrega 6 — Certificación SRI

Realizar únicamente con configuración segura local.

Corregir cualquier diferencia protocolaria detectada.

Build/test.

## Entrega 7 — Integración de Ventas

Conectar el caso de uso con el módulo de Ventas existente, sin romper MVVM.

## Entrega 8 — RIDE

Implementar como módulo separado.

---

# 46. Criterios de aceptación

El trabajo no se considera terminado hasta cumplir todo esto:

- [ ] `dotnet build` sin errores nuevos.
- [ ] `dotnet test` sin regresiones.
- [ ] No existe requisito de Java/JRE/JAR para emitir.
- [ ] La clave de acceso tiene 49 dígitos y Módulo 11 validado.
- [ ] La factura se valida contra XSD oficial.
- [ ] Un P12 se carga con APIs nativas de .NET.
- [ ] La firma generada es XAdES-BES 1.3.2.
- [ ] La firma usa la estructura exigida por SRI.
- [ ] La propia aplicación puede verificar criptográficamente su XML firmado.
- [ ] Modificar comprobante/SignedProperties/KeyInfo invalida la firma.
- [ ] Recepción SOAP se serializa y parsea correctamente.
- [ ] Autorización SOAP se serializa y parsea correctamente.
- [ ] Se preservan todos los mensajes del SRI.
- [ ] El flujo distingue RECIBIDA, DEVUELTA, PPR, AUTORIZADO y NO AUTORIZADO.
- [ ] Hay idempotencia por clave de acceso.
- [ ] No hay secretos en repositorio/logs.
- [ ] No se fija el certificado TLS del SRI.
- [ ] Las pruebas unitarias no dependen de internet.
- [ ] Existe una prueba de integración opcional contra ambiente de certificación.
- [ ] Se documentaron archivos creados/modificados y decisiones relevantes.

---

# 47. Evidencia final que debe entregar Codex

Al finalizar, genera un resumen con:

## Archivos creados

```text
ruta
responsabilidad
```

## Archivos modificados

```text
ruta
cambio realizado
```

## Paquetes agregados

Para cada uno:

```text
nombre
versión
motivo
```

Si no agregaste paquetes, indicarlo explícitamente.

## Pruebas

Reportar:

```text
dotnet build
dotnet test
cantidad de tests nuevos
resultado
```

## Compatibilidad SRI

Indicar:

- ficha técnica tomada como autoridad;
- versión XSD de Factura implementada;
- algoritmo de firma;
- esquema XAdES;
- endpoints configurados;
- si se realizó o no una prueba real contra certificación.

## Pendientes

Solo pendientes reales. No inventar trabajo adicional.

---

# 48. Referencia funcional de las librerías analizadas

## `FacturaElectronicaSRIOffline.jar`

Clases observadas relevantes:

```text
com.facturaelectronicasri.ComprobanteXML
com.facturaelectronicasri.ConfiguracionesSistema
com.facturaelectronicasri.FacturaElectronicaSRI
com.facturaelectronicasri.Herramientas
com.facturaelectronicasri.Modulo11
com.firmaDigital.GenericXMLSignature
com.firmaDigital.XAdESBESSignature
com.firmaDigital.generarFirmaDigital
```

Se verificó que la configuración de XAdES usa:

```text
XAdES_BES
XAdES 1.3.2
UTF-8
enveloped = true
objeto = comprobante
descripción = contenido comprobante
MIME = text/xml
```

## `cliente-ws-offline.jar`

Contiene clientes/DTO JAX-WS para:

```text
RecepcionComprobantesOffline
AutorizacionComprobantesOffline
```

Operaciones verificadas:

```text
validarComprobante(byte[])
autorizacionComprobante(string)
autorizacionComprobanteLote(string)
```

## `sysconmEnlaceSRIOffline.jar`

Actúa como wrapper para los dos servicios anteriores y contiene los endpoints de pruebas/producción.

## `MITyCLib*`, `xmlsec`, `bcmail`

Son la infraestructura Java antigua para:

- XMLDSIG;
- XAdES;
- certificados;
- criptografía.

No existe una razón arquitectónica para conservarlas si .NET reproduce exactamente el protocolo requerido y la firma es aceptada por el SRI.

---

# 49. Fuentes de autoridad para la implementación

Codex debe consultar siempre fuentes oficiales.

Prioridad:

1. **Servicio de Rentas Internas del Ecuador — Facturación Electrónica — Información técnica y guías.**
2. **Ficha Técnica de Comprobantes Electrónicos Esquema Off-line vigente**.
3. **XSD/XML oficiales de cada tipo de documento**.
4. Microsoft Learn para:
   - `X509CertificateLoader`;
   - `X509Certificate2`;
   - `SignedXml`;
   - `XmlDsigEnvelopedSignatureTransform`;
   - `XmlDsigC14NTransform`.
5. Estándar XMLDSIG/XAdES solo cuando sea necesario resolver un detalle no aclarado por SRI.

No tomar blogs, Stack Overflow ni repositorios aleatorios como autoridad frente al SRI.

---

# 50. Instrucción final

Implementa este trabajo de forma incremental, segura y comprobable.

La meta no es “hacer una versión C# de los JAR”, sino construir un **motor nativo de Facturación Electrónica para KONTAXPRO Desktop**, desacoplado, mantenible y preparado para los demás comprobantes electrónicos.

Prioridad absoluta:

```text
CORRECTITUD DEL XML
+ CORRECTITUD CRIPTOGRÁFICA
+ COMPATIBILIDAD CON EL SRI
+ SEGURIDAD DE SECRETOS
+ IDEMPOTENCIA
+ PRUEBAS
```

Ante cualquier duda protocolaria:

```text
Ficha técnica SRI vigente
> XSD oficial
> prueba en ambiente de certificación
> comportamiento de los JAR históricos
```

No sacrifiques robustez interna por simplificar la implementación. La experiencia para el usuario final debe seguir la filosofía de KONTAXPRO:

> **robustez por dentro, simplicidad por fuera**.
