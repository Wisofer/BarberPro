# BarberPro - Documentación de API para Flutter

## 📋 Índice
1. [Configuración Base](#configuración-base)
2. [Autenticación](#autenticación)
3. [APIs Públicas (Sin Autenticación)](#apis-públicas)
4. [APIs de Barbero](#apis-de-barbero)
5. [APIs de Administrador](#apis-de-administrador)
6. [Modelos de Datos](#modelos-de-datos)
7. [Manejo de Errores](#manejo-de-errores)
8. [Ejemplos de Implementación Flutter](#ejemplos-de-implementación-flutter)

---

## 🔧 Configuración Base

### URL Base
```
Base URL: https://tu-dominio.com/api
```

### Headers Comunes
```dart
{
  'Content-Type': 'application/json',
  'Accept': 'application/json',
}
```

### Headers con Autenticación
```dart
{
  'Content-Type': 'application/json',
  'Accept': 'application/json',
  'Authorization': 'Bearer {token}',
}
```

---

## 🔐 Autenticación

### Login
**Endpoint:** `POST /api/auth/login`

**Autenticación:** No requerida

**Request Body:**
```json
{
  "email": "barbero@example.com",
  "password": "password123"
}
```

**Response 200 OK:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "email": "barbero@example.com",
    "role": "Barber",
    "barber": {
      "id": 1,
      "name": "Juan Pérez",
      "businessName": "Barbería Central",
      "phone": "1234567890",
      "slug": "juan-perez",
      "isActive": true,
      "qrUrl": "https://app.mibarberia.com/b/juan-perez",
      "createdAt": "2025-01-01T00:00:00Z",
      "email": "barbero@example.com"
    }
  },
  "role": "Barber"
}
```

**Response 401 Unauthorized:**
```json
{
  "message": "Credenciales inválidas"
}
```

**Ejemplo Flutter:**
```dart
Future<LoginResponse> login(String email, String password) async {
  final response = await http.post(
    Uri.parse('$baseUrl/auth/login'),
    headers: {'Content-Type': 'application/json'},
    body: jsonEncode({
      'email': email,
      'password': password,
    }),
  );
  
  if (response.statusCode == 200) {
    return LoginResponse.fromJson(jsonDecode(response.body));
  } else {
    throw Exception('Error en login: ${response.body}');
  }
}
```

---

## 🌐 APIs Públicas

### 1. Obtener Información del Barbero
**Endpoint:** `GET /api/public/barbers/{slug}`

**Autenticación:** No requerida

**Parámetros:**
- `slug` (path): Slug único del barbero (ej: "juan-perez")

**Response 200 OK:**
```json
{
  "id": 1,
  "name": "Juan Pérez",
  "businessName": "Barbería Central",
  "phone": "1234567890",
  "slug": "juan-perez",
  "services": [
    {
      "id": 1,
      "name": "Corte de Cabello",
      "price": 15.00,
      "durationMinutes": 30,
      "isActive": true
    }
  ],
  "workingHours": [
    {
      "id": 1,
      "dayOfWeek": 1,
      "startTime": "09:00",
      "endTime": "17:00",
      "isActive": true
    }
  ]
}
```

---

### 2. Obtener Disponibilidad del Barbero
**Endpoint:** `GET /api/public/barbers/{slug}/availability`

**Autenticación:** No requerida

**Parámetros:**
- `slug` (path): Slug único del barbero
- `date` (query, opcional): Fecha en formato `YYYY-MM-DD` (default: hoy)

**Ejemplo:** `/api/public/barbers/juan-perez/availability?date=2025-01-15`

**Response 200 OK:**
```json
{
  "date": "2025-01-15",
  "availableSlots": [
    "09:00",
    "09:30",
    "10:00",
    "10:30",
    "11:00"
  ],
  "blockedSlots": [
    "14:00",
    "14:30"
  ]
}
```

---

### 3. Crear Cita (Público)
**Endpoint:** `POST /api/public/appointments`

**Autenticación:** No requerida

**Request Body:**
```json
{
  "barberSlug": "juan-perez",
  "serviceId": 1,
  "clientName": "María García",
  "clientPhone": "9876543210",
  "date": "2025-01-15",
  "time": "10:00"
}
```

**Response 201 Created:**
```json
{
  "id": 1,
  "barberId": 1,
  "barberName": "Juan Pérez",
  "serviceId": 1,
  "serviceName": "Corte de Cabello",
  "servicePrice": 15.00,
  "clientName": "María García",
  "clientPhone": "9876543210",
  "date": "2025-01-15",
  "time": "10:00",
  "status": "Pending",
  "createdAt": "2025-01-01T10:00:00Z"
}
```

---

## 👨‍💼 APIs de Barbero

**Nota:** Todas las APIs de barbero requieren autenticación JWT con rol "Barber"

### Perfil

#### 1. Obtener Dashboard
**Endpoint:** `GET /api/barber/dashboard`

**Response 200 OK:**
```json
{
  "barber": {
    "id": 1,
    "name": "Juan Pérez",
    "businessName": "Barbería Central",
    "phone": "1234567890",
    "slug": "juan-perez",
    "isActive": true,
    "qrUrl": "https://app.mibarberia.com/b/juan-perez",
    "createdAt": "2025-01-01T00:00:00Z",
    "email": "barbero@example.com"
  },
  "today": {
    "appointments": 5,
    "completed": 3,
    "pending": 2,
    "income": 45.00
  },
  "thisWeek": {
    "appointments": 20,
    "income": 300.00,
    "expenses": 50.00,
    "profit": 250.00
  },
  "thisMonth": {
    "appointments": 80,
    "income": 1200.00,
    "expenses": 200.00,
    "profit": 1000.00
  },
  "recentAppointments": [...],
  "upcomingAppointments": [...]
}
```

#### 2. Obtener Perfil
**Endpoint:** `GET /api/barber/profile`

**Response 200 OK:**
```json
{
  "id": 1,
  "name": "Juan Pérez",
  "businessName": "Barbería Central",
  "phone": "1234567890",
  "slug": "juan-perez",
  "isActive": true,
  "qrUrl": "https://app.mibarberia.com/b/juan-perez",
  "createdAt": "2025-01-01T00:00:00Z",
  "email": "barbero@example.com"
}
```

#### 3. Actualizar Perfil
**Endpoint:** `PUT /api/barber/profile`

**Request Body:**
```json
{
  "name": "Juan Pérez",
  "businessName": "Barbería Central",
  "phone": "1234567890"
}
```

**Response 200 OK:** (Mismo formato que GET /api/barber/profile)

---

### QR Code

#### 4. Obtener URL y Código QR
**Endpoint:** `GET /api/barber/qr-url`

**Response 200 OK:**
```json
{
  "url": "https://app.mibarberia.com/b/juan-perez",
  "qrCode": "iVBORw0KGgoAAAANSUhEUgAA..."
}
```

**Nota:** `qrCode` es una imagen en Base64 (PNG). Puedes decodificarla y mostrarla en Flutter.

---

### Citas

#### 5. Obtener Citas
**Endpoint:** `GET /api/barber/appointments`

**Parámetros Query (opcionales):**
- `date`: Fecha en formato `YYYY-MM-DD`
- `status`: Estado de la cita (`Pending`, `Confirmed`, `Cancelled`, `Completed`)

**Ejemplo:** `/api/barber/appointments?date=2025-01-15&status=Confirmed`

**Response 200 OK:**
```json
[
  {
    "id": 1,
    "barberId": 1,
    "barberName": "Juan Pérez",
    "serviceId": 1,
    "serviceName": "Corte de Cabello",
    "servicePrice": 15.00,
    "clientName": "María García",
    "clientPhone": "9876543210",
    "date": "2025-01-15",
    "time": "10:00",
    "status": "Confirmed",
    "createdAt": "2025-01-01T10:00:00Z"
  }
]
```

#### 6. Crear Cita Manual
**Endpoint:** `POST /api/barber/appointments`

**Nota:** El barbero debe incluir su propio `barberSlug` en el request (puede obtenerlo de su perfil).

**Request Body:**
```json
{
  "barberSlug": "juan-perez",
  "serviceId": 1,
  "clientName": "María García",
  "clientPhone": "9876543210",
  "date": "2025-01-15",
  "time": "10:00"
}
```

**Response 201 Created:** (Mismo formato que GET /api/barber/appointments)

#### 7. Actualizar Cita
**Endpoint:** `PUT /api/barber/appointments/{id}`

**Request Body:**
```json
{
  "status": "Confirmed",
  "date": "2025-01-15",
  "time": "10:30",
  "clientName": "María García",
  "clientPhone": "9876543210"
}
```

**Response 200 OK:** (Mismo formato que GET /api/barber/appointments)

#### 8. Eliminar Cita
**Endpoint:** `DELETE /api/barber/appointments/{id}`

**Response 204 No Content**

---

### Servicios

#### 9. Obtener Servicios
**Endpoint:** `GET /api/barber/services`

**Response 200 OK:**
```json
[
  {
    "id": 1,
    "name": "Corte de Cabello",
    "price": 15.00,
    "durationMinutes": 30,
    "isActive": true
  }
]
```

#### 10. Crear Servicio
**Endpoint:** `POST /api/barber/services`

**Request Body:**
```json
{
  "name": "Corte de Cabello",
  "price": 15.00,
  "durationMinutes": 30
}
```

**Response 201 Created:** (Mismo formato que GET /api/barber/services)

---

### Finanzas

#### 11. Obtener Resumen Financiero
**Endpoint:** `GET /api/barber/finances/summary`

**Parámetros Query (opcionales):**
- `startDate`: Fecha inicio en formato `YYYY-MM-DDTHH:mm:ssZ`
- `endDate`: Fecha fin en formato `YYYY-MM-DDTHH:mm:ssZ`

**Response 200 OK:**
```json
{
  "incomeThisMonth": 1200.00,
  "expensesThisMonth": 200.00,
  "profitThisMonth": 1000.00,
  "totalIncome": 5000.00,
  "totalExpenses": 1000.00,
  "netProfit": 4000.00
}
```

#### 12. Obtener Ingresos
**Endpoint:** `GET /api/barber/finances/income`

**Parámetros Query (opcionales):**
- `startDate`: Fecha inicio
- `endDate`: Fecha fin
- `page`: Número de página (default: 1)
- `pageSize`: Tamaño de página (default: 50)

**Response 200 OK:**
```json
{
  "transactions": [
    {
      "id": 1,
      "type": "Income",
      "amount": 15.00,
      "description": "Cita #1",
      "date": "2025-01-15T10:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 50,
  "totalPages": 2
}
```

#### 13. Obtener Egresos
**Endpoint:** `GET /api/barber/finances/expenses`

**Parámetros:** (Igual que ingresos)

**Response 200 OK:** (Mismo formato que ingresos, pero con `type: "Expense"`)

#### 14. Crear Egreso
**Endpoint:** `POST /api/barber/finances/expenses`

**Request Body:**
```json
{
  "amount": 50.00,
  "description": "Compra de productos",
  "date": "2025-01-15T10:00:00Z"
}
```

**Response 201 Created:**
```json
{
  "id": 1,
  "type": "Expense",
  "amount": 50.00,
  "description": "Compra de productos",
  "date": "2025-01-15T10:00:00Z"
}
```

---

## 👑 APIs de Administrador

**Nota:** Todas las APIs de administrador requieren autenticación JWT con rol "Admin"

### Dashboard

#### 1. Obtener Dashboard del Admin
**Endpoint:** `GET /api/admin/dashboard`

**Response 200 OK:**
```json
{
  "totalBarbers": 10,
  "activeBarbers": 8,
  "inactiveBarbers": 2,
  "totalAppointments": 500,
  "pendingAppointments": 50,
  "confirmedAppointments": 400,
  "cancelledAppointments": 50,
  "totalRevenue": 7500.00,
  "recentBarbers": [
    {
      "id": 1,
      "name": "Juan Pérez",
      "businessName": "Barbería Central",
      "phone": "1234567890",
      "slug": "juan-perez",
      "isActive": true,
      "createdAt": "2025-01-01T00:00:00Z",
      "totalAppointments": 100,
      "totalRevenue": 1500.00
    }
  ]
}
```

### Barberos

#### 2. Obtener Todos los Barberos
**Endpoint:** `GET /api/admin/barbers`

**Parámetros Query (opcionales):**
- `isActive`: `true` o `false` para filtrar por estado

**Response 200 OK:**
```json
[
  {
    "id": 1,
    "name": "Juan Pérez",
    "businessName": "Barbería Central",
    "phone": "1234567890",
    "slug": "juan-perez",
    "isActive": true,
    "qrUrl": "https://app.mibarberia.com/b/juan-perez",
    "createdAt": "2025-01-01T00:00:00Z",
    "email": "barbero@example.com"
  }
]
```

#### 3. Crear Barbero
**Endpoint:** `POST /api/admin/barbers`

**Request Body:**
```json
{
  "email": "nuevo@example.com",
  "password": "password123",
  "name": "Nuevo Barbero",
  "businessName": "Nueva Barbería",
  "phone": "1234567890"
}
```

**Response 201 Created:** (Mismo formato que GET /api/admin/barbers)

#### 4. Actualizar Estado del Barbero
**Endpoint:** `PUT /api/admin/barbers/{id}/status`

**Request Body:**
```json
{
  "isActive": false
}
```

**Response 204 No Content**

#### 5. Eliminar Barbero
**Endpoint:** `DELETE /api/admin/barbers/{id}`

**Response 204 No Content**

---

## 📦 Modelos de Datos

### LoginRequest
```dart
class LoginRequest {
  final String email;
  final String password;
  
  LoginRequest({required this.email, required this.password});
  
  Map<String, dynamic> toJson() => {
    'email': email,
    'password': password,
  };
}
```

### LoginResponse
```dart
class LoginResponse {
  final String token;
  final UserDto user;
  final String role;
  
  LoginResponse({
    required this.token,
    required this.user,
    required this.role,
  });
  
  factory LoginResponse.fromJson(Map<String, dynamic> json) => LoginResponse(
    token: json['token'],
    user: UserDto.fromJson(json['user']),
    role: json['role'],
  );
}
```

### BarberDto
```dart
class BarberDto {
  final int id;
  final String name;
  final String? businessName;
  final String phone;
  final String slug;
  final bool isActive;
  final String qrUrl;
  final DateTime createdAt;
  final String? email;
  
  BarberDto({
    required this.id,
    required this.name,
    this.businessName,
    required this.phone,
    required this.slug,
    required this.isActive,
    required this.qrUrl,
    required this.createdAt,
    this.email,
  });
  
  factory BarberDto.fromJson(Map<String, dynamic> json) => BarberDto(
    id: json['id'],
    name: json['name'],
    businessName: json['businessName'],
    phone: json['phone'],
    slug: json['slug'],
    isActive: json['isActive'],
    qrUrl: json['qrUrl'],
    createdAt: DateTime.parse(json['createdAt']),
    email: json['email'],
  );
}
```

### AppointmentDto
```dart
class AppointmentDto {
  final int id;
  final int barberId;
  final String barberName;
  final int serviceId;
  final String serviceName;
  final double servicePrice;
  final String clientName;
  final String clientPhone;
  final String date; // DateOnly format: "YYYY-MM-DD"
  final String time; // TimeOnly format: "HH:mm"
  final String status; // "Pending", "Confirmed", "Cancelled", "Completed"
  final DateTime createdAt;
  
  AppointmentDto({
    required this.id,
    required this.barberId,
    required this.barberName,
    required this.serviceId,
    required this.serviceName,
    required this.servicePrice,
    required this.clientName,
    required this.clientPhone,
    required this.date,
    required this.time,
    required this.status,
    required this.createdAt,
  });
  
  factory AppointmentDto.fromJson(Map<String, dynamic> json) => AppointmentDto(
    id: json['id'],
    barberId: json['barberId'],
    barberName: json['barberName'],
    serviceId: json['serviceId'],
    serviceName: json['serviceName'],
    servicePrice: (json['servicePrice'] as num).toDouble(),
    clientName: json['clientName'],
    clientPhone: json['clientPhone'],
    date: json['date'],
    time: json['time'],
    status: json['status'],
    createdAt: DateTime.parse(json['createdAt']),
  );
}
```

---

## ⚠️ Manejo de Errores

### Códigos de Estado HTTP

- **200 OK**: Solicitud exitosa
- **201 Created**: Recurso creado exitosamente
- **204 No Content**: Operación exitosa sin contenido
- **400 Bad Request**: Solicitud inválida (validación fallida)
- **401 Unauthorized**: No autenticado o token inválido
- **403 Forbidden**: No tiene permisos (rol incorrecto)
- **404 Not Found**: Recurso no encontrado
- **500 Internal Server Error**: Error interno del servidor

### Formato de Error
```json
{
  "message": "Descripción del error"
}
```

### Ejemplo de Manejo en Flutter
```dart
Future<T> handleResponse<T>(http.Response response, T Function(Map<String, dynamic>) fromJson) async {
  if (response.statusCode >= 200 && response.statusCode < 300) {
    return fromJson(jsonDecode(response.body));
  } else {
    final error = jsonDecode(response.body);
    throw ApiException(
      statusCode: response.statusCode,
      message: error['message'] ?? 'Error desconocido',
    );
  }
}

class ApiException implements Exception {
  final int statusCode;
  final String message;
  
  ApiException({required this.statusCode, required this.message});
  
  @override
  String toString() => 'ApiException($statusCode): $message';
}
```

---

## 💻 Ejemplos de Implementación Flutter

### Clase de Servicio Base
```dart
import 'dart:convert';
import 'package:http/http.dart' as http;

class ApiService {
  final String baseUrl = 'https://tu-dominio.com/api';
  String? _token;
  
  void setToken(String token) {
    _token = token;
  }
  
  Map<String, String> get headers => {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
    if (_token != null) 'Authorization': 'Bearer $_token',
  };
  
  Future<Map<String, dynamic>> get(String endpoint) async {
    final response = await http.get(
      Uri.parse('$baseUrl$endpoint'),
      headers: headers,
    );
    return _handleResponse(response);
  }
  
  Future<Map<String, dynamic>> post(String endpoint, Map<String, dynamic> body) async {
    final response = await http.post(
      Uri.parse('$baseUrl$endpoint'),
      headers: headers,
      body: jsonEncode(body),
    );
    return _handleResponse(response);
  }
  
  Future<Map<String, dynamic>> put(String endpoint, Map<String, dynamic> body) async {
    final response = await http.put(
      Uri.parse('$baseUrl$endpoint'),
      headers: headers,
      body: jsonEncode(body),
    );
    return _handleResponse(response);
  }
  
  Future<void> delete(String endpoint) async {
    final response = await http.delete(
      Uri.parse('$baseUrl$endpoint'),
      headers: headers,
    );
    _handleResponse(response);
  }
  
  Map<String, dynamic> _handleResponse(http.Response response) {
    if (response.statusCode >= 200 && response.statusCode < 300) {
      if (response.body.isEmpty) {
        return {};
      }
      return jsonDecode(response.body);
    } else {
      final error = jsonDecode(response.body);
      throw Exception(error['message'] ?? 'Error desconocido');
    }
  }
}
```

### Servicio de Autenticación
```dart
class AuthService {
  final ApiService _api = ApiService();
  
  Future<LoginResponse> login(String email, String password) async {
    final response = await _api.post('/auth/login', {
      'email': email,
      'password': password,
    });
    
    final loginResponse = LoginResponse.fromJson(response);
    _api.setToken(loginResponse.token);
    
    return loginResponse;
  }
}
```

### Servicio de Barbero
```dart
class BarberService {
  final ApiService _api = ApiService();
  
  Future<BarberDashboardDto> getDashboard() async {
    final response = await _api.get('/barber/dashboard');
    return BarberDashboardDto.fromJson(response);
  }
  
  Future<BarberDto> getProfile() async {
    final response = await _api.get('/barber/profile');
    return BarberDto.fromJson(response);
  }
  
  Future<BarberDto> updateProfile({
    required String name,
    String? businessName,
    required String phone,
  }) async {
    final response = await _api.put('/barber/profile', {
      'name': name,
      'businessName': businessName,
      'phone': phone,
    });
    return BarberDto.fromJson(response);
  }
  
  Future<QrResponse> getQrCode() async {
    final response = await _api.get('/barber/qr-url');
    return QrResponse.fromJson(response);
  }
  
  Future<List<AppointmentDto>> getAppointments({
    String? date,
    String? status,
  }) async {
    String endpoint = '/barber/appointments';
    List<String> params = [];
    if (date != null) params.add('date=$date');
    if (status != null) params.add('status=$status');
    if (params.isNotEmpty) endpoint += '?${params.join('&')}';
    
    final response = await _api.get(endpoint);
    return (response as List)
        .map((json) => AppointmentDto.fromJson(json))
        .toList();
  }
  
  Future<AppointmentDto> createAppointment({
    required String barberSlug, // El barbero debe enviar su propio slug
    required int serviceId,
    required String clientName,
    required String clientPhone,
    required String date,
    required String time,
  }) async {
    final response = await _api.post('/barber/appointments', {
      'barberSlug': barberSlug,
      'serviceId': serviceId,
      'clientName': clientName,
      'clientPhone': clientPhone,
      'date': date,
      'time': time,
    });
    return AppointmentDto.fromJson(response);
  }
  
  Future<AppointmentDto> updateAppointment({
    required int id,
    String? status,
    String? date,
    String? time,
    String? clientName,
    String? clientPhone,
  }) async {
    final body = <String, dynamic>{};
    if (status != null) body['status'] = status;
    if (date != null) body['date'] = date;
    if (time != null) body['time'] = time;
    if (clientName != null) body['clientName'] = clientName;
    if (clientPhone != null) body['clientPhone'] = clientPhone;
    
    final response = await _api.put('/barber/appointments/$id', body);
    return AppointmentDto.fromJson(response);
  }
  
  Future<void> deleteAppointment(int id) async {
    await _api.delete('/barber/appointments/$id');
  }
  
  Future<List<ServiceDto>> getServices() async {
    final response = await _api.get('/barber/services');
    return (response as List)
        .map((json) => ServiceDto.fromJson(json))
        .toList();
  }
  
  Future<ServiceDto> createService({
    required String name,
    required double price,
    required int durationMinutes,
  }) async {
    final response = await _api.post('/barber/services', {
      'name': name,
      'price': price,
      'durationMinutes': durationMinutes,
    });
    return ServiceDto.fromJson(response);
  }
  
  Future<FinanceSummaryDto> getFinanceSummary({
    DateTime? startDate,
    DateTime? endDate,
  }) async {
    String endpoint = '/barber/finances/summary';
    List<String> params = [];
    if (startDate != null) params.add('startDate=${startDate.toIso8601String()}');
    if (endDate != null) params.add('endDate=${endDate.toIso8601String()}');
    if (params.isNotEmpty) endpoint += '?${params.join('&')}';
    
    final response = await _api.get(endpoint);
    return FinanceSummaryDto.fromJson(response);
  }
}
```

### Servicio Público
```dart
class PublicService {
  final ApiService _api = ApiService();
  
  Future<BarberPublicDto> getBarber(String slug) async {
    final response = await _api.get('/public/barbers/$slug');
    return BarberPublicDto.fromJson(response);
  }
  
  Future<AvailabilityResponse> getAvailability(String slug, {String? date}) async {
    String endpoint = '/public/barbers/$slug/availability';
    if (date != null) endpoint += '?date=$date';
    
    final response = await _api.get(endpoint);
    return AvailabilityResponse.fromJson(response);
  }
  
  Future<AppointmentDto> createAppointment({
    required String barberSlug,
    required int serviceId,
    required String clientName,
    required String clientPhone,
    required String date,
    required String time,
  }) async {
    final response = await _api.post('/public/appointments', {
      'barberSlug': barberSlug,
      'serviceId': serviceId,
      'clientName': clientName,
      'clientPhone': clientPhone,
      'date': date,
      'time': time,
    });
    return AppointmentDto.fromJson(response);
  }
}
```

---

## 📝 Notas Importantes

1. **Autenticación JWT**: El token debe incluirse en el header `Authorization: Bearer {token}` para todas las rutas protegidas.

2. **Formato de Fechas**:
   - `DateOnly`: Formato `"YYYY-MM-DD"` (ej: `"2025-01-15"`)
   - `TimeOnly`: Formato `"HH:mm"` (ej: `"10:30"`)
   - `DateTime`: Formato ISO 8601 (ej: `"2025-01-15T10:30:00Z"`)

3. **Manejo de Errores**: Siempre verifica el código de estado HTTP y maneja los errores apropiadamente.

4. **Paginación**: Las APIs que devuelven listas grandes (como ingresos/egresos) soportan paginación con `page` y `pageSize`.

5. **Validaciones**: El servidor valida todos los datos. Asegúrate de enviar datos válidos según las validaciones del servidor.

---

## 🔗 Recursos Adicionales

- **Swagger UI**: Accede a `https://tu-dominio.com/swagger` para ver la documentación interactiva de la API
- **Base URL QR**: `https://app.mibarberia.com/b/{slug}` (configurable en el servidor)

---

**Última actualización:** Enero 2025
**Versión de API:** v1

