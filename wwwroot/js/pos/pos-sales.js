/**
 * POS Sales Processing
 * Procesamiento de ventas directas (sin orden de trabajo)
 * 
 * RESPONSABILIDADES:
 * - Procesar ventas directas desde el carrito
 * - Validar pagos (completo, parcial, transferencia)
 * - Gestionar facturación y tickets
 * - Actualizar stock después de ventas
 * - Limpiar estado después de venta exitosa
 */

/**
 * Procesar venta directa
 */
async function procesarVenta() {
    // Validación de caja abierta (se inyecta desde el servidor)
    if (typeof window.cajaAbierta !== 'undefined' && !window.cajaAbierta) {
        Notify.warning('No se puede procesar la venta. Debes abrir una caja primero.');
        setTimeout(() => {
            window.location.href = '/caja/abrir';
        }, 2000);
        return;
    }

    // Si hay una orden seleccionada, facturar orden
    if (ordenSeleccionada) {
        facturarOrden();
        return;
    }

    if (carrito.length === 0) {
        Notify.warning('El carrito está vacío');
        return;
    }

    const clienteIdEl = getElementSafely('clienteId');
    const tipoPagoEl = getElementSafely('tipoPago');
    const monedaEl = getElementSafely('moneda');
    const montoRecibidoEl = getElementSafely('montoRecibido');
    
    if (!clienteIdEl || !tipoPagoEl) {
        Notify.error('Error: No se pudieron obtener los datos del formulario');
        return;
    }
    
    const clienteId = parseInt(clienteIdEl.value);
    const tipoPago = tipoPagoEl.value;
    
    // Calcular total del carrito en córdobas (todos los productos están en C$)
    const totalCarritoCordobas = carrito.reduce((sum, item) => sum + ((item.precioUnitario || 0) * (item.cantidad || 0)), 0);
    
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
        if (totalPagadoCordobas < totalCarritoCordobas) {
            const confirmarPagoParcial = await validarPagoParcial(totalPagadoCordobas, totalCarritoCordobas);
            if (!confirmarPagoParcial) {
                return;
            }
        }
        
        moneda = 'Ambos'; // Pago mixto usa "Ambos"
    } else {
        // Pago físico o electrónico simple
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
        
        // Validar pago parcial (tanto físico como electrónico)
        if (tipoPago === 'Fisico' && montoRecibidoCordobas < totalCarritoCordobas) {
            const confirmarPagoParcial = await validarPagoParcial(montoRecibidoCordobas, totalCarritoCordobas);
            if (!confirmarPagoParcial) {
                return;
            }
        } else if (tipoPago === 'Electronico' && montoRecibidoCordobas > 0 && montoRecibidoCordobas < totalCarritoCordobas) {
            // Validar que el pago electrónico sea suficiente si se especificó un monto
            const confirmarPagoParcial = await validarPagoParcial(montoRecibidoCordobas, totalCarritoCordobas);
            if (!confirmarPagoParcial) {
                return;
            }
        }
    }

    // Separar productos y servicios (incluyendo información de descuentos y aumentos)
    const items = carrito
        .filter(item => item.tipo !== 'servicio')
        .map(item => ({
            productoId: item.productoId,
            cantidad: item.cantidad,
            precioUnitario: item.precioUnitario, // Precio final (puede incluir descuento o aumento)
            precioUnitarioOriginal: item.precioUnitarioOriginal || item.precioUnitario,
            descuentoAplicado: item.descuentoAplicado || 0,
            porcentajeDescuento: item.porcentajeDescuento || null,
            aumentoAplicado: item.aumentoAplicado || 0,
            porcentajeAumento: item.porcentajeAumento || null
        }));

    const servicios = carrito
        .filter(item => item.tipo === 'servicio')
        .map(item => ({
            servicioId: item.servicioId,
            cantidad: item.cantidad,
            precioUnitario: item.precioUnitario, // Precio final (puede incluir descuento o aumento)
            precioUnitarioOriginal: item.precioUnitarioOriginal || item.precioUnitario,
            descuentoAplicado: item.descuentoAplicado || 0,
            porcentajeDescuento: item.porcentajeDescuento || null,
            aumentoAplicado: item.aumentoAplicado || 0,
            porcentajeAumento: item.porcentajeAumento || null
        }));

    // Mostrar loader
    if (window.Loader) {
        window.Loader.show('Procesando venta...', 'Registrando la transacción');
    }

    // Obtener MesaId si hay una mesa seleccionada
    const mesaId = (mesaSeleccionada && mesaSeleccionada.id) ? mesaSeleccionada.id : 0;

    // Preparar datos del pago
    const pagoData = {
        clienteId,
        mesaId: mesaId,
        tipoPago,
        moneda,
        montoRecibido: tipoPago === 'Mixto' ? 0 : montoRecibido,
        tipoCambio,
        items,
        servicios
    };
    
    // Agregar campos de pago mixto si aplica
    if (tipoPago === 'Mixto') {
        pagoData.montoCordobasFisico = montoCordobasFisico;
        pagoData.montoDolaresFisico = montoDolaresFisico;
        pagoData.montoCordobasElectronico = montoCordobasElectronico;
        pagoData.montoDolaresElectronico = montoDolaresElectronico;
    }

    fetch('/pos/procesar-venta', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(pagoData)
    })
    .then(async res => {
        // Verificar si la respuesta es JSON válida
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
            Notify.success(`¡Venta exitosa! Factura: ${data.facturaNumero} - Total: ${formatearPrecioConSimbolo(data.total)}`);
            }
            
            // Guardar IDs de productos vendidos antes de limpiar el carrito
            const productosVendidos = carrito
                .filter(item => item.tipo !== 'servicio' && item.productoId)
                .map(item => item.productoId);
            
            // Si hay mesa seleccionada, limpiar su carrito y liberarla
            if (mesaSeleccionada && mesaSeleccionada.id) {
                // Limpiar carrito de esta mesa específica (se procesará automáticamente en el backend)
                guardarCarritoMesa(mesaSeleccionada.id, []);
                // Actualizar badge en la vista
                if (window.actualizarBadgeMesa) {
                    actualizarBadgeMesa(mesaSeleccionada.id, 0);
                }
                if (window.actualizarTotalPendienteMesa) {
                    actualizarTotalPendienteMesa(mesaSeleccionada.id, 0);
                }
                // Las mesas se mantienen ordenadas por fecha de creación (no se reordenan)
                Notify.success(`Moto ${mesaSeleccionada.nombre} facturada y liberada`);
            }
            
            carrito = [];
            ordenSeleccionada = null;
            mesaSeleccionada = null; // Liberar mesa después de facturar
            actualizarCarrito();
            
            // Limpiar formulario y estado
            limpiarFormularioVenta();
            
            // Actualizar stock de productos vendidos en la interfaz
            if (productosVendidos.length > 0) {
                actualizarStockProductos(productosVendidos);
            }
            
            // Mostrar modal para imprimir ticket
            mostrarModalImpresion(data.facturaId);
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
        Notify.error('Error al procesar la venta');
    });
}

