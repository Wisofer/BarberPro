# BarberPro - Documentación de API

## Resumen del Sistema

Backend completo de reservas para barberías con:
- **Autenticación JWT** para Admin y Barbero
- **Rutas públicas** para clientes (sin autenticación)
- **Gestión completa** de citas, servicios, finanzas y estadísticas

---

## Estructura del Proyecto

```
BarberPro/
├── Controllers/Api/          # Controladores API
│   ├── AuthController.cs    # Login JWT
│   ├── PublicController.cs  # Rutas públicas
│   ├── BarberController.cs  # Rutas del barbero
│   └── AdminController.cs   # Rutas del admin
├── Models/
│   ├── Entities/            # Entidades de BD
│   └── DTOs/                 # Data Transfer Objects
│       ├── Requests/         # DTOs de entrada
│       └── Responses/          # DTOs de salida
├── Services/
│   ├── Interfaces/           # Interfaces de servicios
│   └── Implementations/      # Implementaciones
├── Utils/                    # Helpers (JWT, Slug, QR)
└── Data/                     # DbContext y migraciones
```

---

## Endpoints de la API

### 🔓 RUTAS PÚBLICAS (Sin autenticación)

#### GET /api/public/barbers/{slug}
Obtener información pública del barbero

**Response:**
```json
{
  "id": 1,
  "name": "Juan Pérez",
  "businessName": "Barbería Juan",
  "phone": "12345678",
  "slug": "juan-perez",
  "services": [
    {
      "id": 1,
      "name": "Corte de cabello",
      "price": 50.00,
      "durationMinutes": 30,
      "isActive": true
    }
  ],
  "workingHours": [
    {
      "id": 1,
      "dayOfWeek": 1,
      "startTime": "09:00",
      "endTime": "18:00",
      "isActive": true
    }
  ]
}
```

#### GET /api/public/barbers/{slug}/availability?date=2024-01-15
Obtener disponibilidad del barbero para una fecha

**Response:**
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
      "isAvailable": false
    }
  ]
}
```

#### POST /api/public/appointments
Crear una cita (público)

**Request:**
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

**Response:**
```json
{
  "id": 123,
  "barberId": 1,
  "barberName": "Juan Pérez",
  "serviceId": 1,
  "serviceName": "Corte de cabello",
  "servicePrice": 50.00,
  "clientName": "María García",
  "clientPhone": "87654321",
  "date": "2024-01-15",
  "time": "10:00",
  "status": "Pending",
  "createdAt": "2024-01-10T10:00:00Z"
}
```

---

### 🔐 RUTAS DE BARBERO (JWT requerido)

#### POST /api/auth/login
Login de Admin o Barbero

**Request:**
```json
{
  "email": "barbero@example.com",
  "password": "password123"
}
```

**Response:**
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
      "businessName": "Barbería Juan",
      "phone": "12345678",
      "slug": "juan-perez",
      "isActive": true,
      "qrUrl": "https://app.mibarberia.com/b/juan-perez",
      "createdAt": "2024-01-01T00:00:00Z"
    }
  },
  "role": "Barber"
}
```

#### GET /api/barber/dashboard
Dashboard completo del barbero

**Headers:** `Authorization: Bearer {token}`

**Response:**
```json
{
  "barber": { ... },
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
  "recentAppointments": [ ... ],
  "upcomingAppointments": [ ... ]
}
```

#### GET /api/barber/profile
Obtener perfil del barbero

#### PUT /api/barber/profile
Actualizar perfil del barbero

**Request:**
```json
{
  "name": "Juan Pérez",
  "businessName": "Barbería Juan",
  "phone": "12345678"
}
```

#### GET /api/barber/qr-url
Obtener URL del QR

**Response:**
```json
{
  "url": "https://app.mibarberia.com/b/juan-perez",
  "qrCode": "data:image/png;base64,..."
}
```

#### GET /api/barber/appointments?date=2024-01-15&status=Pending
Obtener citas del barbero

#### POST /api/barber/appointments
Crear cita manual

#### PUT /api/barber/appointments/{id}
Actualizar cita (confirmar, cancelar, cambiar hora)

**Request:**
```json
{
  "status": "Confirmed"
}
```

#### DELETE /api/barber/appointments/{id}
Eliminar cita

#### GET /api/barber/services
Obtener servicios del barbero

#### POST /api/barber/services
Crear servicio

**Request:**
```json
{
  "name": "Corte de cabello",
  "price": 50.00,
  "durationMinutes": 30
}
```

#### GET /api/barber/finances/summary?startDate=2024-01-01&endDate=2024-01-31
Resumen financiero

**Response:**
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

#### GET /api/barber/finances/income?startDate=2024-01-01&endDate=2024-01-31&page=1&pageSize=50
Obtener ingresos

#### GET /api/barber/finances/expenses?startDate=2024-01-01&endDate=2024-01-31&page=1&pageSize=50
Obtener egresos

#### POST /api/barber/finances/expenses
Registrar egreso

**Request:**
```json
{
  "amount": 200.00,
  "description": "Alquiler local",
  "category": "Rent",
  "date": "2024-01-01T00:00:00Z"
}
```

---

### 👑 RUTAS DE ADMIN (JWT requerido)

#### GET /api/admin/dashboard
Dashboard del administrador

**Response:**
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
  "recentBarbers": [ ... ]
}
```

#### GET /api/admin/barbers?isActive=true
Obtener todos los barberos

#### PUT /api/admin/barbers/{id}/status
Activar/desactivar barbero

**Request:**
```json
{
  "isActive": false
}
```

#### DELETE /api/admin/barbers/{id}
Eliminar barbero

---

## Flujo de Uso

### 1. Cliente crea cita (público)
```
1. Cliente escanea QR → Obtiene slug
2. GET /api/public/barbers/{slug} → Ve servicios
3. GET /api/public/barbers/{slug}/availability?date=2024-01-15 → Ve horarios
4. POST /api/public/appointments → Crea cita
```

### 2. Barbero gestiona citas
```
1. POST /api/auth/login → Obtiene JWT token
2. GET /api/barber/dashboard → Ve resumen
3. GET /api/barber/appointments → Ve citas
4. PUT /api/barber/appointments/{id} → Confirma cita
   → Se crea ingreso automáticamente
```

### 3. Barbero ve finanzas
```
1. GET /api/barber/finances/summary → Resumen
2. GET /api/barber/finances/income → Ingresos
3. GET /api/barber/finances/expenses → Egresos
4. POST /api/barber/finances/expenses → Registrar gasto
```

---

## Características Implementadas

✅ Autenticación JWT completa  
✅ Rutas públicas sin autenticación  
✅ Gestión de barberos (CRUD)  
✅ Gestión de servicios  
✅ Sistema de citas con validaciones  
✅ Cálculo de disponibilidad  
✅ Sistema financiero (ingresos/egresos)  
✅ Dashboards para barbero y admin  
✅ Generación de QR y URLs públicas  
✅ Validaciones de negocio  
✅ Código limpio y organizado  

---

## Próximos Pasos

1. Aplicar migración: `dotnet ef database update`
2. Probar endpoints con Postman/Swagger
3. Implementar frontend móvil
4. Agregar más validaciones si es necesario

---

## Notas Técnicas

- **Base de datos:** PostgreSQL (Neon)
- **ORM:** Entity Framework Core
- **Autenticación:** JWT Bearer
- **CORS:** Habilitado para desarrollo
- **Validaciones:** Data Annotations en DTOs
- **Manejo de errores:** Try-catch en controladores

