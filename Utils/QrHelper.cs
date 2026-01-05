using QRCoder;
using System.Text;

namespace BarberPro.Utils;

/// <summary>
/// Helper para generar códigos QR
/// </summary>
public static class QrHelper
{
    /// <summary>
    /// Genera la URL pública del barbero
    /// </summary>
    public static string GenerateBarberUrl(string slug, string baseUrl = "https://app.mibarberia.com")
    {
        return $"{baseUrl}/b/{slug}";
    }

    /// <summary>
    /// Genera un código QR en formato Base64 (imagen PNG)
    /// </summary>
    public static string GenerateQrCodeBase64(string url, int pixelsPerModule = 20)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("La URL no puede estar vacía", nameof(url));

        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(pixelsPerModule);
        
        return Convert.ToBase64String(qrCodeBytes);
    }
}

