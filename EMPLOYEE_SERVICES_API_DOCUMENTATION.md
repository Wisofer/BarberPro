# 📋 Documentación de API: Servicios para Empleados (Solo Lectura)

## 🎯 Introducción

Este documento describe los nuevos endpoints de **servicios** disponibles para los **empleados (trabajadores)**. Los empleados pueden **ver** los servicios del barbero dueño, pero **NO pueden crear, editar ni borrar** servicios.

**⚠️ IMPORTANTE:** 
- Los empleados solo tienen permisos de **LECTURA** sobre servicios
- Solo el dueño (rol "Barber") puede crear, editar y borrar servicios
- Los empleados necesitan ver servicios para poder crear citas manuales

---

## 🔐 Autenticación

Todos los endpoints requieren:
- **Header:** `Authorization: Bearer {token}`
- **Rol:** `Employee` (trabajador/empleado)

---

## 📋 Endpoints Disponibles

### 1. 📋 Obtener Todos los Servicios

**Endpoint:** `GET /api/employee/services`

**Descripción:** Obtiene la lista completa de servicios del barbero dueño. El empleado puede ver todos los servicios disponibles para poder crear citas manuales.

**Query Parameters:** Ninguno

**Ejemplo de Request:**
```bash
GET /api/employee/services
Authorization: Bearer {token}
```

**Ejemplo de Response (200 OK):**
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
    "name": "Barba",
    "price": 30.00,
    "durationMinutes": 20,
    "isActive": true
  },
  {
    "id": 3,
    "name": "Corte + Barba",
    "price": 70.00,
    "durationMinutes": 45,
    "isActive": true
  }
]
```

**Campos del Response:**
- `id`: ID del servicio
- `name`: Nombre del servicio
- `price`: Precio del servicio
- `durationMinutes`: Duración del servicio en minutos
- `isActive`: Si el servicio está activo

**Nota:** Solo se muestran los servicios del barbero dueño. El empleado no puede ver servicios de otros barberos.

---

### 2. 🔍 Obtener un Servicio por ID

**Endpoint:** `GET /api/employee/services/{id}`

**Descripción:** Obtiene un servicio específico por su ID. Útil para verificar detalles de un servicio antes de crear una cita.

**Path Parameters:**
- `id` (requerido): ID del servicio

**Ejemplo de Request:**
```bash
GET /api/employee/services/1
Authorization: Bearer {token}
```

**Ejemplo de Response (200 OK):**
```json
{
  "id": 1,
  "name": "Corte de Cabello",
  "price": 50.00,
  "durationMinutes": 30,
  "isActive": true
}
```

**Ejemplo de Response (404 Not Found):**
```json
{
  "message": "Servicio no encontrado o no pertenece al barbero"
}
```

**Campos del Response:**
- `id`: ID del servicio
- `name`: Nombre del servicio
- `price`: Precio del servicio
- `durationMinutes`: Duración del servicio en minutos
- `isActive`: Si el servicio está activo

**Nota:** Si el servicio no pertenece al barbero dueño, se retornará un 404.

---

## 🔄 Códigos de Respuesta

- **200 OK**: Request exitoso
- **401 Unauthorized**: Token inválido o expirado
- **403 Forbidden**: Usuario no tiene rol "Employee"
- **404 Not Found**: Servicio no encontrado o no pertenece al barbero dueño
- **500 Internal Server Error**: Error interno del servidor

---

## 🚫 Endpoints NO Disponibles para Empleados

Los siguientes endpoints **NO están disponibles** para empleados (solo para el dueño):

- ❌ `POST /api/barber/services` - Crear servicio
- ❌ `PUT /api/barber/services/{id}` - Editar servicio
- ❌ `DELETE /api/barber/services/{id}` - Borrar servicio

**Razón:** Solo el dueño de la barbería puede gestionar los servicios ofrecidos.

---

## 💡 Casos de Uso

### 1. **Crear Cita Manual**
El empleado necesita ver los servicios disponibles para poder crear una cita manual cuando llega un cliente walk-in:

```dart
// 1. Obtener lista de servicios
final services = await employeeService.getServices();

// 2. Mostrar servicios al empleado
// 3. Empleado selecciona servicio y crea cita
final appointment = await employeeService.createAppointment(
  CreateAppointmentRequest(
    serviceId: selectedService.id,
    clientName: "Cliente Walk-in",
    // ... otros campos
  ),
);
```

### 2. **Verificar Precio de Servicio**
Antes de crear una cita, el empleado puede verificar el precio de un servicio específico:

```dart
// Obtener servicio específico
final service = await employeeService.getService(serviceId);
print("Precio: ${service.price}");
```

### 3. **Listar Servicios Activos**
El empleado puede filtrar solo los servicios activos para mostrar al cliente:

```dart
final services = await employeeService.getServices();
final activeServices = services.where((s) => s.isActive).toList();
```

---

## 📝 Ejemplos de Uso en Flutter/Dart

```dart
// Servicio para empleados
class EmployeeService {
  final Dio dio;

  EmployeeService(this.dio);

  // Obtener todos los servicios
  Future<List<ServiceDto>> getServices() async {
    final response = await dio.get('/employee/services');
    return (response.data as List)
        .map((json) => ServiceDto.fromJson(json))
        .toList();
  }

  // Obtener un servicio por ID
  Future<ServiceDto> getService(int id) async {
    final response = await dio.get('/employee/services/$id');
    return ServiceDto.fromJson(response.data);
  }
}

// Uso en la UI
class ServicesListWidget extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return FutureBuilder<List<ServiceDto>>(
      future: employeeService.getServices(),
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return CircularProgressIndicator();
        }
        
        if (snapshot.hasError) {
          return Text('Error: ${snapshot.error}');
        }
        
        final services = snapshot.data ?? [];
        final activeServices = services.where((s) => s.isActive).toList();
        
        return ListView.builder(
          itemCount: activeServices.length,
          itemBuilder: (context, index) {
            final service = activeServices[index];
            return ListTile(
              title: Text(service.name),
              subtitle: Text('\$${service.price.toStringAsFixed(2)}'),
              trailing: Text('${service.durationMinutes} min'),
              onTap: () {
                // Crear cita con este servicio
                _createAppointment(service);
              },
            );
          },
        );
      },
    );
  }
}
```

---

## ✅ Checklist de Implementación Frontend

- [ ] Crear servicio para llamar a los endpoints de servicios
- [ ] Implementar método `getServices()` para obtener todos los servicios
- [ ] Implementar método `getService(id)` para obtener un servicio específico
- [ ] Mostrar lista de servicios en la UI del empleado
- [ ] Filtrar servicios activos si es necesario
- [ ] Usar servicios al crear citas manuales
- [ ] Manejar errores (401, 403, 404, 500)
- [ ] Agregar loading states
- [ ] Mostrar precios y duraciones de servicios

---

## 🎯 Resumen

Los nuevos endpoints permiten al empleado:
1. ✅ Ver todos los servicios del barbero dueño
2. ✅ Ver detalles de un servicio específico
3. ✅ Usar servicios para crear citas manuales
4. ❌ **NO puede crear servicios** (solo el dueño)
5. ❌ **NO puede editar servicios** (solo el dueño)
6. ❌ **NO puede borrar servicios** (solo el dueño)

**Estos endpoints son de solo lectura y ayudan al empleado a realizar su trabajo sin necesidad de permisos administrativos.**

---

## 🔗 Endpoints Relacionados

Para crear citas usando estos servicios, ver:
- `POST /api/employee/appointments` - Crear cita manual (empleado)

Para gestionar servicios (solo dueño), ver:
- `GET /api/barber/services` - Obtener servicios (dueño)
- `POST /api/barber/services` - Crear servicio (dueño)
- `PUT /api/barber/services/{id}` - Editar servicio (dueño)
- `DELETE /api/barber/services/{id}` - Borrar servicio (dueño)

