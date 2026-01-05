/**
 * POS Modals Management
 * Manejo de modales (impresión de tickets)
 */

/**
 * Mostrar modal de impresión
 */
function mostrarModalImpresion(facturaId) {
    facturaIdParaImprimir = facturaId;
    
    // Verificar si el modal ya existe
    let modal = document.getElementById('modalImpresionTicket');
    if (!modal) {
        // Crear el modal si no existe
        crearModalImpresion();
        modal = document.getElementById('modalImpresionTicket');
    }
    
    if (modal) {
        modal.classList.add('modal-open');
    }
}

/**
 * Crear modal de impresión
 */
function crearModalImpresion() {
    const modalHTML = `
        <div id="modalImpresionTicket" class="modal">
            <div class="modal-box">
                <h3 class="font-bold text-lg mb-4 flex items-center gap-2">
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z" />
                    </svg>
                    ¿Desea imprimir el ticket?
                </h3>
                <p class="mb-6">La venta se ha procesado exitosamente. ¿Desea imprimir el ticket de venta?</p>
                <div class="modal-action">
                    <button onclick="cerrarModalImpresion()" class="btn btn-ghost">No, gracias</button>
                    <button onclick="imprimirTicket()" class="btn btn-primary gap-2">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z" />
                        </svg>
                        Sí, imprimir
                    </button>
                </div>
            </div>
            <div class="modal-backdrop" onclick="cerrarModalImpresion()"></div>
        </div>
    `;
    document.body.insertAdjacentHTML('beforeend', modalHTML);
}

/**
 * Cerrar modal de impresión
 */
function cerrarModalImpresion() {
    const modal = document.getElementById('modalImpresionTicket');
    if (modal) {
        modal.classList.remove('modal-open');
    }
    facturaIdParaImprimir = null;
}

/**
 * Imprimir ticket
 */
function imprimirTicket() {
    if (!facturaIdParaImprimir) {
        Notify.error('Error: No se encontró la información de la factura');
        cerrarModalImpresion();
        return;
    }

    // Abrir el ticket en una nueva ventana
    const urlTicket = `/pos/ticket/${facturaIdParaImprimir}`;
    const ventanaTicket = window.open(urlTicket, '_blank', 'width=400,height=600');
    
    // Cerrar el modal
    cerrarModalImpresion();
    
    // La ventana se auto-imprimirá cuando cargue (está en el HTML del ticket)
}

