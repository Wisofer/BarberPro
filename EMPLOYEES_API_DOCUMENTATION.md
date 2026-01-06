# 👥 API de Trabajadores/Empleados - Documentación Completa

## 🔐 Autenticación

Todas las rutas requieren autenticación JWT:
```
Authorization: Bearer {token}
```

**Roles:**
- `Barber`: Dueño que puede crear/gestionar trabajadores
- `Employee`: Trabajador con permisos limitados

---

## 📋 CONCEPTOS IMPORTANTES

### Jerarquía del Sistema:
1. **Admin** → Crea barberos (dueños)
2. **Barber (Dueño)** → Crea trabajadores desde la app
3. **Employee (Trabajador)** → Solo ve sus citas/ingresos/egresos

### Características:
- ✅ Trabajadores **NO tienen QR propio** (trabajan en la tienda del dueño)
- ✅ Trabajadores **NO pueden crear servicios** (solo el dueño)
- ✅ Trabajadores **NO ven estadísticas** (solo citas, ingresos y egresos)
- ✅ Trabajadores **pueden registrar ingresos manuales** (clientes walk-in)
- ✅ Dueño ve **TODO** (sus datos + datos de todos sus trabajadores)

---

## 🔵 API DEL DUEÑO (Barber) - Gestión de Trabajadores

### 1. Obtener Todos los Trabajadores

**GET** `/api/barber/employees`

Obtiene la lista de todos los trabajadores del barbero dueño.

**Ejemplo:**
```http
GET /api/barber/employees
Authorization: Bearer {token}
```

**Response 200:**
```json
[
  {
    "id": 1,
    "ownerBarberId": 2,
    "ownerBarberName": "Juan Pérez",
    "name": "Carlos Rodríguez",
    "email": "carlos@example.com",
    "phone": "82310100",
    "isActive": true,
    "createdAt": "2026-01-10T10:00:00",
    "updatedAt": "2026-01-10T10:00:00"
  }
]
```

---

### 2. Obtener Trabajador por ID

**GET** `/api/barber/employees/{id}`

Obtiene los detalles de un trabajador específico.

**Ejemplo:**
```http
GET /api/barber/employees/1
Authorization: Bearer {token}
```

**Response 200:**
```json
{
  "id": 1,
  "ownerBarberId": 2,
  "ownerBarberName": "Juan Pérez",
  "name": "Carlos Rodríguez",
  "email": "carlos@example.com",
  "phone": "82310100",
  "isActive": true,
  "createdAt": "2026-01-10T10:00:00",
  "updatedAt": "2026-01-10T10:00:00"
}
```

**Errores:**
- `404`: Trabajador no encontrado o no pertenece al barbero

---

### 3. Crear Trabajador

**POST** `/api/barber/employees`

Crea un nuevo trabajador. El sistema crea automáticamente un usuario con rol "Employee".

**Body:**
```json
{
  "name": "Carlos Rodríguez",
  "email": "carlos@example.com",
  "password": "password123",
  "phone": "82310100"
}
```

**Campos:**
- `name` (requerido): Nombre del trabajador (máx. 200 caracteres)
- `email` (requerido): Email único del trabajador (máx. 200 caracteres)
- `password` (requerido): Contraseña (mín. 6 caracteres)
- `phone` (opcional): Teléfono del trabajador (máx. 20 caracteres)

**Ejemplo:**
```http
POST /api/barber/employees
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Carlos Rodríguez",
  "email": "carlos@example.com",
  "password": "password123",
  "phone": "82310100"
}
```

**Response 201:**
```json
{
  "id": 1,
  "ownerBarberId": 2,
  "ownerBarberName": "Juan Pérez",
  "name": "Carlos Rodríguez",
  "email": "carlos@example.com",
  "phone": "82310100",
  "isActive": true,
  "createdAt": "2026-01-10T10:00:00",
  "updatedAt": "2026-01-10T10:00:00"
}
```

**Errores:**
- `400`: Email ya está en uso
- `400`: Datos inválidos

---

### 4. Actualizar Trabajador

**PUT** `/api/barber/employees/{id}`

Actualiza los datos de un trabajador.

**Body:**
```json
{
  "name": "Carlos Rodríguez Actualizado",
  "phone": "82310101",
  "isActive": true
}
```

**Campos:**
- `name` (requerido): Nombre del trabajador
- `phone` (opcional): Teléfono del trabajador
- `isActive` (requerido): Estado activo/inactivo

**Ejemplo:**
```http
PUT /api/barber/employees/1
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Carlos Rodríguez Actualizado",
  "phone": "82310101",
  "isActive": true
}
```

**Response 200:**
```json
{
  "id": 1,
  "ownerBarberId": 2,
  "ownerBarberName": "Juan Pérez",
  "name": "Carlos Rodríguez Actualizado",
  "email": "carlos@example.com",
  "phone": "82310101",
  "isActive": true,
  "createdAt": "2026-01-10T10:00:00",
  "updatedAt": "2026-01-10T10:30:00"
}
```

**Errores:**
- `404`: Trabajador no encontrado o no pertenece al barbero

---

### 5. Eliminar (Desactivar) Trabajador

**DELETE** `/api/barber/employees/{id}`

Desactiva un trabajador (soft delete). No elimina el registro, solo lo marca como inactivo.

**Ejemplo:**
```http
DELETE /api/barber/employees/1
Authorization: Bearer {token}
```

**Response 204:** No Content

**Errores:**
- `404`: Trabajador no encontrado o no pertenece al barbero

---

## 🟡 API DEL TRABAJADOR (Employee) - Permisos Limitados

### 1. Obtener Citas del Trabajador

**GET** `/api/employee/appointments`

Obtiene solo las citas del trabajador autenticado.

**Query Parameters:**
- `date` (opcional): Fecha específica (formato: `2026-01-10`)

**Ejemplo:**
```http
GET /api/employee/appointments?date=2026-01-10
Authorization: Bearer {token}
```

**Response 200:**
```json
[
  {
    "id": 1,
    "barberId": 2,
    "barberName": "Juan Pérez",
    "employeeId": 1,
    "employeeName": "Carlos Rodríguez",
    "serviceId": 1,
    "serviceName": "Corte de cabello",
    "servicePrice": 100.00,
    "services": [
      {
        "id": 1,
        "name": "Corte de cabello",
        "price": 100.00,
        "durationMinutes": 30,
        "isActive": true
      }
    ],
    "clientName": "María González",
    "clientPhone": "82310100",
    "date": "2026-01-10",
    "time": "14:30:00",
    "status": "Pending",
    "createdAt": "2026-01-10T10:00:00"
  }
]
```

**Nota:** Solo retorna citas donde `employeeId` coincide con el trabajador autenticado.

---

### 2. Crear Cita Manual (Trabajador)

**POST** `/api/employee/appointments`

El trabajador puede crear citas manualmente. La cita se asocia automáticamente al barbero dueño y al trabajador.

**Body:**
```json
{
  "clientName": "María González",
  "clientPhone": "82310100",
  "date": "2026-01-10",
  "time": "14:30:00",
  "serviceIds": [1, 2]
}
```

**Ejemplo:**
```http
POST /api/employee/appointments
Authorization: Bearer {token}
Content-Type: application/json

{
  "clientName": "María González",
  "clientPhone": "82310100",
  "date": "2026-01-10",
  "time": "14:30:00",
  "serviceIds": [1, 2]
}
```

**Response 201:**
```json
{
  "id": 1,
  "barberId": 2,
  "barberName": "Juan Pérez",
  "employeeId": 1,
  "employeeName": "Carlos Rodríguez",
  "serviceId": 1,
  "serviceName": "Corte de cabello",
  "services": [...],
  "clientName": "María González",
  "clientPhone": "82310100",
  "date": "2026-01-10",
  "time": "14:30:00",
  "status": "Pending",
  "createdAt": "2026-01-10T10:00:00"
}
```

**Nota:** La cita se crea automáticamente con `employeeId` del trabajador autenticado.

---

### 3. Obtener Ingresos del Trabajador

**GET** `/api/employee/finances/income`

Obtiene solo los ingresos del trabajador autenticado.

**Query Parameters:**
- `startDate` (opcional): Fecha de inicio (formato: `2026-01-01`)
- `endDate` (opcional): Fecha de fin (formato: `2026-01-31`)

**Ejemplo:**
```http
GET /api/employee/finances/income?startDate=2026-01-01&endDate=2026-01-31
Authorization: Bearer {token}
```

**Response 200:**
```json
{
  "total": 500.00,
  "items": [
    {
      "id": 1,
      "type": "Income",
      "amount": 150.00,
      "description": "Pago directo - María González",
      "category": "Service",
      "date": "2026-01-10T00:00:00",
      "appointmentId": null,
      "employeeId": 1,
      "employeeName": "Carlos Rodríguez"
    }
  ]
}
```

**Nota:** Solo retorna ingresos donde `employeeId` coincide con el trabajador autenticado.

---

### 4. Crear Ingreso Manual (Trabajador)

**POST** `/api/employee/finances/income`

El trabajador puede registrar ingresos manuales (clientes walk-in).

**Body:**
```json
{
  "amount": 150.00,
  "description": "Pago directo - María González",
  "category": "Service",
  "date": "2026-01-10"
}
```

**Ejemplo:**
```http
POST /api/employee/finances/income
Authorization: Bearer {token}
Content-Type: application/json

{
  "amount": 150.00,
  "description": "Pago directo - María González",
  "category": "Service",
  "date": "2026-01-10"
}
```

**Response 201:**
```json
{
  "id": 1,
  "type": "Income",
  "amount": 150.00,
  "description": "Pago directo - María González",
  "category": "Service",
  "date": "2026-01-10T00:00:00",
  "appointmentId": null,
  "employeeId": 1,
  "employeeName": "Carlos Rodríguez"
}
```

**Nota:** El ingreso se asocia automáticamente al barbero dueño y al trabajador.

---

### 5. Obtener Egresos del Trabajador

**GET** `/api/employee/finances/expenses`

Obtiene solo los egresos del trabajador autenticado.

**Query Parameters:**
- `startDate` (opcional): Fecha de inicio
- `endDate` (opcional): Fecha de fin

**Ejemplo:**
```http
GET /api/employee/finances/expenses?startDate=2026-01-01&endDate=2026-01-31
Authorization: Bearer {token}
```

**Response 200:**
```json
{
  "total": 100.00,
  "items": [
    {
      "id": 1,
      "type": "Expense",
      "amount": 50.00,
      "description": "Materiales",
      "category": "Materiales",
      "date": "2026-01-10T00:00:00",
      "appointmentId": null,
      "employeeId": 1,
      "employeeName": "Carlos Rodríguez"
    }
  ]
}
```

---

### 6. Crear Egreso (Trabajador)

**POST** `/api/employee/finances/expenses`

El trabajador puede registrar egresos.

**Body:**
```json
{
  "amount": 50.00,
  "description": "Materiales",
  "category": "Materiales",
  "date": "2026-01-10"
}
```

**Ejemplo:**
```http
POST /api/employee/finances/expenses
Authorization: Bearer {token}
Content-Type: application/json

{
  "amount": 50.00,
  "description": "Materiales",
  "category": "Materiales",
  "date": "2026-01-10"
}
```

**Response 201:**
```json
{
  "id": 1,
  "type": "Expense",
  "amount": 50.00,
  "description": "Materiales",
  "category": "Materiales",
  "date": "2026-01-10T00:00:00",
  "appointmentId": null,
  "employeeId": 1,
  "employeeName": "Carlos Rodríguez"
}
```

---

## 🔐 LOGIN PARA TRABAJADORES

### Login

**POST** `/api/auth/login`

Los trabajadores usan el mismo endpoint de login que los barberos.

**Body:**
```json
{
  "email": "carlos@example.com",
  "password": "password123"
}
```

**Response 200:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "id": 3,
    "email": "carlos@example.com",
    "role": "Employee"
  },
  "role": "Employee"
}
```

**Nota:** El token JWT incluye:
- `EmployeeId`: ID del trabajador
- `OwnerBarberId`: ID del barbero dueño
- `Role`: "Employee"

---

## 📱 IMPLEMENTACIÓN FRONTEND (Flutter)

### Modelos Dart

### EmployeeDto
```dart
class EmployeeDto {
  final int id;
  final int ownerBarberId;
  final String ownerBarberName;
  final String name;
  final String email;
  final String? phone;
  final bool isActive;
  final DateTime createdAt;
  final DateTime updatedAt;

  EmployeeDto({
    required this.id,
    required this.ownerBarberId,
    required this.ownerBarberName,
    required this.name,
    required this.email,
    this.phone,
    required this.isActive,
    required this.createdAt,
    required this.updatedAt,
  });

  factory EmployeeDto.fromJson(Map<String, dynamic> json) {
    return EmployeeDto(
      id: json['id'],
      ownerBarberId: json['ownerBarberId'],
      ownerBarberName: json['ownerBarberName'],
      name: json['name'],
      email: json['email'],
      phone: json['phone'],
      isActive: json['isActive'],
      createdAt: DateTime.parse(json['createdAt']),
      updatedAt: DateTime.parse(json['updatedAt']),
    );
  }
}
```

### CreateEmployeeRequest
```dart
class CreateEmployeeRequest {
  final String name;
  final String email;
  final String password;
  final String? phone;

  CreateEmployeeRequest({
    required this.name,
    required this.email,
    required this.password,
    this.phone,
  });

  Map<String, dynamic> toJson() {
    return {
      'name': name,
      'email': email,
      'password': password,
      'phone': phone,
    };
  }
}
```

### UpdateEmployeeRequest
```dart
class UpdateEmployeeRequest {
  final String name;
  final String? phone;
  final bool isActive;

  UpdateEmployeeRequest({
    required this.name,
    this.phone,
    required this.isActive,
  });

  Map<String, dynamic> toJson() {
    return {
      'name': name,
      'phone': phone,
      'isActive': isActive,
    };
  }
}
```

---

## 🔄 FLUJOS DE TRABAJO

### Flujo 1: Dueño Crea Trabajador

1. Dueño (Barber) entra a la app
2. Va a sección "Trabajadores"
3. Toca "Agregar Trabajador"
4. Completa formulario:
   - Nombre
   - Email
   - Contraseña
   - Teléfono (opcional)
5. Envía `POST /api/barber/employees`
6. Sistema crea:
   - Usuario con rol "Employee"
   - Registro en tabla Employees
   - Vinculado al barbero dueño
7. Trabajador recibe credenciales y puede iniciar sesión

---

### Flujo 2: Trabajador Crea Cita Manual

1. Trabajador entra a la app
2. Cliente llega sin cita
3. Trabajador toca "Crear Cita"
4. Completa formulario:
   - Nombre del cliente
   - Teléfono
   - Fecha y hora
   - Servicios (opcional)
5. Envía `POST /api/employee/appointments`
6. Cita se crea con:
   - `barberId`: ID del dueño
   - `employeeId`: ID del trabajador
7. Cuando se completa, ingresos se crean automáticamente

---

### Flujo 3: Trabajador Registra Ingreso Walk-In

1. Trabajador entra a la app
2. Cliente paga directamente (sin cita)
3. Trabajador toca "Registrar Ingreso"
4. Completa formulario:
   - Monto
   - Descripción
   - Categoría (opcional)
   - Fecha
5. Envía `POST /api/employee/finances/income`
6. Ingreso se crea con:
   - `barberId`: ID del dueño
   - `employeeId`: ID del trabajador
7. Dueño ve el ingreso en su dashboard consolidado

---

### Flujo 4: Dueño Ve Datos Consolidados

1. Dueño entra a la app
2. Ve dashboard con:
   - Sus citas
   - Citas de todos sus trabajadores
   - Ingresos totales (suyos + trabajadores)
   - Ingresos por trabajador
3. Puede filtrar por trabajador
4. Puede ver estadísticas consolidadas

---

## ⚠️ PERMISOS Y RESTRICCIONES

### Dueño (Barber) Puede:
- ✅ Crear/editar/eliminar trabajadores
- ✅ Ver todas sus citas
- ✅ Ver citas de todos sus trabajadores
- ✅ Ver ingresos consolidados
- ✅ Ver ingresos por trabajador
- ✅ Crear servicios
- ✅ Ver estadísticas completas
- ✅ Gestionar horarios de trabajo
- ✅ Exportar datos

### Trabajador (Employee) Puede:
- ✅ Ver solo SUS citas
- ✅ Crear citas manuales
- ✅ Registrar ingresos manuales
- ✅ Registrar egresos
- ✅ Ver solo SUS ingresos
- ✅ Ver solo SUS egresos
- ❌ NO puede crear servicios
- ❌ NO puede ver estadísticas
- ❌ NO puede ver datos de otros trabajadores
- ❌ NO puede crear trabajadores
- ❌ NO tiene QR propio

---

## 📊 ESTRUCTURA DE DATOS

### Employee Entity
```csharp
public class Employee
{
    public int Id { get; set; }
    public int OwnerBarberId { get; set; } // Barbero dueño
    public int UserId { get; set; } // Usuario con rol Employee
    public string Name { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Relaciones:
- `Employee` → `User` (1:1)
- `Employee` → `Barber` (OwnerBarber) (Many:1)
- `Employee` → `Appointment` (1:Many)
- `Employee` → `Transaction` (1:Many)

---

## 🔑 TOKEN JWT PARA TRABAJADORES

Cuando un trabajador hace login, el token incluye:

```json
{
  "UserId": "3",
  "Role": "Employee",
  "EmployeeId": "1",
  "OwnerBarberId": "2"
}
```

El frontend debe usar estos claims para:
- Identificar al trabajador (`EmployeeId`)
- Identificar al dueño (`OwnerBarberId`)
- Filtrar datos por trabajador

---

## 📋 RESUMEN DE ENDPOINTS

### Dueño (Barber):
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/barber/employees` | Listar trabajadores |
| GET | `/api/barber/employees/{id}` | Obtener trabajador |
| POST | `/api/barber/employees` | Crear trabajador |
| PUT | `/api/barber/employees/{id}` | Actualizar trabajador |
| DELETE | `/api/barber/employees/{id}` | Desactivar trabajador |

### Trabajador (Employee):
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/employee/appointments` | Ver mis citas |
| POST | `/api/employee/appointments` | Crear cita manual |
| GET | `/api/employee/finances/income` | Ver mis ingresos |
| POST | `/api/employee/finances/income` | Registrar ingreso |
| GET | `/api/employee/finances/expenses` | Ver mis egresos |
| POST | `/api/employee/finances/expenses` | Registrar egreso |

---

## 🚀 PRÓXIMOS PASOS

1. **Aplicar migración:**
   ```bash
   dotnet ef database update
   ```

2. **Frontend debe:**
   - Detectar rol del usuario (Barber vs Employee)
   - Mostrar UI diferente según rol
   - Implementar CRUD de trabajadores (solo para Barber)
   - Filtrar datos por EmployeeId (para Employee)

3. **Dashboard del Dueño:**
   - Agregar sección "Trabajadores"
   - Mostrar estadísticas consolidadas
   - Permitir filtrar por trabajador

---

**Última actualización:** Enero 2026

