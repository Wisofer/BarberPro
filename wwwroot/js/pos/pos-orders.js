/**
 * POS Orders Management
 * Manejo de órdenes de trabajo desde el POS
 * 
 * RESPONSABILIDADES:
 * - Seleccionar órdenes de trabajo para facturar
 * - Procesar facturación de órdenes completadas
 * - Validar pagos de órdenes
 * - Actualizar estado de órdenes después de facturación
 */

/**
 * Seleccionar orden para facturar
 */
function seleccionarOrden(ordenId, total) {
    ordenSeleccionada = { id: ordenId, total: total };
    
    // Limpiar carrito y mostrar solo la orden seleccionada
    carrito = [];
    actualizarCarrito();
    
    // Mostrar información de la orden en el carrito
    const carritoDiv = getElementSafely('carrito');
    if (carritoDiv) {
        carritoDiv.innerHTML = `
            <div class="card bg-primary/10 border border-primary">
                <div class="card-body p-4">
                    <h4 class="font-bold text-primary">Orden Seleccionada</h4>
                    <p class="text-sm opacity-70">Total: <span class="font-bold text-success">${formatearPrecioConSimbolo(total)}</span></p>
                    <p class="text-xs opacity-60 mt-2">Esta orden será facturada y cobrada</p>
                </div>
            </div>
        `;
    }
    
    // Actualizar totales
    const subtotalEl = getElementSafely('subtotal');
    const totalEl = getElementSafely('total');
    const formularioPagoEl = getElementSafely('formularioPago');
    
    if (subtotalEl) subtotalEl.textContent = formatearPrecioConSimbolo(total);
    if (totalEl) totalEl.textContent = formatearPrecioConSimbolo(total);
    
    // Mostrar formulario de pago
    if (formularioPagoEl) formularioPagoEl.style.display = 'block';
    
    // Actualizar texto del botón
    actualizarTextoBoton();
    
    // Scroll al formulario de pago
    const formularioPago = getElementSafely('formularioPago');
    if (formularioPago) {
        formularioPago.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }
}

/**
 * Facturar orden desde POS
 */
async function facturarOrden() {
    if (!ordenSeleccionada) {
        Notify.warning('No hay orden seleccionada');
        return;
    }

    const tipoPagoEl = getElementSafely('tipoPago');
    
    if (!tipoPagoEl) {
        Notify.error('Error: No se pudieron obtener los datos del formulario');
        return;
    }
    
    const tipoPago = tipoPagoEl.value;
    
    // Obtener el total de la orden en córdobas
    const totalOrdenCordobas = ordenSeleccionada.totalReal || ordenSeleccionada.total || 0;
    
    // Manejar diferentes tipos de pago
    let moneda = 'C$';
    let montoRecibido = 0;
    let montoCordobasFisico = null;
    let montoDolaresFisico = null;
    let montoCordobasElectronico = null;
    let montoDolaresElectronico = null;
    
    if (tipoPago === 'Mixto') {
        // Pago mixto: obtener montos separados
        const monedaFisicoEl = getElementSafely('monedaFisico');
        const montoFisicoEl = getElementSafely('montoFisico');
        const monedaElectronicoEl = getElementSafely('monedaElectronico');
        const montoElectronicoEl = getElementSafely('montoElectronico');
        
        if (!monedaFisicoEl || !montoFisicoEl || !monedaElectronicoEl || !montoElectronicoEl) {
            Notify.error('Error: Campos de pago mixto no encontrados');
            return;
        }
        
        const monedaFisico = monedaFisicoEl.value;
        const montoFisico = parseFloat(montoFisicoEl.value) || 0;
        const monedaElectronico = monedaElectronicoEl.value;
        const montoElectronico = parseFloat(montoElectronicoEl.value) || 0;
        
        if (montoFisico <= 0 && montoElectronico <= 0) {
            Notify.warning('Debe ingresar al menos un monto en pago mixto');
            return;
        }
        
        // Convertir a córdobas para validación
        if (monedaFisico === 'C$') {
            montoCordobasFisico = montoFisico;
        } else {
            montoDolaresFisico = montoFisico;
            montoCordobasFisico = convertirDolaresACordobas(montoFisico, tipoCambio);
        }
        
        if (monedaElectronico === 'C$') {
            montoCordobasElectronico = montoElectronico;
        } else {
            montoDolaresElectronico = montoElectronico;
            montoCordobasElectronico = convertirDolaresACordobas(montoElectronico, tipoCambio);
        }
        
        const totalPagadoCordobas = (montoCordobasFisico || 0) + (montoCordobasElectronico || 0);
        
        // Validar pago parcial
        if (totalPagadoCordobas < totalOrdenCordobas) {
            const confirmarPagoParcial = await validarPagoParcial(totalPagadoCordobas, totalOrdenCordobas);
            if (!confirmarPagoParcial) {
                return;
            }
        }
        
        moneda = 'Ambos'; // Pago mixto usa "Ambos"
    } else {
        // Pago físico o electrónico simple
        const monedaEl = getElementSafely('moneda');
        const montoRecibidoEl = getElementSafely('montoRecibido');
        
        if (!monedaEl || !montoRecibidoEl) {
            Notify.error('Error: Campos de pago no encontrados');
            return;
        }
        
        moneda = monedaEl.value;
        montoRecibido = parseFloat(montoRecibidoEl.value) || 0;
        
        if (tipoPago === 'Fisico' && montoRecibido <= 0) {
            Notify.warning('Debe ingresar el monto recibido');
            return;
        }
        
        // Convertir monto recibido a córdobas para validación
        let montoRecibidoCordobas = montoRecibido;
        if (moneda === '$') {
            montoRecibidoCordobas = convertirDolaresACordobas(montoRecibido, tipoCambio);
        }
        
        // Validar pago parcial
        if (tipoPago === 'Fisico' && montoRecibidoCordobas < totalOrdenCordobas) {
            const confirmarPagoParcial = await validarPagoParcial(montoRecibidoCordobas, totalOrdenCordobas);
            if (!confirmarPagoParcial) {
                return;
            }
        }
    }

    // Mostrar loader
    if (window.Loader) {
        window.Loader.show('Facturando orden...', 'Procesando la orden de trabajo');
    }

    // Preparar datos del pago
    const pagoData = {
        ordenTrabajoId: ordenSeleccionada.id,
        tipoPago,
        moneda,
        montoRecibido: tipoPago === 'Mixto' ? 0 : montoRecibido,
        tipoCambio
    };
    
    // Agregar campos de pago mixto si aplica
    if (tipoPago === 'Mixto') {
        pagoData.montoCordobasFisico = montoCordobasFisico;
        pagoData.montoDolaresFisico = montoDolaresFisico;
        pagoData.montoCordobasElectronico = montoCordobasElectronico;
        pagoData.montoDolaresElectronico = montoDolaresElectronico;
    }

    fetch('/pos/facturar-orden', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(pagoData)
    })
    .then(async res => {
        const contentType = res.headers.get('content-type');
        if (!contentType || !contentType.includes('application/json')) {
            const text = await res.text();
            throw new Error(`Error del servidor: ${res.status} ${res.statusText}. ${text}`);
        }
        return res.json();
    })
    .then(data => {
        // Ocultar loader
        if (window.Loader) {
            window.Loader.hide();
        }
        
        if (data.success) {
            if (data.esPagoParcial) {
                Notify.warning(
                    `⚠️ Pago Parcial Registrado\n` +
                    `Factura: ${data.facturaNumero}\n` +
                    `Pagado: ${formatearPrecioConSimbolo(data.totalPagado)} de ${formatearPrecioConSimbolo(data.total)}\n` +
                    `Saldo pendiente: ${formatearPrecioConSimbolo(data.saldoPendiente)}`
                );
            } else {
            Notify.success(`¡Orden facturada y pagada exitosamente! Factura: ${data.facturaNumero} - Total: ${formatearPrecioConSimbolo(data.total)}`);
            }
            
            // Actualizar stock de productos de la orden facturada
            // Nota: El stock ya fue descontado cuando se agregaron los productos a la orden,
            // pero necesitamos actualizar la interfaz para reflejar el stock actual
            if (data.productosVendidos && Array.isArray(data.productosVendidos) && data.productosVendidos.length > 0) {
                actualizarStockProductos(data.productosVendidos);
            }
            
            // Si hay mesa seleccionada, limpiar su carrito y liberarla
            if (mesaSeleccionada && mesaSeleccionada.id) {
                guardarCarritoMesa(mesaSeleccionada.id, []);
                if (window.actualizarBadgeMesa) {
                    actualizarBadgeMesa(mesaSeleccionada.id, 0);
                }
                if (window.actualizarTotalPendienteMesa) {
                    actualizarTotalPendienteMesa(mesaSeleccionada.id, 0);
                }
                // Las mesas se mantienen ordenadas por fecha de creación (no se reordenan)
            }
            
            // Limpiar y recargar
            ordenSeleccionada = null;
            mesaSeleccionada = null; // Liberar mesa después de facturar orden
            carrito = [];
            actualizarCarrito();
            
            // Limpiar formulario y estado
            limpiarFormularioVenta();
            
            // Recargar página para actualizar lista de órdenes
            setTimeout(() => {
                window.location.reload();
            }, 1500);
        } else {
            Notify.error('Error: ' + data.message);
        }
    })
    .catch(err => {
        console.error('Error:', err);
        // Ocultar loader en caso de error
        if (window.Loader) {
            window.Loader.hide();
        }
        Notify.error('Error al facturar la orden');
    });
}

