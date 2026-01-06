# 📚 Documentación de Nuevas APIs - BarberPro

## 🆕 APIs Implementadas

### 1. Estadísticas Rápidas (Extendidas)
### 2. Exportar Datos
### 3. Ayuda y Soporte
### 4. Horarios de Trabajo (CRUD)

---

## 📊 1. ESTADÍSTICAS RÁPIDAS (EXTENDIDAS)

### GET /api/barber/dashboard

**Descripción:** Obtiene el dashboard del barbero con estadísticas extendidas que incluyen clientes únicos y promedio por cliente.

**Autenticación:** Requerida (JWT Bearer Token)

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
    "qrUrl": "https://barbepro.encuentrame.org/b/juan-perez",
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
    "profit": 250.00,
    "uniqueClients": 15,
    "averagePerClient": 20.00
  },
  "thisMonth": {
    "appointments": 80,
    "income": 1200.00,
    "expenses": 200.00,
    "profit": 1000.00,
    "uniqueClients": 45,
    "averagePerClient": 26.67
  },
  "recentAppointments": [...],
  "upcomingAppointments": [...]
}
```

**Campos Nuevos:**
- `uniqueClients` (int): Número de clientes únicos atendidos en el período
- `averagePerClient` (decimal): Promedio de ingresos por cliente en el período

**Ejemplo Flutter:**
```dart
final response = await dio.get(
  '/api/barber/dashboard',
  options: Options(headers: {'Authorization': 'Bearer $token'}),
);

final dashboard = response.data;
final uniqueClients = dashboard['thisMonth']['uniqueClients'];
final avgPerClient = dashboard['thisMonth']['averagePerClient'];
```

---

## 📥 2. EXPORTAR DATOS

### GET /api/barber/export/appointments

**Descripción:** Exporta un reporte de citas en formato CSV, Excel o PDF.

**Autenticación:** Requerida (JWT Bearer Token)

**Query Parameters:**
- `format` (string, opcional): Formato de exportación. Valores: `csv`, `excel`, `pdf`. Default: `csv`
- `startDate` (string, opcional): Fecha inicio en formato `YYYY-MM-DD`
- `endDate` (string, opcional): Fecha fin en formato `YYYY-MM-DD`

**Headers:**
```
Authorization: Bearer {token}
```

**Ejemplos:**
```
GET /api/barber/export/appointments?format=csv
GET /api/barber/export/appointments?format=excel&startDate=2025-01-01&endDate=2025-01-31
GET /api/barber/export/appointments?format=pdf&startDate=2025-01-01
```

**Response 200 OK:**
- Content-Type: `text/csv`, `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, o `application/pdf`
- Body: Archivo binario descargable
- Filename: `citas_YYYYMMDD.{format}`

**Formato CSV:**
```
Fecha,Hora,Cliente,Teléfono,Servicio,Precio,Estado
2025-01-15,10:00,Juan Pérez,1234567890,Corte de Cabello,15.00,Confirmed
```

**Ejemplo Flutter:**
```dart
final response = await dio.get(
  '/api/barber/export/appointments?format=csv&startDate=2025-01-01&endDate=2025-01-31',
  options: Options(
    headers: {'Authorization': 'Bearer $token'},
    responseType: ResponseType.bytes,
  ),
);

// Guardar archivo
final file = File('/path/to/citas.csv');
await file.writeAsBytes(response.data);
```

---

### GET /api/barber/export/finances

**Descripción:** Exporta un reporte financiero (ingresos y egresos) en formato CSV, Excel o PDF.

**Autenticación:** Requerida (JWT Bearer Token)

**Query Parameters:**
- `format` (string, opcional): Formato de exportación. Valores: `csv`, `excel`, `pdf`. Default: `csv`
- `startDate` (string, opcional): Fecha inicio en formato `YYYY-MM-DD`
- `endDate` (string, opcional): Fecha fin en formato `YYYY-MM-DD`

**Headers:**
```
Authorization: Bearer {token}
```

**Ejemplos:**
```
GET /api/barber/export/finances?format=excel
GET /api/barber/export/finances?format=pdf&startDate=2025-01-01&endDate=2025-01-31
```

**Response 200 OK:**
- Content-Type: `text/csv`, `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, o `application/pdf`
- Body: Archivo binario descargable
- Filename: `finanzas_YYYYMMDD.{format}`

**Formato CSV:**
```
Fecha,Tipo,Monto,Descripción
2025-01-15,Income,15.00,Cita #1
2025-01-15,Expense,50.00,Compra de productos
```

**Ejemplo Flutter:**
```dart
final response = await dio.get(
  '/api/barber/export/finances?format=excel',
  options: Options(
    headers: {'Authorization': 'Bearer $token'},
    responseType: ResponseType.bytes,
  ),
);

final file = File('/path/to/finanzas.xlsx');
await file.writeAsBytes(response.data);
```

---

### GET /api/barber/export/clients

**Descripción:** Exporta un reporte de clientes con estadísticas en formato CSV, Excel o PDF.

**Autenticación:** Requerida (JWT Bearer Token)

**Query Parameters:**
- `format` (string, opcional): Formato de exportación. Valores: `csv`, `excel`, `pdf`. Default: `csv`

**Headers:**
```
Authorization: Bearer {token}
```

**Ejemplos:**
```
GET /api/barber/export/clients?format=csv
GET /api/barber/export/clients?format=pdf
```

**Response 200 OK:**
- Content-Type: `text/csv`, `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, o `application/pdf`
- Body: Archivo binario descargable
- Filename: `clientes_YYYYMMDD.{format}`

**Formato CSV:**
```
Cliente,Teléfono,Total Citas,Última Cita,Total Gastado
Juan Pérez,1234567890,5,2025-01-15,75.00
María García,9876543210,3,2025-01-10,45.00
```

**Ejemplo Flutter:**
```dart
final response = await dio.get(
  '/api/barber/export/clients?format=pdf',
  options: Options(
    headers: {'Authorization': 'Bearer $token'},
    responseType: ResponseType.bytes,
  ),
);

final file = File('/path/to/clientes.pdf');
await file.writeAsBytes(response.data);
```

---

### GET /api/barber/export/backup

**Descripción:** Crea un backup completo de todos los datos del barbero en formato JSON.

**Autenticación:** Requerida (JWT Bearer Token)

**Headers:**
```
Authorization: Bearer {token}
```

**Response 200 OK:**
- Content-Type: `application/json`
- Body: JSON con todos los datos del barbero
- Filename: `backup_YYYYMMDD_HHMMSS.json`

**Response Body:**
```json
{
  "barber": {
    "id": 1,
    "name": "Juan Pérez",
    "businessName": "Barbería Central",
    "phone": "1234567890",
    "slug": "juan-perez",
    "isActive": true,
    "createdAt": "2025-01-01T00:00:00Z"
  },
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
      "dayOfWeek": 1,
      "startTime": "09:00:00",
      "endTime": "18:00:00",
      "isActive": true
    }
  ],
  "appointments": [
    {
      "id": 1,
      "clientName": "Juan Pérez",
      "clientPhone": "1234567890",
      "date": "2025-01-15",
      "time": "10:00:00",
      "status": "Confirmed",
      "serviceName": "Corte de Cabello",
      "servicePrice": 15.00,
      "createdAt": "2025-01-01T10:00:00Z"
    }
  ],
  "transactions": [
    {
      "id": 1,
      "type": "Income",
      "amount": 15.00,
      "description": "Cita #1",
      "date": "2025-01-15T10:00:00Z"
    }
  ]
}
```

**Ejemplo Flutter:**
```dart
final response = await dio.get(
  '/api/barber/export/backup',
  options: Options(
    headers: {'Authorization': 'Bearer $token'},
    responseType: ResponseType.bytes,
  ),
);

final file = File('/path/to/backup.json');
await file.writeAsBytes(response.data);
```

---

## ❓ 3. AYUDA Y SOPORTE

### GET /api/barber/help-support

**Descripción:** Obtiene información de contacto y preguntas frecuentes (FAQs).

**Autenticación:** Requerida (JWT Bearer Token)

**Headers:**
```
Authorization: Bearer {token}
```

**Response 200 OK:**
```json
{
  "contact": {
    "email": "info@cowib.es",
    "phones": [
      "+505 8139569",
      "+505 82310100"
    ],
    "website": "https://www.cowib.es"
  },
  "faqs": [
    {
      "id": 1,
      "question": "¿Cómo agendo una cita?",
      "answer": "Puedes agendar una cita escaneando el código QR del barbero o visitando su perfil público. Selecciona el servicio, fecha y hora disponible, completa tus datos y confirma la cita.",
      "order": 1
    },
    {
      "id": 2,
      "question": "¿Puedo cancelar o modificar una cita?",
      "answer": "Sí, puedes cancelar o modificar una cita desde la aplicación. Si necesitas ayuda, contacta directamente con el barbero.",
      "order": 2
    },
    {
      "id": 3,
      "question": "¿Cómo veo mis estadísticas?",
      "answer": "En la sección de Estadísticas Rápidas puedes ver tus citas del mes, ingresos, clientes atendidos y promedio por cliente. También puedes exportar reportes detallados.",
      "order": 3
    },
    {
      "id": 4,
      "question": "¿Cómo configuro mis horarios de trabajo?",
      "answer": "Ve a la sección 'Horarios de Trabajo' en la aplicación. Puedes activar/desactivar días y configurar las horas de inicio y fin para cada día de la semana.",
      "order": 4
    },
    {
      "id": 5,
      "question": "¿Necesito conexión a internet para usar la aplicación?",
      "answer": "Sí, necesitas conexión a internet para sincronizar tus datos, agendar citas y acceder a todas las funcionalidades de la aplicación.",
      "order": 5
    }
  ]
}
```

**Ejemplo Flutter:**
```dart
final response = await dio.get(
  '/api/barber/help-support',
  options: Options(headers: {'Authorization': 'Bearer $token'}),
);

final helpSupport = response.data;
final email = helpSupport['contact']['email'];
final phones = helpSupport['contact']['phones'];
final faqs = helpSupport['faqs'];
```

---

## ⏰ 4. HORARIOS DE TRABAJO (CRUD)

### GET /api/barber/working-hours

**Descripción:** Obtiene todos los horarios de trabajo del barbero.

**Autenticación:** Requerida (JWT Bearer Token)

**Headers:**
```
Authorization: Bearer {token}
```

**Response 200 OK:**
```json
[
  {
    "id": 1,
    "dayOfWeek": 1,
    "startTime": "09:00:00",
    "endTime": "18:00:00",
    "isActive": true
  },
  {
    "id": 2,
    "dayOfWeek": 2,
    "startTime": "09:00:00",
    "endTime": "18:00:00",
    "isActive": true
  }
]
```

**Nota:** `dayOfWeek` usa enum de C#: 0=Domingo, 1=Lunes, 2=Martes, ..., 6=Sábado

**Ejemplo Flutter:**
```dart
final response = await dio.get(
  '/api/barber/working-hours',
  options: Options(headers: {'Authorization': 'Bearer $token'}),
);

final workingHours = response.data;
```

---

### PUT /api/barber/working-hours

**Descripción:** Actualiza o crea horarios de trabajo (upsert). Si el horario para ese día ya existe, lo actualiza; si no, lo crea.

**Autenticación:** Requerida (JWT Bearer Token)

**Headers:**
```
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "workingHours": [
    {
      "dayOfWeek": 1,
      "startTime": "09:00:00",
      "endTime": "18:00:00",
      "isActive": true
    },
    {
      "dayOfWeek": 2,
      "startTime": "09:00:00",
      "endTime": "18:00:00",
      "isActive": true
    },
    {
      "dayOfWeek": 6,
      "startTime": "10:00:00",
      "endTime": "17:00:00",
      "isActive": true
    },
    {
      "dayOfWeek": 0,
      "startTime": "09:00:00",
      "endTime": "18:00:00",
      "isActive": false
    }
  ]
}
```

**Response 200 OK:**
```json
[
  {
    "id": 1,
    "dayOfWeek": 1,
    "startTime": "09:00:00",
    "endTime": "18:00:00",
    "isActive": true
  },
  ...
]
```

**Validaciones:**
- `startTime` debe ser menor que `endTime`
- No puede haber múltiples horarios para el mismo día en el mismo request
- `dayOfWeek` debe estar entre 0 (Domingo) y 6 (Sábado)

**Ejemplo Flutter:**
```dart
final response = await dio.put(
  '/api/barber/working-hours',
  data: {
    'workingHours': [
      {
        'dayOfWeek': 1,
        'startTime': '09:00:00',
        'endTime': '18:00:00',
        'isActive': true,
      },
      {
        'dayOfWeek': 2,
        'startTime': '09:00:00',
        'endTime': '18:00:00',
        'isActive': true,
      },
    ],
  },
  options: Options(headers: {'Authorization': 'Bearer $token'}),
);

final updatedHours = response.data;
```

---

### DELETE /api/barber/working-hours/{id}

**Descripción:** Elimina un horario de trabajo específico.

**Autenticación:** Requerida (JWT Bearer Token)

**Headers:**
```
Authorization: Bearer {token}
```

**Ejemplo:**
```
DELETE /api/barber/working-hours/1
```

**Response 204 No Content** (si se elimina exitosamente)

**Response 404 Not Found:**
```json
{
  "message": "Horario de trabajo no encontrado"
}
```

**Ejemplo Flutter:**
```dart
await dio.delete(
  '/api/barber/working-hours/1',
  options: Options(headers: {'Authorization': 'Bearer $token'}),
);
```

---

## 📝 Notas Importantes

### Formatos de Exportación

1. **CSV**: Texto plano, fácil de abrir en Excel
2. **Excel**: Archivo `.xlsx` con formato profesional
3. **PDF**: Documento PDF con tablas formateadas (solo para citas, finanzas y clientes)
4. **JSON**: Backup completo de todos los datos

### Manejo de Errores

Todos los endpoints retornan errores estándar:

**400 Bad Request:**
```json
{
  "message": "Mensaje de error descriptivo"
}
```

**401 Unauthorized:**
```json
{
  "message": "Token inválido o expirado"
}
```

**404 Not Found:**
```json
{
  "message": "Recurso no encontrado"
}
```

**500 Internal Server Error:**
```json
{
  "message": "Error interno del servidor"
}
```

### Autenticación

Todos los endpoints requieren autenticación JWT. Obtén el token con:

```
POST /api/auth/login
{
  "email": "barbero@example.com",
  "password": "password123"
}
```

---

## 🎯 Resumen de Endpoints

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/barber/dashboard` | Dashboard con estadísticas extendidas |
| GET | `/api/barber/export/appointments` | Exportar citas (CSV/Excel/PDF) |
| GET | `/api/barber/export/finances` | Exportar finanzas (CSV/Excel/PDF) |
| GET | `/api/barber/export/clients` | Exportar clientes (CSV/Excel/PDF) |
| GET | `/api/barber/export/backup` | Backup completo (JSON) |
| GET | `/api/barber/help-support` | Ayuda y soporte |
| GET | `/api/barber/working-hours` | Obtener horarios |
| PUT | `/api/barber/working-hours` | Actualizar/crear horarios |
| DELETE | `/api/barber/working-hours/{id}` | Eliminar horario |

---

**Última actualización:** Enero 2025
**Versión API:** v1

