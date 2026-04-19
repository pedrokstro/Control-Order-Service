/**
 * Carregador de Componentes Bootstrap - Lazy Loading
 * Carrega e inicializa componentes Bootstrap apenas quando necessário
 */

class BootstrapLoader {
    constructor() {
        this.loadedComponents = new Set();
        this.loadingPromises = new Map();
        this.isBootstrapLoaded = false;
        
        // Mapeamento de componentes Bootstrap
        this.components = {
            modal: {
                selector: '[data-bs-toggle="modal"]',
                init: this.initModals.bind(this)
            },
            tooltip: {
                selector: '[data-bs-toggle="tooltip"]',
                init: this.initTooltips.bind(this)
            },
            popover: {
                selector: '[data-bs-toggle="popover"]',
                init: this.initPopovers.bind(this)
            },
            dropdown: {
                selector: '[data-bs-toggle="dropdown"]',
                init: this.initDropdowns.bind(this)
            },
            collapse: {
                selector: '[data-bs-toggle="collapse"]',
                init: this.initCollapse.bind(this)
            },
            carousel: {
                selector: '.carousel',
                init: this.initCarousels.bind(this)
            },
            offcanvas: {
                selector: '[data-bs-toggle="offcanvas"]',
                init: this.initOffcanvas.bind(this)
            },
            tab: {
                selector: '[data-bs-toggle="tab"], [data-bs-toggle="pill"]',
                init: this.initTabs.bind(this)
            }
        };
    }

    /**
     * Verifica se Bootstrap está carregado
     * @returns {boolean} - True se Bootstrap está disponível
     */
    checkBootstrapAvailability() {
        this.isBootstrapLoaded = typeof bootstrap !== 'undefined' || 
                                 (typeof window.bootstrap !== 'undefined');
        return this.isBootstrapLoaded;
    }

    /**
     * Carrega componente Bootstrap específico
     * @param {string} componentName - Nome do componente
     * @returns {Promise} - Promise de carregamento
     */
    async loadComponent(componentName) {
        // Verifica se componente existe
        if (!this.components[componentName]) {
            throw new Error(`Componente Bootstrap não reconhecido: ${componentName}`);
        }

        // Se já carregado, retorna imediatamente
        if (this.loadedComponents.has(componentName)) {
            console.log(`✅ Componente ${componentName} já carregado`);
            return Promise.resolve();
        }

        // Se já está carregando, retorna promise existente
        if (this.loadingPromises.has(componentName)) {
            console.log(`⏳ Aguardando carregamento de ${componentName}...`);
            return this.loadingPromises.get(componentName);
        }

        // Inicia carregamento
        const loadingPromise = this.performComponentLoad(componentName);
        this.loadingPromises.set(componentName, loadingPromise);

        try {
            await loadingPromise;
            this.loadedComponents.add(componentName);
            console.log(`✅ Componente ${componentName} carregado com sucesso!`);
        } catch (error) {
            console.error(`❌ Erro ao carregar componente ${componentName}:`, error);
            this.loadingPromises.delete(componentName);
            throw error;
        }

        return loadingPromise;
    }

    /**
     * Executa o carregamento do componente
     * @param {string} componentName - Nome do componente
     * @returns {Promise} - Promise de carregamento
     */
    async performComponentLoad(componentName) {
        console.log(`📦 Carregando componente Bootstrap: ${componentName}`);

        // Verifica se Bootstrap está disponível
        if (!this.checkBootstrapAvailability()) {
            console.log('⏳ Aguardando Bootstrap estar disponível...');
            
            // Aguarda Bootstrap estar disponível (máximo 5 segundos)
            let attempts = 0;
            while (!this.checkBootstrapAvailability() && attempts < 50) {
                await new Promise(resolve => setTimeout(resolve, 100));
                attempts++;
            }

            if (!this.checkBootstrapAvailability()) {
                throw new Error('Bootstrap não está disponível após timeout');
            }
        }

        // Inicializa o componente
        const component = this.components[componentName];
        await component.init();

        console.log(`⚙️ Componente ${componentName} inicializado`);
    }

    /**
     * Inicializa modais Bootstrap
     */
    async initModals() {
        const modalElements = document.querySelectorAll(this.components.modal.selector);
        
        modalElements.forEach(element => {
            if (!element.hasAttribute('data-bs-initialized')) {
                // Inicializa modal
                new bootstrap.Modal(element);
                element.setAttribute('data-bs-initialized', 'true');
                
                console.log(`🪟 Modal inicializado: ${element.id || 'sem ID'}`);
            }
        });

        // Adiciona listeners globais para modais
        this.setupModalEventListeners();
    }

    /**
     * Configura event listeners para modais
     */
    setupModalEventListeners() {
        // Listener para abertura de modais
        document.addEventListener('show.bs.modal', (event) => {
            console.log(`🪟 Abrindo modal: ${event.target.id || 'sem ID'}`);
            
            // Adiciona classe para animação customizada se necessário
            event.target.classList.add('modal-opening');
        });

        // Listener para fechamento de modais
        document.addEventListener('hidden.bs.modal', (event) => {
            console.log(`🪟 Modal fechado: ${event.target.id || 'sem ID'}`);
            
            // Remove classe de animação
            event.target.classList.remove('modal-opening');
            
            // Limpa backdrop se necessário
            const backdrop = document.querySelector('.modal-backdrop');
            if (backdrop && !document.querySelector('.modal.show')) {
                backdrop.remove();
            }
        });
    }

    /**
     * Inicializa tooltips Bootstrap
     */
    async initTooltips() {
        const tooltipElements = document.querySelectorAll(this.components.tooltip.selector);
        
        tooltipElements.forEach(element => {
            if (!element.hasAttribute('data-bs-initialized')) {
                // Configurações padrão para tooltips
                const options = {
                    delay: { show: 500, hide: 100 },
                    placement: element.getAttribute('data-bs-placement') || 'top'
                };

                new bootstrap.Tooltip(element, options);
                element.setAttribute('data-bs-initialized', 'true');
                
                console.log(`💡 Tooltip inicializado: ${element.getAttribute('title') || 'sem título'}`);
            }
        });
    }

    /**
     * Inicializa popovers Bootstrap
     */
    async initPopovers() {
        const popoverElements = document.querySelectorAll(this.components.popover.selector);
        
        popoverElements.forEach(element => {
            if (!element.hasAttribute('data-bs-initialized')) {
                // Configurações padrão para popovers
                const options = {
                    trigger: element.getAttribute('data-bs-trigger') || 'click',
                    placement: element.getAttribute('data-bs-placement') || 'right',
                    html: element.hasAttribute('data-bs-html')
                };

                new bootstrap.Popover(element, options);
                element.setAttribute('data-bs-initialized', 'true');
                
                console.log(`💬 Popover inicializado: ${element.getAttribute('data-bs-content') || 'sem conteúdo'}`);
            }
        });
    }

    /**
     * Inicializa dropdowns Bootstrap
     */
    async initDropdowns() {
        const dropdownElements = document.querySelectorAll(this.components.dropdown.selector);
        
        dropdownElements.forEach(element => {
            if (!element.hasAttribute('data-bs-initialized')) {
                new bootstrap.Dropdown(element);
                element.setAttribute('data-bs-initialized', 'true');
                
                console.log(`📋 Dropdown inicializado: ${element.textContent.trim() || 'sem texto'}`);
            }
        });
    }

    /**
     * Inicializa componentes de collapse Bootstrap
     */
    async initCollapse() {
        const collapseElements = document.querySelectorAll(this.components.collapse.selector);
        
        collapseElements.forEach(element => {
            if (!element.hasAttribute('data-bs-initialized')) {
                new bootstrap.Collapse(element);
                element.setAttribute('data-bs-initialized', 'true');
                
                console.log(`📁 Collapse inicializado: ${element.getAttribute('data-bs-target') || 'sem target'}`);
            }
        });
    }

    /**
     * Inicializa carousels Bootstrap
     */
    async initCarousels() {
        const carouselElements = document.querySelectorAll(this.components.carousel.selector);
        
        carouselElements.forEach(element => {
            if (!element.hasAttribute('data-bs-initialized')) {
                // Configurações padrão para carousel
                const options = {
                    interval: parseInt(element.getAttribute('data-bs-interval')) || 5000,
                    wrap: element.hasAttribute('data-bs-wrap') ? 
                          element.getAttribute('data-bs-wrap') === 'true' : true
                };

                new bootstrap.Carousel(element, options);
                element.setAttribute('data-bs-initialized', 'true');
                
                console.log(`🎠 Carousel inicializado: ${element.id || 'sem ID'}`);
            }
        });
    }

    /**
     * Inicializa offcanvas Bootstrap
     */
    async initOffcanvas() {
        const offcanvasElements = document.querySelectorAll(this.components.offcanvas.selector);
        
        offcanvasElements.forEach(element => {
            if (!element.hasAttribute('data-bs-initialized')) {
                new bootstrap.Offcanvas(element);
                element.setAttribute('data-bs-initialized', 'true');
                
                console.log(`📱 Offcanvas inicializado: ${element.id || 'sem ID'}`);
            }
        });
    }

    /**
     * Inicializa tabs Bootstrap
     */
    async initTabs() {
        const tabElements = document.querySelectorAll(this.components.tab.selector);
        
        tabElements.forEach(element => {
            if (!element.hasAttribute('data-bs-initialized')) {
                new bootstrap.Tab(element);
                element.setAttribute('data-bs-initialized', 'true');
                
                console.log(`📑 Tab inicializado: ${element.textContent.trim() || 'sem texto'}`);
            }
        });
    }

    /**
     * Detecta e carrega automaticamente componentes Bootstrap na página
     */
    async autoLoadComponents() {
        console.log('🔍 Detectando componentes Bootstrap na página...');

        const foundComponents = [];

        // Verifica cada tipo de componente
        for (const [componentName, config] of Object.entries(this.components)) {
            const elements = document.querySelectorAll(config.selector);
            
            if (elements.length > 0) {
                foundComponents.push(componentName);
                console.log(`📋 Encontrados ${elements.length} elementos ${componentName}`);
            }
        }

        if (foundComponents.length > 0) {
            console.log(`🚀 Carregando componentes: ${foundComponents.join(', ')}`);

            // Carrega todos os componentes encontrados em paralelo
            const loadPromises = foundComponents.map(component => 
                this.loadComponent(component)
            );

            await Promise.all(loadPromises);
            console.log('✅ Todos os componentes Bootstrap carregados!');
        } else {
            console.log('ℹ️ Nenhum componente Bootstrap encontrado na página');
        }
    }

    /**
     * Carrega componente específico sob demanda
     * @param {string} componentName - Nome do componente
     * @param {string} selector - Seletor específico (opcional)
     */
    async loadOnDemand(componentName, selector = null) {
        console.log(`🎯 Carregamento sob demanda: ${componentName}`);

        // Se seletor específico fornecido, verifica se elemento existe
        if (selector) {
            const element = document.querySelector(selector);
            if (!element) {
                console.warn(`Elemento não encontrado: ${selector}`);
                return;
            }
        }

        await this.loadComponent(componentName);
    }

    /**
     * Observa mudanças no DOM para carregar componentes dinamicamente
     */
    setupDOMObserver() {
        // Cria observer para detectar novos elementos Bootstrap
        const observer = new MutationObserver((mutations) => {
            let hasNewBootstrapElements = false;

            mutations.forEach((mutation) => {
                if (mutation.type === 'childList') {
                    mutation.addedNodes.forEach((node) => {
                        if (node.nodeType === Node.ELEMENT_NODE) {
                            // Verifica se o novo elemento ou seus filhos têm atributos Bootstrap
                            const hasBootstrapAttrs = node.matches && 
                                (node.matches('[data-bs-toggle]') || 
                                 node.querySelector('[data-bs-toggle]') ||
                                 node.matches('.carousel') ||
                                 node.querySelector('.carousel'));

                            if (hasBootstrapAttrs) {
                                hasNewBootstrapElements = true;
                            }
                        }
                    });
                }
            });

            // Se novos elementos Bootstrap foram adicionados, recarrega componentes
            if (hasNewBootstrapElements) {
                console.log('🔄 Novos elementos Bootstrap detectados, recarregando...');
                setTimeout(() => this.autoLoadComponents(), 100);
            }
        });

        // Inicia observação
        observer.observe(document.body, {
            childList: true,
            subtree: true
        });

        console.log('👁️ Observer de DOM configurado para Bootstrap');
    }

    /**
     * Verifica se um componente específico está carregado
     * @param {string} componentName - Nome do componente
     * @returns {boolean} - True se carregado
     */
    isComponentLoaded(componentName) {
        return this.loadedComponents.has(componentName);
    }

    /**
     * Lista todos os componentes carregados
     * @returns {Array} - Array com nomes dos componentes carregados
     */
    getLoadedComponents() {
        return Array.from(this.loadedComponents);
    }
}

// Cria instância global do BootstrapLoader
window.bootstrapLoader = new BootstrapLoader();

// Registra no LazyLoader se disponível
if (window.lazyLoader) {
    window.lazyLoader.registerModule('BootstrapLoader', BootstrapLoader);
}

// Funções de conveniência globais
window.loadBootstrapComponent = async function(componentName, selector = null) {
    return await window.bootstrapLoader.loadOnDemand(componentName, selector);
};

window.initBootstrapComponents = async function() {
    return await window.bootstrapLoader.autoLoadComponents();
};

// Auto-inicialização quando DOM está pronto
$(document).ready(function() {
    // Configura observer de DOM
    window.bootstrapLoader.setupDOMObserver();
    
    // Carrega componentes automaticamente após um pequeno delay
    setTimeout(() => {
        window.bootstrapLoader.autoLoadComponents();
    }, 300);
});

console.log('🅱️ BootstrapLoader inicializado e pronto para uso!');