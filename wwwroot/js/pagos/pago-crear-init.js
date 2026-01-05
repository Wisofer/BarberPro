/**
 * Pago Crear - Initialization
 * Inicialización y funciones auxiliares para el formulario de creación de pago
 */

function mostrarPaso2() { 
    document.getElementById('paso2')?.classList.remove('paso-oculto'); 
    document.getElementById('paso2')?.classList.add('paso-activo'); 
}

function mostrarPaso3() { 
    document.getElementById('paso3')?.classList.remove('paso-oculto'); 
    document.getElementById('paso3')?.classList.add('paso-activo'); 
}

function mostrarPaso4() { 
    document.getElementById('paso4')?.classList.remove('paso-oculto'); 
    document.getElementById('paso4')?.classList.add('paso-activo'); 
}

function mostrarPaso5() { 
    document.getElementById('paso5')?.classList.remove('paso-oculto'); 
    document.getElementById('paso5')?.classList.add('paso-activo'); 
}

function mostrarPaso6() { 
    document.getElementById('paso6')?.classList.remove('paso-oculto'); 
    document.getElementById('paso6')?.classList.add('paso-activo'); 
}

function actualizarMonedaEnInterfaz() {
    const moneda = document.getElementById('Moneda')?.value;
    const grupoMontoCordobasFisico = document.getElementById('grupoMontoCordobasFisico');
    const grupoMontoDolaresFisico = document.getElementById('grupoMontoDolaresFisico');
    const grupoRecibidoUnico = document.getElementById('grupoRecibidoUnico');
    const grupoRecibidoCordobasFisico = document.getElementById('grupoRecibidoCordobasFisico');
    const grupoRecibidoDolaresFisico = document.getElementById('grupoRecibidoDolaresFisico');
    const grupoMontoCordobasElectronico = document.getElementById('grupoMontoCordobasElectronico');
    const grupoMontoDolaresElectronico = document.getElementById('grupoMontoDolaresElectronico');
    
    if (moneda === 'Ambos') {
        if (grupoMontoCordobasFisico) grupoMontoCordobasFisico.style.display = 'block';
        if (grupoMontoDolaresFisico) grupoMontoDolaresFisico.style.display = 'block';
        if (grupoRecibidoUnico) grupoRecibidoUnico.style.display = 'none';
        if (grupoRecibidoCordobasFisico) grupoRecibidoCordobasFisico.style.display = 'block';
        if (grupoRecibidoDolaresFisico) grupoRecibidoDolaresFisico.style.display = 'block';
        if (grupoMontoCordobasElectronico) grupoMontoCordobasElectronico.style.display = 'block';
        if (grupoMontoDolaresElectronico) grupoMontoDolaresElectronico.style.display = 'block';
    } else if (moneda === '$') {
        if (grupoMontoCordobasFisico) grupoMontoCordobasFisico.style.display = 'none';
        if (grupoMontoDolaresFisico) grupoMontoDolaresFisico.style.display = 'block';
        if (grupoRecibidoUnico) grupoRecibidoUnico.style.display = 'block';
        if (grupoRecibidoCordobasFisico) grupoRecibidoCordobasFisico.style.display = 'none';
        if (grupoRecibidoDolaresFisico) grupoRecibidoDolaresFisico.style.display = 'none';
        if (grupoMontoCordobasElectronico) grupoMontoCordobasElectronico.style.display = 'none';
        if (grupoMontoDolaresElectronico) grupoMontoDolaresElectronico.style.display = 'block';
        document.getElementById('monedaRecibidoLabel').textContent = '$';
    } else {
        if (grupoMontoCordobasFisico) grupoMontoCordobasFisico.style.display = 'block';
        if (grupoMontoDolaresFisico) grupoMontoDolaresFisico.style.display = 'none';
        if (grupoRecibidoUnico) grupoRecibidoUnico.style.display = 'block';
        if (grupoRecibidoCordobasFisico) grupoRecibidoCordobasFisico.style.display = 'none';
        if (grupoRecibidoDolaresFisico) grupoRecibidoDolaresFisico.style.display = 'none';
        if (grupoMontoCordobasElectronico) grupoMontoCordobasElectronico.style.display = 'block';
        if (grupoMontoDolaresElectronico) grupoMontoDolaresElectronico.style.display = 'none';
        document.getElementById('monedaRecibidoLabel').textContent = 'C$';
    }
}

// Hacer funciones globales
window.mostrarPaso2 = mostrarPaso2;
window.mostrarPaso3 = mostrarPaso3;
window.mostrarPaso4 = mostrarPaso4;
window.mostrarPaso5 = mostrarPaso5;
window.mostrarPaso6 = mostrarPaso6;
window.actualizarMonedaEnInterfaz = actualizarMonedaEnInterfaz;

