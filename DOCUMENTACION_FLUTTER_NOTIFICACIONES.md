# 📱 Documentación: Sistema de Notificaciones Push para Flutter

## 📋 Tabla de Contenidos

1. [Introducción](#introducción)
2. [Configuración Inicial](#configuración-inicial)
3. [Registro de Dispositivo](#registro-de-dispositivo)
4. [Tipos de Notificaciones](#tipos-de-notificaciones)
5. [Endpoints API](#endpoints-api)
6. [Estructura de Datos](#estructura-de-datos)
7. [Ejemplos de Código Flutter](#ejemplos-de-código-flutter)
8. [Manejo de Notificaciones](#manejo-de-notificaciones)
9. [Casos de Uso](#casos-de-uso)
10. [Troubleshooting](#troubleshooting)

---

## 🎯 Introducción

Este documento explica cómo integrar el sistema de notificaciones push de BarberPro en tu aplicación Flutter. El sistema utiliza **Firebase Cloud Messaging (FCM)** para enviar notificaciones a dispositivos Android e iOS.

### ¿Qué notificaciones recibirá la app?

1. **Notificaciones Automáticas:**
   - Cuando un cliente agenda una cita → El barbero recibe notificación
   - Cuando se confirma una cita → (Futuro)
   - Cuando se cancela una cita → (Futuro)

2. **Notificaciones Manuales:**
   - Anuncios del administrador
   - Actualizaciones del sistema
   - Promociones especiales

---

## 🔧 Configuración Inicial

### Paso 1: Instalar Dependencias

Agrega estas dependencias a tu `pubspec.yaml`:

```yaml
dependencies:
  firebase_core: ^2.24.2
  firebase_messaging: ^14.7.9
  flutter_local_notifications: ^16.3.0
```

### Paso 2: Configurar Firebase en Flutter

1. **Android:**
   - Descarga `google-services.json` de Firebase Console
   - Colócalo en `android/app/`
   - Agrega en `android/build.gradle`:
     ```gradle
     dependencies {
         classpath 'com.google.gms:google-services:4.4.0'
     }
     ```
   - Agrega en `android/app/build.gradle`:
     ```gradle
     apply plugin: 'com.google.gms.google-services'
     ```

2. **iOS:**
   - Descarga `GoogleService-Info.plist` de Firebase Console
   - Colócalo en `ios/Runner/`
   - Habilita Push Notifications en Xcode

### Paso 3: Inicializar Firebase

```dart
import 'package:firebase_core/firebase_core.dart';
import 'firebase_options.dart'; // Generado automáticamente

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await Firebase.initializeApp(
    options: DefaultFirebaseOptions.currentPlatform,
  );
  runApp(MyApp());
}
```

---

## 📱 Registro de Dispositivo

### Paso 1: Obtener Token FCM

```dart
import 'package:firebase_messaging/firebase_messaging.dart';

class NotificationService {
  final FirebaseMessaging _firebaseMessaging = FirebaseMessaging.instance;
  
  Future<String?> getFCMToken() async {
    try {
      // Solicitar permisos (iOS)
      NotificationSettings settings = await _firebaseMessaging.requestPermission(
        alert: true,
        badge: true,
        sound: true,
      );
      
      if (settings.authorizationStatus == AuthorizationStatus.authorized) {
        // Obtener token
        String? token = await _firebaseMessaging.getToken();
        return token;
      }
      return null;
    } catch (e) {
      print('Error al obtener token FCM: $e');
      return null;
    }
  }
}
```

### Paso 2: Registrar Dispositivo en el Backend

**Endpoint:** `POST /api/notifications/devices`

**Headers:**
```
Authorization: Bearer {token_jwt}
Content-Type: application/json
```

**Body:**
```json
{
  "fcmToken": "token_fcm_del_dispositivo",
  "platform": "android" // o "ios"
}
```

**Ejemplo Flutter:**

```dart
import 'package:http/http.dart' as http;
import 'dart:convert';

class DeviceRegistrationService {
  final String baseUrl = 'https://tu-api.com'; // Cambiar por tu URL
  final String? authToken; // Token JWT del usuario autenticado
  
  Future<bool> registerDevice(String fcmToken, String platform) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/api/notifications/devices'),
        headers: {
          'Authorization': 'Bearer $authToken',
          'Content-Type': 'application/json',
        },
        body: jsonEncode({
          'fcmToken': fcmToken,
          'platform': platform, // 'android' o 'ios'
        }),
      );
      
      if (response.statusCode == 201 || response.statusCode == 200) {
        print('✅ Dispositivo registrado exitosamente');
        return true;
      } else {
        print('❌ Error al registrar dispositivo: ${response.body}');
        return false;
      }
    } catch (e) {
      print('❌ Error: $e');
      return false;
    }
  }
}
```

### Paso 3: Flujo Completo de Registro

```dart
class NotificationManager {
  final NotificationService _notificationService = NotificationService();
  final DeviceRegistrationService _deviceService = DeviceRegistrationService();
  
  Future<void> initializeNotifications(String authToken) async {
    // 1. Obtener token FCM
    String? fcmToken = await _notificationService.getFCMToken();
    
    if (fcmToken == null) {
      print('⚠️ No se pudo obtener token FCM');
      return;
    }
    
    // 2. Detectar plataforma
    String platform = Platform.isAndroid ? 'android' : 'ios';
    
    // 3. Registrar en backend
    bool success = await _deviceService.registerDevice(fcmToken, platform);
    
    if (success) {
      print('✅ Notificaciones configuradas correctamente');
      
      // 4. Configurar listeners
      _setupNotificationListeners();
    }
  }
  
  void _setupNotificationListeners() {
    // Escuchar cuando la app está en primer plano
    FirebaseMessaging.onMessage.listen((RemoteMessage message) {
      print('📨 Notificación recibida en primer plano');
      _handleNotification(message);
    });
    
    // Escuchar cuando se toca la notificación
    FirebaseMessaging.onMessageOpenedApp.listen((RemoteMessage message) {
      print('👆 Usuario tocó la notificación');
      _handleNotificationTap(message);
    });
    
    // Verificar si la app se abrió desde una notificación
    FirebaseMessaging.instance.getInitialMessage().then((message) {
      if (message != null) {
        print('🚀 App abierta desde notificación');
        _handleNotificationTap(message);
      }
    });
  }
}
```

---

## 📨 Tipos de Notificaciones

### 1. Notificación Automática: Nueva Cita Agendada

**Cuándo se envía:**
- Cuando un cliente agenda una cita desde la web pública

**Estructura del payload:**
```json
{
  "notification": {
    "title": "Nueva cita agendada",
    "body": "Maria González agendó una cita para el 15/01/2025 a las 10:00"
  },
  "data": {
    "type": "appointment",
    "appointmentId": "123",
    "clientName": "Maria González",
    "clientPhone": "12345678",
    "date": "2025-01-15",
    "time": "10:00",
    "title": "Nueva cita agendada",
    "body": "Maria González agendó una cita para el 15/01/2025 a las 10:00"
  }
}
```

**Manejo en Flutter:**
```dart
void _handleNotification(RemoteMessage message) {
  final data = message.data;
  final type = data['type'];
  
  if (type == 'appointment') {
    final appointmentId = data['appointmentId'];
    final clientName = data['clientName'];
    final date = data['date'];
    final time = data['time'];
    
    // Navegar a la pantalla de citas o mostrar detalles
    _navigateToAppointmentDetails(appointmentId);
  }
}
```

### 2. Notificación Manual: Anuncio del Admin

**Cuándo se envía:**
- Cuando el administrador envía una notificación manual desde `/admin/notifications`

**Estructura del payload:**
```json
{
  "notification": {
    "title": "Nueva actualización disponible",
    "body": "Hemos agregado nuevas funcionalidades a la app",
    "imageUrl": "https://ejemplo.com/imagen.jpg"
  },
  "data": {
    "type": "announcement",
    "templateId": "5",
    "title": "Nueva actualización disponible",
    "body": "Hemos agregado nuevas funcionalidades a la app",
    "imageUrl": "https://ejemplo.com/imagen.jpg"
  }
}
```

### 3. Notificación Data-Only (Solo Datos)

**Cuándo se envía:**
- Actualizaciones en segundo plano sin mostrar notificación del sistema

**Estructura del payload:**
```json
{
  "data": {
    "type": "data_update",
    "action": "refresh_appointments",
    "message": "Hay nuevas citas disponibles"
  }
}
```

**Nota:** No tiene `notification`, solo `data`. Debes manejar esto manualmente.

---

## 🔌 Endpoints API

### Base URL
```
https://tu-api.com/api/notifications
```

### 1. Registrar Dispositivo

**POST** `/api/notifications/devices`

**Autenticación:** Requerida (JWT Bearer Token)

**Body:**
```json
{
  "fcmToken": "string (requerido)",
  "platform": "string (requerido)" // "android" o "ios"
}
```

**Respuesta Exitosa (201):**
```json
{
  "id": 1,
  "fcmToken": "token_aqui",
  "platform": "android",
  "lastActiveAt": "2025-01-14T12:00:00Z",
  "userId": 5,
  "createdAt": "2025-01-14T12:00:00Z",
  "updatedAt": "2025-01-14T12:00:00Z"
}
```

**Nota:** Si el dispositivo ya existe (mismo token), se actualiza y retorna 200.

---

### 2. Actualizar Token FCM

**POST** `/api/notifications/devices/refresh-token`

**Autenticación:** Requerida (JWT Bearer Token)

**Body:**
```json
{
  "currentFcmToken": "token_antiguo",
  "newFcmToken": "token_nuevo",
  "platform": "android"
}
```

**Cuándo usar:**
- Cuando Firebase genera un nuevo token (puede cambiar periódicamente)
- Cuando detectas que el token cambió

**Ejemplo Flutter:**
```dart
Future<void> refreshToken(String oldToken, String newToken) async {
  final response = await http.post(
    Uri.parse('$baseUrl/api/notifications/devices/refresh-token'),
    headers: {
      'Authorization': 'Bearer $authToken',
      'Content-Type': 'application/json',
    },
    body: jsonEncode({
      'currentFcmToken': oldToken,
      'newFcmToken': newToken,
      'platform': Platform.isAndroid ? 'android' : 'ios',
    }),
  );
  
  if (response.statusCode == 200) {
    print('✅ Token actualizado');
  }
}
```

---

### 3. Obtener Mis Dispositivos

**GET** `/api/notifications/devices`

**Autenticación:** Requerida (JWT Bearer Token)

**Respuesta:**
```json
[
  {
    "id": 1,
    "fcmToken": "token_aqui",
    "platform": "android",
    "lastActiveAt": "2025-01-14T12:00:00Z",
    "userId": 5,
    "createdAt": "2025-01-14T12:00:00Z",
    "updatedAt": "2025-01-14T12:00:00Z"
  }
]
```

---

### 4. Eliminar Dispositivo

**DELETE** `/api/notifications/devices/{id}`

**Autenticación:** Requerida (JWT Bearer Token)

**Cuándo usar:**
- Cuando el usuario cierra sesión
- Cuando quieres desactivar notificaciones

---

### 5. Ver Logs de Notificaciones

**GET** `/api/notifications/logs?page=1&pageSize=50`

**Autenticación:** Requerida (JWT Bearer Token)

**Respuesta:**
```json
[
  {
    "id": 1,
    "status": "sent", // "sent", "opened", "failed"
    "payload": "{\"title\":\"...\",\"body\":\"...\"}",
    "sentAt": "2025-01-14T12:00:00Z",
    "deviceId": 1,
    "templateId": 5,
    "userId": 5,
    "createdAt": "2025-01-14T12:00:00Z"
  }
]
```

---

## 📊 Estructura de Datos

### RemoteMessage (Firebase)

```dart
class RemoteMessage {
  RemoteNotification? notification; // Notificación del sistema
  Map<String, dynamic> data;        // Datos adicionales
  String? messageId;
  String? sentTime;
  // ...
}
```

### Estructura de `data` (Payload)

Todos los payloads incluyen estos campos en `data`:

```dart
{
  "type": String,           // Tipo de notificación
  "title": String,          // Título
  "body": String,           // Cuerpo del mensaje
  "imageUrl": String?,      // URL de imagen (opcional)
  "templateId": String?,    // ID de plantilla (opcional)
  // Campos específicos según el tipo...
}
```

### Tipos de Notificación (`type`)

| Tipo | Descripción | Campos Adicionales |
|------|-------------|-------------------|
| `appointment` | Nueva cita agendada | `appointmentId`, `clientName`, `clientPhone`, `date`, `time` |
| `announcement` | Anuncio del admin | `templateId` |
| `data_update` | Actualización de datos | `action` |

---

## 💻 Ejemplos de Código Flutter

### Ejemplo Completo: Servicio de Notificaciones

```dart
import 'dart:io';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';

class PushNotificationService {
  final FirebaseMessaging _firebaseMessaging = FirebaseMessaging.instance;
  final FlutterLocalNotificationsPlugin _localNotifications = 
      FlutterLocalNotificationsPlugin();
  
  String? _fcmToken;
  String? _authToken;
  
  // Inicializar servicio
  Future<void> initialize(String authToken) async {
    _authToken = authToken;
    
    // Configurar notificaciones locales
    await _setupLocalNotifications();
    
    // Obtener y registrar token
    await _registerDevice();
    
    // Configurar listeners
    _setupMessageHandlers();
    
    // Escuchar cambios de token
    _firebaseMessaging.onTokenRefresh.listen(_onTokenRefresh);
  }
  
  // Configurar notificaciones locales (para mostrar cuando la app está abierta)
  Future<void> _setupLocalNotifications() async {
    const androidSettings = AndroidInitializationSettings('@mipmap/ic_launcher');
    const iosSettings = DarwinInitializationSettings(
      requestAlertPermission: true,
      requestBadgePermission: true,
      requestSoundPermission: true,
    );
    
    const initSettings = InitializationSettings(
      android: androidSettings,
      iOS: iosSettings,
    );
    
    await _localNotifications.initialize(
      initSettings,
      onDidReceiveNotificationResponse: _onNotificationTapped,
    );
    
    // Crear canal para Android
    const androidChannel = AndroidNotificationChannel(
      'barberpro_notifications',
      'BarberPro Notificaciones',
      description: 'Notificaciones de citas y anuncios',
      importance: Importance.high,
    );
    
    await _localNotifications
        .resolvePlatformSpecificImplementation<
            AndroidFlutterLocalNotificationsPlugin>()
        ?.createNotificationChannel(androidChannel);
  }
  
  // Registrar dispositivo
  Future<void> _registerDevice() async {
    try {
      // Solicitar permisos
      NotificationSettings settings = await _firebaseMessaging.requestPermission(
        alert: true,
        badge: true,
        sound: true,
        provisional: false,
      );
      
      if (settings.authorizationStatus != AuthorizationStatus.authorized) {
        print('⚠️ Permisos de notificación denegados');
        return;
      }
      
      // Obtener token
      _fcmToken = await _firebaseMessaging.getToken();
      
      if (_fcmToken == null) {
        print('⚠️ No se pudo obtener token FCM');
        return;
      }
      
      print('📱 Token FCM obtenido: $_fcmToken');
      
      // Registrar en backend
      final platform = Platform.isAndroid ? 'android' : 'ios';
      await _registerDeviceInBackend(_fcmToken!, platform);
      
    } catch (e) {
      print('❌ Error al registrar dispositivo: $e');
    }
  }
  
  // Registrar dispositivo en backend
  Future<void> _registerDeviceInBackend(String fcmToken, String platform) async {
    try {
      final response = await http.post(
        Uri.parse('https://tu-api.com/api/notifications/devices'),
        headers: {
          'Authorization': 'Bearer $_authToken',
          'Content-Type': 'application/json',
        },
        body: jsonEncode({
          'fcmToken': fcmToken,
          'platform': platform,
        }),
      );
      
      if (response.statusCode == 201 || response.statusCode == 200) {
        print('✅ Dispositivo registrado en backend');
      } else {
        print('❌ Error al registrar: ${response.body}');
      }
    } catch (e) {
      print('❌ Error: $e');
    }
  }
  
  // Configurar handlers de mensajes
  void _setupMessageHandlers() {
    // Cuando la app está en primer plano
    FirebaseMessaging.onMessage.listen((RemoteMessage message) {
      print('📨 Notificación recibida en primer plano');
      _handleForegroundMessage(message);
    });
    
    // Cuando el usuario toca la notificación (app en segundo plano)
    FirebaseMessaging.onMessageOpenedApp.listen((RemoteMessage message) {
      print('👆 Usuario tocó la notificación');
      _handleNotificationTap(message);
    });
    
    // Cuando la app se abre desde una notificación (app cerrada)
    FirebaseMessaging.instance.getInitialMessage().then((message) {
      if (message != null) {
        print('🚀 App abierta desde notificación');
        _handleNotificationTap(message);
      }
    });
  }
  
  // Manejar notificación en primer plano
  Future<void> _handleForegroundMessage(RemoteMessage message) async {
    // Mostrar notificación local
    final notification = message.notification;
    final android = message.notification?.android;
    
    if (notification != null) {
      await _localNotifications.show(
        notification.hashCode,
        notification.title,
        notification.body,
        NotificationDetails(
          android: AndroidNotificationDetails(
            'barberpro_notifications',
            'BarberPro Notificaciones',
            channelDescription: 'Notificaciones de citas y anuncios',
            importance: Importance.high,
            priority: Priority.high,
            icon: android?.smallIcon,
          ),
          iOS: const DarwinNotificationDetails(),
        ),
        payload: jsonEncode(message.data),
      );
    }
    
    // Procesar datos
    _processNotificationData(message.data);
  }
  
  // Manejar cuando se toca la notificación
  void _onNotificationTapped(NotificationResponse response) {
    if (response.payload != null) {
      final data = jsonDecode(response.payload!);
      _handleNotificationTapData(data);
    }
  }
  
  // Procesar datos de la notificación
  void _processNotificationData(Map<String, dynamic> data) {
    final type = data['type'];
    
    switch (type) {
      case 'appointment':
        _handleAppointmentNotification(data);
        break;
      case 'announcement':
        _handleAnnouncementNotification(data);
        break;
      case 'data_update':
        _handleDataUpdateNotification(data);
        break;
      default:
        print('⚠️ Tipo de notificación desconocido: $type');
    }
  }
  
  // Manejar notificación de cita
  void _handleAppointmentNotification(Map<String, dynamic> data) {
    final appointmentId = data['appointmentId'];
    final clientName = data['clientName'];
    final date = data['date'];
    final time = data['time'];
    
    print('📅 Nueva cita: $clientName el $date a las $time');
    
    // Navegar a la pantalla de citas o refrescar datos
    // Ejemplo: navigatorKey.currentState?.pushNamed('/appointments');
  }
  
  // Manejar anuncio
  void _handleAnnouncementNotification(Map<String, dynamic> data) {
    final title = data['title'];
    final body = data['body'];
    
    print('📢 Anuncio: $title - $body');
    
    // Mostrar diálogo o navegar a pantalla de anuncios
  }
  
  // Manejar actualización de datos
  void _handleDataUpdateNotification(Map<String, dynamic> data) {
    final action = data['action'];
    
    print('🔄 Actualización de datos: $action');
    
    // Refrescar datos según la acción
    // Ejemplo: if (action == 'refresh_appointments') { refreshAppointments(); }
  }
  
  // Manejar tap en notificación
  void _handleNotificationTap(RemoteMessage message) {
    _handleNotificationTapData(message.data);
  }
  
  void _handleNotificationTapData(Map<String, dynamic> data) {
    final type = data['type'];
    
    if (type == 'appointment') {
      final appointmentId = data['appointmentId'];
      // Navegar a detalles de la cita
      // navigatorKey.currentState?.pushNamed('/appointments/$appointmentId');
    } else if (type == 'announcement') {
      // Navegar a pantalla de anuncios
      // navigatorKey.currentState?.pushNamed('/announcements');
    }
  }
  
  // Manejar cambio de token
  void _onTokenRefresh(String newToken) async {
    print('🔄 Token FCM actualizado: $newToken');
    
    if (_fcmToken != null && _authToken != null) {
      // Actualizar token en backend
      try {
        final response = await http.post(
          Uri.parse('https://tu-api.com/api/notifications/devices/refresh-token'),
          headers: {
            'Authorization': 'Bearer $_authToken',
            'Content-Type': 'application/json',
          },
          body: jsonEncode({
            'currentFcmToken': _fcmToken,
            'newFcmToken': newToken,
            'platform': Platform.isAndroid ? 'android' : 'ios',
          }),
        );
        
        if (response.statusCode == 200) {
          _fcmToken = newToken;
          print('✅ Token actualizado en backend');
        }
      } catch (e) {
        print('❌ Error al actualizar token: $e');
      }
    }
  }
  
  // Eliminar dispositivo (al cerrar sesión)
  Future<void> unregisterDevice(int deviceId) async {
    try {
      final response = await http.delete(
        Uri.parse('https://tu-api.com/api/notifications/devices/$deviceId'),
        headers: {
          'Authorization': 'Bearer $_authToken',
        },
      );
      
      if (response.statusCode == 204) {
        print('✅ Dispositivo eliminado');
      }
    } catch (e) {
      print('❌ Error al eliminar dispositivo: $e');
    }
  }
}
```

### Ejemplo: Uso en la App

```dart
// main.dart
void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await Firebase.initializeApp();
  
  // Inicializar servicio de notificaciones después del login
  // ...
  
  runApp(MyApp());
}

// Después del login exitoso
Future<void> onLoginSuccess(String authToken) async {
  final notificationService = PushNotificationService();
  await notificationService.initialize(authToken);
}

// Al cerrar sesión
Future<void> onLogout(int? deviceId) async {
  if (deviceId != null) {
    final notificationService = PushNotificationService();
    await notificationService.unregisterDevice(deviceId);
  }
}
```

---

## 🎯 Casos de Uso

### Caso 1: Usuario Inicia Sesión

```dart
// 1. Usuario hace login
final authResponse = await login(email, password);
final authToken = authResponse.token;

// 2. Inicializar notificaciones
final notificationService = PushNotificationService();
await notificationService.initialize(authToken);

// 3. El dispositivo queda registrado y listo para recibir notificaciones
```

### Caso 2: Cliente Agenda una Cita

**Flujo automático (no requiere acción del frontend):**

1. Cliente agenda cita desde web pública
2. Backend detecta nueva cita
3. Backend busca dispositivos del barbero
4. Backend envía notificación push automáticamente
5. **App Flutter recibe notificación:**
   - Si app está abierta → Se muestra notificación local
   - Si app está en segundo plano → Se muestra notificación del sistema
   - Si app está cerrada → Se muestra notificación del sistema

**El barbero ve:**
```
📱 Notificación: "Nueva cita agendada"
   "Maria González agendó una cita para el 15/01/2025 a las 10:00"
```

**Al tocar la notificación:**
- La app se abre
- Navega a la pantalla de citas
- Muestra la nueva cita

### Caso 3: Admin Envía Anuncio

**Flujo:**
1. Admin crea plantilla en `/admin/notifications`
2. Admin envía notificación a todos los barberos
3. **Todos los barberos reciben la notificación**

**El barbero ve:**
```
📱 Notificación: "Nueva actualización disponible"
   "Hemos agregado nuevas funcionalidades..."
```

### Caso 4: Token FCM Cambia

**Flujo automático:**
1. Firebase genera nuevo token
2. `onTokenRefresh` se ejecuta automáticamente
3. El servicio actualiza el token en el backend
4. Las notificaciones continúan funcionando

---

## 🔍 Manejo de Notificaciones

### Estados de la App

#### 1. App en Primer Plano (Abierta)

```dart
FirebaseMessaging.onMessage.listen((RemoteMessage message) {
  // Mostrar notificación local
  _showLocalNotification(message);
  
  // Procesar datos
  _processData(message.data);
  
  // Opcional: Mostrar snackbar o diálogo
  _showInAppNotification(message);
});
```

#### 2. App en Segundo Plano

```dart
FirebaseMessaging.onMessageOpenedApp.listen((RemoteMessage message) {
  // Usuario tocó la notificación
  _handleNotificationTap(message);
});
```

#### 3. App Cerrada

```dart
FirebaseMessaging.instance.getInitialMessage().then((message) {
  if (message != null) {
    // App se abrió desde notificación
    _handleNotificationTap(message);
  }
});
```

---

## 📋 Checklist de Implementación

- [ ] Instalar dependencias (`firebase_messaging`, `flutter_local_notifications`)
- [ ] Configurar Firebase en Android (`google-services.json`)
- [ ] Configurar Firebase en iOS (`GoogleService-Info.plist`)
- [ ] Inicializar Firebase en `main.dart`
- [ ] Crear servicio `PushNotificationService`
- [ ] Implementar registro de dispositivo después del login
- [ ] Implementar listeners de notificaciones
- [ ] Manejar notificaciones en primer plano
- [ ] Manejar taps en notificaciones
- [ ] Implementar navegación según tipo de notificación
- [ ] Implementar actualización de token
- [ ] Implementar eliminación de dispositivo al cerrar sesión
- [ ] Probar notificaciones en Android
- [ ] Probar notificaciones en iOS

---

## 🐛 Troubleshooting

### Problema 1: No se reciben notificaciones

**Soluciones:**
1. Verificar que el dispositivo está registrado:
   ```dart
   GET /api/notifications/devices
   ```

2. Verificar que el token FCM es válido:
   ```dart
   print('Token FCM: ${await _firebaseMessaging.getToken()}');
   ```

3. Verificar permisos (iOS):
   - Ir a Configuración → App → Notificaciones
   - Asegurarse de que están habilitadas

4. Verificar logs del backend:
   - Revisar `NotificationLogs` en la base de datos
   - Ver si hay errores al enviar

### Problema 2: Token FCM no se registra

**Soluciones:**
1. Verificar que el usuario está autenticado (token JWT válido)
2. Verificar que la URL del API es correcta
3. Verificar logs de red en Flutter DevTools
4. Verificar respuesta del servidor

### Problema 3: Notificaciones no se muestran en Android

**Soluciones:**
1. Verificar que el canal de notificaciones está creado
2. Verificar permisos de notificaciones en AndroidManifest.xml
3. Verificar que `flutter_local_notifications` está configurado

### Problema 4: Notificaciones no se muestran en iOS

**Soluciones:**
1. Verificar que los permisos están solicitados
2. Verificar que Push Notifications está habilitado en Xcode
3. Verificar certificados APNS en Firebase Console

---

## 📚 Referencias

- [Firebase Cloud Messaging Flutter](https://firebase.google.com/docs/cloud-messaging/flutter/client)
- [Flutter Local Notifications](https://pub.dev/packages/flutter_local_notifications)
- [Firebase Console](https://console.firebase.google.com/)

---

## 📞 Soporte

Si tienes problemas:
1. Revisa los logs del backend
2. Revisa los logs de Flutter (`flutter logs`)
3. Verifica que el dispositivo está registrado en la BD
4. Verifica que Firebase está configurado correctamente

---

**Última actualización:** 2025-01-14  
**Versión del backend:** Actualizada y verificada  
**Compatibilidad:** Flutter 3.0+ | Android 5.0+ | iOS 12.0+
