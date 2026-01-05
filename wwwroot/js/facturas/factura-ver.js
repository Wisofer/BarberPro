/**
 * Factura Ver - Payment Management
 * Manejo de pagos desde la vista de factura
 */

// Variables globales
let facturaIdParaPago = null;
let saldoPendienteActual = 0;
let facturaIdParaImprimir = null;

// Inicializar saldo pendiente desde el servidor
document.addEventListener('DOMContentLoaded', function() {
    if (typeof window.saldoPendienteInicial !== 'undefined') {
        saldoPendienteActual = window.saldoPendienteInicial;
    }
});

// Función para mostrar modal de impresión (global)
window.mostrarModalImpresion = function(facturaId) {
    facturaIdParaImprimir = facturaId;
    const modal = document.getElementById('modalImpresionTicket');
    if (modal) {
        modal.classList.add('modal-open');
    }
};

// Función para cerrar el modal (global)
window.cerrarModalImpresion = function() {
    const modal = document.getElementById('modalImpresionTicket');
    if (modal) {
        modal.classList.remove('modal-open');
    }
    facturaIdParaImprimir = null;
};

// Función para imprimir el ticket (global)
window.imprimirTicket = function() {
    if (!facturaIdParaImprimir) {
        Notify.error('Error: No se encontró la información de la factura');
        cerrarModalImpresion();
        return;
    }

    const urlTicket = `/facturas/ticket/${facturaIdParaImprimir}`;
    const ventanaTicket = window.open(urlTicket, '_blank', 'width=400,height=600');
    
    cerrarModalImpresion();
};

// Funciones para el modal de pago
function mostrarModalPago(facturaId, saldoPendiente) {
    facturaIdParaPago = facturaId;
    saldoPendienteActual = saldoPendiente;
    
    document.getElementById('saldoPendienteModal').textContent = formatearPrecioConSimbolo(saldoPendiente);
    document.getElementById('maximoPagoModal').textContent = saldoPendiente.toFixed(2); // Mantener formato numérico para input
    
    const montoModal = document.getElementById('montoModal');
    montoModal.value = saldoPendiente.toFixed(2);
    montoModal.setAttribute('max', saldoPendiente.toFixed(2));
    
    document.getElementById('montoRecibidoModal').value = '';
    document.getElementById('vueltoModal').value = 'C$ 0.00';
    document.getElementById('bancoModal').value = '';
    document.getElementById('tipoCuentaModal').value = '';
    
    actualizarCamposPago();
    document.getElementById('modalPago').classList.add('modal-open');
}

function cerrarModalPago() {
    document.getElementById('modalPago').classList.remove('modal-open');
    facturaIdParaPago = null;
}

function actualizarCamposPago() {
    const tipoPago = document.querySelector('input[name="tipoPagoModal"]:checked')?.value || 'Fisico';
    const camposFisico = document.getElementById('camposFisicoModal');
    const camposElectronico = document.getElementById('camposElectronicoModal');
    
    if (tipoPago === 'Fisico') {
        camposFisico.classList.remove('hidden');
        camposElectronico.classList.add('hidden');
        document.getElementById('bancoModal').removeAttribute('required');
        document.getElementById('tipoCuentaModal').removeAttribute('required');
    } else if (tipoPago === 'Electronico') {
        camposFisico.classList.add('hidden');
        camposElectronico.classList.remove('hidden');
        document.getElementById('bancoModal').setAttribute('required', 'required');
        document.getElementById('tipoCuentaModal').setAttribute('required', 'required');
    } else { // Mixto
        camposFisico.classList.remove('hidden');
        camposElectronico.classList.remove('hidden');
        document.getElementById('bancoModal').setAttribute('required', 'required');
        document.getElementById('tipoCuentaModal').setAttribute('required', 'required');
    }
}

// Calcular vuelto cuando cambia el monto recibido
document.getElementById('montoRecibidoModal')?.addEventListener('input', function() {
    const montoRecibido = parseFloat(this.value) || 0;
    const monto = parseFloat(document.getElementById('montoModal').value) || 0;
    const vuelto = montoRecibido - monto;
    document.getElementById('vueltoModal').value = vuelto >= 0 ? `C$ ${vuelto.toFixed(2)}` : 'C$ 0.00';
    
    if (montoRecibido > 0 && montoRecibido < monto) {
        this.classList.add('input-error');
    } else {
        this.classList.remove('input-error');
    }
});

// Actualizar vuelto cuando cambia el monto a pagar
document.getElementById('montoModal')?.addEventListener('input', function() {
    const monto = parseFloat(this.value) || 0;
    const montoRecibido = parseFloat(document.getElementById('montoRecibidoModal').value) || 0;
    
    if (monto > saldoPendienteActual) {
        this.value = saldoPendienteActual.toFixed(2);
        Notify.warning(`El monto no puede ser mayor al saldo pendiente (${formatearPrecioConSimbolo(saldoPendienteActual)})`);
        return;
    }
    
    if (montoRecibido > 0) {
        const vuelto = montoRecibido - monto;
        document.getElementById('vueltoModal').value = vuelto >= 0 ? formatearPrecioConSimbolo(vuelto) : formatearPrecioConSimbolo(0);
    }
    
    const esAbono = monto < saldoPendienteActual;
    const montoModalEl = document.getElementById('montoModal');
    if (esAbono && monto > 0) {
        montoModalEl.classList.add('input-success');
        montoModalEl.classList.remove('input-error');
    } else {
        montoModalEl.classList.remove('input-success');
    }
});

// Actualizar campos al cambiar tipo de pago
document.querySelectorAll('input[name="tipoPagoModal"]').forEach(radio => {
    radio.addEventListener('change', actualizarCamposPago);
});

async function procesarPago() {
    if (!facturaIdParaPago) {
        Notify.error('Error: No se encontró la información de la factura');
        return;
    }

    const monto = parseFloat(document.getElementById('montoModal').value);
    const tipoPago = document.querySelector('input[name="tipoPagoModal"]:checked')?.value;
    const moneda = document.getElementById('monedaModal').value;
    const banco = document.getElementById('bancoModal').value;
    const tipoCuenta = document.getElementById('tipoCuentaModal').value;
    const montoRecibido = parseFloat(document.getElementById('montoRecibidoModal').value) || null;

    if (!monto || monto <= 0) {
        Notify.error('El monto debe ser mayor a cero');
        return;
    }

    if (monto > saldoPendienteActual) {
        Notify.error(`El monto no puede ser mayor al saldo pendiente (${formatearPrecioConSimbolo(saldoPendienteActual)})`);
        return;
    }

    const esAbono = monto < saldoPendienteActual;

    if (tipoPago === 'Electronico' || tipoPago === 'Mixto') {
        if (!banco) {
            Notify.error('Debe seleccionar un banco');
            return;
        }
        if (!tipoCuenta) {
            Notify.error('Debe seleccionar un tipo de cuenta');
            return;
        }
    }

    if (tipoPago === 'Fisico' || tipoPago === 'Mixto') {
        if (!montoRecibido || montoRecibido <= 0) {
            Notify.error('Debe ingresar el monto recibido');
            return;
        }
        
        if (montoRecibido < monto) {
            Notify.error(`El monto recibido (${formatearPrecioConSimbolo(montoRecibido)}) no puede ser menor al monto a pagar (${formatearPrecioConSimbolo(monto)})`);
            return;
        }
    }

    if (window.Loader) {
        window.Loader.show('Registrando pago...', 'Procesando la transacción');
    }

    try {
        const tipoCambio = window.tipoCambio || 36.80;
        const response = await fetch(`/facturas/registrar-pago/${facturaIdParaPago}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify({
                Monto: monto,
                TipoPago: tipoPago,
                Moneda: moneda,
                Banco: banco || null,
                TipoCuenta: tipoCuenta || null,
                TipoCambio: tipoCambio,
                MontoRecibido: montoRecibido
            })
        });

        const data = await response.json();

        if (window.Loader) {
            window.Loader.hide();
        }

        if (data.success) {
            const fueAbono = data.saldoPendiente > 0;
            
            if (fueAbono) {
                Notify.warning(
                    `💰 Abono Registrado\n` +
                    `Factura: ${data.facturaNumero}\n` +
                    `Abono: ${formatearPrecioConSimbolo(data.totalPagado)} de ${formatearPrecioConSimbolo(data.totalPagado + data.saldoPendiente)}\n` +
                    `Saldo pendiente: ${formatearPrecioConSimbolo(data.saldoPendiente)}`
                );
            } else {
                Notify.success(
                    `✅ Pago Completo Registrado\n` +
                    `Factura: ${data.facturaNumero}\n` +
                    `Monto pagado: ${formatearPrecioConSimbolo(data.totalPagado)}`
                );
            }
            
            cerrarModalPago();
            setTimeout(() => {
                window.location.reload();
            }, 1500);
        } else {
            Notify.error(data.message || 'Error al registrar el pago');
        }
    } catch (error) {
        if (window.Loader) {
            window.Loader.hide();
        }
        console.error('Error:', error);
        Notify.error('Error al registrar el pago. Por favor, intenta de nuevo.');
    }
}

// Hacer funciones globales
window.mostrarModalPago = mostrarModalPago;
window.cerrarModalPago = cerrarModalPago;
window.procesarPago = procesarPago;

