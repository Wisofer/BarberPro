# 📊 API de Finanzas - Documentación Completa

## 🔐 Autenticación

Todas las rutas requieren autenticación JWT:
```
Authorization: Bearer {token}
```

---

## 💰 INGRESOS (Income)

### 1. Obtener Ingresos

**GET** `/api/barber/finances/income`

Obtiene la lista de ingresos del barbero con paginación.

**Query Parameters:**
- `startDate` (opcional): Fecha de inicio (formato: `2026-01-01`)
- `endDate` (opcional): Fecha de fin (formato: `2026-01-31`)
- `page` (opcional): Número de página (default: 1)
- `pageSize` (opcional): Tamaño de página (default: 50)

**Ejemplo:**
```http
GET /api/barber/finances/income?startDate=2026-01-01&endDate=2026-01-31&page=1&pageSize=50
Authorization: Bearer {token}
```

**Response 200:**
```json
{
  "total": 1500.00,
  "items": [
    {
      "id": 1,
      "type": "Income",
      "amount": 150.00,
      "description": "Cita - Corte de cabello - Juan Pérez",
      "category": "Service",
      "date": "2026-01-10T00:00:00",
      "appointmentId": 5
    },
    {
      "id": 2,
      "type": "Income",
      "amount": 100.00,
      "description": "Pago directo - María González",
      "category": "Service",
      "date": "2026-01-10T00:00:00",
      "appointmentId": null
    }
  ]
}
```

---

### 2. Crear Ingreso Manual

**POST** `/api/barber/finances/income`

Crea un ingreso manual (para clientes walk-in o pagos directos).

**Body:**
```json
{
  "amount": 150.00,
  "description": "Pago directo - Juan Pérez",
  "category": "Service",
  "date": "2026-01-10"
}
```

**Campos:**
- `amount` (requerido): Monto del ingreso (0.01 - 999999.99)
- `description` (requerido): Descripción del ingreso (máx. 500 caracteres)
- `category` (opcional): Categoría del ingreso (máx. 100 caracteres)
- `date` (requerido): Fecha del ingreso (formato: `YYYY-MM-DD`)

**Ejemplo:**
```http
POST /api/barber/finances/income
Authorization: Bearer {token}
Content-Type: application/json

{
  "amount": 150.00,
  "description": "Pago directo - Juan Pérez",
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
  "description": "Pago directo - Juan Pérez",
  "category": "Service",
  "date": "2026-01-10T00:00:00",
  "appointmentId": null
}
```

---

## 💸 EGRESOS (Expenses)

### 1. Obtener Egresos

**GET** `/api/barber/finances/expenses`

Obtiene la lista de egresos del barbero con paginación.

**Query Parameters:**
- `startDate` (opcional): Fecha de inicio (formato: `2026-01-01`)
- `endDate` (opcional): Fecha de fin (formato: `2026-01-31`)
- `page` (opcional): Número de página (default: 1)
- `pageSize` (opcional): Tamaño de página (default: 50)

**Ejemplo:**
```http
GET /api/barber/finances/expenses?startDate=2026-01-01&endDate=2026-01-31&page=1&pageSize=50
Authorization: Bearer {token}
```

**Response 200:**
```json
{
  "total": 5000.00,
  "items": [
    {
      "id": 1,
      "type": "Expense",
      "amount": 5000.00,
      "description": "Alquiler enero",
      "category": "Alquiler",
      "date": "2026-01-01T00:00:00",
      "appointmentId": null
    }
  ]
}
```

---

### 2. Crear Egreso

**POST** `/api/barber/finances/expenses`

Crea un nuevo egreso.

**Body:**
```json
{
  "amount": 5000.00,
  "description": "Alquiler enero",
  "category": "Alquiler",
  "date": "2026-01-01"
}
```

**Campos:**
- `amount` (requerido): Monto del egreso (0.01 - 999999.99)
- `description` (requerido): Descripción del egreso (máx. 500 caracteres)
- `category` (opcional): Categoría del egreso (máx. 100 caracteres)
- `date` (requerido): Fecha del egreso (formato: `YYYY-MM-DD`)

**Ejemplo:**
```http
POST /api/barber/finances/expenses
Authorization: Bearer {token}
Content-Type: application/json

{
  "amount": 5000.00,
  "description": "Alquiler enero",
  "category": "Alquiler",
  "date": "2026-01-01"
}
```

**Response 201:**
```json
{
  "id": 1,
  "type": "Expense",
  "amount": 5000.00,
  "description": "Alquiler enero",
  "category": "Alquiler",
  "date": "2026-01-01T00:00:00",
  "appointmentId": null
}
```

---

### 3. Actualizar Egreso

**PUT** `/api/barber/finances/expenses/{id}`

Actualiza un egreso existente.

**Body:**
```json
{
  "amount": 5500.00,
  "description": "Alquiler enero (actualizado)",
  "category": "Alquiler",
  "date": "2026-01-01"
}
```

**Ejemplo:**
```http
PUT /api/barber/finances/expenses/1
Authorization: Bearer {token}
Content-Type: application/json

{
  "amount": 5500.00,
  "description": "Alquiler enero (actualizado)",
  "category": "Alquiler",
  "date": "2026-01-01"
}
```

**Response 200:**
```json
{
  "id": 1,
  "type": "Expense",
  "amount": 5500.00,
  "description": "Alquiler enero (actualizado)",
  "category": "Alquiler",
  "date": "2026-01-01T00:00:00",
  "appointmentId": null
}
```

**Errores:**
- `404`: Egreso no encontrado o no pertenece al barbero

---

### 4. Eliminar Egreso

**DELETE** `/api/barber/finances/expenses/{id}`

Elimina un egreso.

**Ejemplo:**
```http
DELETE /api/barber/finances/expenses/1
Authorization: Bearer {token}
```

**Response 204:** No Content

**Errores:**
- `404`: Egreso no encontrado o no pertenece al barbero

---

## 📊 RESUMEN FINANCIERO

### Obtener Resumen Financiero

**GET** `/api/barber/finances/summary`

Obtiene un resumen financiero completo del barbero.

**Query Parameters:**
- `startDate` (opcional): Fecha de inicio (formato: `2026-01-01`)
- `endDate` (opcional): Fecha de fin (formato: `2026-01-31`)

**Ejemplo:**
```http
GET /api/barber/finances/summary?startDate=2026-01-01&endDate=2026-01-31
Authorization: Bearer {token}
```

**Response 200:**
```json
{
  "totalIncome": 15000.00,
  "totalExpenses": 8000.00,
  "netProfit": 7000.00,
  "incomeThisMonth": 5000.00,
  "expensesThisMonth": 3000.00,
  "profitThisMonth": 2000.00
}
```

---

## 🏷️ CATEGORÍAS

### Obtener Categorías Predefinidas

**GET** `/api/barber/finances/categories`

Obtiene la lista de categorías predefinidas para ingresos y egresos.

**Ejemplo:**
```http
GET /api/barber/finances/categories
Authorization: Bearer {token}
```

**Response 200:**
```json
[
  "Alquiler",
  "Servicios Públicos",
  "Materiales",
  "Salarios",
  "Marketing",
  "Service",
  "Otros"
]
```

---

# 📋 ESCENARIOS DE INGRESOS - Documentación Completa

## 🔵 ESCENARIO 1: Cliente con Servicios Seleccionados

### Flujo Completo:

1. **Cliente escanea QR** → Ve perfil del barbero
2. **Cliente selecciona servicios** (ej: Corte $100 + Ceja $50)
3. **Cliente agenda cita** → Se crea con `ServiceIds: [1, 2]`
4. **Barbero ve cita** en estado "Pending"
5. **Barbero toca "Confirmar"** → Estado cambia a "Confirmed"
6. **✅ Ingresos se crean automáticamente** (uno por cada servicio)

### API Involucrada:

**PUT** `/api/barber/appointments/{id}`

**Body:**
```json
{
  "status": "Confirmed"
}
```

**Resultado:**
- Se crean 2 ingresos automáticamente:
  - Ingreso 1: $100 - "Cita - Corte de cabello - Juan Pérez"
  - Ingreso 2: $50 - "Cita - Arreglo de cejas - Juan Pérez"
- Total: $150 en ingresos automáticos

**Nota:** Los ingresos se crean automáticamente cuando el barbero confirma o completa la cita que tiene servicios asociados.

---

## 🔴 ESCENARIO 2: Cliente sin Servicios (Asignar al Completar)

### Flujo Completo:

1. **Cliente escanea QR** → Ve perfil del barbero
2. **Cliente NO selecciona servicios** → Agenda cita sin servicios
3. **Barbero ve cita** en estado "Pending"
4. **Cliente llega** → Le dice al barbero qué quiere
5. **Barbero toca "Completar"**
6. **🆕 Modal aparece:** "¿Quieres agregar servicios?"
7. **Barbero selecciona servicios** (ej: Corte + Barba)
8. **Barbero confirma** → Cita se completa
9. **✅ Ingresos se crean automáticamente**

### API Involucrada:

**PUT** `/api/barber/appointments/{id}`

**Body:**
```json
{
  "status": "Completed",
  "serviceIds": [1, 2]
}
```

**Campos:**
- `status` (requerido): Nuevo estado de la cita (`Confirmed` o `Completed`)
- `serviceIds` (opcional): Array de IDs de servicios a asignar

**Ejemplo Completo:**
```http
PUT /api/barber/appointments/5
Authorization: Bearer {token}
Content-Type: application/json

{
  "status": "Completed",
  "serviceIds": [1, 2]
}
```

**Response 200:**
```json
{
  "id": 5,
  "barberId": 2,
  "barberName": "wisofer 17",
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
    },
    {
      "id": 2,
      "name": "Arreglo de barba",
      "price": 50.00,
      "durationMinutes": 20,
      "isActive": true
    }
  ],
  "clientName": "Juan Pérez",
  "clientPhone": "82310100",
  "date": "2026-01-10",
  "time": "14:30:00",
  "status": "Completed",
  "createdAt": "2026-01-10T10:00:00"
}
```

**Resultado:**
- Se asignan servicios a `AppointmentServices`
- Se crean 2 ingresos automáticamente:
  - Ingreso 1: $100 - "Cita - Corte de cabello - Juan Pérez"
  - Ingreso 2: $50 - "Cita - Arreglo de barba - Juan Pérez"
- Total: $150 en ingresos automáticos

### Implementación Frontend (Flutter):

**Modal Sugerido:**
```dart
// Cuando el barbero toca "Completar" y la cita NO tiene servicios
if (appointment.services.isEmpty) {
  showDialog(
    context: context,
    builder: (context) => AlertDialog(
      title: Text('¿Agregar servicios?'),
      content: ServiceSelectionWidget(
        services: availableServices,
        onSelected: (selectedServiceIds) {
          // Actualizar cita con servicios
          updateAppointment(
            appointmentId: appointment.id,
            status: 'Completed',
            serviceIds: selectedServiceIds,
          );
        },
      ),
      actions: [
        TextButton(
          onPressed: () {
            // Completar sin servicios (no se crea ingreso automático)
            updateAppointment(
              appointmentId: appointment.id,
              status: 'Completed',
            );
          },
          child: Text('Completar sin servicios'),
        ),
      ],
    ),
  );
}
```

---

## 🟡 ESCENARIO 3: Cliente Walk-In (Sin Cita)

### Flujo Completo:

1. **Cliente NO escanea QR** → Llega directamente al local
2. **Barbero hace el servicio**
3. **Cliente paga**
4. **Barbero crea ingreso manual**

### API Involucrada:

**POST** `/api/barber/finances/income`

**Body:**
```json
{
  "amount": 150.00,
  "description": "Pago directo - Juan Pérez",
  "category": "Service",
  "date": "2026-01-10"
}
```

**Ejemplo:**
```http
POST /api/barber/finances/income
Authorization: Bearer {token}
Content-Type: application/json

{
  "amount": 150.00,
  "description": "Pago directo - Juan Pérez",
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
  "description": "Pago directo - Juan Pérez",
  "category": "Service",
  "date": "2026-01-10T00:00:00",
  "appointmentId": null
}
```

**Nota:** `appointmentId` es `null` porque no hay cita asociada.

---

# 📱 IMPLEMENTACIÓN FRONTEND (Flutter)

## Modelos Dart

### TransactionDto
```dart
class TransactionDto {
  final int id;
  final String type; // "Income" o "Expense"
  final double amount;
  final String description;
  final String? category;
  final DateTime date;
  final int? appointmentId;

  TransactionDto({
    required this.id,
    required this.type,
    required this.amount,
    required this.description,
    this.category,
    required this.date,
    this.appointmentId,
  });

  factory TransactionDto.fromJson(Map<String, dynamic> json) {
    return TransactionDto(
      id: json['id'],
      type: json['type'],
      amount: (json['amount'] as num).toDouble(),
      description: json['description'],
      category: json['category'],
      date: DateTime.parse(json['date']),
      appointmentId: json['appointmentId'],
    );
  }
}
```

### TransactionsResponse
```dart
class TransactionsResponse {
  final double total;
  final List<TransactionDto> items;

  TransactionsResponse({
    required this.total,
    required this.items,
  });

  factory TransactionsResponse.fromJson(Map<String, dynamic> json) {
    return TransactionsResponse(
      total: (json['total'] as num).toDouble(),
      items: (json['items'] as List)
          .map((item) => TransactionDto.fromJson(item))
          .toList(),
    );
  }
}
```

### FinanceSummaryDto
```dart
class FinanceSummaryDto {
  final double totalIncome;
  final double totalExpenses;
  final double netProfit;
  final double incomeThisMonth;
  final double expensesThisMonth;
  final double profitThisMonth;

  FinanceSummaryDto({
    required this.totalIncome,
    required this.totalExpenses,
    required this.netProfit,
    required this.incomeThisMonth,
    required this.expensesThisMonth,
    required this.profitThisMonth,
  });

  factory FinanceSummaryDto.fromJson(Map<String, dynamic> json) {
    return FinanceSummaryDto(
      totalIncome: (json['totalIncome'] as num).toDouble(),
      totalExpenses: (json['totalExpenses'] as num).toDouble(),
      netProfit: (json['netProfit'] as num).toDouble(),
      incomeThisMonth: (json['incomeThisMonth'] as num).toDouble(),
      expensesThisMonth: (json['expensesThisMonth'] as num).toDouble(),
      profitThisMonth: (json['profitThisMonth'] as num).toDouble(),
    );
  }
}
```

### CreateIncomeRequest
```dart
class CreateIncomeRequest {
  final double amount;
  final String description;
  final String? category;
  final DateTime date;

  CreateIncomeRequest({
    required this.amount,
    required this.description,
    this.category,
    required this.date,
  });

  Map<String, dynamic> toJson() {
    return {
      'amount': amount,
      'description': description,
      'category': category,
      'date': date.toIso8601String().split('T')[0], // YYYY-MM-DD
    };
  }
}
```

### CreateExpenseRequest / UpdateExpenseRequest
```dart
class CreateExpenseRequest {
  final double amount;
  final String description;
  final String? category;
  final DateTime date;

  CreateExpenseRequest({
    required this.amount,
    required this.description,
    this.category,
    required this.date,
  });

  Map<String, dynamic> toJson() {
    return {
      'amount': amount,
      'description': description,
      'category': category,
      'date': date.toIso8601String().split('T')[0], // YYYY-MM-DD
    };
  }
}
```

### UpdateAppointmentRequest (con ServiceIds)
```dart
class UpdateAppointmentRequest {
  final AppointmentStatus? status;
  final DateTime? date;
  final TimeOfDay? time;
  final int? serviceId; // Legacy
  final List<int>? serviceIds; // Nuevo: múltiples servicios

  UpdateAppointmentRequest({
    this.status,
    this.date,
    this.time,
    this.serviceId,
    this.serviceIds,
  });

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> json = {};
    if (status != null) json['status'] = status.toString().split('.').last;
    if (date != null) json['date'] = date!.toIso8601String().split('T')[0];
    if (time != null) {
      json['time'] = '${time!.hour.toString().padLeft(2, '0')}:${time!.minute.toString().padLeft(2, '0')}:00';
    }
    if (serviceId != null) json['serviceId'] = serviceId;
    if (serviceIds != null) json['serviceIds'] = serviceIds;
    return json;
  }
}
```

---

## Servicio Flutter (Ejemplo)

```dart
class FinanceService {
  final Dio _dio;

  FinanceService(this._dio);

  // Obtener ingresos
  Future<TransactionsResponse> getIncome({
    DateTime? startDate,
    DateTime? endDate,
    int page = 1,
    int pageSize = 50,
  }) async {
    final queryParams = <String, dynamic>{
      'page': page,
      'pageSize': pageSize,
    };
    if (startDate != null) {
      queryParams['startDate'] = startDate.toIso8601String().split('T')[0];
    }
    if (endDate != null) {
      queryParams['endDate'] = endDate.toIso8601String().split('T')[0];
    }

    final response = await _dio.get(
      '/api/barber/finances/income',
      queryParameters: queryParams,
    );
    return TransactionsResponse.fromJson(response.data);
  }

  // Crear ingreso manual
  Future<TransactionDto> createIncome(CreateIncomeRequest request) async {
    final response = await _dio.post(
      '/api/barber/finances/income',
      data: request.toJson(),
    );
    return TransactionDto.fromJson(response.data);
  }

  // Obtener egresos
  Future<TransactionsResponse> getExpenses({
    DateTime? startDate,
    DateTime? endDate,
    int page = 1,
    int pageSize = 50,
  }) async {
    final queryParams = <String, dynamic>{
      'page': page,
      'pageSize': pageSize,
    };
    if (startDate != null) {
      queryParams['startDate'] = startDate.toIso8601String().split('T')[0];
    }
    if (endDate != null) {
      queryParams['endDate'] = endDate.toIso8601String().split('T')[0];
    }

    final response = await _dio.get(
      '/api/barber/finances/expenses',
      queryParameters: queryParams,
    );
    return TransactionsResponse.fromJson(response.data);
  }

  // Crear egreso
  Future<TransactionDto> createExpense(CreateExpenseRequest request) async {
    final response = await _dio.post(
      '/api/barber/finances/expenses',
      data: request.toJson(),
    );
    return TransactionDto.fromJson(response.data);
  }

  // Actualizar egreso
  Future<TransactionDto> updateExpense(
    int id,
    CreateExpenseRequest request,
  ) async {
    final response = await _dio.put(
      '/api/barber/finances/expenses/$id',
      data: request.toJson(),
    );
    return TransactionDto.fromJson(response.data);
  }

  // Eliminar egreso
  Future<void> deleteExpense(int id) async {
    await _dio.delete('/api/barber/finances/expenses/$id');
  }

  // Obtener resumen financiero
  Future<FinanceSummaryDto> getFinanceSummary({
    DateTime? startDate,
    DateTime? endDate,
  }) async {
    final queryParams = <String, dynamic>{};
    if (startDate != null) {
      queryParams['startDate'] = startDate.toIso8601String().split('T')[0];
    }
    if (endDate != null) {
      queryParams['endDate'] = endDate.toIso8601String().split('T')[0];
    }

    final response = await _dio.get(
      '/api/barber/finances/summary',
      queryParameters: queryParams,
    );
    return FinanceSummaryDto.fromJson(response.data);
  }

  // Obtener categorías
  Future<List<String>> getCategories() async {
    final response = await _dio.get('/api/barber/finances/categories');
    return (response.data as List).map((e) => e.toString()).toList();
  }
}
```

---

## Manejo de Errores

Todos los endpoints pueden retornar los siguientes errores:

- **400 Bad Request**: Datos inválidos o validación fallida
- **401 Unauthorized**: Token inválido o expirado
- **404 Not Found**: Recurso no encontrado o no pertenece al barbero
- **500 Internal Server Error**: Error interno del servidor

**Ejemplo de Error:**
```json
{
  "message": "Error interno del servidor"
}
```

---

## Resumen de Endpoints

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/barber/finances/income` | Obtener ingresos |
| POST | `/api/barber/finances/income` | Crear ingreso manual |
| GET | `/api/barber/finances/expenses` | Obtener egresos |
| POST | `/api/barber/finances/expenses` | Crear egreso |
| PUT | `/api/barber/finances/expenses/{id}` | Actualizar egreso |
| DELETE | `/api/barber/finances/expenses/{id}` | Eliminar egreso |
| GET | `/api/barber/finances/summary` | Resumen financiero |
| GET | `/api/barber/finances/categories` | Categorías predefinidas |
| PUT | `/api/barber/appointments/{id}` | Actualizar cita (con serviceIds) |

---

## Notas Importantes

1. **Ingresos Automáticos**: Se crean automáticamente cuando:
   - El barbero confirma/completa una cita con servicios
   - Los servicios pueden venir de la creación inicial o asignarse al completar

2. **Ingresos Manuales**: Se crean cuando:
   - Un cliente walk-in paga directamente
   - El barbero necesita registrar un ingreso que no viene de una cita

3. **Egresos**: Siempre son manuales, el barbero los crea, edita o elimina según necesite.

4. **Categorías**: Son sugerencias, el barbero puede usar cualquier categoría personalizada.

5. **Fechas**: Todas las fechas se envían en formato `YYYY-MM-DD` (sin hora).

---

**Última actualización:** Enero 2026

