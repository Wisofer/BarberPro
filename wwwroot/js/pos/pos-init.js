/**
 * POS Initialization
 * Inicialización y configuración de event listeners
 */

/**
 * Inicializar POS
 */
async function inicializarPOS() {
    // Inicializar búsqueda
    inicializarBusqueda();

    // Cargar badges de mesas desde BD
    if (window.cargarBadgesMesas) {
        await cargarBadgesMesas();
    }

    // Configurar event listeners para cálculo de vuelto
    const montoRecibidoEl = getElementSafely('montoRecibido');
    const tipoPagoEl = getElementSafely('tipoPago');
    const monedaEl = getElementSafely('moneda');
    if (montoRecibidoEl) montoRecibidoEl.addEventListener('input', calcularVuelto);
    if (tipoPagoEl) tipoPagoEl.addEventListener('change', () => {
        actualizarFormularioPago();
        calcularVuelto();
    });
    if (monedaEl) monedaEl.addEventListener('change', () => {
        calcularVuelto();
        actualizarFormularioPago();
        // Actualizar label del monto recibido
        if (typeof actualizarLabelMontoRecibido === 'function') {
            actualizarLabelMontoRecibido(monedaEl.value);
        }
    });
    
    // Actualizar formulario al cargar
    actualizarFormularioPago();

    // Inicializar texto del botón al cargar
    actualizarTextoBoton();

    // Hacer funciones globales para que funcionen con onclick
    // Exponer funciones globalmente para uso con onclick en HTML
    window.agregarProductoAlCarrito = agregarProductoAlCarrito;
    window.agregarServicioAlCarrito = agregarServicioAlCarrito;
    window.cambiarPestaña = cambiarPestaña;
    window.procesarVenta = procesarVenta;
    window.facturarOrden = facturarOrden;
    window.limpiarCarrito = function() {
        limpiarCarrito().catch(err => {
            console.error('Error al limpiar carrito:', err);
            Notify.error('Error al limpiar el carrito');
        });
    };
    window.mostrarModalImpresion = mostrarModalImpresion;
    window.cerrarModalImpresion = cerrarModalImpresion;
    window.imprimirTicket = imprimirTicket;
    window.seleccionarOrden = seleccionarOrden;
    window.cambiarCantidad = cambiarCantidad;
    window.eliminarDelCarrito = eliminarDelCarrito;
    
    // Exponer funciones utilitarias
    window.validarPagoParcial = validarPagoParcial;
    window.limpiarFormularioVenta = limpiarFormularioVenta;
    window.actualizarFormularioPago = actualizarFormularioPago;
    window.convertirCordobasADolares = convertirCordobasADolares;
    window.convertirDolaresACordobas = convertirDolaresACordobas;
    window.convertirMoneda = convertirMoneda;
    window.actualizarLabelMontoRecibido = actualizarLabelMontoRecibido;
    
    // Inicializar label del monto recibido
    if (monedaEl && typeof actualizarLabelMontoRecibido === 'function') {
        actualizarLabelMontoRecibido(monedaEl.value);
    }
}

// Ejecutar inicialización cuando el DOM esté listo
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', inicializarPOS);
} else {
    inicializarPOS();
}

