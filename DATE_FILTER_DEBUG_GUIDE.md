# 🔍 Guía de Debug: Filtro de Fechas

## 📋 Problema

El filtro de fechas con formato ISO (`2026-01-06T00:00:00` a `2026-01-06T23:59:59`) está devolviendo registros del día siguiente (7 de enero) cuando no debería.

## 🔧 Logs de Debug Agregados

Se han agregado logs temporales en `FinanceService.GetIncomeAsync` para depurar el problema.

### Logs que verás:

```
[DEBUG] GetIncomeAsync - startDate original: {fecha} (Kind: {tipo})
[DEBUG] GetIncomeAsync - normalizedStart: {fecha} (Kind: {tipo})
[DEBUG] GetIncomeAsync - endDate original: {fecha} (Kind: {tipo})
[DEBUG] GetIncomeAsync - normalizedEnd: {fecha} (Kind: {tipo})
[DEBUG] GetIncomeAsync - nextDayStart: {fecha} (Kind: {tipo})
```

## 📍 Dónde Ver los Logs

### En Desarrollo Local:
- **Consola donde corre `dotnet watch run`**
- Busca líneas que empiecen con `[DEBUG]`

### En Producción (Dokploy):
1. Ve a tu proyecto en Dokploy
2. Haz clic en el contenedor de la aplicación
3. Ve a la pestaña **"Logs"** o **"Console"**
4. Busca líneas que empiecen con `[DEBUG] GetIncomeAsync`

## 🧪 Cómo Probar

### Request de prueba:
```bash
GET /api/barber/finances/income?startDate=2026-01-06T00:00:00&endDate=2026-01-06T23:59:59
```

### Qué buscar en los logs:

1. **startDate original**: Debe ser `2026-01-06T00:00:00` (o similar)
2. **normalizedStart**: Debe ser `2026-01-06T00:00:00 UTC`
3. **endDate original**: Debe ser `2026-01-06T23:59:59` (o similar)
4. **normalizedEnd**: Debe ser `2026-01-06T23:59:59 UTC` (o `2026-01-06T23:59:59.999 UTC`)
5. **nextDayStart**: Debe ser `2026-01-07T00:00:00 UTC`

### ⚠️ Problema Esperado:

Si ves que `endDate original` tiene un `Kind` diferente a `Utc`, o si `normalizedEnd` tiene una fecha diferente a la esperada, ese es el problema.

**Ejemplo de problema:**
```
endDate original: 2026-01-06T23:59:59 (Kind: Local)
normalizedEnd: 2026-01-07T05:59:59 (Kind: Utc)  ← PROBLEMA: Cambió el día
```

## 🔍 Análisis de Zona Horaria

### Verificar zona horaria del servidor/contenedor:

En Dokploy, puedes ejecutar en el contenedor:
```bash
date
timedatectl
```

O verificar en los logs si hay información de zona horaria.

## 📝 Qué Hacer con los Logs

1. **Copia los logs** de las líneas `[DEBUG] GetIncomeAsync`
2. **Verifica**:
   - ¿El `Kind` de las fechas originales es `Unspecified`, `Local` o `Utc`?
   - ¿Las fechas normalizadas tienen el día correcto?
   - ¿`nextDayStart` es el inicio del día siguiente correcto?

3. **Comparte los logs** para ajustar la solución

## 🎯 Solución Esperada

Después de revisar los logs, podremos:
- Ajustar la normalización según la zona horaria real
- Corregir el parseo de fechas en el controlador
- Asegurar que todas las comparaciones sean en UTC

## ⚠️ Nota

Estos logs son **temporales** y se pueden quitar después de resolver el problema.

---

**Fecha:** 2026-01-06
**Ambiente:** VPS con Dokploy (Docker)

