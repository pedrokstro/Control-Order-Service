/**
 * Carregador de Imagens e Assets - Lazy Loading
 * Carrega imagens e outros assets apenas quando necessário
 */

class ImageLazyLoader {
    constructor() {
        this.observer = null;
        this.loadedImages = new Set();
        this.loadingImages = new Set();
        
        // Configurações padrão
        this.config = {
            // Margem para começar a carregar antes da imagem aparecer
            rootMargin: '50px 0px',
            // Threshold para considerar a imagem visível
            threshold: 0.01,
            // Classe para imagens lazy
            lazyClass: 'lazy-load',
            // Classe aplicada durante carregamento
            loadingClass: 'lazy-loading',
            // Classe aplicada após carregamento
            loadedClass: 'lazy-loaded',
            // Classe aplicada em caso de erro
            errorClass: 'lazy-error',
            // Placeholder padrão
            placeholder: 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMzIwIiBoZWlnaHQ9IjE4MCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48cmVjdCB3aWR0aD0iMTAwJSIgaGVpZ2h0PSIxMDAlIiBmaWxsPSIjZGRkIi8+PHRleHQgeD0iNTAlIiB5PSI1MCUiIGZvbnQtZmFtaWx5PSJBcmlhbCwgc2Fucy1zZXJpZiIgZm9udC1zaXplPSIxNCIgZmlsbD0iIzk5OSIgdGV4dC1hbmNob3I9Im1pZGRsZSIgZHk9Ii4zZW0iPkNhcnJlZ2FuZG8uLi48L3RleHQ+PC9zdmc+'
        };
    }

    /**
     * Inicializa o sistema de lazy loading
     */
    init() {
        console.log('🖼️ Inicializando ImageLazyLoader...');

        // Verifica suporte ao Intersection Observer
        if (!('IntersectionObserver' in window)) {
            console.warn('⚠️ Intersection Observer não suportado, carregando todas as imagens');
            this.loadAllImages();
            return;
        }

        // Cria o observer
        this.createObserver();

        // Processa imagens existentes
        this.processExistingImages();

        // Configura observer para novas imagens
        this.setupMutationObserver();

        console.log('✅ ImageLazyLoader inicializado!');
    }

    /**
     * Cria o Intersection Observer
     */
    createObserver() {
        this.observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    this.loadImage(entry.target);
                    this.observer.unobserve(entry.target);
                }
            });
        }, {
            rootMargin: this.config.rootMargin,
            threshold: this.config.threshold
        });
    }

    /**
     * Processa imagens existentes na página
     */
    processExistingImages() {
        // Procura por imagens com data-src (lazy loading tradicional)
        const lazyImages = document.querySelectorAll('img[data-src]');
        
        // Procura por imagens com classe lazy
        const lazyClassImages = document.querySelectorAll(`img.${this.config.lazyClass}`);
        
        // Combina ambas as seleções
        const allLazyImages = new Set([...lazyImages, ...lazyClassImages]);

        console.log(`🔍 Encontradas ${allLazyImages.size} imagens para lazy loading`);

        allLazyImages.forEach(img => {
            this.prepareImage(img);
            this.observer.observe(img);
        });
    }

    /**
     * Prepara uma imagem para lazy loading
     * @param {HTMLImageElement} img - Elemento da imagem
     */
    prepareImage(img) {
        // Adiciona classe de loading
        img.classList.add(this.config.lazyClass);

        // Define placeholder se não tiver src
        if (!img.src || img.src === window.location.href) {
            img.src = this.config.placeholder;
        }

        // Adiciona atributos de acessibilidade
        if (!img.alt) {
            img.alt = 'Imagem carregando...';
        }

        // Adiciona loading="lazy" nativo se suportado
        if ('loading' in HTMLImageElement.prototype) {
            img.loading = 'lazy';
        }
    }

    /**
     * Carrega uma imagem específica
     * @param {HTMLImageElement} img - Elemento da imagem
     */
    async loadImage(img) {
        // Verifica se já está carregando ou carregada
        if (this.loadingImages.has(img) || this.loadedImages.has(img)) {
            return;
        }

        // Obtém a URL da imagem
        const src = img.dataset.src || img.getAttribute('data-src');
        
        if (!src) {
            console.warn('⚠️ Imagem sem data-src:', img);
            return;
        }

        console.log(`🖼️ Carregando imagem: ${src}`);

        // Marca como carregando
        this.loadingImages.add(img);
        img.classList.add(this.config.loadingClass);

        try {
            // Pré-carrega a imagem
            await this.preloadImage(src);

            // Aplica a imagem carregada
            img.src = src;
            img.removeAttribute('data-src');

            // Atualiza classes
            img.classList.remove(this.config.loadingClass);
            img.classList.add(this.config.loadedClass);

            // Atualiza alt text
            if (img.alt === 'Imagem carregando...') {
                img.alt = img.dataset.alt || '';
            }

            // Marca como carregada
            this.loadedImages.add(img);
            this.loadingImages.delete(img);

            console.log(`✅ Imagem carregada: ${src}`);

            // Dispara evento customizado
            img.dispatchEvent(new CustomEvent('lazyloaded', {
                detail: { src: src }
            }));

        } catch (error) {
            console.error(`❌ Erro ao carregar imagem: ${src}`, error);

            // Aplica classe de erro
            img.classList.remove(this.config.loadingClass);
            img.classList.add(this.config.errorClass);

            // Remove da lista de carregamento
            this.loadingImages.delete(img);

            // Dispara evento de erro
            img.dispatchEvent(new CustomEvent('lazyerror', {
                detail: { src: src, error: error }
            }));
        }
    }

    /**
     * Pré-carrega uma imagem
     * @param {string} src - URL da imagem
     * @returns {Promise} - Promise que resolve quando imagem carrega
     */
    preloadImage(src) {
        return new Promise((resolve, reject) => {
            const img = new Image();
            
            img.onload = () => resolve(img);
            img.onerror = () => reject(new Error(`Falha ao carregar: ${src}`));
            
            img.src = src;
        });
    }

    /**
     * Configura observer para detectar novas imagens
     */
    setupMutationObserver() {
        const mutationObserver = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                if (mutation.type === 'childList') {
                    mutation.addedNodes.forEach((node) => {
                        if (node.nodeType === Node.ELEMENT_NODE) {
                            // Verifica se o novo elemento é uma imagem lazy
                            if (node.tagName === 'IMG' && (node.dataset.src || node.classList.contains(this.config.lazyClass))) {
                                this.prepareImage(node);
                                this.observer.observe(node);
                            }

                            // Verifica imagens filhas
                            const lazyImages = node.querySelectorAll && node.querySelectorAll('img[data-src], img.lazy-load');
                            if (lazyImages) {
                                lazyImages.forEach(img => {
                                    this.prepareImage(img);
                                    this.observer.observe(img);
                                });
                            }
                        }
                    });
                }
            });
        });

        mutationObserver.observe(document.body, {
            childList: true,
            subtree: true
        });

        console.log('👁️ Observer de mutação configurado para novas imagens');
    }

    /**
     * Carrega todas as imagens imediatamente (fallback)
     */
    loadAllImages() {
        const lazyImages = document.querySelectorAll('img[data-src]');
        
        lazyImages.forEach(img => {
            const src = img.dataset.src;
            if (src) {
                img.src = src;
                img.removeAttribute('data-src');
                img.classList.add(this.config.loadedClass);
            }
        });

        console.log(`🖼️ ${lazyImages.length} imagens carregadas imediatamente`);
    }

    /**
     * Força o carregamento de uma imagem específica
     * @param {HTMLImageElement|string} target - Elemento ou seletor da imagem
     */
    async forceLoad(target) {
        const img = typeof target === 'string' ? document.querySelector(target) : target;
        
        if (!img) {
            console.warn('⚠️ Imagem não encontrada:', target);
            return;
        }

        if (this.observer) {
            this.observer.unobserve(img);
        }

        await this.loadImage(img);
    }

    /**
     * Carrega imagens em uma área específica
     * @param {HTMLElement|string} container - Container ou seletor
     */
    async loadImagesInContainer(container) {
        const element = typeof container === 'string' ? document.querySelector(container) : container;
        
        if (!element) {
            console.warn('⚠️ Container não encontrado:', container);
            return;
        }

        const lazyImages = element.querySelectorAll('img[data-src], img.lazy-load');
        
        console.log(`🖼️ Carregando ${lazyImages.length} imagens no container`);

        const loadPromises = Array.from(lazyImages).map(img => this.forceLoad(img));
        await Promise.all(loadPromises);

        console.log('✅ Todas as imagens do container carregadas!');
    }

    /**
     * Adiciona suporte a imagens responsivas
     * @param {HTMLImageElement} img - Elemento da imagem
     */
    setupResponsiveImage(img) {
        // Verifica se tem srcset
        const srcset = img.dataset.srcset;
        if (srcset) {
            img.srcset = srcset;
            img.removeAttribute('data-srcset');
        }

        // Verifica se tem sizes
        const sizes = img.dataset.sizes;
        if (sizes) {
            img.sizes = sizes;
            img.removeAttribute('data-sizes');
        }
    }

    /**
     * Cria placeholder SVG personalizado
     * @param {number} width - Largura
     * @param {number} height - Altura
     * @param {string} text - Texto do placeholder
     * @returns {string} - Data URL do SVG
     */
    createPlaceholder(width = 320, height = 180, text = 'Carregando...') {
        const svg = `
            <svg width="${width}" height="${height}" xmlns="http://www.w3.org/2000/svg">
                <rect width="100%" height="100%" fill="#f0f0f0"/>
                <text x="50%" y="50%" font-family="Arial, sans-serif" font-size="14" 
                      fill="#999" text-anchor="middle" dy=".3em">${text}</text>
            </svg>
        `;
        
        return `data:image/svg+xml;base64,${btoa(svg)}`;
    }

    /**
     * Configura lazy loading para imagens de fundo
     */
    setupBackgroundImages() {
        const bgElements = document.querySelectorAll('[data-bg-src]');
        
        bgElements.forEach(element => {
            const bgObserver = new IntersectionObserver((entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        const bgSrc = entry.target.dataset.bgSrc;
                        
                        // Pré-carrega a imagem
                        this.preloadImage(bgSrc).then(() => {
                            entry.target.style.backgroundImage = `url(${bgSrc})`;
                            entry.target.classList.add('bg-loaded');
                            entry.target.removeAttribute('data-bg-src');
                        }).catch(error => {
                            console.error('❌ Erro ao carregar imagem de fundo:', error);
                            entry.target.classList.add('bg-error');
                        });

                        bgObserver.unobserve(entry.target);
                    }
                });
            }, {
                rootMargin: this.config.rootMargin,
                threshold: this.config.threshold
            });

            bgObserver.observe(element);
        });

        console.log(`🎨 ${bgElements.length} imagens de fundo configuradas para lazy loading`);
    }

    /**
     * Obtém estatísticas de carregamento
     * @returns {Object} - Estatísticas
     */
    getStats() {
        return {
            loaded: this.loadedImages.size,
            loading: this.loadingImages.size,
            total: document.querySelectorAll('img[data-src], img.lazy-load').length
        };
    }

    /**
     * Limpa recursos e desconecta observers
     */
    destroy() {
        if (this.observer) {
            this.observer.disconnect();
            this.observer = null;
        }

        this.loadedImages.clear();
        this.loadingImages.clear();

        console.log('🗑️ ImageLazyLoader destruído');
    }
}

// Cria instância global do ImageLazyLoader
window.imageLazyLoader = new ImageLazyLoader();

// Registra no LazyLoader se disponível
if (window.lazyLoader) {
    window.lazyLoader.registerModule('ImageLazyLoader', ImageLazyLoader);
}

// Funções de conveniência globais
window.loadImage = async function(target) {
    return await window.imageLazyLoader.forceLoad(target);
};

window.loadImagesInArea = async function(container) {
    return await window.imageLazyLoader.loadImagesInContainer(container);
};

// Auto-inicialização quando DOM está pronto
$(document).ready(function() {
    // Inicializa o sistema de lazy loading
    window.imageLazyLoader.init();
    
    // Configura imagens de fundo
    window.imageLazyLoader.setupBackgroundImages();
    
    // Log de estatísticas após um tempo
    setTimeout(() => {
        const stats = window.imageLazyLoader.getStats();
        console.log(`📊 Estatísticas de imagens: ${stats.loaded} carregadas, ${stats.loading} carregando, ${stats.total} total`);
    }, 2000);
});

// Event listeners para debugging
document.addEventListener('lazyloaded', (event) => {
    console.log(`✅ Imagem lazy loaded: ${event.detail.src}`);
});

document.addEventListener('lazyerror', (event) => {
    console.error(`❌ Erro no lazy loading: ${event.detail.src}`, event.detail.error);
});

console.log('🖼️ ImageLazyLoader inicializado e pronto para uso!');