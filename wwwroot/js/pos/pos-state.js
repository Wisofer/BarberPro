/**
 * POS State Management
 * Variables de estado globales del punto de venta y gestión de almacenamiento
 * 
 * ESTRATEGIA DE ALMACENAMIENTO (Storage Strategy):
 * ================================================
 * 
 * Los carritos de mesas se almacenan con una estrategia híbrida:
 * 
 * 1. BASE DE DATOS (Fuente de verdad - Source of Truth):
 *    - Los carritos se persisten en la tabla CarritosMesa
 *    - Se guardan automáticamente cada vez que se modifica el carrito
 *    - Permite persistencia entre sesiones y sincronización entre dispositivos
 * 
 * 2. SESSIONSTORAGE (Caché local - Local Cache):
 *    - Se usa como caché para acceso rápido y offline
 *    - Se sincroniza con BD cuando hay conexión
 *    - Se limpia automáticamente al cerrar la pestaña/navegador
 *    - Clave: 'pos_carritos_mesas' -> { mesaId: [items...] }
 * 
 * 3. FLUJO DE SINCRONIZACIÓN:
 *    - Al guardar: sessionStorage -> BD (ambos se actualizan)
 *    - Al cargar: BD -> sessionStorage (si no hay en cache o se fuerza desde BD)
 *    - Fallback: Si falla BD, usa sessionStorage
 * 
 * NOTA: Esta estrategia permite:
 *    - Mejor rendimiento (caché local)
 *    - Persistencia confiable (BD)
 *    - Funcionamiento offline básico (sessionStorage)
 *    - Sincronización multi-dispositivo (BD)
 */

// Estado del carrito (se guarda por mesa)
let carrito = [];

// Estado de la pestaña actual
let pestañaActual = 'productos';

// Orden seleccionada para facturar
let ordenSeleccionada = null;

// Mesa seleccionada actualmente (null si no hay mesa seleccionada)
let mesaSeleccionada = null;

// Tipo de cambio (se obtiene dinámicamente del servidor)
let tipoCambio = 36.80; // Valor por defecto, se actualiza al inicializar

/**
 * Obtener tipo de cambio desde el servidor (desde la base de datos)
 * El tipo de cambio se almacena en la tabla Configuraciones con clave "TipoCambioDolar"
 * y puede ser actualizado por el usuario desde la sección de Configuraciones
 */
async function obtenerTipoCambio() {
    try {
        const response = await fetch('/pos/tipo-cambio');
        const data = await response.json();
        if (data.success && data.tipoCambio) {
            const nuevoTipoCambio = parseFloat(data.tipoCambio);
            // Validar que no sea NaN ni menor o igual a 0
            if (!isNaN(nuevoTipoCambio) && nuevoTipoCambio > 0) {
                tipoCambio = nuevoTipoCambio;
                return tipoCambio;
            } else {
                console.warn('Tipo de cambio inválido desde BD, usando valor por defecto:', data.tipoCambio);
            }
        } else {
            console.warn('No se pudo obtener tipo de cambio desde BD, usando valor por defecto:', data.message);
        }
    } catch (error) {
        console.warn('Error al obtener tipo de cambio desde BD, usando valor por defecto:', error);
    }
    return tipoCambio;
}

// Cargar tipo de cambio desde la base de datos al inicializar
obtenerTipoCambio();

// Variable global para guardar el ID de la factura para impresión
let facturaIdParaImprimir = null;

// Clave para sessionStorage de carritos por mesa
const STORAGE_KEY_CARRITOS_MESAS = 'pos_carritos_mesas';

/**
 * Guardar carrito de una mesa en sessionStorage Y en base de datos
 * 
 * @param {number} mesaId - ID de la mesa
 * @param {Array} carritoData - Array de items del carrito
 * 
 * ESTRATEGIA: Guarda primero en sessionStorage (rápido), luego en BD (persistente).
 * Si falla BD, al menos se guardó en caché local.
 */
async function guardarCarritoMesa(mesaId, carritoData) {
    try {
        // Guardar en sessionStorage (caché local)
        const carritosMesas = obtenerCarritosMesas();
        if (carritoData && carritoData.length > 0) {
            carritosMesas[mesaId] = carritoData;
        } else {
            // Si el carrito está vacío, eliminarlo
            delete carritosMesas[mesaId];
        }
        sessionStorage.setItem(STORAGE_KEY_CARRITOS_MESAS, JSON.stringify(carritosMesas));

        // Guardar en base de datos
        const clienteId = parseInt(getElementSafely('clienteId')?.value || '0') || 0;
        
        // Convertir carrito a formato del backend
        const items = carritoData.map(item => ({
            Tipo: item.tipo === 'servicio' ? 'Servicio' : 'Producto',
            ProductoId: item.productoId || null,
            ServicioTallerId: item.servicioId || null,
            Cantidad: item.cantidad || 1,
            PrecioUnitario: item.precioUnitario || 0,
            PrecioUnitarioOriginal: item.precioUnitarioOriginal || item.precioUnitario || 0,
            DescuentoAplicado: item.descuentoAplicado || 0,
            PorcentajeDescuento: item.porcentajeDescuento || null
        }));

        const response = await fetch('/pos/guardar-carrito-mesa', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify({
                MesaId: mesaId,
                ClienteId: clienteId,
                Items: items
            })
        });

        if (!response.ok) {
            console.warn('Error al guardar carrito en BD, pero se guardó en caché local');
        }
    } catch (e) {
        console.error('Error al guardar carrito de mesa:', e);
        // No fallar si hay error, al menos se guardó en sessionStorage
    }
}

/**
 * Obtener carrito de una mesa desde sessionStorage o base de datos
 * 
 * @param {number} mesaId - ID de la mesa
 * @param {boolean} desdeBD - Si es true, fuerza carga desde BD (ignora caché)
 * @returns {Promise<Array>} Array de items del carrito
 * 
 * ESTRATEGIA:
 * - Si desdeBD=true: Carga desde BD -> Guarda en sessionStorage -> Retorna
 * - Si desdeBD=false: Intenta sessionStorage primero -> Si no existe, carga desde BD -> Guarda en caché -> Retorna
 * - Si falla BD: Retorna desde sessionStorage como fallback
 */
async function obtenerCarritoMesa(mesaId, desdeBD = false) {
    try {
        // Si se solicita desde BD o no hay en sessionStorage, cargar desde BD
        if (desdeBD) {
            const response = await fetch(`/pos/carrito-mesa/${mesaId}`);
            const data = await response.json();
            
            if (data.success && data.items && data.items.length > 0) {
                // Convertir items de BD a formato del frontend
                const carrito = data.items.map(item => ({
                    tipo: item.tipo.toLowerCase() === 'servicio' ? 'servicio' : 'producto',
                    productoId: item.productoId,
                    servicioId: item.servicioId,
                    nombre: item.nombre,
                    cantidad: item.cantidad,
                    precioUnitario: item.precioUnitario,
                    precioUnitarioOriginal: item.precioUnitarioOriginal,
                    descuentoAplicado: item.descuentoAplicado,
                    porcentajeDescuento: item.porcentajeDescuento,
                    stock: null // Se actualizará al cargar
                }));

                // Guardar en sessionStorage para caché
                const carritosMesas = obtenerCarritosMesas();
                carritosMesas[mesaId] = carrito;
                sessionStorage.setItem(STORAGE_KEY_CARRITOS_MESAS, JSON.stringify(carritosMesas));

                return carrito;
            }
        }

        // Intentar desde sessionStorage primero
        const carritosMesas = obtenerCarritosMesas();
        if (carritosMesas[mesaId] && carritosMesas[mesaId].length > 0) {
            return carritosMesas[mesaId];
        }

        // Si no hay en sessionStorage, cargar desde BD
        const response = await fetch(`/pos/carrito-mesa/${mesaId}`);
        const data = await response.json();
        
        if (data.success && data.items && data.items.length > 0) {
            const carrito = data.items.map(item => ({
                tipo: item.tipo.toLowerCase() === 'servicio' ? 'servicio' : 'producto',
                productoId: item.productoId,
                servicioId: item.servicioId,
                nombre: item.nombre,
                cantidad: item.cantidad,
                precioUnitario: item.precioUnitario,
                precioUnitarioOriginal: item.precioUnitarioOriginal,
                descuentoAplicado: item.descuentoAplicado,
                porcentajeDescuento: item.porcentajeDescuento,
                stock: null
            }));

            // Guardar en sessionStorage
            carritosMesas[mesaId] = carrito;
            sessionStorage.setItem(STORAGE_KEY_CARRITOS_MESAS, JSON.stringify(carritosMesas));

            return carrito;
        }

        return [];
    } catch (e) {
        console.error('Error al obtener carrito de mesa:', e);
        // Fallback a sessionStorage
        try {
            const carritosMesas = obtenerCarritosMesas();
            return carritosMesas[mesaId] || [];
        } catch {
        return [];
        }
    }
}

/**
 * Obtener todos los carritos de mesas guardados
 */
function obtenerCarritosMesas() {
    try {
        const data = sessionStorage.getItem(STORAGE_KEY_CARRITOS_MESAS);
        return data ? JSON.parse(data) : {};
    } catch (e) {
        console.error('Error al obtener carritos de mesas:', e);
        return {};
    }
}

/**
 * Obtener cantidad de items en el carrito de una mesa desde sessionStorage (síncrono)
 * NOTA: Esta función accede directamente a sessionStorage para uso en contextos síncronos.
 * Para obtener datos actualizados desde BD, usar obtenerCarritoMesa(mesaId) y calcular manualmente.
 */
function obtenerCantidadItemsMesa(mesaId) {
    try {
        const carritosMesas = obtenerCarritosMesas();
        const carritoMesa = carritosMesas[mesaId] || [];
        return carritoMesa.reduce((sum, item) => sum + (item.cantidad || 0), 0);
    } catch (e) {
        console.error('Error al obtener cantidad de items de mesa:', e);
        return 0;
    }
}

/**
 * Calcular total pendiente del carrito de una mesa desde sessionStorage (síncrono)
 * NOTA: Esta función accede directamente a sessionStorage para uso en contextos síncronos.
 * Para obtener datos actualizados desde BD, usar obtenerCarritoMesa(mesaId) y calcular manualmente.
 */
function obtenerTotalPendienteMesa(mesaId) {
    try {
        const carritosMesas = obtenerCarritosMesas();
        const carritoMesa = carritosMesas[mesaId] || [];
        return carritoMesa.reduce((sum, item) => {
            const precio = item.precioUnitario || 0;
            const cantidad = item.cantidad || 0;
            return sum + (precio * cantidad);
        }, 0);
    } catch (e) {
        console.error('Error al obtener total pendiente de mesa:', e);
        return 0;
    }
}

// Hacer funciones globales
window.guardarCarritoMesa = guardarCarritoMesa;
window.obtenerCarritoMesa = obtenerCarritoMesa;
window.obtenerCantidadItemsMesa = obtenerCantidadItemsMesa;
window.obtenerTotalPendienteMesa = obtenerTotalPendienteMesa;

