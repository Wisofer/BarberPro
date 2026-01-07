# 📊 Documentación de API: Reportes de Empleados

## 🎯 Introducción

Este documento describe los nuevos endpoints de reportes de empleados disponibles para el **dueño de la barbería (Barber)**. Estos endpoints permiten al dueño analizar el rendimiento y la actividad de sus empleados.

**⚠️ IMPORTANTE:** Estos endpoints son **SOLO para el rol "Barber" (dueño)**, no para empleados.

---

## 🔐 Autenticación

Todos los endpoints requieren:
- **Header:** `Authorization: Bearer {token}`
- **Rol:** `Barber` (dueño de la barbería)

---

## 📋 Endpoints Disponibles

### 1. 📅 Reporte de Citas por Empleado

**Endpoint:** `GET /api/barber/reports/employees/appointments`

**Descripción:** Obtiene un reporte detallado de las citas agrupadas por empleado, incluyendo estadísticas de estado, ingresos generados y promedios.

**Query Parameters:**
- `startDate` (opcional): Fecha de inicio del período (formato: `YYYY-MM-DD` o `YYYY-MM-DDTHH:mm:ss`)
- `endDate` (opcional): Fecha de fin del período (formato: `YYYY-MM-DD` o `YYYY-MM-DDTHH:mm:ss`)
- `employeeId` (opcional): Filtrar por un empleado específico

**Ejemplo de Request:**
```bash
GET /api/barber/reports/employees/appointments?startDate=2026-01-01&endDate=2026-01-31
```

**Ejemplo de Response (200 OK):**
```json
{
  "startDate": "2026-01-01T00:00:00",
  "endDate": "2026-01-31T23:59:59",
  "totalAppointments": 45,
  "byEmployee": [
    {
      "employeeId": 2,
      "employeeName": "Juan Pérez",
      "completed": 20,
      "pending": 3,
      "confirmed": 5,
      "cancelled": 2,
      "total": 30,
      "totalIncome": 1500.00,
      "averagePerAppointment": 75.00
    },
    {
      "employeeId": null,
      "employeeName": "Barbero (Dueño)",
      "completed": 10,
      "pending": 2,
      "confirmed": 3,
      "cancelled": 0,
      "total": 15,
      "totalIncome": 800.00,
      "averagePerAppointment": 80.00
    }
  ]
}
```

**Campos del Response:**
- `startDate`: Fecha de inicio del período consultado
- `endDate`: Fecha de fin del período consultado
- `totalAppointments`: Total de citas en el período
- `byEmployee`: Array de estadísticas por empleado
  - `employeeId`: ID del empleado (null si es el dueño)
  - `employeeName`: Nombre del empleado o "Barbero (Dueño)"
  - `completed`: Citas completadas
  - `pending`: Citas pendientes
  - `confirmed`: Citas confirmadas
  - `cancelled`: Citas canceladas
  - `total`: Total de citas del empleado
  - `totalIncome`: Ingresos totales generados por este empleado
  - `averagePerAppointment`: Promedio de ingresos por cita completada/confirmada

---

### 2. 💰 Reporte de Ingresos por Empleado

**Endpoint:** `GET /api/barber/reports/employees/income`

**Descripción:** Obtiene un reporte detallado de los ingresos generados por cada empleado, diferenciando entre ingresos de citas y manuales.

**Query Parameters:**
- `startDate` (opcional): Fecha de inicio del período
- `endDate` (opcional): Fecha de fin del período
- `employeeId` (opcional): Filtrar por un empleado específico

**Ejemplo de Request:**
```bash
GET /api/barber/reports/employees/income?startDate=2026-01-01&endDate=2026-01-31
```

**Ejemplo de Response (200 OK):**
```json
{
  "startDate": "2026-01-01T00:00:00",
  "endDate": "2026-01-31T23:59:59",
  "totalIncome": 3500.00,
  "byEmployee": [
    {
      "employeeId": 2,
      "employeeName": "Juan Pérez",
      "totalIncome": 2000.00,
      "count": 25,
      "fromAppointments": 1500.00,
      "manual": 500.00,
      "averagePerTransaction": 80.00
    },
    {
      "employeeId": null,
      "employeeName": "Barbero (Dueño)",
      "totalIncome": 1500.00,
      "count": 18,
      "fromAppointments": 1200.00,
      "manual": 300.00,
      "averagePerTransaction": 83.33
    }
  ]
}
```

**Campos del Response:**
- `totalIncome`: Ingresos totales del período
- `byEmployee`: Array de estadísticas por empleado
  - `employeeId`: ID del empleado (null si es el dueño)
  - `employeeName`: Nombre del empleado
  - `totalIncome`: Ingresos totales del empleado
  - `count`: Número de transacciones de ingreso
  - `fromAppointments`: Ingresos provenientes de citas completadas
  - `manual`: Ingresos registrados manualmente (walk-in)
  - `averagePerTransaction`: Promedio de ingresos por transacción

---

### 3. 💸 Reporte de Egresos por Empleado

**Endpoint:** `GET /api/barber/reports/employees/expenses`

**Descripción:** Obtiene un reporte detallado de los egresos registrados por cada empleado, agrupados por categoría.

**Query Parameters:**
- `startDate` (opcional): Fecha de inicio del período
- `endDate` (opcional): Fecha de fin del período
- `employeeId` (opcional): Filtrar por un empleado específico

**Ejemplo de Request:**
```bash
GET /api/barber/reports/employees/expenses?startDate=2026-01-01&endDate=2026-01-31
```

**Ejemplo de Response (200 OK):**
```json
{
  "startDate": "2026-01-01T00:00:00",
  "endDate": "2026-01-31T23:59:59",
  "totalExpenses": 1200.00,
  "byEmployee": [
    {
      "employeeId": 2,
      "employeeName": "Juan Pérez",
      "totalExpenses": 800.00,
      "count": 12,
      "categories": {
        "Materiales": 400.00,
        "Servicios": 200.00,
        "Otros": 200.00
      },
      "averagePerTransaction": 66.67
    },
    {
      "employeeId": null,
      "employeeName": "Barbero (Dueño)",
      "totalExpenses": 400.00,
      "count": 8,
      "categories": {
        "Materiales": 250.00,
        "Servicios": 150.00
      },
      "averagePerTransaction": 50.00
    }
  ]
}
```

**Campos del Response:**
- `totalExpenses`: Egresos totales del período
- `byEmployee`: Array de estadísticas por empleado
  - `employeeId`: ID del empleado (null si es el dueño)
  - `employeeName`: Nombre del empleado
  - `totalExpenses`: Egresos totales del empleado
  - `count`: Número de transacciones de egreso
  - `categories`: Diccionario con el total por categoría
  - `averagePerTransaction`: Promedio de egresos por transacción

---

### 4. 📊 Reporte General de Actividad de Empleados

**Endpoint:** `GET /api/barber/reports/employees/activity`

**Descripción:** Obtiene un reporte consolidado de la actividad de todos los empleados, incluyendo citas, ingresos, egresos y contribución neta.

**Query Parameters:**
- `startDate` (opcional): Fecha de inicio del período
- `endDate` (opcional): Fecha de fin del período

**Ejemplo de Request:**
```bash
GET /api/barber/reports/employees/activity?startDate=2026-01-01&endDate=2026-01-31
```

**Ejemplo de Response (200 OK):**
```json
{
  "startDate": "2026-01-01T00:00:00",
  "endDate": "2026-01-31T23:59:59",
  "employees": [
    {
      "employeeId": 2,
      "employeeName": "Juan Pérez",
      "email": "juan@example.com",
      "isActive": true,
      "appointmentsCompleted": 20,
      "appointmentsPending": 3,
      "totalIncome": 2000.00,
      "totalExpenses": 800.00,
      "netContribution": 1200.00,
      "averagePerAppointment": 100.00,
      "lastActivity": "2026-01-31T18:30:00"
    },
    {
      "employeeId": null,
      "employeeName": "Barbero (Dueño)",
      "email": "",
      "isActive": true,
      "appointmentsCompleted": 15,
      "appointmentsPending": 2,
      "totalIncome": 1500.00,
      "totalExpenses": 400.00,
      "netContribution": 1100.00,
      "averagePerAppointment": 100.00,
      "lastActivity": "2026-01-31T19:00:00"
    }
  ]
}
```

**Campos del Response:**
- `employees`: Array de estadísticas de actividad por empleado (ordenado por contribución neta descendente)
  - `employeeId`: ID del empleado (null si es el dueño)
  - `employeeName`: Nombre del empleado
  - `email`: Email del empleado (vacío para el dueño)
  - `isActive`: Si el empleado está activo
  - `appointmentsCompleted`: Citas completadas
  - `appointmentsPending`: Citas pendientes
  - `totalIncome`: Ingresos totales generados
  - `totalExpenses`: Egresos totales registrados
  - `netContribution`: Contribución neta (ingresos - egresos)
  - `averagePerAppointment`: Promedio de ingresos por cita completada
  - `lastActivity`: Fecha de última actividad (última cita actualizada)

---

### 5. 📈 Estadísticas de Empleados en Dashboard

**Endpoint:** `GET /api/barber/dashboard`

**Descripción:** El endpoint de dashboard ahora incluye estadísticas de empleados en el campo `employeeStats`.

**Ejemplo de Response (200 OK):**
```json
{
  "barber": { ... },
  "today": { ... },
  "thisWeek": { ... },
  "thisMonth": { ... },
  "recentAppointments": [ ... ],
  "upcomingAppointments": [ ... ],
  "employeeStats": {
    "totalEmployees": 3,
    "activeEmployees": 2,
    "topPerformers": [
      {
        "employeeId": 2,
        "employeeName": "Juan Pérez",
        "appointmentsCompleted": 20,
        "totalIncome": 2000.00,
        "averagePerAppointment": 100.00
      },
      {
        "employeeId": 3,
        "employeeName": "María García",
        "appointmentsCompleted": 15,
        "totalIncome": 1500.00,
        "averagePerAppointment": 100.00
      }
    ]
  }
}
```

**Campos de `employeeStats`:**
- `totalEmployees`: Total de empleados registrados
- `activeEmployees`: Total de empleados activos
- `topPerformers`: Top 3 empleados por ingresos generados (del mes actual)
  - `employeeId`: ID del empleado
  - `employeeName`: Nombre del empleado
  - `appointmentsCompleted`: Citas completadas
  - `totalIncome`: Ingresos totales generados
  - `averagePerAppointment`: Promedio de ingresos por cita

---

## 🔄 Códigos de Respuesta

- **200 OK**: Request exitoso
- **401 Unauthorized**: Token inválido o expirado
- **403 Forbidden**: Usuario no tiene rol "Barber"
- **500 Internal Server Error**: Error interno del servidor

---

## 💡 Notas Importantes para el Frontend

### 1. **Filtrado por Fechas**
- Si no se proporcionan fechas, el reporte incluirá todos los datos históricos
- Las fechas pueden enviarse en formato `YYYY-MM-DD` o `YYYY-MM-DDTHH:mm:ss`
- El frontend puede usar componentes de selección de fecha para facilitar el filtrado

### 2. **Filtrado por Empleado**
- El parámetro `employeeId` es opcional
- Si se proporciona, el reporte mostrará solo datos de ese empleado
- Si no se proporciona, mostrará datos de todos los empleados (incluyendo el dueño)

### 3. **Datos del Dueño**
- El dueño aparece en los reportes con `employeeId: null` y `employeeName: "Barbero (Dueño)"`
- Esto permite comparar el rendimiento del dueño con el de sus empleados

### 4. **Visualización Recomendada**
- **Reporte de Citas**: Gráfico de barras mostrando citas por estado, tabla con detalles
- **Reporte de Ingresos**: Gráfico de pastel mostrando distribución, tabla con desglose
- **Reporte de Egresos**: Gráfico de barras por categoría, tabla con detalles
- **Reporte de Actividad**: Tabla ordenable con todos los empleados, destacar contribución neta

### 5. **Dashboard**
- El campo `employeeStats` puede ser `null` si no hay empleados registrados
- Mostrar un mensaje amigable si no hay datos
- Los top performers se pueden mostrar como tarjetas o una lista destacada

---

## 📝 Ejemplos de Uso en Flutter/Dart

```dart
// Ejemplo: Obtener reporte de citas por empleado
Future<EmployeeAppointmentsReportDto> getEmployeeAppointmentsReport({
  DateTime? startDate,
  DateTime? endDate,
  int? employeeId,
}) async {
  final queryParams = <String, dynamic>{};
  if (startDate != null) {
    queryParams['startDate'] = startDate.toIso8601String();
  }
  if (endDate != null) {
    queryParams['endDate'] = endDate.toIso8601String();
  }
  if (employeeId != null) {
    queryParams['employeeId'] = employeeId.toString();
  }

  final response = await dio.get(
    '/barber/reports/employees/appointments',
    queryParameters: queryParams,
  );

  return EmployeeAppointmentsReportDto.fromJson(response.data);
}
```

---

## ✅ Checklist de Implementación Frontend

- [ ] Crear modelos/DTOs para los reportes
- [ ] Implementar servicio para llamar a los endpoints
- [ ] Crear pantalla/vista para mostrar reportes
- [ ] Agregar filtros de fecha (startDate, endDate)
- [ ] Agregar filtro de empleado (dropdown con lista de empleados)
- [ ] Implementar visualizaciones (gráficos, tablas)
- [ ] Mostrar estadísticas de empleados en el dashboard
- [ ] Manejar casos sin datos (mensajes amigables)
- [ ] Agregar loading states
- [ ] Manejar errores (401, 403, 500)

---

## 🎯 Resumen

Los nuevos endpoints permiten al dueño de la barbería:
1. ✅ Ver qué citas completó cada empleado
2. ✅ Analizar ingresos generados por empleado
3. ✅ Revisar egresos registrados por empleado
4. ✅ Obtener un reporte consolidado de actividad
5. ✅ Ver top performers en el dashboard

**Todos estos reportes son exclusivos para el dueño (rol "Barber") y ayudan a tomar decisiones informadas sobre el rendimiento de los empleados.**

