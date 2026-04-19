/**
 * Carregador de Componentes de Relatórios - Lazy Loading
 * Carrega bibliotecas e componentes de relatórios apenas quando necessário
 */

class ReportsLoader {
    constructor() {
        this.loadedComponents = new Set();
        this.loadingPromises = new Map();
        
        // Configuração de componentes de relatórios
        this.reportComponents = {
            // Bibliotecas de gráficos
            chartjs: {
                url: 'https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.js',
                id: 'chartjs',
                global: 'Chart'
            },
            
            // Biblioteca para exportação de dados
            xlsx: {
                url: 'https://cdn.jsdelivr.net/npm/xlsx@0.18.5/dist/xlsx.full.min.js',
                id: 'xlsx',
                global: 'XLSX'
            },
            
            // Biblioteca para geração de PDF
            jspdf: {
                url: 'https://cdn.jsdelivr.net/npm/jspdf@2.5.1/dist/jspdf.umd.min.js',
                id: 'jspdf',
                global: 'jsPDF'
            },
            
            // Biblioteca para tabelas avançadas
            datatables: {
                url: 'https://cdn.datatables.net/1.13.7/js/jquery.dataTables.min.js',
                id: 'datatables',
                global: 'DataTable',
                css: 'https://cdn.datatables.net/1.13.7/css/jquery.dataTables.min.css'
            },
            
            // Extensões do DataTables
            datatables_buttons: {
                url: 'https://cdn.datatables.net/buttons/2.4.2/js/dataTables.buttons.min.js',
                id: 'datatables-buttons',
                dependencies: ['datatables'],
                css: 'https://cdn.datatables.net/buttons/2.4.2/css/buttons.dataTables.min.css'
            },
            
            // Biblioteca para filtros avançados
            daterangepicker: {
                url: 'https://cdn.jsdelivr.net/npm/daterangepicker@3.1.0/daterangepicker.min.js',
                id: 'daterangepicker',
                global: 'daterangepicker',
                css: 'https://cdn.jsdelivr.net/npm/daterangepicker@3.1.0/daterangepicker.css',
                dependencies: ['moment']
            },
            
            // Biblioteca para manipulação de datas
            moment: {
                url: 'https://cdn.jsdelivr.net/npm/moment@2.29.4/moment.min.js',
                id: 'moment',
                global: 'moment'
            }
        };
    }

    /**
     * Carrega componente específico de relatório
     * @param {string} componentName - Nome do componente
     * @returns {Promise} - Promise de carregamento
     */
    async loadComponent(componentName) {
        // Verifica se componente existe
        if (!this.reportComponents[componentName]) {
            throw new Error(`Componente de relatório não reconhecido: ${componentName}`);
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
        console.log(`📊 Carregando componente de relatório: ${componentName}`);

        const component = this.reportComponents[componentName];

        // Carrega dependências primeiro
        if (component.dependencies) {
            console.log(`📦 Carregando dependências para ${componentName}:`, component.dependencies);
            
            for (const dependency of component.dependencies) {
                await this.loadComponent(dependency);
            }
        }

        // Carrega CSS se especificado
        if (component.css) {
            await this.loadCSS(component.css, `${componentName}-css`);
        }

        // Carrega o script principal
        if (window.lazyLoader) {
            await window.lazyLoader.loadScript(component.url, component.id);
        } else {
            await this.loadScriptManual(component.url);
        }

        // Verifica se a biblioteca foi carregada corretamente
        if (component.global && typeof window[component.global] === 'undefined') {
            throw new Error(`Biblioteca ${componentName} não foi carregada corretamente`);
        }

        console.log(`⚙️ Componente ${componentName} inicializado`);
    }

    /**
     * Carrega CSS de forma assíncrona
     * @param {string} url - URL do CSS
     * @param {string} id - ID do elemento
     * @returns {Promise} - Promise de carregamento
     */
    loadCSS(url, id) {
        return new Promise((resolve, reject) => {
            // Verifica se já existe
            if (document.getElementById(id)) {
                resolve();
                return;
            }

            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = url;
            link.id = id;
            
            link.onload = resolve;
            link.onerror = () => reject(new Error(`Falha ao carregar CSS: ${url}`));
            
            document.head.appendChild(link);
        });
    }

    /**
     * Carregamento manual de script (fallback)
     * @param {string} url - URL do script
     * @returns {Promise} - Promise de carregamento
     */
    loadScriptManual(url) {
        return new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = url;
            script.async = true;
            
            script.onload = resolve;
            script.onerror = () => reject(new Error(`Falha ao carregar: ${url}`));
            
            document.head.appendChild(script);
        });
    }

    /**
     * Carrega múltiplos componentes em paralelo
     * @param {Array} componentNames - Array com nomes dos componentes
     * @returns {Promise} - Promise que resolve quando todos estão carregados
     */
    async loadComponents(componentNames) {
        console.log(`📊 Carregando múltiplos componentes: ${componentNames.join(', ')}`);

        const loadPromises = componentNames.map(name => this.loadComponent(name));
        await Promise.all(loadPromises);

        console.log('✅ Todos os componentes de relatório carregados!');
    }

    /**
     * Inicializa gráficos Chart.js
     * @param {string} canvasId - ID do canvas
     * @param {Object} config - Configuração do gráfico
     * @returns {Promise<Chart>} - Instância do gráfico
     */
    async createChart(canvasId, config) {
        // Carrega Chart.js se necessário
        await this.loadComponent('chartjs');

        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            throw new Error(`Canvas não encontrado: ${canvasId}`);
        }

        console.log(`📈 Criando gráfico: ${canvasId}`);
        return new Chart(canvas, config);
    }

    /**
     * Inicializa DataTable
     * @param {string} tableId - ID da tabela
     * @param {Object} options - Opções do DataTable
     * @returns {Promise<DataTable>} - Instância do DataTable
     */
    async createDataTable(tableId, options = {}) {
        // Carrega DataTables se necessário
        await this.loadComponent('datatables');

        const table = document.getElementById(tableId);
        if (!table) {
            throw new Error(`Tabela não encontrada: ${tableId}`);
        }

        // Configurações padrão
        const defaultOptions = {
            language: {
                url: '//cdn.datatables.net/plug-ins/1.13.7/i18n/pt-BR.json'
            },
            responsive: true,
            pageLength: 25,
            lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "Todos"]],
            dom: 'Bfrtip',
            buttons: []
        };

        const finalOptions = { ...defaultOptions, ...options };

        console.log(`📋 Criando DataTable: ${tableId}`);
        return $(table).DataTable(finalOptions);
    }

    /**
     * Adiciona botões de exportação ao DataTable
     * @param {string} tableId - ID da tabela
     * @returns {Promise} - Promise de configuração
     */
    async addExportButtons(tableId) {
        // Carrega extensões necessárias
        await this.loadComponents(['datatables', 'datatables_buttons', 'xlsx', 'jspdf']);

        const table = $(`#${tableId}`).DataTable();
        
        // Adiciona botões de exportação
        table.buttons().container().appendTo(`#${tableId}_wrapper .col-md-6:eq(0)`);

        console.log(`📤 Botões de exportação adicionados: ${tableId}`);
    }

    /**
     * Cria seletor de intervalo de datas
     * @param {string} inputId - ID do input
     * @param {Object} options - Opções do daterangepicker
     * @returns {Promise} - Promise de inicialização
     */
    async createDateRangePicker(inputId, options = {}) {
        // Carrega daterangepicker e moment
        await this.loadComponents(['moment', 'daterangepicker']);

        const input = document.getElementById(inputId);
        if (!input) {
            throw new Error(`Input não encontrado: ${inputId}`);
        }

        // Configurações padrão
        const defaultOptions = {
            locale: {
                format: 'DD/MM/YYYY',
                separator: ' - ',
                applyLabel: 'Aplicar',
                cancelLabel: 'Cancelar',
                fromLabel: 'De',
                toLabel: 'Até',
                customRangeLabel: 'Personalizado',
                weekLabel: 'S',
                daysOfWeek: ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'],
                monthNames: ['Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
                           'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro'],
                firstDay: 1
            },
            ranges: {
                'Hoje': [moment(), moment()],
                'Ontem': [moment().subtract(1, 'days'), moment().subtract(1, 'days')],
                'Últimos 7 dias': [moment().subtract(6, 'days'), moment()],
                'Últimos 30 dias': [moment().subtract(29, 'days'), moment()],
                'Este mês': [moment().startOf('month'), moment().endOf('month')],
                'Mês passado': [moment().subtract(1, 'month').startOf('month'), 
                               moment().subtract(1, 'month').endOf('month')]
            }
        };

        const finalOptions = { ...defaultOptions, ...options };

        console.log(`📅 Criando seletor de datas: ${inputId}`);
        $(input).daterangepicker(finalOptions);
    }

    /**
     * Exporta dados para Excel
     * @param {Array} data - Dados para exportar
     * @param {string} filename - Nome do arquivo
     * @returns {Promise} - Promise de exportação
     */
    async exportToExcel(data, filename = 'relatorio.xlsx') {
        // Carrega biblioteca XLSX
        await this.loadComponent('xlsx');

        console.log(`📊 Exportando para Excel: ${filename}`);

        const worksheet = XLSX.utils.json_to_sheet(data);
        const workbook = XLSX.utils.book_new();
        XLSX.utils.book_append_sheet(workbook, worksheet, 'Relatório');

        XLSX.writeFile(workbook, filename);
        console.log(`✅ Arquivo Excel exportado: ${filename}`);
    }

    /**
     * Exporta gráfico para PDF
     * @param {string} canvasId - ID do canvas do gráfico
     * @param {string} filename - Nome do arquivo
     * @returns {Promise} - Promise de exportação
     */
    async exportChartToPDF(canvasId, filename = 'grafico.pdf') {
        // Carrega biblioteca jsPDF
        await this.loadComponent('jspdf');

        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            throw new Error(`Canvas não encontrado: ${canvasId}`);
        }

        console.log(`📈 Exportando gráfico para PDF: ${filename}`);

        const imgData = canvas.toDataURL('image/png');
        const pdf = new jsPDF();
        
        // Adiciona o gráfico ao PDF
        pdf.addImage(imgData, 'PNG', 10, 10, 190, 100);
        pdf.save(filename);

        console.log(`✅ Gráfico exportado para PDF: ${filename}`);
    }

    /**
     * Detecta automaticamente componentes de relatório na página
     */
    async autoLoadReportComponents() {
        console.log('🔍 Detectando componentes de relatório na página...');

        const foundComponents = [];

        // Detecta canvas para gráficos
        if (document.querySelector('canvas[data-chart]')) {
            foundComponents.push('chartjs');
        }

        // Detecta tabelas que precisam de DataTables
        if (document.querySelector('table[data-datatable]')) {
            foundComponents.push('datatables');
        }

        // Detecta inputs de data
        if (document.querySelector('input[data-daterange]')) {
            foundComponents.push('moment', 'daterangepicker');
        }

        // Detecta botões de exportação
        if (document.querySelector('[data-export]')) {
            foundComponents.push('xlsx', 'jspdf');
        }

        if (foundComponents.length > 0) {
            console.log(`🚀 Carregando componentes de relatório: ${foundComponents.join(', ')}`);
            await this.loadComponents([...new Set(foundComponents)]);
            console.log('✅ Componentes de relatório carregados automaticamente!');
        } else {
            console.log('ℹ️ Nenhum componente de relatório encontrado na página');
        }
    }

    /**
     * Verifica se um componente está carregado
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

// Cria instância global do ReportsLoader
window.reportsLoader = new ReportsLoader();

// Registra no LazyLoader se disponível
if (window.lazyLoader) {
    window.lazyLoader.registerModule('ReportsLoader', ReportsLoader);
}

// Funções de conveniência globais
window.loadReportComponent = async function(componentName) {
    return await window.reportsLoader.loadComponent(componentName);
};

window.createChart = async function(canvasId, config) {
    return await window.reportsLoader.createChart(canvasId, config);
};

window.createDataTable = async function(tableId, options) {
    return await window.reportsLoader.createDataTable(tableId, options);
};

window.createDateRangePicker = async function(inputId, options) {
    return await window.reportsLoader.createDateRangePicker(inputId, options);
};

// Auto-inicialização quando DOM está pronto
$(document).ready(function() {
    // Detecta automaticamente componentes de relatório
    // Usa timeout para não bloquear o carregamento inicial
    setTimeout(() => {
        window.reportsLoader.autoLoadReportComponents();
    }, 800);
});

console.log('📊 ReportsLoader inicializado e pronto para uso!');