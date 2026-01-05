/**
 * POS Utilities
 * Funciones helper y utilitarias compartidas
 */

/**
 * Formatea un precio con separador de miles y siempre 2 decimales (estándar contable).
 * Ejemplos: 500 → "500.00", 500.50 → "500.50", 1500 → "1,500.00", 15000 → "15,000.00"
 * @param {number} precio - Precio a formatear
 * @returns {string} Precio formateado
 */
function formatearPrecio(precio) {
    if (precio === null || precio === undefined || isNaN(precio)) {
        return '0.00';
    }
    
    // Formato estándar: separador de miles (coma) y siempre 2 decimales (punto)
    return precio.toLocaleString('en-US', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
}

/**
 * Formatea un precio con el prefijo "C$ " con separador de miles y siempre 2 decimales.
 * Ejemplos: 500 → "C$ 500.00", 500.50 → "C$ 500.50", 1500 → "C$ 1,500.00"
 * @param {number} precio - Precio a formatear
 * @returns {string} Precio formateado con símbolo
 */
function formatearPrecioConSimbolo(precio) {
    return `C$ ${formatearPrecio(precio)}`;
}

/**
 * Formatea un precio en dólares con separador de miles y siempre 2 decimales.
 * Ejemplos: 500 → "$ 500.00", 500.50 → "$ 500.50", 1500 → "$ 1,500.00"
 * @param {number} dolares - Cantidad en dólares a formatear
 * @returns {string} Dólares formateados con símbolo
 */
function formatearDolares(dolares) {
    if (dolares === null || dolares === undefined || isNaN(dolares)) {
        return '$ 0.00';
    }
    return `$ ${formatearPrecio(dolares)}`;
}

/**
 * Obtener elemento del DOM de forma segura
 * @param {string} id - ID del elemento
 * @param {string|null} errorMessage - Mensaje de error opcional si no se encuentra
 * @returns {HTMLElement|null} Elemento encontrado o null
 */
function getElementSafely(id, errorMessage = null) {
    const element = document.getElementById(id);
    if (!element && errorMessage) {
        console.error(errorMessage || `Elemento con ID '${id}' no encontrado`);
    }
    return element;
}

/**
 * Actualizar texto del botón según el contexto (venta directa vs orden)
 */
function actualizarTextoBoton() {
    const btnTexto = getElementSafely('btnProcesarTexto');
    if (btnTexto) {
        if (ordenSeleccionada) {
            btnTexto.textContent = 'Facturar y Cobrar Orden';
        } else {
            btnTexto.textContent = 'Procesar Venta';
        }
    }
}

/**
 * Validar pago parcial antes de procesar venta
 * @param {number} montoRecibido - Monto recibido del cliente
 * @param {number} total - Total de la venta/orden
 * @returns {Promise<boolean>} true si se confirma el pago parcial, false si se cancela
 */
async function validarPagoParcial(montoRecibido, total) {
    if (montoRecibido >= total) {
        return true; // No es pago parcial
    }

    const saldoPendiente = total - montoRecibido;
    const mensaje = 
        `⚠️ <strong>ADVERTENCIA: Pago Parcial</strong><br><br>` +
        `El monto recibido (<strong>${formatearPrecioConSimbolo(montoRecibido)}</strong>) es menor al total (<strong>${formatearPrecioConSimbolo(total)}</strong>).<br><br>` +
        `Se registrará un <strong>PAGO PARCIAL</strong> de <strong>${formatearPrecioConSimbolo(montoRecibido)}</strong>.<br>` +
        `Saldo pendiente: <strong>${formatearPrecioConSimbolo(saldoPendiente)}</strong><br><br>` +
        `La factura quedará como <strong>PENDIENTE</strong> hasta que se complete el pago.<br><br>` +
        `¿Desea continuar con el pago parcial?`;
    
    return await showConfirm(mensaje, '⚠️ Pago Parcial');
}

/**
 * Limpiar formulario y estado después de procesar una venta
 * Limpia campos de pago, vuelto, cliente, y oculta elementos relacionados
 */
function limpiarFormularioVenta() {
    // Limpiar campos de formulario
    const montoRecibidoEl = getElementSafely('montoRecibido');
    const clienteIdEl = getElementSafely('clienteId');
    if (montoRecibidoEl) montoRecibidoEl.value = '';
    if (clienteIdEl) clienteIdEl.value = '0';
    
    // Limpiar campos de pago mixto si existen
    const montoFisicoEl = getElementSafely('montoFisico');
    const montoElectronicoEl = getElementSafely('montoElectronico');
    const monedaFisicoEl = getElementSafely('monedaFisico');
    const monedaElectronicoEl = getElementSafely('monedaElectronico');
    if (montoFisicoEl) montoFisicoEl.value = '';
    if (montoElectronicoEl) montoElectronicoEl.value = '';
    if (monedaFisicoEl) monedaFisicoEl.value = 'C$';
    if (monedaElectronicoEl) monedaElectronicoEl.value = 'C$';
    
    // Limpiar y ocultar vuelto
    const vueltoContainer = getElementSafely('vueltoContainer');
    const vueltoEl = getElementSafely('vuelto');
    if (vueltoContainer) vueltoContainer.classList.add('hidden');
    if (vueltoEl) vueltoEl.textContent = 'C$ 0.00';
    
    // Ocultar información de conversión
    const conversionInfoEl = getElementSafely('conversionInfo');
    if (conversionInfoEl) conversionInfoEl.classList.add('hidden');
    
    // Ocultar banner de mesa seleccionada
    const mesaInfoEl = getElementSafely('mesaSeleccionadaInfo');
    if (mesaInfoEl) mesaInfoEl.classList.add('hidden');
}

/**
 * Convertir moneda de Córdobas a Dólares
 * @param {number} montoCordobas - Monto en córdobas
 * @param {number} tipoCambio - Tipo de cambio (C$ por $1)
 * @returns {number} Monto en dólares
 */
function convertirCordobasADolares(montoCordobas, tipoCambio) {
    if (!tipoCambio || tipoCambio <= 0) return 0;
    return montoCordobas / tipoCambio;
}

/**
 * Convertir moneda de Dólares a Córdobas
 * @param {number} montoDolares - Monto en dólares
 * @param {number} tipoCambio - Tipo de cambio (C$ por $1)
 * @returns {number} Monto en córdobas
 */
function convertirDolaresACordobas(montoDolares, tipoCambio) {
    if (!tipoCambio || tipoCambio <= 0) return 0;
    return montoDolares * tipoCambio;
}

/**
 * Convertir un monto de una moneda a otra
 * @param {number} monto - Monto a convertir
 * @param {string} monedaOrigen - Moneda origen ("C$" o "$")
 * @param {string} monedaDestino - Moneda destino ("C$" o "$")
 * @param {number} tipoCambio - Tipo de cambio
 * @returns {number} Monto convertido
 */
function convertirMoneda(monto, monedaOrigen, monedaDestino, tipoCambio) {
    if (monedaOrigen === monedaDestino) return monto;
    
    if (monedaOrigen === 'C$' && monedaDestino === '$') {
        return convertirCordobasADolares(monto, tipoCambio);
    } else if (monedaOrigen === '$' && monedaDestino === 'C$') {
        return convertirDolaresACordobas(monto, tipoCambio);
    }
    
    return monto;
}

/**
 * Obtener el total del carrito en una moneda específica
 * @param {string} monedaDestino - Moneda destino ("C$" o "$")
 * @param {number} tipoCambio - Tipo de cambio
 * @returns {number} Total en la moneda especificada
 */
function obtenerTotalEnMoneda(monedaDestino, tipoCambio) {
    const totalCordobas = ordenSeleccionada 
        ? ordenSeleccionada.total 
        : carrito.reduce((sum, item) => sum + ((item.precioUnitario || 0) * (item.cantidad || 0)), 0);
    
    if (monedaDestino === 'C$') {
        return totalCordobas;
    } else if (monedaDestino === '$') {
        return convertirCordobasADolares(totalCordobas, tipoCambio);
    }
    
    return totalCordobas;
}

/**
 * Actualizar información de conversión de moneda en la UI
 * @param {string} monedaSeleccionada - Moneda seleccionada
 * @param {number} totalCordobas - Total en córdobas
 */
function actualizarInfoConversion(monedaSeleccionada, totalCordobas) {
    const conversionInfoEl = getElementSafely('conversionInfo');
    if (!conversionInfoEl) return;
    
    if (tipoCambio > 0) {
        const totalDolares = convertirCordobasADolares(totalCordobas, tipoCambio);
        if (monedaSeleccionada === '$') {
            conversionInfoEl.textContent = `Total: ${formatearDolares(totalDolares)} (${formatearPrecioConSimbolo(totalCordobas)}) | TC: ${formatearPrecioConSimbolo(tipoCambio)} = $1`;
        } else {
            conversionInfoEl.textContent = `Total: ${formatearPrecioConSimbolo(totalCordobas)} (${formatearDolares(totalDolares)}) | TC: ${formatearPrecioConSimbolo(tipoCambio)} = $1`;
        }
        conversionInfoEl.classList.remove('hidden');
    } else {
        conversionInfoEl.classList.add('hidden');
    }
}

/**
 * Actualizar formulario de pago según el tipo seleccionado
 */
function actualizarFormularioPago() {
    const tipoPagoEl = getElementSafely('tipoPago');
    const monedaEl = getElementSafely('moneda');
    const pagoMixtoContainer = getElementSafely('pagoMixtoContainer');
    const montoRecibidoContainer = getElementSafely('montoRecibidoContainer');
    
    if (!tipoPagoEl) return;
    
    const tipoPago = tipoPagoEl.value;
    
    if (tipoPago === 'Mixto') {
        // Mostrar campos de pago mixto
        if (pagoMixtoContainer) pagoMixtoContainer.classList.remove('hidden');
        if (montoRecibidoContainer) montoRecibidoContainer.classList.add('hidden');
    } else {
        // Ocultar campos de pago mixto
        if (pagoMixtoContainer) pagoMixtoContainer.classList.add('hidden');
        if (montoRecibidoContainer) {
            if (tipoPago === 'Fisico') {
                montoRecibidoContainer.classList.remove('hidden');
            } else {
                montoRecibidoContainer.classList.add('hidden');
            }
        }
    }
    
    // Actualizar label del monto recibido según moneda
    if (monedaEl && tipoPago === 'Fisico') {
        if (typeof actualizarLabelMontoRecibido === 'function') {
            actualizarLabelMontoRecibido(monedaEl.value);
        }
    }
    
    // Recalcular vuelto
    calcularVuelto();
}

