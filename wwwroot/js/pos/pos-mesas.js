/**
 * POS Mesas Management
 * Manejo de selección y gestión de mesas en el POS
 * 
 * CONCEPTO: Sistema tipo "mesas de restaurante" donde cada mesa (moto) tiene su propio carrito temporal
 * 
 * RESPONSABILIDADES:
 * - Seleccionar/deseleccionar mesas
 * - Gestionar badges y totales pendientes por mesa
 * - Crear, editar y eliminar mesas
 * - Sincronizar carritos entre mesas
 * - Actualizar UI de mesas (badges, bordes, totales)
 */

/**
 * Seleccionar una mesa
 */
async function seleccionarMesa(mesaId, nombre) {
    // Si hay una mesa seleccionada anteriormente, guardar su carrito antes de cambiar
    if (mesaSeleccionada && mesaSeleccionada.id) {
        await guardarCarritoMesa(mesaSeleccionada.id, carrito);
        // Actualizar badges antes de cambiar de mesa
        if (window.cargarBadgesMesas) {
            await cargarBadgesMesas();
        }
    }

    // Cargar carrito de la nueva mesa seleccionada desde BD
    const carritoMesa = await obtenerCarritoMesa(mesaId, true);
    carrito = [...carritoMesa]; // Copiar array para evitar referencias

    // Actualizar mesa seleccionada
    mesaSeleccionada = {
        id: mesaId,
        nombre: nombre
    };

    // Mostrar información de mesa seleccionada
    mostrarMesaSeleccionada(nombre);

    // Actualizar carrito visual
    if (typeof actualizarCarrito === 'function') {
        actualizarCarrito();
    }

    // Mostrar notificación con cantidad de items si tiene
    const cantidadItems = carrito.reduce((sum, item) => sum + (item.cantidad || 0), 0);
    if (typeof Notify !== 'undefined') {
        if (cantidadItems > 0) {
            Notify.info(`Moto ${nombre} seleccionada\nTiene ${cantidadItems} item(s) en el carrito`);
        } else {
            Notify.success(`Moto ${nombre} seleccionada\nAgrega productos o servicios`);
        }
    }

    // Cambiar a pestaña de productos para empezar a agregar items
    if (typeof cambiarPestaña === 'function') {
        cambiarPestaña('productos');
    }
}

/**
 * Mostrar información de mesa seleccionada
 */
function mostrarMesaSeleccionada(nombre) {
    const mesaInfoEl = getElementSafely('mesaSeleccionadaInfo');
    const mesaNombreEl = getElementSafely('mesaSeleccionadaNombre');
    
    if (mesaInfoEl && mesaNombreEl) {
        mesaNombreEl.textContent = nombre;
        mesaInfoEl.classList.remove('hidden');
    }
}

/**
 * Limpiar mesa seleccionada (volver a modo normal sin mesa)
 */
async function limpiarMesaSeleccionada() {
    // Guardar carrito de la mesa actual antes de limpiar
    if (mesaSeleccionada && mesaSeleccionada.id) {
        await guardarCarritoMesa(mesaSeleccionada.id, carrito);
    }

    mesaSeleccionada = null;
    carrito = []; // Limpiar carrito global
    
    const mesaInfoEl = getElementSafely('mesaSeleccionadaInfo');
    if (mesaInfoEl) {
        mesaInfoEl.classList.add('hidden');
    }
    
    // Resetear cliente a General
    const clienteIdEl = getElementSafely('clienteId');
    if (clienteIdEl) {
        clienteIdEl.value = '0';
    }
    
    // Actualizar carrito visual
    actualizarCarrito();
    
    Notify.info('Moto deseleccionada - Modo venta normal');
}

/**
 * Actualizar badge y total de una mesa específica
 * @param {number} mesaId - ID de la mesa
 * @param {number} cantidadItems - Cantidad de items en el carrito
 * @param {number} totalPendiente - Total pendiente de pago
 */
function actualizarBadgeYTotalMesa(mesaId, cantidadItems, totalPendiente) {
    // Actualizar badge de cantidad
    if (cantidadItems > 0) {
        actualizarBadgeMesa(mesaId, cantidadItems);
    } else {
        // Ocultar badge si no hay items
        const badge = getElementSafely(`badge-mesa-${mesaId}`);
        if (badge) badge.classList.add('hidden');
    }

    // Actualizar total pendiente
    actualizarTotalPendienteMesa(mesaId, totalPendiente);
    
    // Actualizar borde de color
    actualizarBordeColorMesa(mesaId, cantidadItems > 0);
}

/**
 * Cargar badges de cantidad de items pendientes para todas las mesas
 * También calcula y muestra total pendiente
 * Las mesas se mantienen ordenadas por fecha de creación (orden del servidor)
 */
async function cargarBadgesMesas() {
    const listaMesas = document.getElementById('listaMesas');
    if (!listaMesas) return;

    let carritosMap = new Map();

    try {
        // Cargar carritos activos desde BD
        const response = await fetch('/pos/carritos-activos');
        const data = await response.json();
        
        const carritosBD = data.success ? (data.carritos || []) : [];
        carritosBD.forEach(c => carritosMap.set(c.mesaId, c));
    } catch (error) {
        console.error('Error al cargar badges desde BD, usando sessionStorage:', error);
    }

    // Obtener todos los botones de mesas
    const mesaButtons = Array.from(listaMesas.querySelectorAll('button[data-mesa-id]'));
    
    // Actualizar badges y totales para cada mesa
    for (const btn of mesaButtons) {
        const mesaId = parseInt(btn.getAttribute('data-mesa-id'));
        if (isNaN(mesaId)) continue;

        // Obtener datos desde BD si están disponibles, sino desde sessionStorage
        let cantidadItems = 0;
        let totalPendiente = 0;

        if (carritosMap.has(mesaId)) {
            const carritoBD = carritosMap.get(mesaId);
            cantidadItems = carritoBD.cantidadItems || 0;
            totalPendiente = carritoBD.total || 0;
        } else {
            // Fallback a sessionStorage
            cantidadItems = obtenerCantidadItemsMesa(mesaId);
            totalPendiente = obtenerTotalPendienteMesa(mesaId);
        }

        // Actualizar badge, total y borde usando función auxiliar
        actualizarBadgeYTotalMesa(mesaId, cantidadItems, totalPendiente);
    }
}

/**
 * Actualizar total pendiente en la card de mesa
 */
function actualizarTotalPendienteMesa(mesaId, total) {
    const totalEl = getElementSafely(`total-pendiente-mesa-${mesaId}`);
    if (!totalEl) return;

    if (total > 0) {
        totalEl.textContent = formatearPrecioConSimbolo(total);
        totalEl.classList.remove('hidden');
    } else {
        totalEl.classList.add('hidden');
    }
}


/**
 * Actualizar badge de cantidad de items en la card de mesa
 */
function actualizarBadgeMesa(mesaId, cantidadItems) {
    const badgeById = getElementSafely(`badge-mesa-${mesaId}`);
    if (badgeById) {
        if (cantidadItems > 0) {
            badgeById.textContent = cantidadItems;
            badgeById.classList.remove('hidden');
        } else {
            badgeById.classList.add('hidden');
        }
        // Actualizar borde de color si está habilitado
        actualizarBordeColorMesa(mesaId, cantidadItems > 0);
        return;
    }

    // Fallback: buscar por onclick si no existe badge con ID
    const mesaButtons = document.querySelectorAll(`button[onclick*="seleccionarMesa(${mesaId}"]`);
    mesaButtons.forEach(btn => {
        let badge = btn.querySelector('.badge-items-mesa');
        if (cantidadItems > 0) {
            if (!badge) {
                badge = document.createElement('span');
                badge.className = 'badge badge-primary badge-lg absolute top-3 right-3 z-10 badge-items-mesa';
                badge.id = `badge-mesa-${mesaId}`;
                btn.querySelector('.card-body').appendChild(badge);
            }
            badge.textContent = cantidadItems;
            badge.classList.remove('hidden');
        } else if (badge) {
            badge.classList.add('hidden');
        }
    });
    // Actualizar borde de color si está habilitado
    actualizarBordeColorMesa(mesaId, cantidadItems > 0);
}

/**
 * Actualizar borde de color en la card de mesa según si tiene productos
 */
function actualizarBordeColorMesa(mesaId, tieneProductos) {
    // Verificar si la configuración está habilitada
    const bordesHabilitados = window.bordesColorMotosPOSHabilitados !== false; // Por defecto true
    
    if (!bordesHabilitados) {
        // Si está deshabilitado, remover cualquier borde de color
        const card = getElementSafely(`card-mesa-${mesaId}`);
        if (card) {
            card.classList.remove('border-2', 'border-error', 'border-success');
            card.classList.add('border', 'border-base-300');
        }
        return;
    }
    
    const card = getElementSafely(`card-mesa-${mesaId}`);
    if (!card) return;
    
    // Remover clases de borde anteriores
    card.classList.remove('border-2', 'border-error', 'border-success', 'border', 'border-base-300');
    
    if (tieneProductos) {
        // Borde rojo cuando tiene productos (indica pendiente)
        card.classList.add('border-2', 'border-error');
    } else {
        // Borde normal cuando no tiene productos
        card.classList.add('border', 'border-base-300');
    }
}

/**
 * Abrir modal para crear nueva moto rápida
 */
function abrirModalNuevaMotoRapida() {
    const modal = document.getElementById('modalNuevaMotoRapida');
    if (modal) {
        modal.showModal();
        // Limpiar el input
        const inputNombre = document.getElementById('nuevaMotoNombre');
        if (inputNombre) {
            inputNombre.value = '';
            inputNombre.focus();
        }
    }
}

/**
 * Cerrar modal de nueva moto rápida
 */
function cerrarModalNuevaMotoRapida() {
    const modal = document.getElementById('modalNuevaMotoRapida');
    if (modal) {
        modal.close();
    }
}

/**
 * Escapar string para usar en atributos HTML y JavaScript
 */
function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}

/**
 * Escapar string para usar en JavaScript (onclick)
 */
function escapeJs(str) {
    return str
        .replace(/\\/g, '\\\\')  // Primero escapar backslashes
        .replace(/'/g, "\\'")     // Escapar comillas simples
        .replace(/"/g, '\\"')     // Escapar comillas dobles
        .replace(/\n/g, '\\n')    // Escapar newlines
        .replace(/\r/g, '\\r');   // Escapar carriage returns
}

/**
 * Crear el HTML de una card de moto
 */
function crearCardMesaHTML(mesaId, nombre) {
    const nombreEscapadoJS = escapeJs(nombre);
    const nombreEscapadoHTML = escapeHtml(nombre);
    
    // Clase de borde por defecto (siempre la misma, independiente de configuración)
    const claseBorde = "border border-base-300";
    
    return `
        <div class="card bg-base-100 shadow hover:shadow-xl transition-all group relative overflow-hidden h-full ${claseBorde}"
             data-mesa-id="${mesaId}" id="card-mesa-${mesaId}">
            <div class="card-body p-4 relative flex flex-col justify-between">
                <!-- Badge de cantidad de items (se actualizará dinámicamente) -->
                <span class="badge badge-primary badge-lg absolute top-3 right-3 z-10 badge-items-mesa hidden" id="badge-mesa-${mesaId}">0</span>
                
                <!-- Botones de acción (editar y borrar) -->
                <div class="absolute top-2 left-2 z-20 flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                    <button type="button"
                            onclick="event.stopPropagation(); abrirModalEditarMesa(${mesaId}, '${nombreEscapadoJS.replace(/'/g, "\\'")}')"
                            class="btn btn-xs btn-ghost btn-circle text-primary hover:bg-primary hover:text-primary-content"
                            title="Editar nombre">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                        </svg>
                    </button>
                    <button type="button"
                            onclick="event.stopPropagation(); confirmarEliminarMesa(${mesaId}, '${nombreEscapadoJS.replace(/'/g, "\\'")}')"
                            class="btn btn-xs btn-ghost btn-circle text-error hover:bg-error hover:text-error-content"
                            title="Eliminar">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                        </svg>
                    </button>
                </div>
                
                <!-- Botón principal para seleccionar la mesa -->
                <button type="button" 
                        onclick="seleccionarMesa(${mesaId}, '${nombreEscapadoJS}')"
                        class="w-full flex flex-col items-center justify-center text-center -mt-2">
                    <span class="text-5xl mb-3">🏍️</span>
                    <h3 class="font-bold text-lg group-hover:text-primary transition-colors">${nombreEscapadoHTML}</h3>
                </button>
                
                <!-- Total pendiente (se actualizará dinámicamente) -->
                <div class="mt-3 pt-3 border-t border-base-300">
                    <p class="text-xs opacity-60">Total pendiente</p>
                    <p class="text-sm font-bold text-success hidden" id="total-pendiente-mesa-${mesaId}">C$ 0.00</p>
                </div>
            </div>
        </div>
    `;
}

/**
 * Agregar nueva moto al DOM dinámicamente (al final, por fecha de creación)
 */
function agregarMesaAlDOM(mesaId, nombre) {
    const contenidoMesas = document.getElementById('contenidoMesas');
    if (!contenidoMesas) return;
    
    // Buscar el contenedor listaMesas
    let listaMesas = document.getElementById('listaMesas');
    
    // Si no existe listaMesas, significa que estamos creando la primera moto
    // Necesitamos ocultar el mensaje "No hay motos" y crear el grid
    if (!listaMesas) {
        // Buscar el mensaje "No hay motos registradas" de forma segura
        // Buscar todos los divs con esas clases y verificar que contengan el texto específico
        const posiblesMensajes = contenidoMesas.querySelectorAll('div.flex.flex-col.items-center');
        for (const div of posiblesMensajes) {
            const h3 = div.querySelector('h3');
            if (h3 && h3.textContent.includes('No hay motos registradas')) {
                div.remove(); // Eliminar el mensaje
                break; // Solo hay uno, salir del loop
            }
        }
        
        // Crear el contenedor grid
        listaMesas = document.createElement('div');
        listaMesas.id = 'listaMesas';
        listaMesas.className = 'grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-3 sm:gap-4';
        contenidoMesas.appendChild(listaMesas);
    }
    
    // Crear un elemento temporal para convertir el HTML string en DOM
    const temp = document.createElement('div');
    temp.innerHTML = crearCardMesaHTML(mesaId, nombre).trim();
    const nuevaCard = temp.firstElementChild;
    
    // Agregar la nueva card al final del grid (manteniendo orden cronológico)
    listaMesas.appendChild(nuevaCard);
}

/**
 * Crear nueva moto rápida desde el POS
 */
async function crearMotoRapida() {
    const inputNombre = document.getElementById('nuevaMotoNombre');
    if (!inputNombre) {
        Notify.error('Error: No se encontró el campo de nombre');
        return;
    }

    const nombre = inputNombre.value.trim();
    
    if (!nombre) {
        Notify.warning('Debes ingresar un nombre para la moto');
        inputNombre.focus();
        return;
    }

    // Mostrar loader
    if (window.Loader) {
        window.Loader.show('Creando moto...', 'Por favor espere');
    }

    try {
        const response = await fetch('/pos/crear-mesa-rapida', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify({ Nombre: nombre })
        });

        const data = await response.json();

        // Ocultar loader
        if (window.Loader) {
            window.Loader.hide();
        }

        if (data.success) {
            // Cerrar el modal PRIMERO para que la notificación se vea bien
            cerrarModalNuevaMotoRapida();
            
            // Agregar la nueva moto al DOM sin recargar la página
            agregarMesaAlDOM(data.mesa.id, data.mesa.nombre);
            
            // Mostrar notificación después de cerrar el modal
            setTimeout(() => {
                Notify.success(data.message || `Moto "${data.mesa.nombre}" creada exitosamente`);
            }, 100);
            
            // Limpiar el input
            inputNombre.value = '';
        } else {
            Notify.error(data.message || 'Error al crear la moto');
        }
    } catch (error) {
        // Ocultar loader
        if (window.Loader) {
            window.Loader.hide();
        }
        console.error('Error al crear moto:', error);
        Notify.error('Error al crear la moto. Por favor intenta de nuevo.');
    }
}

/**
 * Abrir modal para editar una mesa (moto)
 */
function abrirModalEditarMesa(mesaId, nombreActual) {
    const modal = document.getElementById('modalEditarMesa');
    const inputId = document.getElementById('editarMesaId');
    const inputNombre = document.getElementById('editarMesaNombre');
    
    if (modal && inputId && inputNombre) {
        inputId.value = mesaId;
        inputNombre.value = nombreActual;
        modal.showModal();
        inputNombre.focus();
        inputNombre.select();
    }
}

/**
 * Cerrar modal de editar mesa
 */
function cerrarModalEditarMesa() {
    const modal = document.getElementById('modalEditarMesa');
    if (modal) {
        modal.close();
    }
}

/**
 * Guardar edición de mesa
 */
async function guardarEdicionMesa() {
    const inputId = document.getElementById('editarMesaId');
    const inputNombre = document.getElementById('editarMesaNombre');
    
    if (!inputId || !inputNombre) {
        Notify.error('Error: No se encontraron los campos del formulario');
        return;
    }

    const mesaId = parseInt(inputId.value);
    const nuevoNombre = inputNombre.value.trim();
    
    if (!mesaId || mesaId <= 0) {
        Notify.error('Error: ID de mesa inválido');
        return;
    }
    
    if (!nuevoNombre) {
        Notify.warning('Debes ingresar un nombre para la moto');
        inputNombre.focus();
        return;
    }

    // Mostrar loader
    if (window.Loader) {
        window.Loader.show('Actualizando moto...', 'Por favor espere');
    }

    try {
        const response = await fetch('/pos/editar-mesa-rapida', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify({ Id: mesaId, Nombre: nuevoNombre })
        });

        const data = await response.json();

        // Ocultar loader
        if (window.Loader) {
            window.Loader.hide();
        }

        if (data.success) {
            // Cerrar el modal
            cerrarModalEditarMesa();
            
            // Actualizar el nombre en la card del DOM
            actualizarNombreMesaEnDOM(mesaId, data.mesa.nombre);
            
            // Si la mesa está seleccionada actualmente, actualizar también el estado
            if (mesaSeleccionada && mesaSeleccionada.id === mesaId) {
                mesaSeleccionada.nombre = data.mesa.nombre;
                mostrarMesaSeleccionada(data.mesa.nombre);
            }
            
            // Mostrar notificación
            setTimeout(() => {
                Notify.success(data.message || `Moto actualizada exitosamente`);
            }, 100);
        } else {
            Notify.error(data.message || 'Error al actualizar la moto');
        }
    } catch (error) {
        // Ocultar loader
        if (window.Loader) {
            window.Loader.hide();
        }
        console.error('Error al actualizar moto:', error);
        Notify.error('Error al actualizar la moto. Por favor intenta de nuevo.');
    }
}

/**
 * Actualizar el nombre de una mesa en el DOM
 */
function actualizarNombreMesaEnDOM(mesaId, nuevoNombre) {
    const cardContainer = document.querySelector(`[data-mesa-id="${mesaId}"]`);
    if (!cardContainer) return;
    
    // Buscar el h3 que contiene el nombre
    const nombreElement = cardContainer.querySelector('h3');
    if (nombreElement) {
        nombreElement.textContent = nuevoNombre;
    }
    
    // Actualizar el onclick del botón de selección si existe
    const botonSeleccionar = cardContainer.querySelector('button[onclick*="seleccionarMesa"]');
    if (botonSeleccionar) {
        const nombreEscapado = escapeJs(nuevoNombre);
        botonSeleccionar.setAttribute('onclick', `seleccionarMesa(${mesaId}, '${nombreEscapado}')`);
    }
}

/**
 * Confirmar eliminación de mesa
 */
async function confirmarEliminarMesa(mesaId, nombre) {
    // Verificar si la mesa tiene items en el carrito
    const carritoMesa = obtenerCarritoMesa(mesaId);
    const tieneItems = carritoMesa && carritoMesa.length > 0;
    
    let mensaje = `¿Estás seguro de que deseas eliminar la moto "${nombre}"?`;
    if (tieneItems) {
        mensaje += `\n\n⚠️ Esta moto tiene ${carritoMesa.length} item(s) en el carrito. Se perderán al eliminar.`;
    }
    
    const confirmado = await showConfirm(mensaje, 'Eliminar Moto');
    if (confirmado) {
        eliminarMesa(mesaId);
    }
}

/**
 * Eliminar una mesa
 */
async function eliminarMesa(mesaId) {
    // Si la mesa está seleccionada, limpiar la selección primero
    if (mesaSeleccionada && mesaSeleccionada.id === mesaId) {
        limpiarMesaSeleccionada();
    }
    
    // Limpiar el carrito de la mesa del sessionStorage
    const key = `carrito_mesa_${mesaId}`;
    sessionStorage.removeItem(key);
    
    // Mostrar loader
    if (window.Loader) {
        window.Loader.show('Eliminando moto...', 'Por favor espere');
    }

    try {
        const response = await fetch('/pos/eliminar-mesa-rapida', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify({ Id: mesaId })
        });

        const data = await response.json();

        // Ocultar loader
        if (window.Loader) {
            window.Loader.hide();
        }

        if (data.success) {
            // Invalidar caché de la mesa eliminada
            if (window.CacheManager && window.CacheManager.invalidar) {
                window.CacheManager.invalidar('mesa', mesaId);
            }
            
            // Remover la card del DOM
            removerMesaDelDOM(mesaId);
            
            // Mostrar notificación
            Notify.success(data.message || 'Moto eliminada exitosamente');
        } else {
            Notify.error(data.message || 'Error al eliminar la moto');
        }
    } catch (error) {
        // Ocultar loader
        if (window.Loader) {
            window.Loader.hide();
        }
        console.error('Error al eliminar moto:', error);
        Notify.error('Error al eliminar la moto. Por favor intenta de nuevo.');
    }
}

/**
 * Remover una mesa del DOM
 */
function removerMesaDelDOM(mesaId) {
    const cardContainer = document.querySelector(`[data-mesa-id="${mesaId}"]`);
    if (cardContainer && cardContainer.parentNode) {
        cardContainer.parentNode.removeChild(cardContainer);
        
        // Si era la última moto, mostrar el mensaje "No hay motos registradas"
        const listaMesas = document.getElementById('listaMesas');
        if (listaMesas && listaMesas.children.length === 0) {
            const contenidoMesas = document.getElementById('contenidoMesas');
            if (contenidoMesas) {
                // Remover el grid vacío
                listaMesas.remove();
                
                // Crear y mostrar el mensaje "No hay motos"
                const mensajeVacio = document.createElement('div');
                mensajeVacio.className = 'flex flex-col items-center justify-center h-full text-center py-12';
                mensajeVacio.innerHTML = `
                    <div class="text-6xl mb-4">🏍️</div>
                    <h3 class="text-xl font-bold mb-2">No hay motos registradas</h3>
                    <p class="opacity-60 mb-4">Crea motos desde el módulo de gestión de motos</p>
                    <a href="/mesas" class="btn btn-primary">Ir a Gestión de Motos</a>
                `;
                contenidoMesas.appendChild(mensajeVacio);
            }
        }
    }
}

// Hacer funciones globales
window.seleccionarMesa = seleccionarMesa;
window.limpiarMesaSeleccionada = limpiarMesaSeleccionada;
window.cargarBadgesMesas = cargarBadgesMesas;
window.actualizarTotalPendienteMesa = actualizarTotalPendienteMesa;
window.actualizarBadgeMesa = actualizarBadgeMesa;
window.actualizarBordeColorMesa = actualizarBordeColorMesa;
window.abrirModalNuevaMotoRapida = abrirModalNuevaMotoRapida;
window.cerrarModalNuevaMotoRapida = cerrarModalNuevaMotoRapida;
window.crearMotoRapida = crearMotoRapida;
window.abrirModalEditarMesa = abrirModalEditarMesa;
window.cerrarModalEditarMesa = cerrarModalEditarMesa;
window.guardarEdicionMesa = guardarEdicionMesa;
window.confirmarEliminarMesa = confirmarEliminarMesa;

