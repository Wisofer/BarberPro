/**
 * POS Tabs Management
 * Manejo de pestañas (Productos, Servicios, Órdenes)
 */

/**
 * Cambiar pestaña activa
 */
function cambiarPestaña(tipo) {
    pestañaActual = tipo;
    const tabProductos = getElementSafely('tabProductos');
    const tabServicios = getElementSafely('tabServicios');
    const tabMesas = getElementSafely('tabMesas');
    const tabOrdenes = getElementSafely('tabOrdenes');
    const contenidoProductos = getElementSafely('contenidoProductos');
    const contenidoServicios = getElementSafely('contenidoServicios');
    const contenidoMesas = getElementSafely('contenidoMesas');
    const contenidoOrdenes = getElementSafely('contenidoOrdenes');
    const buscarInput = getElementSafely('buscarInput');
    const resultadosBusqueda = getElementSafely('resultadosBusqueda');
    const busquedaUnificadaContainer = getElementSafely('busquedaUnificadaContainer');

    // Validar que los elementos existan
    if (!tabProductos || !tabServicios || !tabMesas || !tabOrdenes || !contenidoProductos || 
        !contenidoServicios || !contenidoMesas || !contenidoOrdenes || !buscarInput) {
        console.error('Error: Elementos del DOM no encontrados');
        return;
    }

    // Resetear todas las pestañas
    tabProductos.classList.remove('tab-active');
    tabServicios.classList.remove('tab-active');
    tabMesas.classList.remove('tab-active');
    tabOrdenes.classList.remove('tab-active');
    contenidoProductos.classList.add('hidden');
    contenidoServicios.classList.add('hidden');
    contenidoMesas.classList.add('hidden');
    contenidoOrdenes.classList.add('hidden');

    if (tipo === 'productos') {
        tabProductos.classList.add('tab-active');
        contenidoProductos.classList.remove('hidden');
        if (buscarInput) buscarInput.placeholder = 'Buscar producto por nombre, código o referencia...';
        // Mostrar buscador unificado para productos
        if (busquedaUnificadaContainer) busquedaUnificadaContainer.classList.remove('hidden');
    } else if (tipo === 'servicios') {
        tabServicios.classList.add('tab-active');
        contenidoServicios.classList.remove('hidden');
        if (buscarInput) buscarInput.placeholder = 'Buscar servicio por nombre...';
        // Mostrar buscador unificado para servicios
        if (busquedaUnificadaContainer) busquedaUnificadaContainer.classList.remove('hidden');
    } else if (tipo === 'mesas') {
        tabMesas.classList.add('tab-active');
        contenidoMesas.classList.remove('hidden');
        // Ocultar buscador unificado (las mesas no necesitan búsqueda)
        if (busquedaUnificadaContainer) busquedaUnificadaContainer.classList.add('hidden');
        // Cargar badges de mesas cuando se muestra la pestaña
        if (window.cargarBadgesMesas) {
            cargarBadgesMesas();
        }
    } else if (tipo === 'ordenes') {
        tabOrdenes.classList.add('tab-active');
        contenidoOrdenes.classList.remove('hidden');
        // Ocultar buscador unificado (órdenes no tienen búsqueda integrada)
        if (busquedaUnificadaContainer) busquedaUnificadaContainer.classList.add('hidden');
    }
    if (buscarInput) buscarInput.value = '';
    if (resultadosBusqueda) resultadosBusqueda.classList.add('hidden');
    
    // Resetear orden seleccionada al cambiar de pestaña
    if (tipo !== 'ordenes') {
        ordenSeleccionada = null;
        actualizarTextoBoton();
    }
    
    // NOTA: NO limpiar mesa seleccionada automáticamente al cambiar de pestaña
    // La mesa permanece seleccionada para que el usuario pueda agregar productos/servicios
    // Solo se limpia cuando:
    // 1. El usuario explícitamente la deselecciona (botón X del banner)
    // 2. Se factura la venta
    // 3. Se selecciona otra mesa
}

