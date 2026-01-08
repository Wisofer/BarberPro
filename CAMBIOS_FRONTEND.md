# Cambios Requeridos en el Frontend - BarberNic

## 📋 Resumen

El backend ha implementado mejoras importantes que requieren actualizaciones en el frontend para aprovechar todas las funcionalidades.

---

## 🔐 1. Sistema de Refresh Tokens (CRÍTICO)

### Cambios en el Backend
- El login ahora devuelve `refreshToken` además del `token`
- Nuevo endpoint: `POST /api/auth/refresh`
- Access Token ahora dura **24 horas** (antes 1 hora)
- Refresh Token dura **30 días**

### Cambios Requeridos en el Frontend

#### 1.1. Actualizar el modelo de LoginResponse
```dart
class LoginResponse {
  String token;           // Access Token (24 horas)
  String refreshToken;   // ⚠️ NUEVO - Refresh Token (30 días)
  UserDto user;
  String role;
}
```

#### 1.2. Almacenar el Refresh Token
```dart
// Al hacer login, guardar AMBOS tokens
await storage.write(key: 'accessToken', value: response.token);
await storage.write(key: 'refreshToken', value: response.refreshToken); // ⚠️ NUEVO
```

#### 1.3. Implementar Interceptor para Renovación Automática
```dart
// Interceptor HTTP para manejar tokens expirados
class AuthInterceptor extends Interceptor {
  @override
  void onError(DioException err, ErrorInterceptorHandler handler) async {
    if (err.response?.statusCode == 401) {
      // Token expirado, intentar refresh
      final refreshToken = await storage.read(key: 'refreshToken');
      
      if (refreshToken != null) {
        try {
          // Llamar al endpoint de refresh
          final response = await dio.post(
            '/api/auth/refresh',
            data: {'refreshToken': refreshToken},
          );
          
          // Actualizar tokens
          await storage.write(key: 'accessToken', value: response.data['token']);
          await storage.write(key: 'refreshToken', value: response.data['refreshToken']);
          
          // Reintentar petición original
          final opts = err.requestOptions;
          opts.headers['Authorization'] = 'Bearer ${response.data['token']}';
          final retryResponse = await dio.request(opts.path, options: Options(method: opts.method));
          return handler.resolve(retryResponse);
        } catch (e) {
          // Refresh falló, redirigir a login
          await storage.deleteAll();
          navigatorKey.currentState?.pushReplacementNamed('/login');
        }
      }
    }
    return handler.next(err);
  }
}
```

#### 1.4. Agregar Token a Peticiones
```dart
// Interceptor para agregar token a todas las peticiones
class TokenInterceptor extends Interceptor {
  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) async {
    final token = await storage.read(key: 'accessToken');
    if (token != null) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    return handler.next(options);
  }
}
```

### Beneficios
- ✅ Usuario puede trabajar hasta 30 días sin hacer login
- ✅ Renovación automática transparente
- ✅ Mejor experiencia de usuario

---

## 📅 2. Endpoints de Citas Mejorados

### Nuevos Endpoints Disponibles

#### Para Barberos:
- `GET /api/barber/appointments` - Todas las citas (sin filtro) o con filtros
- `GET /api/barber/appointments/history` - Historial completo (nuevo)
- `GET /api/barber/appointments?date=YYYY-MM-DD` - Citas de una fecha
- `GET /api/barber/appointments?startDate=YYYY-MM-DD&endDate=YYYY-MM-DD` - Rango de fechas

#### Para Empleados:
- `GET /api/employee/appointments` - Todas las citas del barbero dueño
- `GET /api/employee/appointments/history` - Historial completo (nuevo)
- `GET /api/employee/appointments?date=YYYY-MM-DD` - Citas de una fecha
- `GET /api/employee/appointments?startDate=YYYY-MM-DD&endDate=YYYY-MM-DD` - Rango de fechas

### Cambios Recomendados en el Frontend

#### 2.1. Tab "Hoy" (Citas de Hoy)
```dart
// Usar fecha específica
final today = DateTime.now().toIso8601String().split('T')[0];
final response = await dio.get(
  '/api/barber/appointments?date=$today',
);
```

#### 2.2. Tab "Historial" (Todas las Citas)
```dart
// Opción 1: Usar endpoint específico de historial
final response = await dio.get('/api/barber/appointments/history');

// Opción 2: Sin parámetros (también funciona)
final response = await dio.get('/api/barber/appointments');
```

#### 2.3. Filtro por Rango de Fechas
```dart
// Último mes
final startDate = DateTime.now().subtract(Duration(days: 30))
    .toIso8601String().split('T')[0];
final endDate = DateTime.now().toIso8601String().split('T')[0];

final response = await dio.get(
  '/api/barber/appointments?startDate=$startDate&endDate=$endDate',
);
```

### Beneficios
- ✅ Pueden mostrar historial completo de citas
- ✅ Filtros flexibles por fecha o rango
- ✅ Mejor organización de datos

---

## 📊 3. Exportaciones (Sin Cambios Necesarios)

Las exportaciones funcionan igual, solo mejoradas en el backend:
- `GET /api/barber/export/appointments?format=csv|excel|pdf`
- `GET /api/barber/export/finances?format=csv|excel|pdf`
- `GET /api/barber/export/clients?format=csv|excel|pdf`

**No se requieren cambios en el frontend** - Los endpoints funcionan igual.

---

## ✅ Resumen de Cambios Requeridos

### Críticos (Deben implementarse):
1. ✅ **Actualizar LoginResponse** para incluir `refreshToken`
2. ✅ **Almacenar refreshToken** al hacer login
3. ✅ **Implementar interceptor** para renovación automática de tokens
4. ✅ **Manejar errores 401** con refresh automático

### Opcionales (Mejoran la experiencia):
5. ⚠️ **Usar nuevos endpoints de historial** para mostrar todas las citas
6. ⚠️ **Implementar filtros por rango de fechas** si es necesario

### Sin Cambios:
- ❌ Exportaciones (funcionan igual)
- ❌ Crear citas (funcionan igual)
- ❌ Otros endpoints (sin cambios)

---

## 📝 Notas Importantes

1. **Refresh Token es OBLIGATORIO**: Sin él, los usuarios tendrán que hacer login cada 24 horas
2. **Backward Compatibility**: Los endpoints antiguos siguen funcionando, pero se recomienda usar los nuevos
3. **Manejo de Errores**: El refresh puede fallar si el token expiró (30 días), en ese caso redirigir a login

---

## 🔗 Documentación Técnica

Para más detalles sobre el sistema de refresh tokens, consultar:
- `SISTEMA_AUTENTICACION_REFRESH_TOKENS.md`

---

**Fecha de Actualización**: Enero 2026  
**Versión Backend**: BarberNic v1.0

