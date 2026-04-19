// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// ==== IMPLEMENTAÇÃO DO BOTTOM SHEET EM MOBILE PARA SELECTS ====
document.addEventListener('DOMContentLoaded', function() {
    const createBottomSheetHTML = () => {
        if (document.getElementById('mobileSelectBottomSheet')) return;
        const html = `
            <div class="offcanvas offcanvas-bottom" tabindex="-1" id="mobileSelectBottomSheet" style="border-radius: 20px 20px 0 0; height: auto; max-height: 85vh; z-index: 1060;">
                <div class="offcanvas-header pb-2 border-bottom">
                    <h5 class="offcanvas-title fw-bold" id="mobileSelectBottomSheetTitle" style="font-size: 1.1rem; color: var(--brutalist-black, #0f172a);">Selecione uma opção</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="offcanvas" aria-label="Close"></button>
                </div>
                <div class="offcanvas-body pt-2 px-0">
                    <div class="list-group list-group-flush" id="mobileSelectBottomSheetOptions">
                    </div>
                </div>
            </div>
        `;
        document.body.insertAdjacentHTML('beforeend', html);
    };

    const handleSelectClick = function(e) {
        if (window.innerWidth >= 992) return; // Apenas em mobile
        
        e.preventDefault(); 
        e.stopPropagation();

        const selectEl = this;
        createBottomSheetHTML();
        
        const bottomSheet = document.getElementById('mobileSelectBottomSheet');
        const titleEl = document.getElementById('mobileSelectBottomSheetTitle');
        const optionsContainer = document.getElementById('mobileSelectBottomSheetOptions');
        
        // Define o título baseado na label (vários formatos de layout suportados)
        const fieldLabel = selectEl.closest('div').querySelector('.order-mobile-field-label, .form-label-modern, label');
        let labelText = 'Selecione uma opção';
        if (fieldLabel) {
            labelText = fieldLabel.textContent.trim().replace('*', '');
        }
        titleEl.textContent = labelText;
        
        // Popula as opções
        optionsContainer.innerHTML = '';
        Array.from(selectEl.options).forEach(opt => {
            // Pula opções com value vazio ou "Selecione" (placeholder)
            if (opt.value === "" && opt.innerHTML.toLowerCase().includes("selecione")) return;

            const isSelected = opt.value === selectEl.value;
            const btnHtml = `
                <button type="button" class="list-group-item list-group-item-action d-flex align-items-center justify-content-between py-3 px-4 border-0 ${isSelected ? 'bg-light text-primary fw-bold' : ''}" data-value="${opt.value}">
                    <span style="font-size: 1rem;">${opt.innerHTML}</span>
                    ${isSelected ? '<i class="bi bi-check-circle-fill text-primary" style="font-size: 1.25rem;"></i>' : '<i class="bi bi-circle text-muted" style="font-size: 1.2rem; opacity: 0.3;"></i>'}
                </button>
            `;
            optionsContainer.insertAdjacentHTML('beforeend', btnHtml);
        });

        // Inicializa e mostra
        const bsOffcanvas = new bootstrap.Offcanvas(bottomSheet);
        bsOffcanvas.show();
        
        // Add click listeners to new buttons
        const optionBtns = optionsContainer.querySelectorAll('button');
        optionBtns.forEach(btn => {
            btn.addEventListener('click', function() {
                const val = this.getAttribute('data-value');
                selectEl.value = val;
                // Dispara o evento change original para o sistema (validação ou AJAX)
                selectEl.dispatchEvent(new Event('change', { bubbles: true }));
                bsOffcanvas.hide();
            });
        });

        // Força blur no select original caso o teclado do celular tente se abrir
        selectEl.blur();
    };

    // Aplica o interceptador a todos os selects com class .form-select-modern ou dentro do card mobile
    const mobileSelects = document.querySelectorAll('.order-mobile-card select, select.form-select-modern, .form-select, .form-control');
    mobileSelects.forEach(select => {
        if (select.tagName === 'SELECT') {
            select.addEventListener('mousedown', handleSelectClick);
            select.addEventListener('touchstart', handleSelectClick, { passive: false });
        }
    });
});
