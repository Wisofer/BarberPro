/**
 * POS Stock Management
 * Actualización de stock en tiempo real
 */

/**
 * Actualizar tarjeta de producto con nuevo stock
 */
function actualizarTarjetaProducto(productoId, stockReal) {
    const botones = document.querySelectorAll(`button[onclick*="agregarProductoAlCarrito(${productoId}"]`);
    botones.forEach(boton => {
        const stockElement = boton.querySelector('.stock-display');
        if (stockElement) {
            stockElement.textContent = `Stock: ${stockReal}`;
            if (stockReal <= 0) {
                boton.classList.add('opacity-50', 'cursor-not-allowed');
                boton.disabled = true;
                if (!boton.querySelector('.badge-error')) {
                    const badge = document.createElement('span');
                    badge.className = 'badge badge-error badge-sm absolute top-2 right-2 z-10';
                    badge.textContent = 'Sin stock';
                    boton.appendChild(badge);
                }
            } else {
                boton.classList.remove('opacity-50', 'cursor-not-allowed');
                boton.disabled = false;
                const badge = boton.querySelector('.badge-error');
                if (badge) {
                    badge.remove();
                }
            }
        }
    });
}

/**
 * Actualizar stock de múltiples productos después de una venta
 */
async function actualizarStockProductos(productoIds) {
    if (!productoIds || productoIds.length === 0) return;
    
    // Actualizar cada producto vendido
    for (const productoId of productoIds) {
        try {
            const response = await fetch(`/pos/verificar-stock/${productoId}`);
            const data = await response.json();
            
            if (data.success) {
                actualizarTarjetaProducto(productoId, data.stock);
            }
        } catch (error) {
            console.error(`Error al actualizar stock del producto ${productoId}:`, error);
        }
    }
}

/**
 * Recargar todos los productos visibles en el POS
 * Útil después de una devolución
 */
async function cargarProductos() {
    
    // Obtener todos los botones de productos visibles
    const botonesProductos = document.querySelectorAll('button[onclick*="agregarProductoAlCarrito"]');
    const productoIds = [];
    
    botonesProductos.forEach(boton => {
        const onclick = boton.getAttribute('onclick');
        // Extraer el ID del producto del onclick
        const match = onclick.match(/agregarProductoAlCarrito\((\d+)/);
        if (match && match[1]) {
            const productoId = parseInt(match[1]);
            if (!productoIds.includes(productoId)) {
                productoIds.push(productoId);
            }
        }
    });
    
    
    // Actualizar el stock de todos los productos
    await actualizarStockProductos(productoIds);
    
}

// Exportar función para uso global
window.cargarProductos = cargarProductos;

