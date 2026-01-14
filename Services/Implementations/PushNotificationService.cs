using BarberNic.Data;
using BarberNic.Models.Entities;
using BarberNic.Services.Interfaces;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BarberNic.Services.Implementations;

/// <summary>
/// Servicio para enviar notificaciones push usando Firebase Cloud Messaging
/// </summary>
public class PushNotificationService : IPushNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(
        ApplicationDbContext context,
        ILogger<PushNotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SendPushNotificationAsync(
        Template? template, 
        List<Device> devices, 
        IDictionary<string, string>? extraData = null, 
        bool dataOnly = false)
    {
        if (template == null)
        {
            _logger.LogWarning("No se puede enviar notificación: template es null");
            return;
        }

        if (devices == null || !devices.Any())
        {
            _logger.LogWarning("No se puede enviar notificación: no hay dispositivos");
            return;
        }

        // Filtrar dispositivos con tokens válidos
        var validDevices = devices
            .Where(d => !string.IsNullOrWhiteSpace(d.FcmToken))
            .ToList();

        if (!validDevices.Any())
        {
            _logger.LogWarning("No hay dispositivos con tokens FCM válidos");
            return;
        }

        // Validar URL de imagen si existe
        if (!string.IsNullOrWhiteSpace(template.ImageUrl))
        {
            if (!Uri.TryCreate(template.ImageUrl, UriKind.Absolute, out var imageUri) ||
                (imageUri.Scheme != Uri.UriSchemeHttp && imageUri.Scheme != Uri.UriSchemeHttps))
            {
                _logger.LogWarning("URL de imagen inválida: {ImageUrl}", template.ImageUrl);
                template.ImageUrl = null; // Ignorar imagen inválida
            }
        }

        try
        {
            // Construir notificación
            var notification = new Notification
            {
                Title = template.Title,
                Body = template.Body,
                ImageUrl = template.ImageUrl
            };

            // Construir diccionario de datos
            var data = new Dictionary<string, string>();

            // Agregar datos adicionales
            if (extraData != null)
            {
                foreach (var item in extraData)
                {
                    data[item.Key] = item.Value;
                }
            }

            // Agregar datos de la plantilla
            data["title"] = template.Title;
            data["body"] = template.Body;
            if (!string.IsNullOrWhiteSpace(template.ImageUrl))
            {
                data["imageUrl"] = template.ImageUrl;
            }
            if (template.Id > 0)
            {
                data["templateId"] = template.Id.ToString();
            }

            // Construir mensaje base
            var message = new Message
            {
                Notification = dataOnly ? null : notification,
                Data = data
            };

            // Configurar Android
            message.Android = new AndroidConfig
            {
                Priority = Priority.High,
                Notification = new AndroidNotification
                {
                    Title = template.Title,
                    Body = template.Body,
                    ImageUrl = template.ImageUrl,
                    Sound = "default",
                    ChannelId = "default"
                }
            };

            // Configurar iOS (APNS)
            message.Apns = new ApnsConfig
            {
                Headers = new Dictionary<string, string>
                {
                    ["apns-priority"] = "10"
                },
                Aps = new Aps
                {
                    Alert = new ApsAlert
                    {
                        Title = template.Title,
                        Body = template.Body
                    },
                    Sound = "default",
                    Badge = 1
                }
            };

            // Configurar Web
            message.Webpush = new WebpushConfig
            {
                Notification = new WebpushNotification
                {
                    Title = template.Title,
                    Body = template.Body,
                    Icon = template.ImageUrl,
                    Image = template.ImageUrl
                }
            };

            // Dividir tokens en lotes de 500 (límite de FCM)
            const int batchSize = 500;
            var batches = validDevices
                .Select((device, index) => new { device, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.device).ToList())
                .ToList();

            var totalSent = 0;
            var totalFailed = 0;

            foreach (var batch in batches)
            {
                var tokens = batch.Select(d => d.FcmToken).ToList();
                
                try
                {
                    // Enviar a múltiples dispositivos
                    var multicastMessage = new MulticastMessage
                    {
                        Tokens = tokens,
                        Notification = dataOnly ? null : notification,
                        Data = message.Data,
                        Android = message.Android,
                        Apns = message.Apns,
                        Webpush = message.Webpush
                    };

                    var response = await FirebaseMessaging.DefaultInstance
                        .SendEachForMulticastAsync(multicastMessage);

                    totalSent += response.SuccessCount;
                    totalFailed += response.FailureCount;

                    // Registrar logs de éxito
                    for (int i = 0; i < batch.Count && i < response.Responses.Count; i++)
                    {
                        var device = batch[i];
                        var fcmResponse = response.Responses[i];

                        var log = new NotificationLog
                        {
                            Status = fcmResponse.IsSuccess ? "sent" : "failed",
                            Payload = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                title = template.Title,
                                body = template.Body,
                                imageUrl = template.ImageUrl,
                                extraData = extraData
                            }),
                            SentAt = DateTime.UtcNow,
                            DeviceId = device.Id,
                            TemplateId = template.Id,
                            UserId = device.UserId
                        };

                        _context.NotificationLogs.Add(log);
                    }

                    // Eliminar tokens inválidos
                    for (int i = 0; i < batch.Count && i < response.Responses.Count; i++)
                    {
                        var fcmResponse = response.Responses[i];
                        if (!fcmResponse.IsSuccess && 
                            (fcmResponse.Exception?.MessagingErrorCode == MessagingErrorCode.InvalidArgument ||
                             fcmResponse.Exception?.MessagingErrorCode == MessagingErrorCode.Unregistered))
                        {
                            var device = batch[i];
                            _logger.LogWarning("Token FCM inválido, eliminando dispositivo: {DeviceId}", device.Id);
                            _context.Devices.Remove(device);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al enviar notificación a lote de {Count} dispositivos", batch.Count);
                    totalFailed += batch.Count;

                    // Registrar logs de error
                    foreach (var device in batch)
                    {
                        var log = new NotificationLog
                        {
                            Status = "failed",
                            Payload = $"Error: {ex.Message}",
                            SentAt = DateTime.UtcNow,
                            DeviceId = device.Id,
                            TemplateId = template.Id,
                            UserId = device.UserId
                        };
                        _context.NotificationLogs.Add(log);
                    }
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Notificación enviada: {Sent} exitosas, {Failed} fallidas de {Total} dispositivos",
                totalSent, totalFailed, validDevices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crítico al enviar notificaciones push");
            throw;
        }
    }
}
