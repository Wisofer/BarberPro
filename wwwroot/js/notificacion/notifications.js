// Esperar a que el DOM esté listo
document.addEventListener('DOMContentLoaded', function() {
    // Vista previa de plantilla seleccionada
    const selectedTemplate = document.getElementById('selectedTemplate');
    if (selectedTemplate) {
        selectedTemplate.addEventListener('change', function() {
            const option = this.options[this.selectedIndex];
            if (this.value) {
                const previewSection = document.getElementById('previewSection');
                const previewTitle = document.getElementById('previewTitle');
                const previewBody = document.getElementById('previewBody');
                const previewImage = document.getElementById('previewImage');
                
                if (previewTitle) previewTitle.textContent = option.dataset.title || '';
                if (previewBody) previewBody.textContent = option.dataset.body || '';
                
                if (previewImage) {
                    if (option.dataset.image) {
                        previewImage.src = option.dataset.image;
                        previewImage.classList.remove('hidden');
                    } else {
                        previewImage.classList.add('hidden');
                    }
                }
                
                if (previewSection) previewSection.classList.remove('hidden');
            } else {
                const previewSection = document.getElementById('previewSection');
                if (previewSection) previewSection.classList.add('hidden');
            }
        });
    }
});

// Crear plantilla
const createTemplateForm = document.getElementById('createTemplateForm');
if (createTemplateForm) {
    createTemplateForm.addEventListener('submit', async function(e) {
        e.preventDefault();
        
        const btn = this.querySelector('button[type="submit"]');
        const originalText = btn.innerHTML;
        btn.disabled = true;
        btn.innerHTML = '<span class="loading loading-spinner"></span> Guardando...';
        
        try {
            const response = await fetch('/admin/notifications/create-template', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    name: document.getElementById('templateName').value,
                    title: document.getElementById('templateTitle').value,
                    body: document.getElementById('templateBody').value,
                    imageUrl: document.getElementById('templateImageUrl').value || null
                })
            });
            
            const result = await response.json();
            
            if (result.success) {
                Notify.success(result.message);
                // Limpiar formulario
                this.reset();
                // Recargar página para mostrar nueva plantilla
                setTimeout(() => location.reload(), 1000);
            } else {
                Notify.error(result.message);
                btn.disabled = false;
                btn.innerHTML = originalText;
            }
        } catch (error) {
            Notify.error('Error al crear plantilla: ' + error.message);
            btn.disabled = false;
            btn.innerHTML = originalText;
        }
    });
}

// Toggle selección de usuarios
const sendToAllCheckbox = document.getElementById('sendToAll');
if (sendToAllCheckbox) {
    sendToAllCheckbox.addEventListener('change', function() {
        const userSelectionContainer = document.getElementById('userSelectionContainer');
        const sendButtonText = document.getElementById('sendButtonText');
        
        if (this.checked) {
            if (userSelectionContainer) userSelectionContainer.classList.add('hidden');
            if (sendButtonText) sendButtonText.textContent = 'Enviar a Todos los Barberos';
            // Deseleccionar todos los checkboxes individuales
            document.querySelectorAll('.barber-checkbox').forEach(cb => cb.checked = false);
            const selectAllCheckbox = document.getElementById('selectAllCheckbox');
            if (selectAllCheckbox) selectAllCheckbox.checked = false;
            updateSelectedCount();
        } else {
            if (userSelectionContainer) userSelectionContainer.classList.remove('hidden');
            if (sendButtonText) sendButtonText.textContent = 'Enviar a Barberos Seleccionados';
        }
    });
}

// Actualizar contador de seleccionados
function updateSelectedCount() {
    const selected = document.querySelectorAll('.barber-checkbox:checked').length;
    const total = document.querySelectorAll('.barber-checkbox').length;
    const countElement = document.getElementById('selectedCount');
    
    if (!countElement) return;
    
    if (selected === 0) {
        countElement.textContent = '0 barberos seleccionados';
        countElement.className = 'text-gray-500';
    } else if (selected === total && total > 0) {
        countElement.textContent = `✓ Todos los barberos seleccionados (${selected})`;
        countElement.className = 'text-success font-semibold';
    } else {
        countElement.textContent = `${selected} de ${total} barberos seleccionados`;
        countElement.className = 'text-primary font-semibold';
    }
}

// Seleccionar/deseleccionar todos los barberos
function toggleAllBarbers(checked) {
    document.querySelectorAll('.barber-checkbox').forEach(cb => {
        cb.checked = checked;
    });
    updateSelectedCount();
}

// Seleccionar todos los barberos
function selectAllBarbers() {
    const selectAllCheckbox = document.getElementById('selectAllCheckbox');
    if (selectAllCheckbox) {
        selectAllCheckbox.checked = true;
        toggleAllBarbers(true);
    }
}

// Deseleccionar todos los barberos
function deselectAllBarbers() {
    const selectAllCheckbox = document.getElementById('selectAllCheckbox');
    if (selectAllCheckbox) {
        selectAllCheckbox.checked = false;
        toggleAllBarbers(false);
    }
}

// Mejorar la interacción de las filas de la tabla
function initializeBarberCards() {
    document.querySelectorAll('.barber-row').forEach(row => {
        row.addEventListener('click', function(e) {
            // No toggle si se hace clic directamente en el checkbox
            if (e.target.type === 'checkbox') return;
            
            const checkbox = this.querySelector('.barber-checkbox');
            if (checkbox) {
                checkbox.checked = !checkbox.checked;
                updateSelectedCount();
                
                // Actualizar checkbox de "seleccionar todos"
                const selectAllCheckbox = document.getElementById('selectAllCheckbox');
                if (selectAllCheckbox) {
                    const allChecked = document.querySelectorAll('.barber-checkbox:checked').length === document.querySelectorAll('.barber-checkbox').length;
                    selectAllCheckbox.checked = allChecked;
                }
            }
        });
    });
}

// Actualizar estilo visual cuando cambia el checkbox
function initializeBarberCheckboxes() {
    document.querySelectorAll('.barber-checkbox').forEach(checkbox => {
        checkbox.addEventListener('change', function() {
            updateSelectedCount();
            
            // Actualizar checkbox de "seleccionar todos"
            const selectAllCheckbox = document.getElementById('selectAllCheckbox');
            if (selectAllCheckbox) {
                const allChecked = document.querySelectorAll('.barber-checkbox:checked').length === document.querySelectorAll('.barber-checkbox').length;
                selectAllCheckbox.checked = allChecked;
            }
        });
    });
}

// Enviar notificación
const sendNotificationForm = document.getElementById('sendNotificationForm');
if (sendNotificationForm) {
    sendNotificationForm.addEventListener('submit', async function(e) {
        e.preventDefault();
        
        const selectedTemplate = document.getElementById('selectedTemplate');
        if (!selectedTemplate) {
            Notify.error('Error: No se encontró el selector de plantilla');
            return;
        }
        
        const templateId = selectedTemplate.value;
        if (!templateId) {
            Notify.error('Selecciona una plantilla');
            return;
        }
        
        // Determinar destinatarios
        let userIds = null;
        const sendToAllCheckbox = document.getElementById('sendToAll');
        const sendToAll = sendToAllCheckbox ? sendToAllCheckbox.checked : false;
        
        if (!sendToAll) {
            const selectedUsers = Array.from(document.querySelectorAll('.barber-checkbox:checked'))
                .map(checkbox => parseInt(checkbox.value));
            
            if (selectedUsers.length === 0) {
                Notify.error('Selecciona al menos un barbero o marca "Seleccionar todos"');
                return;
            }
            
            userIds = selectedUsers;
        }
        
        const btn = document.getElementById('sendBtn');
        if (!btn) {
            Notify.error('Error: No se encontró el botón de envío');
            return;
        }
        const originalText = btn.innerHTML;
        btn.disabled = true;
        btn.innerHTML = '<span class="loading loading-spinner"></span> Enviando...';
        
        try {
            const dataOnlyCheckbox = document.getElementById('dataOnly');
            const requestBody = {
                templateId: parseInt(templateId),
                dataOnly: dataOnlyCheckbox ? dataOnlyCheckbox.checked : false,
                extraData: null
            };
            
            if (userIds !== null) {
                requestBody.userIds = userIds;
            }
            
            const response = await fetch('/admin/notifications/send', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(requestBody)
            });
            
            const result = await response.json();
            
            if (result.success) {
                Notify.success(result.message);
                // Limpiar formulario
                const selectedTemplate = document.getElementById('selectedTemplate');
                if (selectedTemplate) selectedTemplate.value = '';
                
                const previewSection = document.getElementById('previewSection');
                if (previewSection) previewSection.classList.add('hidden');
                
                const dataOnly = document.getElementById('dataOnly');
                if (dataOnly) dataOnly.checked = false;
                
                const sendToAll = document.getElementById('sendToAll');
                if (sendToAll) sendToAll.checked = false;
                
                const userSelectionContainer = document.getElementById('userSelectionContainer');
                if (userSelectionContainer) userSelectionContainer.classList.remove('hidden');
                
                const sendButtonText = document.getElementById('sendButtonText');
                if (sendButtonText) sendButtonText.textContent = 'Enviar a Barberos Seleccionados';
                
                // Deseleccionar todos los checkboxes
                document.querySelectorAll('.barber-checkbox').forEach(cb => {
                    cb.checked = false;
                });
                
                const selectAllCheckbox = document.getElementById('selectAllCheckbox');
                if (selectAllCheckbox) {
                    selectAllCheckbox.checked = false;
                }
                updateSelectedCount();
            } else {
                Notify.error(result.message);
            }
        } catch (error) {
            Notify.error('Error al enviar notificación: ' + error.message);
        } finally {
            btn.disabled = false;
            btn.innerHTML = originalText;
        }
    });
}

// Seleccionar plantilla desde la tabla
function selectTemplate(templateId) {
    document.getElementById('selectedTemplate').value = templateId;
    document.getElementById('selectedTemplate').dispatchEvent(new Event('change'));
    // Scroll al formulario de envío
    document.getElementById('sendNotificationForm').scrollIntoView({ behavior: 'smooth' });
}

// Editar plantilla (función global)
function editTemplate(id, name, title, body, imageUrl) {
    console.log('editTemplate llamado:', { id, name, title, body, imageUrl });
    
    const editTemplateId = document.getElementById('editTemplateId');
    const editTemplateName = document.getElementById('editTemplateName');
    const editTemplateTitle = document.getElementById('editTemplateTitle');
    const editTemplateBody = document.getElementById('editTemplateBody');
    const editTemplateImageUrl = document.getElementById('editTemplateImageUrl');
    
    if (!editTemplateId || !editTemplateName || !editTemplateTitle || !editTemplateBody || !editTemplateImageUrl) {
        console.error('Elementos del formulario de edición no encontrados');
        Notify.error('Error: No se encontraron los elementos del formulario');
        return;
    }
    
    editTemplateId.value = id;
    editTemplateName.value = name || '';
    editTemplateTitle.value = title || '';
    editTemplateBody.value = body || '';
    editTemplateImageUrl.value = imageUrl || '';
    
    const modal = document.getElementById('editTemplateModal');
    if (modal) {
        if (typeof modal.showModal === 'function') {
            modal.showModal();
        } else {
            // Fallback para navegadores que no soportan showModal
            modal.style.display = 'block';
            modal.classList.add('modal-open');
        }
    } else {
        console.error('Modal de edición no encontrado');
        Notify.error('Error: No se encontró el modal de edición');
    }
}

// Event listeners para botones de editar y borrar usando delegación de eventos
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOMContentLoaded - Inicializando notificaciones');
    
    // Inicializar cards y checkboxes de barberos
    initializeBarberCards();
    initializeBarberCheckboxes();
    
    // Usar delegación de eventos para botones de editar (funciona incluso si se agregan dinámicamente)
    document.addEventListener('click', function(e) {
        // Botón de editar
        if (e.target.closest('.edit-template-btn')) {
            e.preventDefault();
            e.stopPropagation();
            const btn = e.target.closest('.edit-template-btn');
            const id = btn.getAttribute('data-template-id');
            const name = btn.getAttribute('data-template-name') || '';
            const title = btn.getAttribute('data-template-title') || '';
            const body = btn.getAttribute('data-template-body') || '';
            const imageUrl = btn.getAttribute('data-template-image') || '';
            
            console.log('Click en botón editar:', { id, name, title });
            editTemplate(parseInt(id), name, title, body, imageUrl);
            return false;
        }
        
        // Botón de borrar
        if (e.target.closest('.delete-template-btn')) {
            e.preventDefault();
            e.stopPropagation();
            const btn = e.target.closest('.delete-template-btn');
            const id = btn.getAttribute('data-template-id');
            const title = btn.getAttribute('data-template-title') || 'esta plantilla';
            
            console.log('Click en botón borrar:', { id, title });
            deleteTemplate(parseInt(id), title);
            return false;
        }
    });
    
    // También registrar directamente por si acaso
    setTimeout(function() {
        const editButtons = document.querySelectorAll('.edit-template-btn');
        const deleteButtons = document.querySelectorAll('.delete-template-btn');
        console.log('Botones encontrados:', { edit: editButtons.length, delete: deleteButtons.length });
        
        editButtons.forEach(btn => {
            btn.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                const id = this.getAttribute('data-template-id');
                const name = this.getAttribute('data-template-name') || '';
                const title = this.getAttribute('data-template-title') || '';
                const body = this.getAttribute('data-template-body') || '';
                const imageUrl = this.getAttribute('data-template-image') || '';
                editTemplate(parseInt(id), name, title, body, imageUrl);
                return false;
            });
        });
        
        deleteButtons.forEach(btn => {
            btn.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                const id = this.getAttribute('data-template-id');
                const title = this.getAttribute('data-template-title') || 'esta plantilla';
                deleteTemplate(parseInt(id), title);
                return false;
            });
        });
    }, 100);
    
    // Formulario de edición
    const editTemplateForm = document.getElementById('editTemplateForm');
    if (editTemplateForm) {
        editTemplateForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            const templateId = document.getElementById('editTemplateId').value;
            const btn = this.querySelector('button[type="submit"]');
            const originalText = btn.innerHTML;
            btn.disabled = true;
            btn.innerHTML = '<span class="loading loading-spinner"></span> Guardando...';
            
            try {
                const response = await fetch(`/admin/notifications/update-template/${templateId}`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        name: document.getElementById('editTemplateName').value,
                        title: document.getElementById('editTemplateTitle').value,
                        body: document.getElementById('editTemplateBody').value,
                        imageUrl: document.getElementById('editTemplateImageUrl').value || null
                    })
                });
                
                const result = await response.json();
                
                if (result.success) {
                    Notify.success(result.message);
                    closeEditModal();
                    // Recargar página para mostrar cambios
                    setTimeout(() => location.reload(), 1000);
                } else {
                    Notify.error(result.message);
                    btn.disabled = false;
                    btn.innerHTML = originalText;
                }
            } catch (error) {
                Notify.error('Error al actualizar plantilla: ' + error.message);
                btn.disabled = false;
                btn.innerHTML = originalText;
            }
        });
    }
});

// Cerrar modal de edición
function closeEditModal() {
    const modal = document.getElementById('editTemplateModal');
    if (modal && typeof modal.close === 'function') {
        modal.close();
    }
    document.getElementById('editTemplateForm').reset();
}


// Borrar plantilla (función global)
function deleteTemplate(id, title) {
    console.log('deleteTemplate llamado:', { id, title });
    
    if (!confirm(`¿Estás seguro de que deseas eliminar la plantilla "${title}"?`)) {
        return;
    }
    
    fetch(`/admin/notifications/delete-template/${id}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        }
    })
    .then(response => response.json())
    .then(result => {
        if (result.success) {
            Notify.success(result.message);
            // Recargar página para mostrar cambios
            setTimeout(() => location.reload(), 1000);
        } else {
            Notify.error(result.message);
        }
    })
    .catch(error => {
        console.error('Error al eliminar plantilla:', error);
        Notify.error('Error al eliminar plantilla: ' + error.message);
    });
}
