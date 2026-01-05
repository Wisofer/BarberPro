/**
 * POS Search
 * Búsqueda de productos y servicios
 */

let timeoutBusqueda;

/**
 * Inicializar búsqueda
 */
function inicializarBusqueda() {
    const buscarInput = getElementSafely('buscarInput');
    const resultadosDiv = getElementSafely('resultadosBusqueda');

    if (!buscarInput || !resultadosDiv) {
        console.error('Error: Elementos de búsqueda no encontrados');
        return;
    }

    buscarInput.addEventListener('input', function() {
        clearTimeout(timeoutBusqueda);
        const termino = this.value.trim();
        
        if (termino.length < 2) {
            resultadosDiv.classList.add('hidden');
            return;
        }

        timeoutBusqueda = setTimeout(() => {
            if (pestañaActual === 'productos') {
                fetch(`/pos/buscar-producto?termino=${encodeURIComponent(termino)}`)
                    .then(res => res.json())
                    .then(data => mostrarResultadosBusqueda(data, 'producto'))
                    .catch(err => console.error('Error en búsqueda:', err));
            } else {
                fetch(`/pos/buscar-servicio?termino=${encodeURIComponent(termino)}`)
                    .then(res => res.json())
                    .then(data => mostrarResultadosBusqueda(data, 'servicio'))
                    .catch(err => console.error('Error en búsqueda:', err));
            }
        }, 300);
    });

    // Cerrar resultados al hacer click fuera
    document.addEventListener('click', function(e) {
        const buscarInputEl = getElementSafely('buscarInput');
        const resultadosDivEl = getElementSafely('resultadosBusqueda');
        if (buscarInputEl && resultadosDivEl && 
            !buscarInputEl.contains(e.target) && !resultadosDivEl.contains(e.target)) {
            resultadosDivEl.classList.add('hidden');
        }
    });
}

/**
 * Mostrar resultados de búsqueda
 */
function mostrarResultadosBusqueda(items, tipo) {
    const resultadosDiv = getElementSafely('resultadosBusqueda');
    if (!resultadosDiv) return;

    if (items.length === 0) {
        resultadosDiv.innerHTML = '<div class="p-4 text-center opacity-60">No se encontraron resultados</div>';
        resultadosDiv.classList.remove('hidden');
        return;
    }

    if (tipo === 'producto') {
        resultadosDiv.innerHTML = items.map(p => {
            const tieneStock = p.stock > 0;
            const tienePromocion = p.tienePromocion || false;
            const precioOriginal = p.precioOriginal || p.precio;
            const precioEfectivo = p.precio || 0;
            const precioOriginalParam = tienePromocion && precioOriginal > precioEfectivo ? precioOriginal : 0;
            return `
                <button type="button" onclick="agregarProductoAlCarrito(${p.id}, '${p.nombre.replace(/'/g, "\\'")}', ${precioEfectivo}, ${p.stock}, ${precioOriginalParam})" 
                        class="flex items-center justify-between w-full p-3 hover:bg-base-200 transition-colors border-b border-base-200 last:border-0 ${!tieneStock ? 'opacity-50' : ''} relative">
                    ${tienePromocion ? '<span class="absolute top-1 left-1 badge badge-warning badge-xs">🔥</span>' : ''}
                    <div class="text-left ${tienePromocion ? 'pl-6' : ''}">
                        <div class="font-bold">${p.nombre}</div>
                        <div class="text-sm opacity-60">${p.codigo}</div>
                    </div>
                    <div class="text-right">
                        ${tienePromocion && precioOriginal > precioEfectivo ? `
                            <div class="text-xs line-through opacity-50">${formatearPrecioConSimbolo(precioOriginal)}</div>
                            <div class="font-bold text-warning">${formatearPrecioConSimbolo(precioEfectivo)}</div>
                        ` : `
                            <div class="font-bold text-success">${formatearPrecioConSimbolo(precioEfectivo)}</div>
                        `}
                        <div class="text-xs ${tieneStock ? 'opacity-60' : 'text-error font-bold'}">Stock: ${p.stock}</div>
                    </div>
                </button>
            `;
        }).join('');
    } else {
        resultadosDiv.innerHTML = items.map(s => {
            return `
                <button type="button" onclick="agregarServicioAlCarrito(${s.id}, '${s.nombre.replace(/'/g, "\\'")}', ${s.precio})" 
                        class="flex items-center justify-between w-full p-3 hover:bg-base-200 transition-colors border-b border-base-200 last:border-0">
                    <div class="text-left">
                        <div class="font-bold">${s.nombre}</div>
                        ${s.descripcion ? `<div class="text-sm opacity-60">${s.descripcion}</div>` : ''}
                    </div>
                    <div class="text-right">
                        <div class="font-bold text-success">${formatearPrecioConSimbolo(s.precio)}</div>
                        ${s.tiempoEstimado > 0 ? `<div class="text-xs opacity-60">⏱️ ${s.tiempoEstimado}h</div>` : ''}
                    </div>
                </button>
            `;
        }).join('');
    }
    resultadosDiv.classList.remove('hidden');
}

