# 🚀 Guía Rápida: Notificaciones Push en Flutter

## ⚡ Inicio Rápido (5 minutos)

### 1. Instalar Dependencias

```yaml
# pubspec.yaml
dependencies:
  firebase_core: ^2.24.2
  firebase_messaging: ^14.7.9
  flutter_local_notifications: ^16.3.0
```

### 2. Código Mínimo para Empezar

```dart
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';
import 'dart:io';

class NotificationHelper {
  static Future<void> registerDevice(String authToken) async {
    // 1. Obtener token FCM
    final fcm = FirebaseMessaging.instance;
    await fcm.requestPermission();
    final fcmToken = await fcm.getToken();
    
    if (fcmToken == null) return;
    
    // 2. Registrar en backend
    await http.post(
      Uri.parse('https://tu-api.com/api/notifications/devices'),
      headers: {
        'Authorization': 'Bearer $authToken',
        'Content-Type': 'application/json',
      },
      body: jsonEncode({
        'fcmToken': fcmToken,
        'platform': Platform.isAndroid ? 'android' : 'ios',
      }),
    );
    
    // 3. Escuchar notificaciones
    FirebaseMessaging.onMessage.listen((message) {
      print('📨 Notificación: ${message.notification?.title}');
      // Manejar notificación aquí
    });
  }
}
```

### 3. Usar Después del Login

```dart
// Después de login exitoso
await NotificationHelper.registerDevice(authToken);
```

---

## 📨 Tipos de Notificaciones que Recibirás

### 1. Nueva Cita Agendada (Automática)

**Payload:**
```json
{
  "data": {
    "type": "appointment",
    "appointmentId": "123",
    "clientName": "Maria González",
    "date": "2025-01-15",
    "time": "10:00"
  }
}
```

**Acción:** Navegar a pantalla de citas

---

### 2. Anuncio del Admin (Manual)

**Payload:**
```json
{
  "data": {
    "type": "announcement",
    "title": "Nueva actualización",
    "body": "Mensaje del admin"
  }
}
```

**Acción:** Mostrar diálogo o pantalla de anuncios

---

## 🔌 Endpoints Principales

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `/api/notifications/devices` | Registrar dispositivo |
| POST | `/api/notifications/devices/refresh-token` | Actualizar token |
| DELETE | `/api/notifications/devices/{id}` | Eliminar dispositivo |
| GET | `/api/notifications/logs` | Ver historial |

---

## 📝 Flujo Completo

```
1. Usuario hace login
   ↓
2. Obtener token FCM
   ↓
3. Registrar dispositivo en backend
   ↓
4. Configurar listeners
   ↓
5. ✅ Listo para recibir notificaciones
```

---

## 🎯 Ejemplo de Manejo

```dart
FirebaseMessaging.onMessage.listen((RemoteMessage message) {
  final data = message.data;
  final type = data['type'];
  
  switch (type) {
    case 'appointment':
      // Nueva cita
      final appointmentId = data['appointmentId'];
      navigator.pushNamed('/appointments/$appointmentId');
      break;
      
    case 'announcement':
      // Anuncio
      showDialog(...);
      break;
  }
});
```

---

## ⚠️ Importante

1. **Siempre registrar dispositivo después del login**
2. **Actualizar token cuando cambie** (se hace automático)
3. **Eliminar dispositivo al cerrar sesión**
4. **Manejar notificaciones en todos los estados** (abierta, segundo plano, cerrada)

---

**Para más detalles, ver:** `DOCUMENTACION_FLUTTER_NOTIFICACIONES.md`
