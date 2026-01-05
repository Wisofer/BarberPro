/**
 * POS Cart Management
 * Manejo del carrito de compras y operaciones relacionadas
 * 
 * RESPONSABILIDADES:
 * - Agregar/quitar productos y servicios al carrito
 * - Gestionar cantidades y precios
 * - Validar descuentos y promociones
 * - Renderizar el carrito en la UI
 * - Calcular totales y descuentos
 */

/**
 * Agregar producto al carrito
 * @param {number} productoId - ID del producto
 * @param {string} nombre - Nombre del producto
 * @param {number} precioEfectivo - Precio final (puede tener descuento de promoción)
 * @param {number} stockMostrado - Stock mostrado en la UI
 * @param {number} [precioOriginal] - Precio original sin descuento (opcional, para promociones)
 */
function agregarProductoAlCarrito(productoId, nombre, precioEfectivo, stockMostrado, precioOriginal = null) {
    fetch(`/pos/verificar-stock/${productoId}`)
        .then(async res => {
            const contentType = res.headers.get('content-type');
            if (!contentType || !contentType.includes('application/json')) {
                const text = await res.text();
                throw new Error(`Error del servidor: ${res.status} ${res.statusText}. ${text}`);
            }
            return res.json();
        })
        .then(data => {
            if (!data.success) {
                Notify.error(`Error: ${data.message}`);
                return;
            }

            const stockReal = data.stock;
            const tienePromocionReal = data.tienePromocion === true; // Verificar promoción desde el servidor
            
            if (stockMostrado !== stockReal) {
                actualizarTarjetaProducto(productoId, stockReal);
            }

            if (stockReal <= 0) {
                Notify.warning(`"${nombre}" sin stock disponible. Stock actual: ${stockReal}`);
                return;
            }

            // Si no se proporciona precio original, usar el precio efectivo (sin promoción)
            let precioOriginalFinal = precioOriginal !== null && precioOriginal > 0 ? precioOriginal : precioEfectivo;
            
            // Solo calcular descuento si realmente hay promoción activa Y hay diferencia de precios
            let descuentoAplicado = 0;
            let porcentajeDescuento = null;
            if (tienePromocionReal && precioOriginalFinal > precioEfectivo) {
                descuentoAplicado = precioOriginalFinal - precioEfectivo;
                porcentajeDescuento = precioOriginalFinal > 0 ? (descuentoAplicado / precioOriginalFinal) * 100 : 0;
            } else {
                // Si no hay promoción real, asegurar que no haya descuento
                precioOriginalFinal = precioEfectivo;
            }

            const itemExistente = carrito.find(item => item.productoId === productoId);
            if (itemExistente) {
                if (itemExistente.cantidad >= stockReal) {
                    Notify.warning(`No hay más stock para "${nombre}". Disponible: ${stockReal}, En carrito: ${itemExistente.cantidad}`);
                    return;
                }
                itemExistente.cantidad++;
                itemExistente.stock = stockReal;
                // Mantener precio original si no está definido o actualizar si hay promoción
                if (!itemExistente.precioUnitarioOriginal || precioOriginalFinal > precioEfectivo) {
                    itemExistente.precioUnitarioOriginal = precioOriginalFinal;
                    itemExistente.precioUnitario = precioEfectivo;
                    itemExistente.descuentoAplicado = descuentoAplicado;
                    itemExistente.porcentajeDescuento = porcentajeDescuento;
                }
            } else {
                carrito.push({
                    productoId,
                    nombre,
                    precioUnitario: precioEfectivo, // Precio final (puede tener descuento de promoción)
                    precioUnitarioOriginal: precioOriginalFinal, // Precio original sin descuento
                    cantidad: 1,
                    stock: stockReal,
                    descuentoAplicado: descuentoAplicado,
                    porcentajeDescuento: porcentajeDescuento
                });
            }

            actualizarCarrito();
            guardarCarritoActual();
            const buscarInput = getElementSafely('buscarInput');
            const resultadosDiv = getElementSafely('resultadosBusqueda');
            if (buscarInput) buscarInput.value = '';
            if (resultadosDiv) resultadosDiv.classList.add('hidden');
        })
        .catch(err => {
            console.error('Error:', err);
            Notify.error('Error al verificar stock');
        });
}

/**
 * Guardar carrito actual (si hay mesa seleccionada)
 */
async function guardarCarritoActual() {
    if (mesaSeleccionada && mesaSeleccionada.id) {
        await guardarCarritoMesa(mesaSeleccionada.id, carrito);
        
        // Calcular cantidad total de items (sumando todas las cantidades, no items únicos)
        const cantidadItems = carrito.reduce((sum, item) => sum + (item.cantidad || 0), 0);
        const totalPendiente = carrito.reduce((sum, item) => {
            const precio = item.precioUnitario || 0;
            const cantidad = item.cantidad || 0;
            return sum + (precio * cantidad);
        }, 0);

        // Actualizar badge y total pendiente en la vista de mesas
        if (window.actualizarBadgeMesa) {
            actualizarBadgeMesa(mesaSeleccionada.id, cantidadItems);
        }
        if (window.actualizarTotalPendienteMesa) {
            actualizarTotalPendienteMesa(mesaSeleccionada.id, totalPendiente);
        }
        // Actualizar borde de color si está habilitado
        if (window.actualizarBordeColorMesa) {
            actualizarBordeColorMesa(mesaSeleccionada.id, cantidadItems > 0);
        }
        // Las mesas se mantienen ordenadas por fecha de creación (no se reordenan)
    }
}

/**
 * Agregar servicio al carrito
 */
function agregarServicioAlCarrito(servicioId, nombre, precio) {
    const itemExistente = carrito.find(item => item.servicioId === servicioId && item.tipo === 'servicio');
    if (itemExistente) {
        itemExistente.cantidad++;
        // Mantener precio original si no está definido
        if (!itemExistente.precioUnitarioOriginal) {
            itemExistente.precioUnitarioOriginal = precio;
        }
    } else {
        carrito.push({
            tipo: 'servicio',
            servicioId,
            nombre,
            precioUnitario: precio, // Precio final (puede tener descuento aplicado)
            precioUnitarioOriginal: precio, // Precio original sin descuento
            cantidad: 1,
            descuentoAplicado: 0,
            porcentajeDescuento: null
        });
    }
    actualizarCarrito();
    const buscarInput = getElementSafely('buscarInput');
    const resultadosDiv = getElementSafely('resultadosBusqueda');
    if (buscarInput) buscarInput.value = '';
    if (resultadosDiv) resultadosDiv.classList.add('hidden');
}

/**
 * Validar y corregir descuentos incorrectos en el carrito
 * Verifica contra el servidor qué productos realmente tienen promoción activa
 */
async function validarYCorregirDescuentosCarrito() {
    // ❌ DESHABILITADO: Esta validación estaba reseteando descuentos manuales
    // Los descuentos manuales aplicados por el usuario (con el botón 💸)
    // no deben ser validados contra promociones del servidor
    return false;
    
    /* CÓDIGO ORIGINAL DESHABILITADO:
    let carritoModificado = false;
    
    // Obtener productos con descuentos que necesitan validación
    const productosParaValidar = carrito.filter(item => 
        item.tipo !== 'servicio' && 
        item.productoId && 
        item.precioUnitarioOriginal && 
        item.precioUnitario &&
        item.precioUnitarioOriginal > item.precioUnitario
    );
    
    if (productosParaValidar.length === 0) {
        return false;
    }
    
    // Validar cada producto contra el servidor
    const promesas = productosParaValidar.map(async (item) => {
        // Si los precios son iguales o inconsistentes, corregir directamente
        const diferencia = Math.abs(item.precioUnitarioOriginal - item.precioUnitario);
        if (diferencia < 0.01) {
            item.descuentoAplicado = 0;
            item.porcentajeDescuento = null;
            item.precioUnitarioOriginal = item.precioUnitario;
            return true;
        }
        if (item.precioUnitarioOriginal < item.precioUnitario) {
            item.precioUnitarioOriginal = item.precioUnitario;
            item.descuentoAplicado = 0;
            item.porcentajeDescuento = null;
            return true;
        }
        
        // Verificar en el servidor si el producto tiene promoción activa
        try {
            const response = await fetch(\`/pos/verificar-stock/\${item.productoId}\`);
            const data = await response.json();
            
            // Si el producto NO tiene promoción activa, resetear descuento
            // Verificar explícitamente que tienePromocion sea false (no undefined, no null, no true)
            if (data.success && data.tienePromocion === false) {
                item.precioUnitarioOriginal = item.precioUnitario;
                item.descuentoAplicado = 0;
                item.porcentajeDescuento = null;
                return true;
            }
            // Si tienePromocion es true o undefined, mantener el descuento (podría ser válido)
        } catch (error) {
            console.error(\`Error al validar producto \${item.productoId}:\`, error);
        }
        
        return false;
    });
    
    const resultados = await Promise.all(promesas);
    carritoModificado = resultados.some(r => r === true);
    
    return carritoModificado;
    */
}

/**
 * Actualizar visualización del carrito
 */
function actualizarCarrito() {
    const carritoDiv = getElementSafely('carrito');
    const formularioPago = getElementSafely('formularioPago');

    if (!carritoDiv) {
        console.error('Error: Elemento carrito no encontrado');
        return;
    }

    if (carrito.length === 0) {
        carritoDiv.innerHTML = `
            <div class="text-center py-12 opacity-30">
                <div class="w-16 h-16 mx-auto mb-3 rounded-full bg-base-300/50 flex items-center justify-center">
                    <span class="text-2xl">🛒</span>
                </div>
                <p class="text-xs font-medium">Carrito vacío</p>
                <p class="text-[10px] opacity-60 mt-1">Agrega productos o servicios</p>
            </div>`;
        if (formularioPago) formularioPago.style.display = 'none';
        actualizarTotales();
        return;
    }

    if (formularioPago) formularioPago.style.display = 'block';

    renderizarCarrito();
}

/**
 * Renderizar el contenido HTML del carrito
 */
function renderizarCarrito() {
    const carritoDiv = getElementSafely('carrito');
    if (!carritoDiv) return;
    
    carritoDiv.innerHTML = carrito.map((item, index) => {
        const esServicio = item.tipo === 'servicio';
        const iconoTipo = esServicio ? '🔧' : '📦';
        const stockInfo = !esServicio && item.stock !== undefined ? item.stock : '';
        
        // Manejar descuentos y aumentos - MEJORADO
        const precioOriginalNum = parseFloat(item.precioUnitarioOriginal || item.precioUnitario);
        const precioFinalNum = parseFloat(item.precioUnitario);
        const descuentoAplicadoNum = parseFloat(item.descuentoAplicado || 0);
        const aumentoAplicadoNum = parseFloat(item.aumentoAplicado || 0);
        
        // Detectar si tiene descuento o aumento
        const diferencia = precioFinalNum - precioOriginalNum;
        const tieneDescuento = diferencia < -0.01 || descuentoAplicadoNum > 0;
        const tieneAumento = diferencia > 0.01 || aumentoAplicadoNum > 0;
        const tieneAjuste = tieneDescuento || tieneAumento;
        
        const precioOriginal = formatearPrecio(precioOriginalNum);
        const precioFinal = formatearPrecio(precioFinalNum);
        const subtotal = formatearPrecio(precioFinalNum * item.cantidad);
        const descuentoTotal = tieneDescuento ? (precioOriginalNum - precioFinalNum) * item.cantidad : 0;
        const aumentoTotal = tieneAumento ? (precioFinalNum - precioOriginalNum) * item.cantidad : 0;
        const porcentajeDescuento = item.porcentajeDescuento ? item.porcentajeDescuento.toFixed(1) : 
                                    (tieneDescuento && precioOriginalNum > 0 ? (((precioOriginalNum - precioFinalNum) / precioOriginalNum) * 100).toFixed(1) : null);
        const porcentajeAumento = item.porcentajeAumento ? item.porcentajeAumento.toFixed(1) : 
                                  (tieneAumento && precioOriginalNum > 0 ? (((precioFinalNum - precioOriginalNum) / precioOriginalNum) * 100).toFixed(1) : null);
        
        // Determinar clase de borde según tipo de ajuste
        let borderClass = 'border-base-300/50';
        if (tieneDescuento) borderClass = 'border-success/50 bg-success/5';
        else if (tieneAumento) borderClass = 'border-warning/50 bg-warning/5';
        
        return `
        <div class="group relative bg-base-100 rounded-lg border ${borderClass} hover:border-base-300 hover:shadow-sm transition-all">
            <div class="p-2 flex items-center gap-2">
                <!-- Icono tipo grande -->
                <div class="flex-shrink-0 w-8 h-8 rounded-md ${esServicio ? 'bg-info/20' : 'bg-secondary/20'} flex items-center justify-center text-sm">
                    ${iconoTipo}
                </div>
                
                <!-- Información principal compacta -->
                <div class="flex-1 min-w-0">
                    <div class="flex items-center gap-2 mb-0.5">
                        <h4 class="font-medium text-xs line-clamp-1 flex-1">${item.nombre}</h4>
                        ${stockInfo ? `<span class="badge badge-xs badge-ghost opacity-60">${stockInfo}</span>` : ''}
                    </div>
                    <div class="flex items-center gap-2 text-[10px]">
                        ${tieneAjuste ? `<span class="line-through opacity-50 text-base-content font-medium">${formatearPrecioConSimbolo(precioOriginalNum)}</span>` : ''}
                        <span class="${tieneDescuento ? 'text-success font-bold text-sm' : tieneAumento ? 'text-warning font-bold text-sm' : 'opacity-70'}">${formatearPrecioConSimbolo(precioFinalNum)}</span>
                        <span class="opacity-40">•</span>
                        <span class="font-semibold">${item.cantidad}x</span>
                        ${tieneAjuste ? `<span class="${tieneDescuento ? 'text-success' : 'text-warning'} font-semibold">= ${formatearPrecioConSimbolo(precioFinalNum * item.cantidad)}</span>` : ''}
                    </div>
                </div>
                
                <!-- Controles compactos -->
                <div class="flex items-center gap-1.5 flex-shrink-0">
                    <!-- Botón ajustar precio (más visible) -->
                    <button type="button" onclick="abrirModalDescuento(${index})" 
                            class="btn btn-ghost btn-sm h-6 px-1.5 text-primary hover:text-primary-focus hover:bg-primary/20 rounded transition-all border border-primary/30" 
                            title="Ajustar precio (aumentar o disminuir)">
                        <span class="text-xs">💸</span>
                    </button>
                    
                    <!-- Controles cantidad tipo stepper -->
                    <div class="flex flex-col gap-0.5">
                        <button type="button" onclick="cambiarCantidad(${index}, 1)" 
                                class="btn btn-xs h-4 w-4 min-h-0 p-0 text-[10px] leading-none rounded ${esServicio || (item.stock !== undefined && item.cantidad >= item.stock) ? 'btn-disabled' : 'btn-ghost'}"
                                title="+1">
                            ▲
                        </button>
                        <button type="button" onclick="cambiarCantidad(${index}, -1)" 
                                class="btn btn-xs h-4 w-4 min-h-0 p-0 text-[10px] leading-none rounded btn-ghost"
                                title="-1">
                            ▼
                        </button>
                    </div>
                    
                    <!-- Total destacado -->
                    ${!tieneDescuento ? `
                    <div class="text-right min-w-[55px]">
                        <div class="font-bold text-success text-xs">C$ ${subtotal}</div>
                    </div>
                    ` : ''}
                    
                    <!-- Botón eliminar siempre visible pero discreto -->
                    <button type="button" onclick="eliminarDelCarrito(${index})" 
                            class="btn btn-ghost btn-xs h-5 w-5 min-h-0 p-0 text-error/60 hover:text-error hover:bg-error/10 rounded transition-all" 
                            title="Eliminar">
                        <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                        </svg>
                    </button>
                </div>
            </div>
        </div>
    `;
    }).join('');

    actualizarTotales();
}

/**
 * Cambiar cantidad de un item en el carrito
 */
function cambiarCantidad(index, cambio) {
    // Validar que el índice sea válido
    if (index < 0 || index >= carrito.length) {
        console.error('Índice de carrito inválido:', index);
        return;
    }
    
    const item = carrito[index];
    if (!item) {
        console.error('Item no encontrado en el carrito:', index);
        return;
    }
    
    const nuevaCantidad = (item.cantidad || 0) + cambio;

    if (nuevaCantidad <= 0) {
        eliminarDelCarrito(index);
        return;
    }

    // Validar stock solo para productos
    if (item.tipo !== 'servicio' && item.stock !== undefined && nuevaCantidad > item.stock) {
        Notify.warning('No hay suficiente stock disponible');
        return;
    }

    // Asegurar que precioUnitarioOriginal esté definido
    if (!item.precioUnitarioOriginal) {
        item.precioUnitarioOriginal = item.precioUnitario;
    }

    item.cantidad = nuevaCantidad;
    actualizarCarrito();
    guardarCarritoActual();
}

/**
 * Eliminar item del carrito
 */
function eliminarDelCarrito(index) {
    // Validar que el índice sea válido
    if (index < 0 || index >= carrito.length) {
        console.error('Índice de carrito inválido para eliminar:', index);
        return;
    }
    
    carrito.splice(index, 1);
    actualizarCarrito();
    guardarCarritoActual();
}

/**
 * Actualizar totales del carrito (incluyendo descuentos y aumentos)
 */
function actualizarTotales() {
    const subtotal = carrito.reduce((sum, item) => sum + ((item.precioUnitario || 0) * (item.cantidad || 0)), 0);
    
    // Calcular descuentos totales
    const totalDescuentos = carrito.reduce((sum, item) => {
        const descuentoItem = (item.descuentoAplicado || 0) * item.cantidad;
        return sum + descuentoItem;
    }, 0);
    
    // Calcular aumentos totales
    const totalAumentos = carrito.reduce((sum, item) => {
        const aumentoItem = (item.aumentoAplicado || 0) * item.cantidad;
        return sum + aumentoItem;
    }, 0);
    
    const subtotalEl = getElementSafely('subtotal');
    const totalEl = getElementSafely('total');
    const descuentosEl = getElementSafely('totalDescuentos');
    const aumentosEl = getElementSafely('totalAumentos');
    
    if (subtotalEl) subtotalEl.textContent = formatearPrecioConSimbolo(subtotal);
    if (totalEl) totalEl.textContent = formatearPrecioConSimbolo(subtotal);
    
    // Mostrar/ocultar línea de descuentos totales si hay descuentos
    if (descuentosEl) {
        if (totalDescuentos > 0) {
            descuentosEl.style.display = 'flex';
            descuentosEl.querySelector('span:last-child').textContent = `-${formatearPrecioConSimbolo(totalDescuentos)}`;
        } else {
            descuentosEl.style.display = 'none';
        }
    }
    
    // Mostrar/ocultar línea de aumentos totales si hay aumentos
    if (aumentosEl) {
        if (totalAumentos > 0) {
            aumentosEl.style.display = 'flex';
            aumentosEl.querySelector('span:last-child').textContent = `+${formatearPrecioConSimbolo(totalAumentos)}`;
        } else {
            aumentosEl.style.display = 'none';
        }
    }

    const montoRecibidoEl = getElementSafely('montoRecibido');
    const montoRecibido = montoRecibidoEl ? parseFloat(montoRecibidoEl.value) || 0 : 0;
    
    // Si no hay monto recibido o el carrito está vacío, ocultar el vuelto
    if (montoRecibido > 0 && carrito.length > 0) {
        calcularVuelto();
    } else {
        // Limpiar y ocultar el vuelto
        const vueltoContainer = getElementSafely('vueltoContainer');
        const vueltoEl = getElementSafely('vuelto');
        if (vueltoContainer) vueltoContainer.classList.add('hidden');
        if (vueltoEl) vueltoEl.textContent = 'C$ 0.00';
    }
}

/**
 * Calcular vuelto considerando la moneda seleccionada
 */
function calcularVuelto() {
    const tipoPagoEl = getElementSafely('tipoPago');
    const monedaEl = getElementSafely('moneda');
    const montoRecibidoEl = getElementSafely('montoRecibido');
    const vueltoEl = getElementSafely('vuelto');
    const vueltoContainer = getElementSafely('vueltoContainer');
    
    if (!tipoPagoEl || !montoRecibidoEl || !vueltoEl || !vueltoContainer) return;
    
    const tipoPago = tipoPagoEl.value;
    const moneda = monedaEl ? monedaEl.value : 'C$';
    const montoRecibido = parseFloat(montoRecibidoEl.value) || 0;
    
    // Obtener total en córdobas (todos los productos están en C$)
    const totalCordobas = ordenSeleccionada 
        ? ordenSeleccionada.total 
        : carrito.reduce((sum, item) => sum + ((item.precioUnitario || 0) * (item.cantidad || 0)), 0);

    if (tipoPago === 'Mixto') {
        // Pago mixto: calcular vuelto de la parte física
        const monedaFisicoEl = getElementSafely('monedaFisico');
        const montoFisicoEl = getElementSafely('montoFisico');
        
        if (monedaFisicoEl && montoFisicoEl) {
            const monedaFisico = monedaFisicoEl.value;
            const montoFisico = parseFloat(montoFisicoEl.value) || 0;
            
            if (montoFisico > 0) {
                let vueltoCordobas = 0;
                
                // Calcular vuelto considerando que el total está en córdobas
                // IMPORTANTE: El vuelto físico SIEMPRE se da en córdobas, independientemente de la moneda de pago
                if (monedaFisico === 'C$') {
                    // Pago en córdobas: calcular vuelto directamente
                    vueltoCordobas = montoFisico - totalCordobas;
                } else {
                    // Pago en dólares: calcular vuelto en dólares y convertir a córdobas
                    const totalDolares = convertirCordobasADolares(totalCordobas, tipoCambio);
                    const vueltoDolares = montoFisico - totalDolares;
                    // Convertir vuelto a córdobas (el vuelto físico siempre se da en C$)
                    vueltoCordobas = convertirDolaresACordobas(vueltoDolares, tipoCambio);
                }
                
                if (vueltoCordobas >= 0) {
                    vueltoEl.textContent = formatearPrecioConSimbolo(vueltoCordobas);
                    vueltoContainer.classList.remove('hidden');
                } else {
                    vueltoEl.textContent = formatearPrecioConSimbolo(0);
                    vueltoContainer.classList.remove('hidden');
                }
            } else {
                vueltoContainer.classList.add('hidden');
            }
        } else {
            vueltoContainer.classList.add('hidden');
        }
    } else if ((tipoPago === 'Fisico') && montoRecibido > 0) {
        let vueltoCordobas = 0;
        
        // IMPORTANTE: El vuelto físico SIEMPRE se da en córdobas, independientemente de la moneda de pago
        if (moneda === 'C$') {
            // Pago en córdobas: calcular vuelto directamente
            vueltoCordobas = montoRecibido - totalCordobas;
        } else if (moneda === '$') {
            // Pago en dólares: calcular vuelto en dólares y convertir a córdobas
            const totalDolares = convertirCordobasADolares(totalCordobas, tipoCambio);
            const vueltoDolares = montoRecibido - totalDolares;
            // Convertir vuelto a córdobas (el vuelto físico siempre se da en C$)
            vueltoCordobas = convertirDolaresACordobas(vueltoDolares, tipoCambio);
        }
        
        if (vueltoCordobas >= 0) {
            vueltoEl.textContent = formatearPrecioConSimbolo(vueltoCordobas);
            vueltoContainer.classList.remove('hidden');
        } else {
            // Si no hay vuelto (pago insuficiente), mostrar 0
            vueltoEl.textContent = formatearPrecioConSimbolo(0);
            vueltoContainer.classList.remove('hidden');
        }
    } else {
        vueltoContainer.classList.add('hidden');
    }
    
    // Actualizar información de conversión y label del monto recibido
    if (monedaEl) {
        actualizarInfoConversion(monedaEl.value, totalCordobas);
        actualizarLabelMontoRecibido(monedaEl.value);
    }
}

/**
 * Actualizar el label del campo "Monto Recibido" según la moneda seleccionada
 */
function actualizarLabelMontoRecibido(moneda) {
    const labelMontoRecibido = document.querySelector('#montoRecibidoContainer label .label-text');
    if (labelMontoRecibido) {
        if (moneda === '$') {
            labelMontoRecibido.innerHTML = '💰 Monto Recibido <span class="badge badge-warning badge-sm">$ Dólares</span>';
        } else {
            labelMontoRecibido.innerHTML = '💰 Monto Recibido <span class="badge badge-success badge-sm">C$ Córdobas</span>';
        }
    }
    
    // Actualizar placeholder también con símbolo de moneda
    const montoRecibidoInput = getElementSafely('montoRecibido');
    if (montoRecibidoInput) {
        if (moneda === '$') {
            montoRecibidoInput.placeholder = `Ej: 1.13 (en dólares)`;
            // Agregar prefijo visual si es posible
            const montoRecibidoContainer = getElementSafely('montoRecibidoContainer');
            if (montoRecibidoContainer) {
                // Buscar si ya existe un prefijo
                let prefijo = montoRecibidoContainer.querySelector('.input-prefix');
                if (!prefijo) {
                    prefijo = document.createElement('span');
                    prefijo.className = 'input-prefix absolute left-3 top-1/2 -translate-y-1/2 text-lg font-bold text-warning';
                    prefijo.textContent = '$';
                    montoRecibidoInput.parentElement.style.position = 'relative';
                    montoRecibidoInput.style.paddingLeft = '2rem';
                    montoRecibidoInput.parentElement.appendChild(prefijo);
                } else {
                    prefijo.textContent = '$';
                    prefijo.className = 'input-prefix absolute left-3 top-1/2 -translate-y-1/2 text-lg font-bold text-warning';
                }
            }
        } else {
            montoRecibidoInput.placeholder = `Ej: 45.00 (en córdobas)`;
            // Remover prefijo si existe o cambiarlo a C$
            const montoRecibidoContainer = getElementSafely('montoRecibidoContainer');
            if (montoRecibidoContainer) {
                let prefijo = montoRecibidoContainer.querySelector('.input-prefix');
                if (prefijo) {
                    prefijo.textContent = 'C$';
                    prefijo.className = 'input-prefix absolute left-3 top-1/2 -translate-y-1/2 text-lg font-bold text-success';
                } else {
                    prefijo = document.createElement('span');
                    prefijo.className = 'input-prefix absolute left-3 top-1/2 -translate-y-1/2 text-lg font-bold text-success';
                    prefijo.textContent = 'C$';
                    montoRecibidoInput.parentElement.style.position = 'relative';
                    montoRecibidoInput.style.paddingLeft = '2rem';
                    montoRecibidoInput.parentElement.appendChild(prefijo);
                }
            }
        }
    }
}

/**
 * Limpiar carrito
 */
async function limpiarCarrito() {
    try {
        // Verificar que las funciones necesarias estén disponibles
        if (typeof Notify === 'undefined') {
            console.error('Notify no está disponible - usando fallback');
            // Fallback: crear notificación básica si Notify no está disponible
            const notification = document.createElement('div');
            notification.style.cssText = 'position:fixed;top:20px;right:20px;background:#ef4444;color:white;padding:15px;border-radius:8px;z-index:10000;';
            notification.textContent = 'Error: Sistema de notificaciones no disponible';
            document.body.appendChild(notification);
            setTimeout(() => notification.remove(), 3000);
            return;
        }

    if (carrito.length === 0) {
        Notify.info('El carrito ya está vacío');
        return;
    }
    
        // Verificar que showConfirm esté disponible
        if (typeof showConfirm === 'undefined') {
            // Fallback a confirm nativo si showConfirm no está disponible
            const confirmarLimpiar = confirm('¿Estás seguro de que deseas limpiar el carrito?');
            if (!confirmarLimpiar) {
                return;
            }
        } else {
    const confirmarLimpiar = await showConfirm('¿Estás seguro de que deseas limpiar el carrito?', 'Limpiar Carrito');
            if (!confirmarLimpiar) {
                return;
            }
        }
        
        // Si hay una mesa seleccionada, guardar el carrito vacío en la mesa
        if (typeof mesaSeleccionada !== 'undefined' && mesaSeleccionada && mesaSeleccionada.id) {
            const guardarFunc = window.guardarCarritoMesa || guardarCarritoMesa;
            if (typeof guardarFunc === 'function') {
                try {
                    await guardarFunc(mesaSeleccionada.id, []);
                    // Actualizar badge de la mesa
                    if (typeof window.actualizarBadgeMesa === 'function') {
                        window.actualizarBadgeMesa(mesaSeleccionada.id, 0);
                    } else if (typeof actualizarBadgeMesa === 'function') {
                        actualizarBadgeMesa(mesaSeleccionada.id, 0);
                    }
                    if (typeof window.actualizarTotalPendienteMesa === 'function') {
                        window.actualizarTotalPendienteMesa(mesaSeleccionada.id, 0);
                    } else if (typeof actualizarTotalPendienteMesa === 'function') {
                        actualizarTotalPendienteMesa(mesaSeleccionada.id, 0);
                    }
                } catch (err) {
                    console.error('Error al guardar carrito de mesa:', err);
                    // Continuar con la limpieza aunque falle el guardado
                }
            }
        }
        
        // Limpiar carrito
        carrito = [];
        ordenSeleccionada = null;
        
        // Actualizar visualización del carrito
        if (typeof actualizarCarrito === 'function') {
        actualizarCarrito();
        }
        
        // Guardar carrito vacío (se eliminará del storage)
        if (typeof guardarCarritoActual === 'function') {
            guardarCarritoActual();
        }
        
        // Limpiar campos del formulario
        const montoRecibidoEl = getElementSafely('montoRecibido');
        const clienteIdEl = getElementSafely('clienteId');
        if (montoRecibidoEl) montoRecibidoEl.value = '';
        if (clienteIdEl) clienteIdEl.value = '0';
        
        // Limpiar y ocultar el vuelto
        const vueltoContainer = getElementSafely('vueltoContainer');
        const vueltoEl = getElementSafely('vuelto');
        if (vueltoContainer) vueltoContainer.classList.add('hidden');
        if (vueltoEl) vueltoEl.textContent = 'C$ 0.00';
        
        // Limpiar campos de pago mixto si existen
        const montoFisicoEl = getElementSafely('montoFisico');
        const montoElectronicoEl = getElementSafely('montoElectronico');
        if (montoFisicoEl) montoFisicoEl.value = '';
        if (montoElectronicoEl) montoElectronicoEl.value = '';
        
        // Actualizar formulario de pago si existe la función
        if (typeof actualizarFormularioPago === 'function') {
            actualizarFormularioPago();
        }
        
        Notify.success('Carrito limpiado');
    } catch (error) {
        console.error('Error al limpiar carrito:', error);
        if (typeof Notify !== 'undefined') {
            Notify.error('Error al limpiar el carrito: ' + error.message);
        } else {
            console.error('Error al limpiar el carrito:', error);
        }
    }
}

