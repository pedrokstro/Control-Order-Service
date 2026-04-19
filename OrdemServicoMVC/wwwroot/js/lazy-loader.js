/**
 * Sistema de Lazy Loading para COS - Control Order Service
 * Gerencia o carregamento assíncrono de componentes JavaScript para melhorar a performance
 */

class LazyLoader {
    constructor() {
        // Cache de scripts carregados para evitar carregamentos duplicados
        this.loadedScripts = new Set();
        // Cache de módulos carregados
        this.loadedModules = new Map();
        // Fila de carregamento para controlar dependências
        this.loadingQueue = new Map();
    }

    /**
     * Carrega um script JavaScript de forma assíncrona
     * @param {string} url - URL do script a ser carregado
     * @param {string} id - ID único para o script (opcional)
     * @returns {Promise} - Promise que resolve quando o script é carregado
     */
    async loadScript(url, id = null) {
        // Usa a URL como ID se não fornecido
        const scriptId = id || url;
        
        // Verifica se o script já foi carregado
        if (this.loadedScripts.has(scriptId)) {
            console.log(`📦 Script já carregado: ${scriptId}`);
            return Promise.resolve();
        }

        // Verifica se o script está sendo carregado
        if (this.loadingQueue.has(scriptId)) {
            console.log(`⏳ Aguardando carregamento em progresso: ${scriptId}`);
            return this.loadingQueue.get(scriptId);
        }

        // Cria a promise de carregamento
        const loadPromise = new Promise((resolve, reject) => {
            console.log(`🚀 Iniciando carregamento: ${scriptId}`);
            
            const script = document.createElement('script');
            script.src = url;
            script.async = true;
            
            // Callback de sucesso
            script.onload = () => {
                console.log(`✅ Script carregado com sucesso: ${scriptId}`);
                this.loadedScripts.add(scriptId);
                this.loadingQueue.delete(scriptId);
                resolve();
            };
            
            // Callback de erro
            script.onerror = () => {
                console.error(`❌ Erro ao carregar script: ${scriptId}`);
                this.loadingQueue.delete(scriptId);
                reject(new Error(`Falha ao carregar script: ${url}`));
            };
            
            // Adiciona o script ao head
            document.head.appendChild(script);
        });

        // Adiciona à fila de carregamento
        this.loadingQueue.set(scriptId, loadPromise);
        
        return loadPromise;
    }

    /**
     * Carrega múltiplos scripts em sequência
     * @param {Array} scripts - Array de objetos {url, id} ou strings
     * @returns {Promise} - Promise que resolve quando todos os scripts são carregados
     */
    async loadScripts(scripts) {
        console.log(`📚 Carregando ${scripts.length} scripts em sequência...`);
        
        for (const script of scripts) {
            if (typeof script === 'string') {
                await this.loadScript(script);
            } else {
                await this.loadScript(script.url, script.id);
            }
        }
        
        console.log(`✅ Todos os scripts carregados com sucesso!`);
    }

    /**
     * Carrega múltiplos scripts em paralelo
     * @param {Array} scripts - Array de objetos {url, id} ou strings
     * @returns {Promise} - Promise que resolve quando todos os scripts são carregados
     */
    async loadScriptsParallel(scripts) {
        console.log(`🚀 Carregando ${scripts.length} scripts em paralelo...`);
        
        const promises = scripts.map(script => {
            if (typeof script === 'string') {
                return this.loadScript(script);
            } else {
                return this.loadScript(script.url, script.id);
            }
        });
        
        await Promise.all(promises);
        console.log(`✅ Todos os scripts carregados em paralelo!`);
    }

    /**
     * Carrega um módulo CSS de forma assíncrona
     * @param {string} url - URL do CSS a ser carregado
     * @param {string} id - ID único para o CSS (opcional)
     * @returns {Promise} - Promise que resolve quando o CSS é carregado
     */
    async loadCSS(url, id = null) {
        const cssId = id || url;
        
        // Verifica se o CSS já foi carregado
        if (this.loadedScripts.has(cssId)) {
            console.log(`🎨 CSS já carregado: ${cssId}`);
            return Promise.resolve();
        }

        return new Promise((resolve, reject) => {
            console.log(`🎨 Carregando CSS: ${cssId}`);
            
            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = url;
            
            link.onload = () => {
                console.log(`✅ CSS carregado com sucesso: ${cssId}`);
                this.loadedScripts.add(cssId);
                resolve();
            };
            
            link.onerror = () => {
                console.error(`❌ Erro ao carregar CSS: ${cssId}`);
                reject(new Error(`Falha ao carregar CSS: ${url}`));
            };
            
            document.head.appendChild(link);
        });
    }

    /**
     * Registra um módulo carregado para reutilização
     * @param {string} name - Nome do módulo
     * @param {*} module - Objeto/função do módulo
     */
    registerModule(name, module) {
        console.log(`📝 Registrando módulo: ${name}`);
        this.loadedModules.set(name, module);
    }

    /**
     * Obtém um módulo registrado
     * @param {string} name - Nome do módulo
     * @returns {*} - Módulo registrado ou null
     */
    getModule(name) {
        return this.loadedModules.get(name) || null;
    }

    /**
     * Verifica se um script foi carregado
     * @param {string} id - ID do script
     * @returns {boolean} - True se carregado
     */
    isLoaded(id) {
        return this.loadedScripts.has(id);
    }

    /**
     * Carrega componente sob demanda com callback
     * @param {string} componentName - Nome do componente
     * @param {Function} loader - Função que carrega o componente
     * @param {Function} callback - Callback executado após carregamento
     */
    async loadOnDemand(componentName, loader, callback) {
        try {
            console.log(`🔄 Carregando componente sob demanda: ${componentName}`);
            
            // Executa o loader
            await loader();
            
            // Executa o callback se fornecido
            if (callback && typeof callback === 'function') {
                callback();
            }
            
            console.log(`✅ Componente carregado: ${componentName}`);
        } catch (error) {
            console.error(`❌ Erro ao carregar componente ${componentName}:`, error);
            throw error;
        }
    }
}

// Cria instância global do LazyLoader
window.lazyLoader = new LazyLoader();

// Exporta para uso em módulos
if (typeof module !== 'undefined' && module.exports) {
    module.exports = LazyLoader;
}

console.log('🎯 LazyLoader inicializado e pronto para uso!');