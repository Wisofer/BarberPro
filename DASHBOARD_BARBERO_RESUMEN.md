# 📊 Resumen: Dashboard del Barbero (Dueño)

## 🎯 Endpoint
**GET** `/api/barber/dashboard`

---

## 📋 Lo que muestra el Dashboard

### 1. 📌 **Información del Barbero**
- Datos del perfil del barbero (nombre, negocio, teléfono, etc.)

---

### 2. 📅 **Estadísticas del Día (Today)**
- **Total de citas** del día
- **Citas completadas** del día
- **Citas pendientes** del día
- **Ingresos** del día (solo de citas confirmadas/completadas)
- **Egresos** del día (transacciones de egreso registradas hoy)
- **Ganancia** del día (ingresos - egresos)

---

### 3. 📊 **Estadísticas de la Semana (ThisWeek)**
- **Total de citas** de la semana
- **Ingresos** de la semana
- **Egresos** de la semana
- **Ganancia** (ingresos - egresos)
- **Clientes únicos** atendidos en la semana
- **Promedio por cliente** (ingresos / clientes únicos)

---

### 4. 💰 **Estadísticas del Mes (ThisMonth)**
- **Total de citas** del mes
- **Ingresos** del mes
- **Egresos** del mes
- **Ganancia** del mes (ingresos - egresos)
- **Clientes únicos** atendidos en el mes
- **Promedio por cliente** del mes

---

### 5. 📝 **Citas Recientes (RecentAppointments)**
- Últimas **5 citas pasadas** (ya atendidas)
- Ordenadas por fecha/hora más reciente
- Incluye: cliente, servicio, fecha, hora, estado, precio

---

### 6. 🔜 **Próximas Citas (UpcomingAppointments)**
- Próximas **5 citas** (hoy y futuras)
- Ordenadas por fecha/hora más próxima
- Incluye: cliente, servicio, fecha, hora, estado, precio

---

### 7. 👥 **Estadísticas de Empleados (EmployeeStats)**
- **Total de empleados** registrados
- **Empleados activos**
- **Top 3 empleados** del mes (por ingresos generados):
  - Nombre del empleado
  - Citas completadas
  - Ingresos totales generados
  - Promedio por cita

---

## 📊 Estructura del Response

```json
{
  "barber": {
    // Datos del barbero
  },
  "today": {
    "appointments": 5,
    "completed": 3,
    "pending": 2,
    "income": 250.00,
    "expenses": 50.00,
    "profit": 200.00
  },
  "thisWeek": {
    "appointments": 25,
    "income": 1500.00,
    "expenses": 300.00,
    "profit": 1200.00,
    "uniqueClients": 18,
    "averagePerClient": 83.33
  },
  "thisMonth": {
    "appointments": 100,
    "income": 6000.00,
    "expenses": 1200.00,
    "profit": 4800.00,
    "uniqueClients": 65,
    "averagePerClient": 92.31
  },
  "recentAppointments": [
    // Últimas 5 citas pasadas
  ],
  "upcomingAppointments": [
    // Próximas 5 citas
  ],
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
      }
      // Top 3 empleados
    ]
  }
}
```

---

## 🎯 Resumen Visual

```
┌─────────────────────────────────────────┐
│  DASHBOARD DEL BARBERO (DUEÑO)          │
├─────────────────────────────────────────┤
│                                         │
│  📌 PERFIL DEL BARBERO                 │
│                                         │
│  📅 HOY                                │
│  • Citas: 5                            │
│  • Completadas: 3                      │
│  • Pendientes: 2                       │
│  • Ingresos: $250                      │
│  • Egresos: $50                        │
│  • Ganancia: $200                      │
│                                         │
│  📊 ESTA SEMANA                        │
│  • Citas: 25                           │
│  • Ingresos: $1,500                    │
│  • Egresos: $300                       │
│  • Ganancia: $1,200                    │
│  • Clientes únicos: 18                │
│  • Promedio/cliente: $83.33            │
│                                         │
│  💰 ESTE MES                           │
│  • Citas: 100                          │
│  • Ingresos: $6,000                    │
│  • Egresos: $1,200                     │
│  • Ganancia: $4,800                    │
│  • Clientes únicos: 65                │
│  • Promedio/cliente: $92.31            │
│                                         │
│  📝 CITAS RECIENTES (5)                │
│  • [Lista de últimas 5 citas]          │
│                                         │
│  🔜 PRÓXIMAS CITAS (5)                 │
│  • [Lista de próximas 5 citas]         │
│                                         │
│  👥 EMPLEADOS                          │
│  • Total: 3                            │
│  • Activos: 2                          │
│  • Top 3:                              │
│    1. Juan Pérez - $2,000 (20 citas)  │
│    2. María García - $1,500 (15 citas)│
│    3. Pedro López - $1,200 (12 citas)│
│                                         │
└─────────────────────────────────────────┘
```

---

## ✅ Puntos Clave

1. **Estadísticas en tiempo real** del día, semana y mes
2. **Vista rápida** de citas recientes y próximas
3. **Análisis financiero** (ingresos, egresos, ganancia)
4. **Métricas de clientes** (únicos, promedio por cliente)
5. **Rendimiento de empleados** (top 3 del mes)
6. **Todo en un solo endpoint** para carga rápida

---

## 🔗 Nota

- El campo `employeeStats` puede ser `null` si no hay empleados registrados
- Las estadísticas se calculan en tiempo real al hacer la petición
- Las citas incluyen información del servicio y cliente

