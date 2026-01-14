# Endpoints de Notificaciones Push

## Base URL
- **API Principal**: `/api/notifications`
- **Compatibilidad Frontend**: `/v1/push/notificationlog`

---

## 📋 Templates (Plantillas)

### 1. Obtener todas las plantillas
- **Método**: `GET`
- **Ruta**: `/api/notifications/templates`
- **Autenticación**: Requerida (JWT)
- **Rol**: Admin
- **Respuesta**: `List<TemplateDto>`

### 2. Obtener plantilla por ID
- **Método**: `GET`
- **Ruta**: `/api/notifications/templates/{id}`
- **Autenticación**: Requerida (JWT)
- **Rol**: Admin
- **Respuesta**: `TemplateDto`

### 3. Crear plantilla
- **Método**: `POST`
- **Ruta**: `/api/notifications/templates`
- **Autenticación**: Requerida (JWT)
- **Rol**: Admin
- **Body**: `CreateTemplateRequest`
- **Respuesta**: `TemplateDto` (201 Created)

### 4. Actualizar plantilla
- **Método**: `PUT`
- **Ruta**: `/api/notifications/templates/{id}`
- **Autenticación**: Requerida (JWT)
- **Rol**: Admin
- **Body**: `CreateTemplateRequest`
- **Respuesta**: `TemplateDto`

### 5. Eliminar plantilla
- **Método**: `DELETE`
- **Ruta**: `/api/notifications/templates/{id}`
- **Autenticación**: Requerida (JWT)
- **Rol**: Admin
- **Respuesta**: 204 No Content

---

## 📱 Devices (Dispositivos)

### 1. Registrar dispositivo
- **Método**: `POST`
- **Ruta**: `/api/notifications/devices`
- **Autenticación**: Requerida (JWT)
- **Body**: `RegisterDeviceRequest`
- **Respuesta**: `DeviceDto` (201 Created)

### 2. Obtener dispositivo por ID
- **Método**: `GET`
- **Ruta**: `/api/notifications/devices/{id}`
- **Autenticación**: Requerida (JWT)
- **Respuesta**: `DeviceDto`

### 3. Obtener todos los dispositivos del usuario
- **Método**: `GET`
- **Ruta**: `/api/notifications/devices`
- **Autenticación**: Requerida (JWT)
- **Respuesta**: `List<DeviceDto>`

### 4. Actualizar token FCM
- **Método**: `POST`
- **Ruta**: `/api/notifications/devices/refresh-token`
- **Autenticación**: Requerida (JWT)
- **Body**: `UpdateDeviceTokenRequest`
- **Respuesta**: `DeviceDto`

### 5. Eliminar dispositivo
- **Método**: `DELETE`
- **Ruta**: `/api/notifications/devices/{id}`
- **Autenticación**: Requerida (JWT)
- **Respuesta**: 204 No Content

---

## 📤 Enviar Notificaciones

### 1. Enviar notificación (Admin)
- **Método**: `POST`
- **Ruta**: `/api/notifications/send`
- **Autenticación**: Requerida (JWT)
- **Rol**: Admin
- **Body**: `SendNotificationRequest`
- **Respuesta**: `SendNotificationResponse`

---

## 📊 Notification Logs (Logs de Notificaciones)

### 1. Obtener logs de notificaciones
- **Método**: `GET`
- **Ruta**: `/api/notifications/logs`
- **Query Params**: `page` (default: 1), `pageSize` (default: 50)
- **Autenticación**: Requerida (JWT)
- **Respuesta**: `List<NotificationLogDto>`
- **Ejemplo**: `/api/notifications/logs?page=1&pageSize=50`

### 2. Marcar notificación como leída
- **Método**: `POST`
- **Ruta**: `/api/notifications/logs/{id}/opened`
- **Autenticación**: Requerida (JWT)
- **Respuesta**: `{ message: string, id: int }` (200 OK)

### 3. Eliminar notificación
- **Método**: `DELETE`
- **Ruta**: `/api/notifications/logs/{id}`
- **Autenticación**: Requerida (JWT)
- **Respuesta**: 204 No Content

### 4. Marcar todas como leídas
- **Método**: `POST`
- **Ruta**: `/api/notifications/logs/opened-all`
- **Autenticación**: Requerida (JWT)
- **Respuesta**: `{ message: string, count: int }` (200 OK)

---

## 🔄 Endpoints de Compatibilidad (Frontend)

Estos endpoints están disponibles en `/v1/push/notificationlog` para compatibilidad con el frontend:

### 1. Marcar notificación como leída
- **Método**: `POST`
- **Ruta**: `/v1/push/notificationlog/{id}/opened`
- **Autenticación**: Requerida (JWT)
- **Respuesta**: `{ message: string, id: int }` (200 OK)

### 2. Eliminar notificación
- **Método**: `DELETE`
- **Ruta**: `/v1/push/notificationlog/{id}`
- **Autenticación**: Requerida (JWT)
- **Respuesta**: 204 No Content

### 3. Marcar todas como leídas
- **Método**: `POST`
- **Ruta**: `/v1/push/notificationlog/opened-all`
- **Autenticación**: Requerida (JWT)
- **Respuesta**: `{ message: string, count: int }` (200 OK)

---

## 📝 Notas Importantes

1. **Autenticación**: Todos los endpoints requieren JWT Bearer token en el header:
   ```
   Authorization: Bearer {token}
   ```

2. **Roles**: 
   - Endpoints de Templates y Send requieren rol `Admin`
   - Endpoints de Devices y Logs están disponibles para todos los usuarios autenticados

3. **Status de NotificationLog**:
   - `"sent"`: Notificación enviada
   - `"opened"`: Notificación leída
   - `"failed"`: Notificación fallida

4. **Paginación**: El endpoint de logs soporta paginación con `page` y `pageSize`

5. **Seguridad**: Los usuarios solo pueden ver/modificar sus propias notificaciones (filtrado por `UserId`)

---

## ✅ Endpoints Implementados

- ✅ GET `/api/notifications/logs` - Obtener notificaciones
- ✅ POST `/api/notifications/logs/{id}/opened` - Marcar como leída
- ✅ DELETE `/api/notifications/logs/{id}` - Eliminar notificación
- ✅ POST `/api/notifications/logs/opened-all` - Marcar todas como leídas
- ✅ POST `/v1/push/notificationlog/{id}/opened` - Marcar como leída (compatibilidad)
- ✅ DELETE `/v1/push/notificationlog/{id}` - Eliminar notificación (compatibilidad)
- ✅ POST `/v1/push/notificationlog/opened-all` - Marcar todas como leídas (compatibilidad)
