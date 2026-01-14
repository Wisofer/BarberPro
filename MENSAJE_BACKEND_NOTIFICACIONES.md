# 🔍 Mensaje para el Backend: Revisión del Sistema de Notificaciones

## ✅ Cambios Implementados

### 1. **Selección de Usuarios Específicos**
- ✅ Ahora puedes seleccionar usuarios específicos o enviar a todos
- ✅ Checkbox "Enviar a todos" o selección múltiple de barberos
- ✅ El backend filtra correctamente por `UserIds` seleccionados

### 2. **Logs Detallados Agregados**
- ✅ Logs antes de enviar (template, usuarios, dispositivos, tokens)
- ✅ Logs durante el envío (respuestas de Firebase)
- ✅ Logs de errores detallados por dispositivo
- ✅ Logs finales con resumen de éxito/fallos

### 3. **Correcciones en el Formato del Mensaje**
- ✅ `dataOnly` ahora funciona correctamente (no muestra notificación cuando es solo datos)
- ✅ Formato correcto: `Notification` + `Data` cuando es notificación normal
- ✅ Solo `Data` cuando es `dataOnly`

---

## 🔍 Qué Revisar en el Backend

### 1. **Verificar Token FCM en Base de Datos**

**Consulta SQL:**
```sql
SELECT id, "FcmToken", "UserId", "Platform", "LastActiveAt" 
FROM "Devices" 
WHERE id = 2;
```

**Verificar:**
- ✅ El token del dispositivo ID 2 es: `e6vnOYfxR8-gr-IeIqa-zV:APA91bHqs_ecwuDFupGrUs5y20i9g2PvfMGHEv_QvhG5VY-OMF9lNgbS9q8mOs3FAg2gKmGNLCQ5jSpwLIRe5YKP2EbCY1WfMxtnh1QhG_Zzal8ol7MOkk0`
- ✅ El `UserId` es correcto (debe ser el ID del barbero)
- ✅ El token no está expirado o inválido

---

### 2. **Revisar Logs del Backend al Enviar**

Cuando envíes una notificación desde `/admin/notifications`, revisa los logs. Deberías ver:

```
📤 Enviando notificación:
   - Template ID: X
   - Template Title: ...
   - Template Body: ...
   - Usuarios destino: X
   - Dispositivos destino: X
   - Tokens FCM: ...
   - UserIds seleccionados: ... (o "Enviando a TODOS los usuarios")

🔔 Iniciando envío de notificación push
   - Template ID: X
   - Template Title: ...
   - Template Body: ...
   - Template ImageUrl: ...
   - Dispositivos válidos: X
   - DataOnly: false/true
   - Tokens FCM (muestra): ...
   - Data payload: ...

📤 Enviando lote de X tokens a Firebase
✅ Respuesta de Firebase: X exitosas, Y fallidas
```

**Si hay errores:**
```
❌ Error al enviar a dispositivo X (Usuario Y): InvalidArgument - Token inválido
```

---

### 3. **Verificar Formato del Payload**

El backend debe enviar este formato a Firebase:

**Cuando NO es `dataOnly`:**
```json
{
  "message": {
    "token": "token_fcm_aqui",
    "notification": {
      "title": "Título",
      "body": "Mensaje"
    },
    "data": {
      "type": "announcement",
      "title": "Título",
      "body": "Mensaje",
      "templateId": "5"
    },
    "android": {
      "priority": "high",
      "notification": {
        "title": "Título",
        "body": "Mensaje",
        "sound": "default"
      }
    },
    "apns": {
      "headers": {
        "apns-priority": "10"
      },
      "payload": {
        "aps": {
          "alert": {
            "title": "Título",
            "body": "Mensaje"
          },
          "sound": "default",
          "badge": 1
        }
      }
    }
  }
}
```

**Cuando SÍ es `dataOnly`:**
```json
{
  "message": {
    "token": "token_fcm_aqui",
    "data": {
      "type": "announcement",
      "title": "Título",
      "body": "Mensaje",
      "templateId": "5"
    },
    "android": {
      "priority": "high"
    },
    "apns": {
      "headers": {
        "apns-priority": "10"
      },
      "payload": {
        "aps": {
          "contentAvailable": true
        }
      }
    }
  }
}
```

**Nota:** El código ya genera este formato correctamente usando `MulticastMessage` de Firebase Admin SDK.

---

### 4. **Verificar Firebase Admin SDK**

**Verificar en `Program.cs`:**
- ✅ Firebase está inicializado: `FirebaseApp.DefaultInstance != null`
- ✅ Credenciales correctas: `Secrets/firebase_credentials.json` existe
- ✅ Proyecto correcto: `project_id` en el JSON debe ser `barbenic-6d215`

**Logs esperados al iniciar:**
```
✅ Firebase inicializado correctamente
```

Si ves:
```
⚠️ Error al inicializar Firebase: ...
```
→ Revisa las credenciales.

---

### 5. **Verificar que el Usuario Correcto Recibe la Notificación**

**Consulta SQL:**
```sql
-- Verificar qué usuarios tienen dispositivos registrados
SELECT u.id as user_id, u.email, b.name as barber_name, d.id as device_id, d."FcmToken"
FROM "Users" u
LEFT JOIN "Barbers" b ON b."UserId" = u.id
LEFT JOIN "Devices" d ON d."UserId" = u.id
WHERE d."FcmToken" IS NOT NULL;
```

**Verificar:**
- ✅ El `UserId` del dispositivo coincide con el `UserId` del barbero
- ✅ El barbero tiene un dispositivo registrado

---

### 6. **Probar Envío Manual**

1. **Ir a `/admin/notifications`**
2. **Crear una plantilla de prueba:**
   - Título: "Prueba"
   - Mensaje: "Esta es una prueba"
3. **Seleccionar la plantilla**
4. **Seleccionar usuario específico** (desmarcar "Enviar a todos" y seleccionar el barbero)
5. **Enviar**
6. **Revisar logs del backend** para ver:
   - ¿Qué token se está usando?
   - ¿Qué payload se envía?
   - ¿Qué respuesta devuelve Firebase?

---

### 7. **Verificar NotificationLogs**

Después de enviar, revisa la tabla `NotificationLogs`:

```sql
SELECT id, "Status", "Payload", "SentAt", "DeviceId", "UserId", "TemplateId"
FROM "NotificationLogs"
ORDER BY "SentAt" DESC
LIMIT 10;
```

**Verificar:**
- ✅ `Status` = "sent" (éxito) o "failed" (fallo)
- ✅ `Payload` contiene el JSON enviado
- ✅ `DeviceId` corresponde al dispositivo correcto
- ✅ `UserId` corresponde al usuario correcto

---

## 🐛 Troubleshooting

### Problema: Notificaciones no llegan

**Pasos a seguir:**

1. **Verificar token FCM en BD:**
   ```sql
   SELECT * FROM "Devices" WHERE id = 2;
   ```

2. **Verificar logs del backend:**
   - Buscar líneas que empiecen con `📤`, `🔔`, `✅`, `❌`
   - Ver qué token se está usando
   - Ver qué respuesta devuelve Firebase

3. **Verificar Firebase Console:**
   - Ir a Firebase Console → Cloud Messaging
   - Ver si hay errores reportados

4. **Probar token directamente desde Firebase Console:**
   - Si funciona desde Console pero no desde backend → problema de formato o autenticación
   - Si no funciona desde Console → token inválido o expirado

### Problema: Error "Invalid credentials"

**Solución:**
1. Verificar que `Secrets/firebase_credentials.json` existe
2. Verificar que el JSON es válido
3. Verificar que el `project_id` es `barbenic-6d215`
4. Regenerar credenciales si es necesario

### Problema: Error "Token not found"

**Solución:**
1. Verificar que el dispositivo está registrado en BD
2. Verificar que el `UserId` es correcto
3. Verificar que el token FCM no ha cambiado (el frontend debe actualizarlo)

---

## 📋 Checklist de Verificación

- [ ] Token FCM del dispositivo ID 2 es correcto en BD
- [ ] `UserId` del dispositivo coincide con el barbero correcto
- [ ] Firebase está inicializado correctamente (`Program.cs`)
- [ ] Credenciales de Firebase son correctas (`barbenic-6d215`)
- [ ] Logs del backend muestran el token correcto al enviar
- [ ] Logs del backend muestran respuesta de Firebase (éxito/fallo)
- [ ] `NotificationLogs` registra el envío correctamente
- [ ] Formato del payload incluye `notification` + `data` (cuando no es `dataOnly`)
- [ ] Formato del payload solo incluye `data` (cuando es `dataOnly`)

---

## 🎯 Próximos Pasos

1. **Probar envío manual** desde `/admin/notifications`
2. **Revisar logs** del backend durante el envío
3. **Verificar `NotificationLogs`** después del envío
4. **Si sigue sin funcionar**, compartir:
   - Logs completos del backend al enviar
   - Resultado de la consulta SQL del dispositivo ID 2
   - Respuesta de Firebase (si está en los logs)

---

**Última actualización:** 2025-01-14  
**Estado:** ✅ Código compilado y listo para probar
