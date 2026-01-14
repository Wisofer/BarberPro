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
            // Verificar que Firebase esté inicializado
            if (FirebaseApp.DefaultInstance == null)
            {
                _logger.LogError("❌ FirebaseApp.DefaultInstance es null. Firebase no está inicializado.");
                throw new InvalidOperationException("Firebase no está inicializado. Verifica la configuración en Program.cs");
            }

            if (FirebaseMessaging.DefaultInstance == null)
            {
                _logger.LogError("❌ FirebaseMessaging.DefaultInstance es null aunque FirebaseApp está inicializado.");
                throw new InvalidOperationException("FirebaseMessaging no está disponible. Verifica la configuración.");
            }
            
            _logger.LogInformation("✅ Firebase verificado: FirebaseApp y FirebaseMessaging están disponibles");

            _logger.LogInformation("🔔 Iniciando envío de notificación push");
            _logger.LogInformation("   - Template ID: {TemplateId}", template.Id);
            _logger.LogInformation("   - Template Title: {Title}", template.Title);
            _logger.LogInformation("   - Template Body: {Body}", template.Body);
            _logger.LogInformation("   - Template ImageUrl: {ImageUrl}", template.ImageUrl ?? "null");
            _logger.LogInformation("   - Dispositivos válidos: {Count}", validDevices.Count);
            _logger.LogInformation("   - DataOnly: {DataOnly}", dataOnly);
            
            // Log de tokens (solo primeros 3 para seguridad)
            var sampleTokens = validDevices.Take(3).Select(d => d.FcmToken).ToList();
            _logger.LogInformation("   - Tokens FCM (muestra): {Tokens}", string.Join(", ", sampleTokens));
            
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
                _logger.LogInformation("   - ExtraData: {ExtraData}", string.Join(", ", extraData.Select(kv => $"{kv.Key}={kv.Value}")));
            }

            // Agregar datos de la plantilla
            data["title"] = template.Title;
            data["body"] = template.Body;
            data["type"] = "announcement"; // Tipo por defecto para notificaciones manuales
            if (!string.IsNullOrWhiteSpace(template.ImageUrl))
            {
                data["imageUrl"] = template.ImageUrl;
            }
            if (template.Id > 0)
            {
                data["templateId"] = template.Id.ToString();
            }
            
            _logger.LogInformation("   - Data payload: {Data}", string.Join(", ", data.Select(kv => $"{kv.Key}={kv.Value}")));

            // Asegurar que data no sea null
            if (data == null)
            {
                _logger.LogError("❌ Error: data es null después de construir el diccionario");
                data = new Dictionary<string, string>();
            }

            // Construir mensaje base
            var message = new Message
            {
                Notification = dataOnly ? null : notification,
                Data = data ?? new Dictionary<string, string>()
            };

            // Configurar Android
            message.Android = new AndroidConfig
            {
                Priority = Priority.High,
                Notification = dataOnly ? null : new AndroidNotification
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
                    Alert = dataOnly ? null : new ApsAlert
                    {
                        Title = template.Title,
                        Body = template.Body
                    },
                    Sound = dataOnly ? null : "default",
                    Badge = dataOnly ? (int?)null : 1,
                    ContentAvailable = dataOnly // iOS: indica que es solo datos
                }
            };

            // Configurar Web
            message.Webpush = new WebpushConfig
            {
                Notification = dataOnly ? null : new WebpushNotification
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
                var tokens = batch.Select(d => d.FcmToken).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
                
                if (!tokens.Any())
                {
                    _logger.LogWarning("⚠️ Lote sin tokens válidos, saltando...");
                    continue;
                }
                
                try
                {
                    // Verificar nuevamente que Firebase esté disponible
                    if (FirebaseApp.DefaultInstance == null)
                    {
                        _logger.LogError("❌ FirebaseApp.DefaultInstance es null al intentar enviar");
                        throw new InvalidOperationException("FirebaseApp no está inicializado");
                    }
                    
                    if (FirebaseMessaging.DefaultInstance == null)
                    {
                        _logger.LogError("❌ FirebaseMessaging.DefaultInstance es null al intentar enviar");
                        throw new InvalidOperationException("FirebaseMessaging no está disponible");
                    }

                    // Validar que message.Data no sea null
                    var messageData = message.Data ?? new Dictionary<string, string>();
                    
                    // Enviar a múltiples dispositivos
                    var multicastMessage = new MulticastMessage
                    {
                        Tokens = tokens,
                        Notification = dataOnly ? null : notification,
                        Data = messageData,
                        Android = message.Android,
                        Apns = message.Apns,
                        Webpush = message.Webpush
                    };

                    _logger.LogInformation("📤 Enviando lote de {Count} tokens a Firebase", tokens.Count);
                    _logger.LogInformation("   - Firebase.DefaultInstance disponible: {Available}", FirebaseMessaging.DefaultInstance != null);
                    _logger.LogInformation("   - Message.Data count: {Count}", messageData.Count);
                    
                    // Obtener instancia de Firebase (ya validada arriba)
                    var firebaseMessaging = FirebaseMessaging.DefaultInstance;
                    if (firebaseMessaging == null)
                    {
                        throw new InvalidOperationException("FirebaseMessaging.DefaultInstance es null");
                    }
                    
                    var response = await firebaseMessaging.SendEachForMulticastAsync(multicastMessage);

                    _logger.LogInformation("✅ Respuesta de Firebase: {Success} exitosas, {Failed} fallidas", 
                        response.SuccessCount, response.FailureCount);
                    
                    totalSent += response.SuccessCount;
                    totalFailed += response.FailureCount;
                    
                    // Log de errores detallados
                    if (response.FailureCount > 0)
                    {
                        for (int i = 0; i < response.Responses.Count; i++)
                        {
                            var fcmResponse = response.Responses[i];
                            if (!fcmResponse.IsSuccess)
                            {
                                var device = batch[i];
                                _logger.LogWarning("❌ Error al enviar a dispositivo {DeviceId} (Usuario {UserId}): {ErrorCode} - {ErrorMessage}", 
                                    device.Id, device.UserId, 
                                    fcmResponse.Exception?.MessagingErrorCode, 
                                    fcmResponse.Exception?.Message);
                            }
                        }
                    }

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
                    if (response?.Responses != null)
                    {
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
                }
                catch (NullReferenceException ex)
                {
                    _logger.LogError(ex, "❌ NullReferenceException al enviar notificación a lote de {Count} dispositivos", batch.Count);
                    _logger.LogError("   - StackTrace: {StackTrace}", ex.StackTrace);
                    _logger.LogError("   - FirebaseApp.DefaultInstance: {IsNull}", FirebaseApp.DefaultInstance == null ? "NULL" : "OK");
                    _logger.LogError("   - FirebaseMessaging.DefaultInstance: {IsNull}", FirebaseMessaging.DefaultInstance == null ? "NULL" : "OK");
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
                "✅ Notificación completada: {Sent} exitosas, {Failed} fallidas de {Total} dispositivos",
                totalSent, totalFailed, validDevices.Count);
            
            if (totalFailed > 0)
            {
                _logger.LogWarning("⚠️ Algunas notificaciones fallaron. Revisa los logs anteriores para más detalles.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crítico al enviar notificaciones push");
            throw;
        }
    }
}
