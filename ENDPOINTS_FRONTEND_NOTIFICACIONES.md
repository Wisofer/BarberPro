# Endpoints de Notificaciones para Frontend

## Base URL
```
http://localhost:5229  (o tu URL del servidor)
```

## Autenticación
Todos los endpoints requieren el header:
```
Authorization: Bearer {token}
```

---

## 📱 Endpoints Disponibles

### 1. Obtener Notificaciones (Logs)
```
GET /api/notifications/logs?page=1&pageSize=50
```

**Query Parameters:**
- `page` (opcional, default: 1) - Número de página
- `pageSize` (opcional, default: 50) - Cantidad de elementos por página

**Respuesta:**
```json
[
  {
    "id": 1,
    "status": "sent",
    "payload": "{...}",
    "sentAt": "2024-01-15T10:30:00Z",
    "deviceId": 1,
    "templateId": 1,
    "userId": 1,
    "createdAt": "2024-01-15T10:30:00Z"
  }
]
```

---

### 2. Marcar Notificación como Leída
```
POST /v1/push/notificationlog/{id}/opened
```

**Parámetros:**
- `{id}` - ID de la notificación (NotificationLogId)

**Body:** (opcional, puede ser vacío)
```json
{
  "id": 123
}
```

**Respuesta:**
```json
{
  "message": "Notificación marcada como leída",
  "id": 123
}
```

**Código de estado:** 200 OK

---

### 3. Eliminar Notificación
```
DELETE /v1/push/notificationlog/{id}
```

**Parámetros:**
- `{id}` - ID de la notificación (NotificationLogId)

**Respuesta:** 
- 204 No Content (éxito)
- 404 Not Found (notificación no encontrada)

---

### 4. Marcar Todas las Notificaciones como Leídas
```
POST /v1/push/notificationlog/opened-all
```

**Body:** (opcional, puede ser vacío)
```json
{}
```

**Respuesta:**
```json
{
  "message": "5 notificaciones marcadas como leídas",
  "count": 5
}
```

**Código de estado:** 200 OK

---

## 📝 Ejemplos de Uso

### Flutter/Dart
```dart
// Obtener notificaciones
final response = await http.get(
  Uri.parse('http://localhost:5229/api/notifications/logs?page=1&pageSize=50'),
  headers: {
    'Authorization': 'Bearer $token',
    'Content-Type': 'application/json',
  },
);

// Marcar como leída
final response = await http.post(
  Uri.parse('http://localhost:5229/v1/push/notificationlog/$notificationId/opened'),
  headers: {
    'Authorization': 'Bearer $token',
    'Content-Type': 'application/json',
  },
  body: jsonEncode({'id': notificationId}),
);

// Eliminar notificación
final response = await http.delete(
  Uri.parse('http://localhost:5229/v1/push/notificationlog/$notificationId'),
  headers: {
    'Authorization': 'Bearer $token',
  },
);

// Marcar todas como leídas
final response = await http.post(
  Uri.parse('http://localhost:5229/v1/push/notificationlog/opened-all'),
  headers: {
    'Authorization': 'Bearer $token',
    'Content-Type': 'application/json',
  },
  body: jsonEncode({}),
);
```

### JavaScript/TypeScript
```javascript
// Obtener notificaciones
const response = await fetch('http://localhost:5229/api/notifications/logs?page=1&pageSize=50', {
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json',
  },
});

// Marcar como leída
const response = await fetch(`http://localhost:5229/v1/push/notificationlog/${notificationId}/opened`, {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({ id: notificationId }),
});

// Eliminar notificación
const response = await fetch(`http://localhost:5229/v1/push/notificationlog/${notificationId}`, {
  method: 'DELETE',
  headers: {
    'Authorization': `Bearer ${token}`,
  },
});

// Marcar todas como leídas
const response = await fetch('http://localhost:5229/v1/push/notificationlog/opened-all', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({}),
});
```

---

## ✅ Resumen de Endpoints

| Acción | Método | Endpoint | Status Code |
|--------|--------|----------|-------------|
| Obtener notificaciones | GET | `/api/notifications/logs?page=1&pageSize=50` | 200 |
| Marcar como leída | POST | `/v1/push/notificationlog/{id}/opened` | 200 |
| Eliminar notificación | DELETE | `/v1/push/notificationlog/{id}` | 204 |
| Marcar todas como leídas | POST | `/v1/push/notificationlog/opened-all` | 200 |

---

## 🔒 Seguridad

- Todos los endpoints requieren autenticación JWT
- Los usuarios solo pueden ver/modificar sus propias notificaciones
- El `userId` se obtiene automáticamente del token JWT

---

## 📊 Estados de Notificación

- `"sent"` - Notificación enviada (no leída)
- `"opened"` - Notificación leída
- `"failed"` - Notificación fallida

---

## ⚠️ Notas Importantes

1. **NotificationLogId**: El `{id}` en los endpoints es el `id` del `NotificationLog`, no el `templateId` ni `deviceId`.

2. **Paginación**: El endpoint de obtener notificaciones soporta paginación. Usa `page` y `pageSize` para controlar la cantidad de resultados.

3. **Filtrado**: Las notificaciones se filtran automáticamente por el usuario autenticado. No necesitas pasar el `userId` manualmente.

4. **Errores comunes**:
   - 401 Unauthorized: Token inválido o expirado
   - 404 Not Found: Notificación no encontrada o no pertenece al usuario
   - 500 Internal Server Error: Error del servidor
