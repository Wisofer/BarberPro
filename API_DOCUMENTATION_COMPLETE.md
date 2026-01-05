# BarberPro - Documentación Completa de API

## 📋 Tabla de Contenidos

1. [Información General](#información-general)
2. [Autenticación](#autenticación)
3. [Rutas Públicas](#rutas-públicas-sin-autenticación)
4. [Rutas de Autenticación](#rutas-de-autenticación)
5. [Rutas de Barbero](#rutas-de-barbero-jwt-requerido)
6. [Rutas de Administrador](#rutas-de-administrador-jwt-requerido)
7. [Rutas Web MVC](#rutas-web-mvc)
8. [Modelos de Datos](#modelos-de-datos)
9. [Códigos de Estado HTTP](#códigos-de-estado-http)
10. [Ejemplos de Uso](#ejemplos-de-uso)
11. [Errores Comunes](#errores-comunes)

---

## Información General

**Base URL:** `https://app.mibarberia.com` (o tu dominio)

**Versión de API:** v1

**Formato de Respuesta:** JSON

**Autenticación:** JWT Bearer Token (para rutas protegidas)

---

## Autenticación

### JWT Token

Todas las rutas protegidas requieren un token JWT en el header:

```
Authorization: Bearer {token}
```

El token se obtiene mediante el endpoint `/api/auth/login` y tiene una duración configurable (por defecto 60 minutos).

---

## Rutas Públicas (Sin Autenticación)

### 1. GET /api/public/barbers/{slug}

Obtiene la información pública de un barbero, incluyendo servicios y horarios de trabajo.

**Parámetros:**
- `slug` (path, requerido): Slug único del barbero (ej: `juan-perez`)

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
      "price": 50.00,
      "durationMinutes": 30,
      "isActive": true
    },
    {
      "id": 2,
      "name": "Afeitado Clásico",
      "price": 75.00,
      "durationMinutes": 45,
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
    },
    {
      "id": 2,
      "dayOfWeek": 2,
      "startTime": "09:00",
      "endTime": "17:00",
      "isActive": true
    }
  ]
}
```

**Códigos de Estado:**
- `200 OK`: Barbero encontrado
- `404 Not Found`: Barbero no encontrado
- `500 Internal Server Error`: Error del servidor

---

### 2. GET /api/public/barbers/{slug}/availability

Obtiene los horarios disponibles de un barbero para una fecha específica.

**Parámetros:**
- `slug` (path, requerido): Slug único del barbero
- `date` (query, opcional): Fecha en formato `YYYY-MM-DD`. Si no se proporciona, usa la fecha actual.

**Ejemplo de Request:**
```
GET /api/public/barbers/juan-perez/availability?date=2024-01-15
```

**Response 200 OK:**
```json
{
  "date": "2024-01-15",
  "availableSlots": [
    {
      "startTime": "09:00",
      "endTime": "09:30",
      "isAvailable": true
    },
    {
      "startTime": "09:30",
      "endTime": "10:00",
      "isAvailable": true
    },
    {
      "startTime": "10:00",
      "endTime": "10:30",
      "isAvailable": false
    },
    {
      "startTime": "10:30",
      "endTime": "11:00",
      "isAvailable": true
    }
  ]
}
```

**Códigos de Estado:**
- `200 OK`: Disponibilidad obtenida
- `400 Bad Request`: Fecha inválida
- `404 Not Found`: Barbero no encontrado
- `500 Internal Server Error`: Error del servidor

---

### 3. POST /api/public/appointments

Crea una nueva cita sin necesidad de autenticación (público).

**Request Body:**
```json
{
  "barberSlug": "juan-perez",
  "serviceId": 1,
  "clientName": "María García",
  "clientPhone": "87654321",
  "date": "2024-01-15",
  "time": "10:00"
}
```

**Validaciones:**
- `barberSlug`: Requerido, debe existir
- `serviceId`: Requerido, debe pertenecer al barbero
- `clientName`: Requerido, máximo 200 caracteres
- `clientPhone`: Requerido, máximo 20 caracteres
- `date`: Requerido, formato `YYYY-MM-DD`, no puede ser en el pasado
- `time`: Requerido, formato `HH:mm`, debe estar dentro de horarios laborales y disponible

**Response 201 Created:**
```json
{
  "id": 123,
  "barberId": 1,
  "barberName": "Juan Pérez",
  "serviceId": 1,
  "serviceName": "Corte de Cabello",
  "servicePrice": 50.00,
  "clientName": "María García",
  "clientPhone": "87654321",
  "date": "2024-01-15",
  "time": "10:00",
  "status": "Pending",
  "createdAt": "2024-01-10T10:00:00Z"
}
```

**Códigos de Estado:**
- `201 Created`: Cita creada exitosamente
- `400 Bad Request`: Datos inválidos o horario no disponible
- `404 Not Found`: Barbero o servicio no encontrado
- `500 Internal Server Error`: Error del servidor

---

## Rutas de Autenticación

### 4. POST /api/auth/login

Autentica un usuario (Admin o Barbero) y retorna un token JWT.

**Request Body:**
```json
{
  "email": "barbero@example.com",
  "password": "password123"
}
```

**Validaciones:**
- `email`: Requerido, debe ser un email válido
- `password`: Requerido, mínimo 6 caracteres

**Response 200 OK:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6Ikp1YW4gUMOpcmV6IiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
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
      "createdAt": "2024-01-01T00:00:00Z"
    }
  },
  "role": "Barber"
}
```

**Response para Admin:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "email": "admin@barberpro.com",
    "role": "Admin",
    "barber": null
  },
  "role": "Admin"
}
```

**Códigos de Estado:**
- `200 OK`: Login exitoso
- `400 Bad Request`: Datos inválidos
- `401 Unauthorized`: Credenciales inválidas
- `500 Internal Server Error`: Error del servidor

---

## Rutas de Barbero (JWT Requerido)

**Nota:** Todas las rutas de barbero requieren el header `Authorization: Bearer {token}` y el usuario debe tener rol `Barber`.

### 5. GET /api/barber/dashboard

Obtiene el dashboard completo del barbero con estadísticas y citas recientes.

**Headers:**
```
Authorization: Bearer {token}
```

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
    "createdAt": "2024-01-01T00:00:00Z"
  },
  "today": {
    "appointments": 5,
    "completed": 3,
    "pending": 2,
    "income": 250.00
  },
  "thisWeek": {
    "appointments": 25,
    "income": 1250.00,
    "expenses": 300.00,
    "profit": 950.00
  },
  "thisMonth": {
    "appointments": 100,
    "income": 5000.00,
    "expenses": 1200.00,
    "profit": 3800.00
  },
  "recentAppointments": [
    {
      "id": 123,
      "barberId": 1,
      "barberName": "Juan Pérez",
      "serviceId": 1,
      "serviceName": "Corte de Cabello",
      "servicePrice": 50.00,
      "clientName": "María García",
      "clientPhone": "87654321",
      "date": "2024-01-15",
      "time": "10:00",
      "status": "Completed",
      "createdAt": "2024-01-10T10:00:00Z"
    }
  ],
  "upcomingAppointments": [
    {
      "id": 124,
      "barberId": 1,
      "barberName": "Juan Pérez",
      "serviceId": 2,
      "serviceName": "Afeitado Clásico",
      "servicePrice": 75.00,
      "clientName": "Carlos López",
      "clientPhone": "12345678",
      "date": "2024-01-16",
      "time": "14:00",
      "status": "Confirmed",
      "createdAt": "2024-01-11T09:00:00Z"
    }
  ]
}
```

**Códigos de Estado:**
- `200 OK`: Dashboard obtenido
- `401 Unauthorized`: Token inválido o expirado
- `403 Forbidden`: Usuario no es barbero
- `500 Internal Server Error`: Error del servidor

---

### 6. GET /api/barber/profile

Obtiene el perfil del barbero autenticado.

**Headers:**
```
Authorization: Bearer {token}
```

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
  "createdAt": "2024-01-01T00:00:00Z"
}
```

**Códigos de Estado:**
- `200 OK`: Perfil obtenido
- `401 Unauthorized`: Token inválido
- `404 Not Found`: Barbero no encontrado
- `500 Internal Server Error`: Error del servidor

---

### 7. PUT /api/barber/profile

Actualiza el perfil del barbero autenticado.

**Headers:**
```
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "name": "Juan Pérez",
  "businessName": "Barbería Central Actualizada",
  "phone": "9876543210"
}
```

**Validaciones:**
- `name`: Requerido, máximo 200 caracteres
- `businessName`: Opcional, máximo 200 caracteres
- `phone`: Requerido, máximo 20 caracteres

**Response 200 OK:**
```json
{
  "id": 1,
  "name": "Juan Pérez",
  "businessName": "Barbería Central Actualizada",
  "phone": "9876543210",
  "slug": "juan-perez",
  "isActive": true,
  "qrUrl": "https://app.mibarberia.com/b/juan-perez",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

**Códigos de Estado:**
- `200 OK`: Perfil actualizado
- `400 Bad Request`: Datos inválidos
- `401 Unauthorized`: Token inválido
- `404 Not Found`: Barbero no encontrado
- `500 Internal Server Error`: Error del servidor

---

### 8. GET /api/barber/qr-url

Obtiene la URL pública y el código QR (en Base64) del barbero autenticado.

**Headers:**
```
Authorization: Bearer {token}
```

**Response 200 OK:**
```json
{
  "url": "https://app.mibarberia.com/b/juan-perez",
  "qrCode": "iVBORw0KGgoAAAANSUhEUgAA..."
}
```

**Nota:** `qrCode` es una imagen PNG codificada en Base64. Para mostrarla en HTML:
```html
<img src="data:image/png;base64,{qrCode}" alt="QR Code" />
```

**Códigos de Estado:**
- `200 OK`: QR obtenido
- `401 Unauthorized`: Token inválido
- `404 Not Found`: Barbero no encontrado
- `500 Internal Server Error`: Error del servidor

---

### 9. GET /api/barber/appointments

Obtiene las citas del barbero autenticado con filtros opcionales.

**Headers:**
```
Authorization: Bearer {token}
```

**Query Parameters:**
- `date` (opcional): Filtrar por fecha específica (formato `YYYY-MM-DD`)
- `status` (opcional): Filtrar por estado (`Pending`, `Confirmed`, `Completed`, `Cancelled`)

**Ejemplo de Request:**
```
GET /api/barber/appointments?date=2024-01-15&status=Pending
```

**Response 200 OK:**
```json
[
  {
    "id": 123,
    "barberId": 1,
    "barberName": "Juan Pérez",
    "serviceId": 1,
    "serviceName": "Corte de Cabello",
    "servicePrice": 50.00,
    "clientName": "María García",
    "clientPhone": "87654321",
    "date": "2024-01-15",
    "time": "10:00",
    "status": "Pending",
    "createdAt": "2024-01-10T10:00:00Z"
  },
  {
    "id": 124,
    "barberId": 1,
    "barberName": "Juan Pérez",
    "serviceId": 2,
    "serviceName": "Afeitado Clásico",
    "servicePrice": 75.00,
    "clientName": "Carlos López",
    "clientPhone": "12345678",
    "date": "2024-01-15",
    "time": "14:00",
    "status": "Confirmed",
    "createdAt": "2024-01-11T09:00:00Z"
  }
]
```

**Códigos de Estado:**
- `200 OK`: Citas obtenidas
- `401 Unauthorized`: Token inválido
- `500 Internal Server Error`: Error del servidor

---

### 10. POST /api/barber/appointments

Crea una cita manualmente para el barbero autenticado.

**Headers:**
```
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "barberSlug": "juan-perez",
  "serviceId": 1,
  "clientName": "Pedro Martínez",
  "clientPhone": "5551234",
  "date": "2024-01-20",
  "time": "11:00"
}
```

**Nota:** El `barberSlug` debe coincidir con el barbero autenticado.

**Response 201 Created:**
```json
{
  "id": 125,
  "barberId": 1,
  "barberName": "Juan Pérez",
  "serviceId": 1,
  "serviceName": "Corte de Cabello",
  "servicePrice": 50.00,
  "clientName": "Pedro Martínez",
  "clientPhone": "5551234",
  "date": "2024-01-20",
  "time": "11:00",
  "status": "Pending",
  "createdAt": "2024-01-12T10:00:00Z"
}
```

**Códigos de Estado:**
- `201 Created`: Cita creada
- `400 Bad Request`: Datos inválidos o horario no disponible
- `401 Unauthorized`: Token inválido
- `404 Not Found`: Servicio no encontrado
- `500 Internal Server Error`: Error del servidor

---

### 11. PUT /api/barber/appointments/{id}

Actualiza una cita específica (cambiar estado, fecha u hora).

**Headers:**
```
Authorization: Bearer {token}
```

**Parámetros:**
- `id` (path, requerido): ID de la cita

**Request Body:**
```json
{
  "status": "Confirmed",
  "date": "2024-01-21",
  "time": "15:00"
}
```

**Nota:** Todos los campos son opcionales. Solo se actualizan los campos proporcionados.

**Estados válidos:**
- `Pending`: Pendiente (por defecto)
- `Confirmed`: Confirmada
- `Completed`: Completada (genera ingreso automáticamente)
- `Cancelled`: Cancelada

**Response 200 OK:**
```json
{
  "id": 125,
  "barberId": 1,
  "barberName": "Juan Pérez",
  "serviceId": 1,
  "serviceName": "Corte de Cabello",
  "servicePrice": 50.00,
  "clientName": "Pedro Martínez",
  "clientPhone": "5551234",
  "date": "2024-01-21",
  "time": "15:00",
  "status": "Confirmed",
  "createdAt": "2024-01-12T10:00:00Z"
}
```

**Códigos de Estado:**
- `200 OK`: Cita actualizada
- `400 Bad Request`: Datos inválidos o horario no disponible
- `401 Unauthorized`: Token inválido
- `404 Not Found`: Cita no encontrada
- `500 Internal Server Error`: Error del servidor

---

### 12. DELETE /api/barber/appointments/{id}

Elimina una cita específica.

**Headers:**
```
Authorization: Bearer {token}
```

**Parámetros:**
- `id` (path, requerido): ID de la cita

**Response 204 No Content:** (Sin cuerpo)

**Códigos de Estado:**
- `204 No Content`: Cita eliminada
- `401 Unauthorized`: Token inválido
- `404 Not Found`: Cita no encontrada
- `500 Internal Server Error`: Error del servidor

---

### 13. GET /api/barber/services

Obtiene todos los servicios del barbero autenticado.

**Headers:**
```
Authorization: Bearer {token}
```

**Response 200 OK:**
```json
[
  {
    "id": 1,
    "name": "Corte de Cabello",
    "price": 50.00,
    "durationMinutes": 30,
    "isActive": true
  },
  {
    "id": 2,
    "name": "Afeitado Clásico",
    "price": 75.00,
    "durationMinutes": 45,
    "isActive": true
  },
  {
    "id": 3,
    "name": "Corte + Afeitado",
    "price": 120.00,
    "durationMinutes": 60,
    "isActive": false
  }
]
```

**Códigos de Estado:**
- `200 OK`: Servicios obtenidos
- `401 Unauthorized`: Token inválido
- `500 Internal Server Error`: Error del servidor

---

### 14. POST /api/barber/services

Crea un nuevo servicio para el barbero autenticado.

**Headers:**
```
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "name": "Corte + Barba",
  "price": 90.00,
  "durationMinutes": 45
}
```

**Validaciones:**
- `name`: Requerido, máximo 200 caracteres
- `price`: Requerido, debe ser mayor a 0.01
- `durationMinutes`: Requerido, entre 15 y 480 minutos

**Response 201 Created:**
```json
{
  "id": 4,
  "name": "Corte + Barba",
  "price": 90.00,
  "durationMinutes": 45,
  "isActive": true
}
```

**Códigos de Estado:**
- `201 Created`: Servicio creado
- `400 Bad Request`: Datos inválidos
- `401 Unauthorized`: Token inválido
- `500 Internal Server Error`: Error del servidor

---

### 15. GET /api/barber/finances/summary

Obtiene el resumen financiero del barbero autenticado.

**Headers:**
```
Authorization: Bearer {token}
```

**Query Parameters:**
- `startDate` (opcional): Fecha de inicio (formato `YYYY-MM-DDTHH:mm:ss`)
- `endDate` (opcional): Fecha de fin (formato `YYYY-MM-DDTHH:mm:ss`)

**Ejemplo de Request:**
```
GET /api/barber/finances/summary?startDate=2024-01-01T00:00:00&endDate=2024-01-31T23:59:59
```

**Response 200 OK:**
```json
{
  "totalIncome": 5000.00,
  "totalExpenses": 1200.00,
  "netProfit": 3800.00,
  "incomeThisMonth": 2500.00,
  "expensesThisMonth": 600.00,
  "profitThisMonth": 1900.00
}
```

**Códigos de Estado:**
- `200 OK`: Resumen obtenido
- `401 Unauthorized`: Token inválido
- `500 Internal Server Error`: Error del servidor

---

### 16. GET /api/barber/finances/income

Obtiene la lista de ingresos del barbero autenticado con paginación.

**Headers:**
```
Authorization: Bearer {token}
```

**Query Parameters:**
- `startDate` (opcional): Fecha de inicio
- `endDate` (opcional): Fecha de fin
- `page` (opcional): Número de página (por defecto: 1)
- `pageSize` (opcional): Tamaño de página (por defecto: 50)

**Ejemplo de Request:**
```
GET /api/barber/finances/income?startDate=2024-01-01T00:00:00&endDate=2024-01-31T23:59:59&page=1&pageSize=20
```

**Response 200 OK:**
```json
{
  "total": 2500.00,
  "items": [
    {
      "id": 1,
      "type": "Income",
      "amount": 50.00,
      "description": "Cita - Corte de Cabello - María García",
      "category": "Service",
      "date": "2024-01-15T10:00:00Z",
      "appointmentId": 123
    },
    {
      "id": 2,
      "type": "Income",
      "amount": 75.00,
      "description": "Cita - Afeitado Clásico - Carlos López",
      "category": "Service",
      "date": "2024-01-16T14:00:00Z",
      "appointmentId": 124
    }
  ]
}
```

**Códigos de Estado:**
- `200 OK`: Ingresos obtenidos
- `401 Unauthorized`: Token inválido
- `500 Internal Server Error`: Error del servidor

---

### 17. GET /api/barber/finances/expenses

Obtiene la lista de egresos del barbero autenticado con paginación.

**Headers:**
```
Authorization: Bearer {token}
```

**Query Parameters:**
- `startDate` (opcional): Fecha de inicio
- `endDate` (opcional): Fecha de fin
- `page` (opcional): Número de página (por defecto: 1)
- `pageSize` (opcional): Tamaño de página (por defecto: 50)

**Response 200 OK:**
```json
{
  "total": 1200.00,
  "items": [
    {
      "id": 10,
      "type": "Expense",
      "amount": 500.00,
      "description": "Alquiler del local",
      "category": "Rent",
      "date": "2024-01-01T00:00:00Z",
      "appointmentId": null
    },
    {
      "id": 11,
      "type": "Expense",
      "amount": 200.00,
      "description": "Compra de productos",
      "category": "Supplies",
      "date": "2024-01-10T00:00:00Z",
      "appointmentId": null
    }
  ]
}
```

**Códigos de Estado:**
- `200 OK`: Egresos obtenidos
- `401 Unauthorized`: Token inválido
- `500 Internal Server Error`: Error del servidor

---

### 18. POST /api/barber/finances/expenses

Registra un nuevo egreso para el barbero autenticado.

**Headers:**
```
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "amount": 300.00,
  "description": "Pago de servicios públicos",
  "category": "Utilities",
  "date": "2024-01-15T00:00:00Z"
}
```

**Validaciones:**
- `amount`: Requerido, debe ser mayor a 0.01
- `description`: Requerido, máximo 500 caracteres
- `category`: Opcional, máximo 100 caracteres
- `date`: Requerido, formato `YYYY-MM-DDTHH:mm:ss`

**Response 201 Created:**
```json
{
  "id": 12,
  "type": "Expense",
  "amount": 300.00,
  "description": "Pago de servicios públicos",
  "category": "Utilities",
  "date": "2024-01-15T00:00:00Z",
  "appointmentId": null
}
```

**Códigos de Estado:**
- `201 Created`: Egreso registrado
- `400 Bad Request`: Datos inválidos
- `401 Unauthorized`: Token inválido
- `500 Internal Server Error`: Error del servidor

---

## Rutas de Administrador (JWT Requerido)

**Nota:** Todas las rutas de administrador requieren el header `Authorization: Bearer {token}` y el usuario debe tener rol `Admin`.

### 19. GET /api/admin/dashboard

Obtiene el dashboard del administrador con estadísticas generales del sistema.

**Headers:**
```
Authorization: Bearer {token}
```

**Response 200 OK:**
```json
{
  "totalBarbers": 25,
  "activeBarbers": 20,
  "inactiveBarbers": 5,
  "totalAppointments": 150,
  "pendingAppointments": 10,
  "confirmedAppointments": 120,
  "cancelledAppointments": 20,
  "totalRevenue": 15000.00,
  "recentBarbers": [
    {
      "id": 1,
      "name": "Juan Pérez",
      "businessName": "Barbería Central",
      "slug": "juan-perez",
      "isActive": true,
      "createdAt": "2024-01-01T00:00:00Z",
      "totalAppointments": 50,
      "totalRevenue": 2500.00
    },
    {
      "id": 2,
      "name": "Carlos López",
      "businessName": "Barbería Elite",
      "slug": "carlos-lopez",
      "isActive": true,
      "createdAt": "2024-01-05T00:00:00Z",
      "totalAppointments": 30,
      "totalRevenue": 1500.00
    }
  ]
}
```

**Códigos de Estado:**
- `200 OK`: Dashboard obtenido
- `401 Unauthorized`: Token inválido o expirado
- `403 Forbidden`: Usuario no es administrador
- `500 Internal Server Error`: Error del servidor

---

### 20. GET /api/admin/barbers

Obtiene la lista de todos los barberos del sistema.

**Headers:**
```
Authorization: Bearer {token}
```

**Query Parameters:**
- `isActive` (opcional): Filtrar por estado activo/inactivo (`true` o `false`)

**Ejemplo de Request:**
```
GET /api/admin/barbers?isActive=true
```

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
    "createdAt": "2024-01-01T00:00:00Z"
  },
  {
    "id": 2,
    "name": "Carlos López",
    "businessName": "Barbería Elite",
    "phone": "9876543210",
    "slug": "carlos-lopez",
    "isActive": true,
    "qrUrl": "https://app.mibarberia.com/b/carlos-lopez",
    "createdAt": "2024-01-05T00:00:00Z"
  }
]
```

**Códigos de Estado:**
- `200 OK`: Barberos obtenidos
- `401 Unauthorized`: Token inválido
- `403 Forbidden`: Usuario no es administrador
- `500 Internal Server Error`: Error del servidor

---

### 21. POST /api/admin/barbers

Crea un nuevo barbero en el sistema.

**Headers:**
```
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "email": "nuevo@barbero.com",
  "password": "password123",
  "name": "Pedro Martínez",
  "businessName": "Barbería Nueva",
  "phone": "5551234"
}
```

**Validaciones:**
- `email`: Requerido, debe ser un email válido y único
- `password`: Requerido, mínimo 6 caracteres
- `name`: Requerido, máximo 200 caracteres
- `businessName`: Opcional, máximo 200 caracteres
- `phone`: Requerido, máximo 20 caracteres

**Nota:** Al crear un barbero, se crea automáticamente:
- Usuario con rol `Barber`
- Perfil del barbero con slug único
- Horarios de trabajo por defecto (Lunes a Viernes, 9:00 - 17:00)

**Response 201 Created:**
```json
{
  "id": 3,
  "name": "Pedro Martínez",
  "businessName": "Barbería Nueva",
  "phone": "5551234",
  "slug": "pedro-martinez",
  "isActive": true,
  "qrUrl": "https://app.mibarberia.com/b/pedro-martinez",
  "createdAt": "2024-01-12T10:00:00Z"
}
```

**Códigos de Estado:**
- `201 Created`: Barbero creado
- `400 Bad Request`: Datos inválidos o email ya existe
- `401 Unauthorized`: Token inválido
- `403 Forbidden`: Usuario no es administrador
- `500 Internal Server Error`: Error del servidor

---

### 22. PUT /api/admin/barbers/{id}/status

Actualiza el estado (activo/inactivo) de un barbero.

**Headers:**
```
Authorization: Bearer {token}
```

**Parámetros:**
- `id` (path, requerido): ID del barbero

**Request Body:**
```json
{
  "isActive": false
}
```

**Response 204 No Content:** (Sin cuerpo)

**Códigos de Estado:**
- `204 No Content`: Estado actualizado
- `400 Bad Request`: Datos inválidos
- `401 Unauthorized`: Token inválido
- `403 Forbidden`: Usuario no es administrador
- `404 Not Found`: Barbero no encontrado
- `500 Internal Server Error`: Error del servidor

---

### 23. DELETE /api/admin/barbers/{id}

Elimina un barbero del sistema.

**Headers:**
```
Authorization: Bearer {token}
```

**Parámetros:**
- `id` (path, requerido): ID del barbero

**Nota:** Esta acción elimina el barbero y su perfil, pero NO elimina el usuario asociado.

**Response 204 No Content:** (Sin cuerpo)

**Códigos de Estado:**
- `204 No Content`: Barbero eliminado
- `401 Unauthorized`: Token inválido
- `403 Forbidden`: Usuario no es administrador
- `404 Not Found`: Barbero no encontrado
- `500 Internal Server Error`: Error del servidor

---

## Rutas Web MVC

Estas rutas son para la interfaz web (no API REST). Usan autenticación por cookies.

### 24. GET /login

Muestra la página de login del sistema web.

**Response:** HTML (página de login)

---

### 25. POST /login

Autentica un usuario en el sistema web (MVC).

**Request Body (form-data):**
```
email: admin@barberpro.com
password: admin123
```

**Response:** Redirección a `/admin/dashboard` o `/home` según el rol

---

### 26. GET /access-denied

Muestra la página de acceso denegado.

**Response:** HTML (página de error)

---

### 27. POST /logout

Cierra la sesión del usuario en el sistema web.

**Response:** Redirección a `/login`

---

### 28. GET /auth/keep-alive

Mantiene la sesión activa (heartbeat).

**Response 200 OK:**
```json
{
  "status": "ok"
}
```

---

### 29. GET /admin/dashboard

Muestra el dashboard del administrador (interfaz web).

**Autenticación:** Cookie de sesión (rol Administrador)

**Response:** HTML (dashboard del admin)

---

### 30. POST /admin/createbarber

Crea un barbero desde la interfaz web del administrador.

**Autenticación:** Cookie de sesión (rol Administrador)

**Request Body (form-data o JSON):**
```json
{
  "email": "nuevo@barbero.com",
  "password": "password123",
  "name": "Pedro Martínez",
  "businessName": "Barbería Nueva",
  "phone": "5551234"
}
```

**Response:** JSON
```json
{
  "success": true,
  "message": "Barbero creado exitosamente"
}
```

---

## Modelos de Datos

### AppointmentStatus (Enum)

Estados posibles de una cita:

- `Pending`: Pendiente (por defecto)
- `Confirmed`: Confirmada
- `Completed`: Completada
- `Cancelled`: Cancelada

### DayOfWeek (Enum)

Días de la semana (C#):

- `0`: Domingo
- `1`: Lunes
- `2`: Martes
- `3`: Miércoles
- `4`: Jueves
- `5`: Viernes
- `6`: Sábado

### UserRole (Enum)

Roles de usuario:

- `Admin`: Administrador
- `Barber`: Barbero

---

## Códigos de Estado HTTP

| Código | Significado | Descripción |
|--------|-------------|-------------|
| 200 | OK | Solicitud exitosa |
| 201 | Created | Recurso creado exitosamente |
| 204 | No Content | Operación exitosa sin contenido |
| 400 | Bad Request | Datos inválidos o faltantes |
| 401 | Unauthorized | Token inválido o expirado |
| 403 | Forbidden | Usuario sin permisos |
| 404 | Not Found | Recurso no encontrado |
| 500 | Internal Server Error | Error del servidor |

---

## Ejemplos de Uso

### Flujo Completo: Cliente Agenda Cita

```bash
# 1. Cliente escanea QR y obtiene slug: "juan-perez"

# 2. Obtener información del barbero
curl -X GET "https://app.mibarberia.com/api/public/barbers/juan-perez"

# 3. Obtener disponibilidad para una fecha
curl -X GET "https://app.mibarberia.com/api/public/barbers/juan-perez/availability?date=2024-01-15"

# 4. Crear cita
curl -X POST "https://app.mibarberia.com/api/public/appointments" \
  -H "Content-Type: application/json" \
  -d '{
    "barberSlug": "juan-perez",
    "serviceId": 1,
    "clientName": "María García",
    "clientPhone": "87654321",
    "date": "2024-01-15",
    "time": "10:00"
  }'
```

### Flujo Completo: Barbero Gestiona Citas

```bash
# 1. Login del barbero
curl -X POST "https://app.mibarberia.com/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "barbero@example.com",
    "password": "password123"
  }'

# Respuesta incluye token JWT
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# 2. Obtener dashboard
curl -X GET "https://app.mibarberia.com/api/barber/dashboard" \
  -H "Authorization: Bearer $TOKEN"

# 3. Obtener citas del día
curl -X GET "https://app.mibarberia.com/api/barber/appointments?date=2024-01-15" \
  -H "Authorization: Bearer $TOKEN"

# 4. Confirmar una cita
curl -X PUT "https://app.mibarberia.com/api/barber/appointments/123" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "Confirmed"
  }'

# 5. Obtener resumen financiero
curl -X GET "https://app.mibarberia.com/api/barber/finances/summary" \
  -H "Authorization: Bearer $TOKEN"
```

### Flujo Completo: Admin Crea Barbero

```bash
# 1. Login del admin
curl -X POST "https://app.mibarberia.com/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@barberpro.com",
    "password": "admin123"
  }'

TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# 2. Crear nuevo barbero
curl -X POST "https://app.mibarberia.com/api/admin/barbers" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "nuevo@barbero.com",
    "password": "password123",
    "name": "Pedro Martínez",
    "businessName": "Barbería Nueva",
    "phone": "5551234"
  }'

# 3. Obtener todos los barberos
curl -X GET "https://app.mibarberia.com/api/admin/barbers" \
  -H "Authorization: Bearer $TOKEN"
```

---

## Errores Comunes

### Error 401 Unauthorized

**Causa:** Token JWT inválido, expirado o no proporcionado.

**Solución:**
- Verificar que el header `Authorization: Bearer {token}` esté presente
- Hacer login nuevamente para obtener un token fresco

### Error 400 Bad Request

**Causa:** Datos inválidos en el request body.

**Solución:**
- Verificar que todos los campos requeridos estén presentes
- Verificar que los tipos de datos sean correctos
- Revisar las validaciones de cada campo

### Error 404 Not Found

**Causa:** Recurso no encontrado (barbero, cita, servicio, etc.).

**Solución:**
- Verificar que el ID o slug sea correcto
- Verificar que el recurso exista en la base de datos

### Error 403 Forbidden

**Causa:** Usuario sin permisos para acceder al recurso.

**Solución:**
- Verificar que el usuario tenga el rol correcto (Admin o Barber)
- Verificar que el barbero esté accediendo solo a sus propios recursos

---

## Notas Importantes

1. **Slug Único:** Cada barbero tiene un slug único generado automáticamente a partir de su nombre. Este slug se usa para las URLs públicas.

2. **Generación de Ingresos:** Cuando una cita cambia a estado `Confirmed` o `Completed`, se genera automáticamente un ingreso en el sistema financiero.

3. **Horarios de Trabajo:** Por defecto, los barberos tienen horarios de Lunes a Viernes de 9:00 a 17:00. Estos pueden ser modificados posteriormente.

4. **QR Code:** El código QR se genera automáticamente y apunta a la URL pública del barbero: `https://app.mibarberia.com/b/{slug}`

5. **Validación de Disponibilidad:** El sistema valida automáticamente que:
   - La fecha no sea en el pasado
   - El horario esté dentro de los horarios laborales
   - No haya conflictos con otras citas
   - No haya bloqueos de tiempo

6. **Paginación:** Los endpoints de finanzas (`/income` y `/expenses`) soportan paginación con parámetros `page` y `pageSize`.

---

## Soporte

Para más información o soporte técnico, contacta al equipo de desarrollo.

**Versión del Documento:** 1.0  
**Última Actualización:** Enero 2024

