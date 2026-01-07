# 🔄 Actualización Frontend: Dashboard del Barbero

## ⚠️ IMPORTANTE: Cambios en el DTO del Dashboard

Se han realizado cambios en la estructura del dashboard que requieren actualización en el frontend.

---

## 📋 Cambios en `TodayStatsDto` (Estadísticas del Día)

### ✅ Campos NUEVOS agregados:

```dart
// ANTES (solo tenía estos campos):
class TodayStatsDto {
  int appointments;
  int completed;
  int pending;
  decimal income;
}

// AHORA (incluye campos nuevos):
class TodayStatsDto {
  int appointments;
  int completed;
  int pending;
  decimal income;
  decimal expenses;  // ⭐ NUEVO
  decimal profit;    // ⭐ NUEVO
}
```

### 📝 Ejemplo de Response:

```json
{
  "today": {
    "appointments": 5,
    "completed": 3,
    "pending": 2,
    "income": 250.00,
    "expenses": 50.00,    // ⭐ NUEVO
    "profit": 200.00      // ⭐ NUEVO
  }
}
```

---

## 🔧 Cambios en el Cálculo de Datos

### ⚠️ IMPORTANTE: Cambio en la fuente de datos

**ANTES:**
- Día: Ingresos calculados desde **citas** (precio de servicios)
- Semana: Ingresos calculados desde **citas** (precio de servicios)
- Mes: Ingresos calculados desde **transacciones** ✅

**AHORA (Consistente):**
- Día: Ingresos y egresos desde **transacciones** ✅
- Semana: Ingresos y egresos desde **transacciones** ✅
- Mes: Ingresos y egresos desde **transacciones** ✅

**Razón del cambio:** Para que los datos del dashboard coincidan exactamente con los de finanzas.

---

## 📝 Checklist de Actualización Frontend

### 1. **Actualizar Modelos/DTOs**

```dart
// Actualizar TodayStatsDto
class TodayStatsDto {
  final int appointments;
  final int completed;
  final int pending;
  final double income;
  final double expenses;  // ⭐ AGREGAR
  final double profit;    // ⭐ AGREGAR

  TodayStatsDto({
    required this.appointments,
    required this.completed,
    required this.pending,
    required this.income,
    required this.expenses,  // ⭐ AGREGAR
    required this.profit,    // ⭐ AGREGAR
  });

  factory TodayStatsDto.fromJson(Map<String, dynamic> json) {
    return TodayStatsDto(
      appointments: json['appointments'] ?? 0,
      completed: json['completed'] ?? 0,
      pending: json['pending'] ?? 0,
      income: (json['income'] ?? 0).toDouble(),
      expenses: (json['expenses'] ?? 0).toDouble(),  // ⭐ AGREGAR
      profit: (json['profit'] ?? 0).toDouble(),       // ⭐ AGREGAR
    );
  }
}
```

### 2. **Actualizar UI del Dashboard**

Si ya mostraban los datos del día, ahora pueden mostrar también:

```dart
// Ejemplo de widget para mostrar estadísticas del día
Widget buildTodayStats(TodayStatsDto today) {
  return Card(
    child: Column(
      children: [
        Text('Hoy'),
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceAround,
          children: [
            _StatItem('Citas', today.appointments.toString()),
            _StatItem('Completadas', today.completed.toString()),
            _StatItem('Pendientes', today.pending.toString()),
          ],
        ),
        Divider(),
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceAround,
          children: [
            _StatItem('Ingresos', '\$${today.income.toStringAsFixed(2)}'),
            _StatItem('Egresos', '\$${today.expenses.toStringAsFixed(2)}'),  // ⭐ NUEVO
            _StatItem('Ganancia', '\$${today.profit.toStringAsFixed(2)}'),    // ⭐ NUEVO
          ],
        ),
      ],
    ),
  );
}
```

### 3. **Verificar Parsing de JSON**

Asegurarse de que el parsing maneje los nuevos campos (pueden ser null en versiones antiguas):

```dart
factory TodayStatsDto.fromJson(Map<String, dynamic> json) {
  return TodayStatsDto(
    // ... campos existentes
    expenses: json['expenses'] != null 
        ? (json['expenses'] as num).toDouble() 
        : 0.0,  // Valor por defecto si no existe
    profit: json['profit'] != null 
        ? (json['profit'] as num).toDouble() 
        : 0.0,  // Valor por defecto si no existe
  );
}
```

### 4. **Actualizar Visualizaciones**

Si tienen gráficos o visualizaciones del dashboard, pueden incluir:

- **Tarjeta de ganancia del día** (profit)
- **Comparación ingresos vs egresos del día**
- **Indicador visual** de si la ganancia es positiva o negativa

---

## 🎨 Sugerencias de UI

### Mostrar los nuevos datos:

```dart
// Ejemplo: Tarjeta de resumen financiero del día
Card(
  child: Padding(
    padding: EdgeInsets.all(16),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Resumen Financiero de Hoy', 
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
        SizedBox(height: 12),
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Column(
              children: [
                Text('Ingresos', style: TextStyle(color: Colors.green)),
                Text('\$${today.income.toStringAsFixed(2)}',
                  style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
              ],
            ),
            Column(
              children: [
                Text('Egresos', style: TextStyle(color: Colors.red)),
                Text('\$${today.expenses.toStringAsFixed(2)}',
                  style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
              ],
            ),
            Column(
              children: [
                Text('Ganancia', 
                  style: TextStyle(
                    color: today.profit >= 0 ? Colors.green : Colors.red
                  )),
                Text('\$${today.profit.toStringAsFixed(2)}',
                  style: TextStyle(
                    fontSize: 20, 
                    fontWeight: FontWeight.bold,
                    color: today.profit >= 0 ? Colors.green : Colors.red
                  )),
              ],
            ),
          ],
        ),
      ],
    ),
  ),
)
```

---

## ✅ Verificación

Después de actualizar, verificar que:

1. ✅ El modelo `TodayStatsDto` incluye `expenses` y `profit`
2. ✅ El parsing de JSON maneja los nuevos campos
3. ✅ La UI muestra los nuevos datos (opcional, pero recomendado)
4. ✅ No hay errores de parsing cuando el backend retorna los nuevos campos
5. ✅ Los valores por defecto están configurados si los campos no existen

---

## 🔄 Compatibilidad

**Nota de compatibilidad:** Si el frontend no actualiza los modelos, puede que:
- Los campos nuevos (`expenses`, `profit`) se ignoren
- No haya errores si el parsing es tolerante
- Pero no se mostrarán los nuevos datos

**Recomendación:** Actualizar los modelos para aprovechar los nuevos datos.

---

## 📊 Resumen de Cambios

| Campo | Estado | Descripción |
|-------|--------|-------------|
| `today.expenses` | ⭐ NUEVO | Egresos del día (transacciones) |
| `today.profit` | ⭐ NUEVO | Ganancia del día (ingresos - egresos) |
| `today.income` | 🔄 CAMBIÓ | Ahora viene de transacciones (antes de citas) |
| `thisWeek.income` | 🔄 CAMBIÓ | Ahora viene de transacciones (antes de citas) |
| `thisWeek.expenses` | 🔄 CAMBIÓ | Ahora es del período de la semana (antes del mes) |

---

## 🎯 Próximos Pasos

1. Actualizar modelos/DTOs en el frontend
2. Actualizar UI para mostrar los nuevos campos (opcional pero recomendado)
3. Probar que el dashboard carga correctamente
4. Verificar que los datos coinciden con el módulo de finanzas

