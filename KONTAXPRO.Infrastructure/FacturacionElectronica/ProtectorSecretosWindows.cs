using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using KONTAXPRO.Application.Interfaces;

namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

public sealed class ProtectorSecretosWindows : IProtectorSecretosLocal
{
    private const int CryptprotectUiForbidden = 0x1;

    public byte[] Proteger(ReadOnlySpan<byte> valor, string proposito) =>
        Ejecutar(valor, proposito, proteger: true);

    public byte[] Desproteger(ReadOnlySpan<byte> valorProtegido, string proposito) =>
        Ejecutar(valorProtegido, proposito, proteger: false);

    private static byte[] Ejecutar(
        ReadOnlySpan<byte> input,
        string purpose,
        bool proteger)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(
                "La protección DPAPI está disponible únicamente en Windows.");
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        var inputBytes = input.ToArray();
        var entropyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(purpose));
        var inputHandle = GCHandle.Alloc(inputBytes, GCHandleType.Pinned);
        var entropyHandle = GCHandle.Alloc(entropyBytes, GCHandleType.Pinned);
        try
        {
            var inputBlob = new DataBlob(inputBytes.Length,
                inputHandle.AddrOfPinnedObject());
            var entropyBlob = new DataBlob(entropyBytes.Length,
                entropyHandle.AddrOfPinnedObject());
            DataBlob output;
            var ok = proteger
                ? CryptProtectData(ref inputBlob, null, ref entropyBlob,
                    IntPtr.Zero, IntPtr.Zero,
                    CryptprotectUiForbidden,
                    out output)
                : CryptUnprotectData(ref inputBlob, IntPtr.Zero,
                    ref entropyBlob, IntPtr.Zero, IntPtr.Zero,
                    CryptprotectUiForbidden, out output);
            if (!ok)
                throw new CryptographicException(
                    new Win32Exception(Marshal.GetLastWin32Error()).Message);
            try
            {
                var result = new byte[output.Size];
                Marshal.Copy(output.Data, result, 0, output.Size);
                return result;
            }
            finally
            {
                LocalFree(output.Data);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(inputBytes);
            CryptographicOperations.ZeroMemory(entropyBytes);
            inputHandle.Free();
            entropyHandle.Free();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob(int size, IntPtr data)
    {
        public int Size = size;
        public IntPtr Data = data;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(
        ref DataBlob dataIn, string? description, ref DataBlob optionalEntropy,
        IntPtr reserved, IntPtr promptStruct, int flags, out DataBlob dataOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(
        ref DataBlob dataIn, IntPtr description, ref DataBlob optionalEntropy,
        IntPtr reserved, IntPtr promptStruct, int flags, out DataBlob dataOut);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LocalFree(IntPtr memory);
}
