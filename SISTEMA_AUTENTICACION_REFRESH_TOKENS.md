# Sistema de Autenticación con Refresh Tokens - BarberNic

## 📋 Resumen Ejecutivo

BarberNic implementa un sistema de autenticación robusto basado en JWT (JSON Web Tokens) con dos tipos de tokens: **Access Tokens** (corto plazo) y **Refresh Tokens** (largo plazo). Este sistema permite mantener sesiones activas durante períodos extendidos sin comprometer la seguridad.

---

## 🔐 Configuración de Tokens

### Access Token (Token de Acceso)
- **Duración**: 24 horas (1440 minutos)
- **Propósito**: Autenticar cada petición HTTP al API
- **Almacenamiento**: Frontend (localStorage/sessionStorage)
- **Uso**: Se envía en el header `Authorization: Bearer {token}`

### Refresh Token (Token de Renovación)
- **Duración**: 30 días
- **Propósito**: Obtener nuevos Access Tokens cuando el actual expira
- **Almacenamiento**: Frontend (localStorage/sessionStorage)
- **Uso**: Solo para llamar al endpoint `/api/auth/refresh`

---

## 🔄 Flujo de Autenticación

### 1. Login Inicial

**Endpoint**: `POST /api/auth/login`

**Request**:
```json
{
  "email": "usuario@ejemplo.com",
  "password": "contraseña"
}
```

**Response** (200 OK):
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 10,
    "email": "usuario@ejemplo.com",
    "role": "Barber",
    "barber": { ... }
  },
  "role": "Barber"
}
```

**Acciones del Frontend**:
1. Almacenar ambos tokens (access y refresh)
2. Usar el `token` para todas las peticiones subsecuentes
3. Guardar el `refreshToken` para renovación automática

---

### 2. Uso Normal (Primeras 24 horas)

**Flujo**:
1. Usuario realiza acciones en la aplicación
2. Frontend envía el Access Token en cada petición:
   ```
   Authorization: Bearer {accessToken}
   ```
3. Backend valida el token y procesa la petición
4. Todo funciona normalmente

**Características**:
- ✅ No hay interrupciones
- ✅ El usuario trabaja sin notar el sistema de tokens
- ✅ Todas las peticiones se autentican automáticamente

---

### 3. Renovación Automática (Después de 24 horas)

**Cuando el Access Token expira**:

1. **Backend responde con error**:
   - Status: `401 Unauthorized`
   - Mensaje: Token expirado o inválido

2. **Frontend detecta el error automáticamente**:
   - Interceptor HTTP captura el 401
   - Identifica que el token expiró

3. **Frontend llama al endpoint de refresh**:
   ```
   POST /api/auth/refresh
   Content-Type: application/json
   
   {
     "refreshToken": "{refreshToken}"
   }
   ```

4. **Backend valida y genera nuevos tokens**:
   - Valida que el Refresh Token sea válido
   - Verifica que no esté expirado
   - Verifica que el usuario/barbero/empleado siga activo
   - Genera nuevos tokens:
     - Nuevo Access Token (24 horas más)
     - Nuevo Refresh Token (30 días más)

5. **Frontend actualiza los tokens**:
   - Reemplaza el Access Token expirado
   - Actualiza el Refresh Token
   - Reintenta la petición original automáticamente

6. **Usuario continúa trabajando**:
   - ✅ No nota la interrupción
   - ✅ Todo funciona transparentemente
   - ✅ La sesión se mantiene activa

---

### 4. Ciclo Continuo (Hasta 30 días)

**Proceso repetitivo**:
- Cada 24 horas, el Access Token expira
- El sistema renueva automáticamente usando el Refresh Token
- El usuario puede trabajar hasta 30 días sin login manual
- El Refresh Token también se renueva en cada refresh

**Ventajas**:
- 🔄 Renovación automática sin intervención del usuario
- ⏰ Sesión extendida hasta 30 días
- 🔒 Seguridad mantenida con tokens de corta duración

---

### 5. Expiración del Refresh Token (Después de 30 días)

**Cuando el Refresh Token expira**:

1. **El refresh falla**:
   - Status: `401 Unauthorized`
   - Mensaje: "Refresh token inválido o expirado"

2. **Frontend detecta que el Refresh Token expiró**:
   - No puede renovar el Access Token
   - Limpia los tokens almacenados

3. **Usuario debe hacer login nuevamente**:
   - Redirige a la pantalla de login
   - Usuario ingresa credenciales
   - Obtiene nuevos tokens (access + refresh)

---

## 🛡️ Validaciones de Seguridad

### Durante el Login
El sistema valida:
- ✅ Credenciales correctas (email y contraseña)
- ✅ Usuario activo (`IsActive = true`)
- ✅ Si es **Barber**: verifica que el barbero exista y esté activo
- ✅ Si es **Employee**: verifica que:
  - El empleado exista y esté activo
  - El barbero dueño exista y esté activo

### Durante el Refresh
El sistema valida:
- ✅ Refresh Token válido y no expirado
- ✅ Usuario existe y está activo
- ✅ Si es **Barber**: verifica que el barbero exista y esté activo
- ✅ Si es **Employee**: verifica que:
  - El empleado exista y esté activo
  - El barbero dueño exista y esté activo

**Si alguna validación falla**:
- ❌ El refresh es rechazado
- ❌ El usuario debe hacer login nuevamente

---

## 📡 Endpoints de la API

### POST /api/auth/login
**Descripción**: Inicia sesión y obtiene tokens

**Request Body**:
```json
{
  "email": "string",
  "password": "string"
}
```

**Response Success (200)**:
```json
{
  "token": "string (JWT)",
  "refreshToken": "string (JWT)",
  "user": { ... },
  "role": "string"
}
```

**Response Error (401)**:
```json
{
  "message": "Credenciales inválidas" | "Tu cuenta está desactivada" | ...
}
```

---

### POST /api/auth/refresh
**Descripción**: Renueva el Access Token usando el Refresh Token

**Request Body**:
```json
{
  "refreshToken": "string (JWT)"
}
```

**Response Success (200)**:
```json
{
  "token": "string (nuevo JWT)",
  "refreshToken": "string (nuevo JWT)",
  "user": { ... },
  "role": "string"
}
```

**Response Error (401)**:
```json
{
  "message": "Refresh token inválido o expirado"
}
```

---

## 🔧 Configuración Técnica

### Archivo: `appsettings.json`
```json
{
  "JwtSettings": {
    "SecretKey": "EstaEsUnaClaveSecretaMuyLargaParaJWT2024BarberNicSystem",
    "Issuer": "BarberNic",
    "Audience": "BarberNicUsers",
    "ExpirationInMinutes": 1440,        // 24 horas
    "RefreshTokenExpirationInDays": 30   // 30 días
  }
}
```

### Claims del Access Token
- `NameIdentifier`: ID del usuario
- `Email`: Email del usuario
- `Role`: Rol (Admin, Barber, Employee)
- `UserId`: ID del usuario
- `BarberId`: ID del barbero (si aplica)
- `EmployeeId`: ID del empleado (si aplica)
- `OwnerBarberId`: ID del barbero dueño (si es empleado)

### Claims del Refresh Token
- Todos los claims del Access Token
- `TokenType`: "RefreshToken" (identificador especial)

---

## 💡 Mejores Prácticas para el Frontend

### 1. Almacenamiento de Tokens
```javascript
// Recomendado: localStorage o sessionStorage
localStorage.setItem('accessToken', response.token);
localStorage.setItem('refreshToken', response.refreshToken);
```

### 2. Interceptor HTTP
```javascript
// Interceptar peticiones para agregar el token
axios.interceptors.request.use(config => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});
```

### 3. Manejo de Errores 401
```javascript
// Interceptar respuestas para manejar tokens expirados
axios.interceptors.response.use(
  response => response,
  async error => {
    if (error.response?.status === 401) {
      // Intentar refresh
      const refreshToken = localStorage.getItem('refreshToken');
      if (refreshToken) {
        try {
          const refreshResponse = await axios.post('/api/auth/refresh', {
            refreshToken: refreshToken
          });
          
          // Actualizar tokens
          localStorage.setItem('accessToken', refreshResponse.data.token);
          localStorage.setItem('refreshToken', refreshResponse.data.refreshToken);
          
          // Reintentar petición original
          error.config.headers.Authorization = `Bearer ${refreshResponse.data.token}`;
          return axios.request(error.config);
        } catch (refreshError) {
          // Refresh falló, redirigir a login
          localStorage.clear();
          window.location.href = '/login';
        }
      }
    }
    return Promise.reject(error);
  }
);
```

### 4. Limpieza de Tokens
```javascript
// Al hacer logout o cuando el refresh falla
localStorage.removeItem('accessToken');
localStorage.removeItem('refreshToken');
```

---

## 📊 Comparación: Antes vs. Ahora

| Aspecto | Antes (Sin Refresh Tokens) | Ahora (Con Refresh Tokens) |
|---------|---------------------------|---------------------------|
| **Duración del Token** | 60 minutos | 24 horas |
| **Login Requerido** | Cada hora | Cada 30 días |
| **Experiencia del Usuario** | Interrupciones frecuentes | Sesión continua |
| **Seguridad** | Media (tokens largos) | Alta (tokens cortos + refresh) |
| **Renovación** | Manual (login) | Automática (transparente) |

---

## ✅ Ventajas del Sistema

1. **Seguridad Mejorada**
   - Access Tokens de corta duración (24 horas)
   - Refresh Tokens validados en cada renovación
   - Validación de estado de usuario/barbero/empleado

2. **Mejor Experiencia de Usuario**
   - Sesión activa hasta 30 días
   - Renovación automática transparente
   - Sin interrupciones frecuentes

3. **Menor Carga en el Servidor**
   - Menos llamadas de login
   - Refresh más eficiente que login completo

4. **Flexibilidad**
   - Configuración fácil (appsettings.json)
   - Fácil ajustar duraciones según necesidades

---

## 🚨 Casos Especiales

### Usuario Desactivado
- **Durante Login**: Rechazado con mensaje específico
- **Durante Refresh**: Rechazado, debe reactivar cuenta

### Barber Eliminado/Desactivado
- **Durante Login**: Rechazado con mensaje específico
- **Durante Refresh**: Rechazado, debe contactar administrador

### Employee con Barber Dueño Eliminado/Desactivado
- **Durante Login**: Rechazado con mensaje específico
- **Durante Refresh**: Rechazado, el barbero dueño debe reactivarse

---

## 📝 Notas Importantes

1. **El Refresh Token se renueva en cada refresh**: Esto extiende la sesión hasta 30 días desde el último uso activo.

2. **Validación en tiempo real**: Cada refresh valida el estado actual del usuario, barbero y empleado en la base de datos.

3. **Tokens no son revocables**: Si un token es comprometido, expirará automáticamente. Para revocación inmediata, se debe desactivar el usuario.

4. **Frontend debe manejar errores**: El frontend debe implementar correctamente el interceptor para manejar renovaciones automáticas.

---

## 🔄 Próximos Pasos Recomendados

1. **Implementar revocación de tokens**: Tabla en BD para tokens revocados
2. **Rate limiting en refresh**: Limitar intentos de refresh por minuto
3. **Logs de seguridad**: Registrar todos los refreshes para auditoría
4. **Notificaciones**: Alertar al usuario cuando su sesión está por expirar

---

## 📞 Soporte

Para más información sobre la implementación técnica, consultar:
- `Services/Implementations/AuthService.cs`
- `Controllers/Api/AuthController.cs`
- `Utils/JwtHelper.cs`
- `Models/DTOs/Responses/LoginResponse.cs`

---

**Documento generado**: Enero 2026  
**Versión del Sistema**: BarberNic v1.0  
**Última actualización**: Configuración de Access Token a 24 horas

