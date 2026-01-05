/**
 * POS Price Adjustment Management
 * Manejo de ajustes de precio manuales en el carrito
 * 
 * RESPONSABILIDADES:
 * - Aplicar descuentos o aumentos manuales a items del carrito
 * - Calcular descuentos/aumentos por monto fijo o porcentaje
 * - Permitir tanto reducciones como aumentos de precio
 * - Gestionar modal de ajuste de precios
 * 
 * NOTA: Los descuentos por promociones se gestionan automáticamente en pos-cart.js
 */

let itemDescuentoActual = null; // Índice del item al que se le está aplicando descuento
let timeoutCalculo = null; // Timeout para evitar cálculos mientras el usuario escribe

/**
 * Abrir modal de descuento para un item del carrito
 */
function abrirModalDescuento(itemIndex) {
    itemDescuentoActual = itemIndex;
    const item = carrito[itemIndex];
    if (!item) {
        Notify.error('Error: Item no encontrado en el carrito');
        return;
    }

    const precioOriginal = item.precioUnitarioOriginal || item.precioUnitario;
    const precioActual = item.precioUnitario;
    const descuentoActual = item.descuentoAplicado || 0;
    const porcentajeActual = item.porcentajeDescuento || null;

    // Llenar campos del modal
    const modalNombreEl = getElementSafely('modalDescuentoNombre');
    const modalPrecioOriginalEl = getElementSafely('modalDescuentoPrecioOriginal');
    const inputDescuentoEl = getElementSafely('inputDescuento');
    const resultadoDescuentoEl = getElementSafely('resultadoDescuento');
    const resultadoPrecioFinalEl = getElementSafely('resultadoPrecioFinal');

    if (modalNombreEl) modalNombreEl.textContent = item.nombre;
    if (modalPrecioOriginalEl) modalPrecioOriginalEl.textContent = formatearPrecioConSimbolo(precioOriginal);

    // Llenar campo con el precio actual (precio final)
    if (inputDescuentoEl) {
        inputDescuentoEl.value = precioActual.toFixed(2); // Mantener formato con decimales para input
    }

    // Calcular y mostrar resultado inicial
    calcularDescuentoDesdeCampoUnico();

    // Mostrar modal
    const modalEl = document.getElementById('modalDescuento');
    if (modalEl) {
        modalEl.classList.add('modal-open');
        
        // Focus en el campo de descuento
        setTimeout(() => {
            if (inputDescuentoEl) {
                inputDescuentoEl.focus();
                inputDescuentoEl.select();
            }
        }, 300);
    } else {
        console.error('Modal de descuento no encontrado en el DOM');
        Notify.error('Error: No se pudo abrir el modal de descuento');
    }
}

/**
 * Cerrar modal de descuento
 */
function cerrarModalDescuento() {
    const modalEl = document.getElementById('modalDescuento');
    if (modalEl) {
        modalEl.classList.remove('modal-open');
    }
    itemDescuentoActual = null;
    if (timeoutCalculo) {
        clearTimeout(timeoutCalculo);
        timeoutCalculo = null;
    }
}

/**
 * Calcular descuento desde el campo único
 * Acepta valores como: "10" (fijo) o "15%" (porcentaje)
 */
function calcularDescuentoDesdeCampoUnico() {
    // Limpiar timeout anterior si existe
    if (timeoutCalculo) {
        clearTimeout(timeoutCalculo);
    }
    
    // Ejecutar después de un pequeño delay para permitir que el usuario termine de escribir
    timeoutCalculo = setTimeout(() => {
        calcularDescuentoDesdeCampoUnicoReal();
    }, 150);
}

/**
 * Lógica real de cálculo de descuentos desde campo único
 * Ahora el input es el PRECIO FINAL directamente
 */
function calcularDescuentoDesdeCampoUnicoReal() {
    if (itemDescuentoActual === null) return;
    
    const item = carrito[itemDescuentoActual];
    if (!item) return;

    const precioOriginal = item.precioUnitarioOriginal || item.precioUnitario;
    const inputDescuentoEl = getElementSafely('inputDescuento');
    const resultadoDescuentoEl = getElementSafely('resultadoDescuento');
    const resultadoPrecioFinalEl = getElementSafely('resultadoPrecioFinal');

    if (!inputDescuentoEl) return;

    const valorInput = inputDescuentoEl.value.trim();
    
    // Si el campo está vacío, mostrar precio original
    if (!valorInput) {
        if (resultadoDescuentoEl) {
            resultadoDescuentoEl.textContent = '';
            resultadoDescuentoEl.classList.add('hidden');
        }
        if (resultadoPrecioFinalEl) {
            resultadoPrecioFinalEl.textContent = formatearPrecioConSimbolo(precioOriginal);
            resultadoPrecioFinalEl.classList.remove('text-success');
            resultadoPrecioFinalEl.classList.add('text-base-content');
        }
        return;
    }

    // Extraer número (remover espacios y caracteres no numéricos excepto punto)
    const numeroStr = valorInput.replace(/[^\d.]/g, '').trim();
    const precioFinal = parseFloat(numeroStr);

    // Validar que sea un número válido
    if (isNaN(precioFinal) || precioFinal < 0) {
        if (resultadoDescuentoEl) {
            resultadoDescuentoEl.textContent = '';
            resultadoDescuentoEl.classList.add('hidden');
        }
        if (resultadoPrecioFinalEl) {
            resultadoPrecioFinalEl.textContent = formatearPrecioConSimbolo(precioOriginal);
            resultadoPrecioFinalEl.classList.remove('text-success');
            resultadoPrecioFinalEl.classList.add('text-base-content');
        }
        return;
    }

    // Permitir tanto descuentos como aumentos de precio
    const precioFinalAplicado = precioFinal;
    
    // Calcular diferencia (puede ser descuento o aumento)
    let diferencia = precioFinalAplicado - precioOriginal;
    let porcentajeDiferencia = precioOriginal > 0 ? (diferencia / precioOriginal) * 100 : 0;

    // Actualizar resultado visual
    if (resultadoDescuentoEl) {
        if (diferencia < 0) {
            // Es un descuento
            const descuentoFijo = Math.abs(diferencia);
            resultadoDescuentoEl.textContent = `-${formatearPrecioConSimbolo(descuentoFijo)} (${Math.abs(porcentajeDiferencia).toFixed(1)}%)`;
            resultadoDescuentoEl.classList.remove('hidden', 'text-error');
            resultadoDescuentoEl.classList.add('text-success');
        } else if (diferencia > 0) {
            // Es un aumento
            resultadoDescuentoEl.textContent = `+${formatearPrecioConSimbolo(diferencia)} (+${porcentajeDiferencia.toFixed(1)}%)`;
            resultadoDescuentoEl.classList.remove('hidden', 'text-success');
            resultadoDescuentoEl.classList.add('text-warning');
        } else {
            // Sin cambio
            resultadoDescuentoEl.textContent = '';
            resultadoDescuentoEl.classList.add('hidden');
        }
    }
    
    if (resultadoPrecioFinalEl) {
        resultadoPrecioFinalEl.textContent = formatearPrecioConSimbolo(precioFinalAplicado);
        if (precioFinalAplicado < precioOriginal) {
            // Descuento aplicado
            resultadoPrecioFinalEl.classList.add('text-success');
            resultadoPrecioFinalEl.classList.remove('text-base-content', 'text-warning');
        } else if (precioFinalAplicado > precioOriginal) {
            // Aumento aplicado
            resultadoPrecioFinalEl.classList.add('text-warning');
            resultadoPrecioFinalEl.classList.remove('text-base-content', 'text-success');
        } else {
            // Sin cambio
            resultadoPrecioFinalEl.classList.remove('text-success', 'text-warning');
            resultadoPrecioFinalEl.classList.add('text-base-content');
        }
    }
}

/**
 * Aplicar descuento al item
 * Ahora el input es el PRECIO FINAL directamente
 */
function aplicarDescuento() {
    if (itemDescuentoActual === null) return;
    
    const item = carrito[itemDescuentoActual];
    if (!item) return;

    const inputDescuentoEl = getElementSafely('inputDescuento');
    if (!inputDescuentoEl) return;

    const valorInput = inputDescuentoEl.value.trim();
    
    // Si no hay valor, restaurar precio original
    if (!valorInput) {
        const precioOriginal = item.precioUnitarioOriginal || item.precioUnitario;
        item.precioUnitario = precioOriginal;
        item.descuentoAplicado = 0;
        item.porcentajeDescuento = null;
        item.aumentoAplicado = 0;
        item.porcentajeAumento = null;
        actualizarCarrito();
        guardarCarritoActual();
        cerrarModalDescuento();
        Notify.info('Precio restaurado al original');
        return;
    }

    const precioOriginal = item.precioUnitarioOriginal || item.precioUnitario;
    
    // El input ahora es el precio final directamente
    const numeroStr = valorInput.replace(/[^\d.]/g, '').trim();
    let precioFinal = parseFloat(numeroStr);

    // Validaciones
    if (isNaN(precioFinal) || precioFinal < 0) {
        Notify.warning('Debe ingresar un precio válido');
        return;
    }

    // Permitir tanto descuentos como aumentos de precio
    // Calcular diferencia (puede ser descuento o aumento)
    let diferencia = precioFinal - precioOriginal;
    let descuentoReal = diferencia < 0 ? Math.abs(diferencia) : 0;
    let aumentoReal = diferencia > 0 ? diferencia : 0;
    let porcentajeReal = precioOriginal > 0 ? (Math.abs(diferencia) / precioOriginal) * 100 : 0;

    // Aplicar al item
    item.precioUnitario = precioFinal;
    // Guardar descuento solo si hay descuento (negativo)
    item.descuentoAplicado = descuentoReal > 0 ? descuentoReal : 0;
    item.porcentajeDescuento = descuentoReal > 0 ? porcentajeReal : null;
    // Guardar aumento si hay aumento (positivo)
    item.aumentoAplicado = aumentoReal > 0 ? aumentoReal : 0;
    item.porcentajeAumento = aumentoReal > 0 ? porcentajeReal : null;

    // Asegurar que precioUnitarioOriginal esté definido
    if (!item.precioUnitarioOriginal) {
        item.precioUnitarioOriginal = precioOriginal;
    }

    // Actualizar carrito
    actualizarCarrito();
    guardarCarritoActual();

    // Cerrar modal
    cerrarModalDescuento();

    if (descuentoReal > 0) {
        Notify.success(`Precio ajustado: ${formatearPrecioConSimbolo(precioFinal)} (Descuento: -${formatearPrecioConSimbolo(descuentoReal)} - ${porcentajeReal.toFixed(1)}%)`);
    } else if (aumentoReal > 0) {
        Notify.success(`Precio ajustado: ${formatearPrecioConSimbolo(precioFinal)} (Aumento: +${formatearPrecioConSimbolo(aumentoReal)} +${porcentajeReal.toFixed(1)}%)`);
    } else {
        Notify.info('Precio actualizado');
    }
}

/**
 * Limpiar descuento del item (restaurar precio original)
 */
function limpiarDescuento() {
    if (itemDescuentoActual === null) return;
    
    const item = carrito[itemDescuentoActual];
    if (!item) return;

    const precioOriginal = item.precioUnitarioOriginal || item.precioUnitario;
    
    item.precioUnitario = precioOriginal;
    item.descuentoAplicado = 0;
    item.porcentajeDescuento = null;
    item.aumentoAplicado = 0;
    item.porcentajeAumento = null;

    // Actualizar carrito
    actualizarCarrito();
    guardarCarritoActual();

    // Cerrar modal
    cerrarModalDescuento();

    Notify.info('Precio restaurado al original');
}

// Hacer funciones globales
window.abrirModalDescuento = abrirModalDescuento;
window.cerrarModalDescuento = cerrarModalDescuento;
window.calcularDescuentoDesdeCampoUnico = calcularDescuentoDesdeCampoUnico;
window.aplicarDescuento = aplicarDescuento;
window.limpiarDescuento = limpiarDescuento;
