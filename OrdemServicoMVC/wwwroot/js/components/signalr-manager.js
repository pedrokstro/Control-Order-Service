/**
 * Gerenciador SignalR - Carregamento Lazy
 * Gerencia conexões SignalR de forma centralizada e otimizada
 */

class SignalRManager {
    constructor() {
        this.connections = new Map();
        this.isSignalRLoaded = false;
        this.loadingPromise = null;
        this.globalConnection = null;
    }

    /**
     * Carrega a biblioteca SignalR se necessário
     * @returns {Promise} - Promise que resolve quando SignalR está carregado
     */
    async ensureSignalRLoaded() {
        // Se já está carregado, retorna imediatamente
        if (this.isSignalRLoaded && typeof signalR !== 'undefined') {
            return Promise.resolve();
        }

        // Se já está carregando, retorna a promise existente
        if (this.loadingPromise) {
            return this.loadingPromise;
        }

        // Inicia o carregamento
        this.loadingPromise = this.loadSignalR();
        
        try {
            await this.loadingPromise;
            this.isSignalRLoaded = true;
            console.log('✅ SignalR carregado com sucesso!');
        } catch (error) {
            console.error('❌ Erro ao carregar SignalR:', error);
            this.loadingPromise = null;
            throw error;
        }

        return this.loadingPromise;
    }

    /**
     * Carrega a biblioteca SignalR
     * @returns {Promise} - Promise de carregamento
     */
    async loadSignalR() {
        console.log('📦 Carregando biblioteca SignalR...');
        
        // Usa o LazyLoader se disponível
        if (window.lazyLoader) {
            await window.lazyLoader.loadScript(
                'https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js',
                'signalr'
            );
        } else {
            // Fallback para carregamento manual
            await this.loadScriptManual('https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js');
        }
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
     * Cria ou obtém uma conexão SignalR
     * @param {string} hubUrl - URL do hub SignalR
     * @param {string} connectionId - ID único da conexão (opcional)
     * @param {Object} options - Opções de configuração
     * @returns {Promise<Object>} - Conexão SignalR
     */
    async getConnection(hubUrl = '/mensagemHub', connectionId = 'default', options = {}) {
        // Garante que SignalR está carregado
        await this.ensureSignalRLoaded();

        // Verifica se já existe uma conexão ativa
        if (this.connections.has(connectionId)) {
            const existingConnection = this.connections.get(connectionId);
            
            // Se a conexão está ativa, retorna ela
            if (existingConnection.state === signalR.HubConnectionState.Connected) {
                console.log(`🔌 Reutilizando conexão existente: ${connectionId}`);
                return existingConnection;
            }
            
            // Se a conexão está desconectada, remove ela
            this.connections.delete(connectionId);
        }

        console.log(`🚀 Criando nova conexão SignalR: ${connectionId}`);

        // Configurações padrão
        const defaultOptions = {
            withAutomaticReconnect: true,
            reconnectDelays: [0, 2000, 10000, 30000],
            ...options
        };

        // Cria nova conexão
        const connectionBuilder = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl);

        // Aplica reconexão automática se habilitada
        if (defaultOptions.withAutomaticReconnect) {
            connectionBuilder.withAutomaticReconnect(defaultOptions.reconnectDelays);
        }

        const connection = connectionBuilder.build();

        // Configura eventos de conexão
        this.setupConnectionEvents(connection, connectionId);

        // Armazena a conexão
        this.connections.set(connectionId, connection);

        try {
            // Inicia a conexão
            await connection.start();
            console.log(`✅ Conexão SignalR estabelecida: ${connectionId}`);
            
            return connection;
        } catch (error) {
            console.error(`❌ Erro ao conectar SignalR (${connectionId}):`, error);
            this.connections.delete(connectionId);
            throw error;
        }
    }

    /**
     * Configura eventos de conexão
     * @param {Object} connection - Conexão SignalR
     * @param {string} connectionId - ID da conexão
     */
    setupConnectionEvents(connection, connectionId) {
        // Evento de reconexão
        connection.onreconnecting((error) => {
            console.warn(`🔄 Reconectando SignalR (${connectionId}):`, error);
        });

        // Evento de reconexão bem-sucedida
        connection.onreconnected((connectionId) => {
            console.log(`✅ SignalR reconectado (${connectionId})`);
        });

        // Evento de desconexão
        connection.onclose((error) => {
            console.warn(`🔌 Conexão SignalR fechada (${connectionId}):`, error);
            this.connections.delete(connectionId);
        });
    }

    /**
     * Obtém a conexão global (para compatibilidade)
     * @returns {Promise<Object>} - Conexão global
     */
    async getGlobalConnection() {
        if (!this.globalConnection) {
            this.globalConnection = await this.getConnection('/mensagemHub', 'global');
        }
        return this.globalConnection;
    }

    /**
     * Entra em um grupo SignalR
     * @param {string} groupName - Nome do grupo
     * @param {string} connectionId - ID da conexão (opcional)
     */
    async joinGroup(groupName, connectionId = 'default') {
        try {
            const connection = this.connections.get(connectionId);
            
            if (!connection) {
                throw new Error(`Conexão não encontrada: ${connectionId}`);
            }

            await connection.invoke('JoinGroup', groupName);
            console.log(`👥 Entrou no grupo: ${groupName} (${connectionId})`);
        } catch (error) {
            console.error(`❌ Erro ao entrar no grupo ${groupName}:`, error);
            throw error;
        }
    }

    /**
     * Sai de um grupo SignalR
     * @param {string} groupName - Nome do grupo
     * @param {string} connectionId - ID da conexão (opcional)
     */
    async leaveGroup(groupName, connectionId = 'default') {
        try {
            const connection = this.connections.get(connectionId);
            
            if (!connection) {
                console.warn(`Conexão não encontrada para sair do grupo: ${connectionId}`);
                return;
            }

            await connection.invoke('LeaveGroup', groupName);
            console.log(`👋 Saiu do grupo: ${groupName} (${connectionId})`);
        } catch (error) {
            console.error(`❌ Erro ao sair do grupo ${groupName}:`, error);
        }
    }

    /**
     * Registra um listener para mensagens
     * @param {string} methodName - Nome do método SignalR
     * @param {Function} callback - Função de callback
     * @param {string} connectionId - ID da conexão (opcional)
     */
    on(methodName, callback, connectionId = 'default') {
        const connection = this.connections.get(connectionId);
        
        if (!connection) {
            console.error(`Conexão não encontrada para registrar listener: ${connectionId}`);
            return;
        }

        connection.on(methodName, callback);
        console.log(`👂 Listener registrado: ${methodName} (${connectionId})`);
    }

    /**
     * Remove um listener
     * @param {string} methodName - Nome do método SignalR
     * @param {string} connectionId - ID da conexão (opcional)
     */
    off(methodName, connectionId = 'default') {
        const connection = this.connections.get(connectionId);
        
        if (!connection) {
            console.warn(`Conexão não encontrada para remover listener: ${connectionId}`);
            return;
        }

        connection.off(methodName);
        console.log(`🔇 Listener removido: ${methodName} (${connectionId})`);
    }

    /**
     * Invoca um método no servidor
     * @param {string} methodName - Nome do método
     * @param {...any} args - Argumentos do método
     * @param {string} connectionId - ID da conexão (opcional)
     */
    async invoke(methodName, ...args) {
        // O último argumento pode ser o connectionId se for string
        let connectionId = 'default';
        let methodArgs = args;
        
        if (args.length > 0 && typeof args[args.length - 1] === 'string' && args[args.length - 1].startsWith('connection:')) {
            connectionId = args[args.length - 1].replace('connection:', '');
            methodArgs = args.slice(0, -1);
        }

        const connection = this.connections.get(connectionId);
        
        if (!connection) {
            throw new Error(`Conexão não encontrada: ${connectionId}`);
        }

        try {
            const result = await connection.invoke(methodName, ...methodArgs);
            console.log(`📤 Método invocado: ${methodName} (${connectionId})`);
            return result;
        } catch (error) {
            console.error(`❌ Erro ao invocar ${methodName}:`, error);
            throw error;
        }
    }

    /**
     * Fecha uma conexão específica
     * @param {string} connectionId - ID da conexão
     */
    async closeConnection(connectionId) {
        const connection = this.connections.get(connectionId);
        
        if (!connection) {
            console.warn(`Conexão não encontrada para fechar: ${connectionId}`);
            return;
        }

        try {
            await connection.stop();
            this.connections.delete(connectionId);
            console.log(`🔌 Conexão fechada: ${connectionId}`);
        } catch (error) {
            console.error(`❌ Erro ao fechar conexão ${connectionId}:`, error);
        }
    }

    /**
     * Fecha todas as conexões
     */
    async closeAllConnections() {
        console.log('🔌 Fechando todas as conexões SignalR...');
        
        const closePromises = Array.from(this.connections.keys()).map(connectionId => 
            this.closeConnection(connectionId)
        );

        await Promise.all(closePromises);
        
        this.globalConnection = null;
        console.log('✅ Todas as conexões SignalR fechadas!');
    }

    /**
     * Obtém informações sobre as conexões ativas
     * @returns {Object} - Informações das conexões
     */
    getConnectionsInfo() {
        const info = {};
        
        this.connections.forEach((connection, id) => {
            info[id] = {
                state: connection.state,
                connectionId: connection.connectionId
            };
        });
        
        return info;
    }

    /**
     * Verifica se uma conexão está ativa
     * @param {string} connectionId - ID da conexão
     * @returns {boolean} - True se ativa
     */
    isConnectionActive(connectionId = 'default') {
        const connection = this.connections.get(connectionId);
        return connection && connection.state === signalR.HubConnectionState.Connected;
    }
}

// Cria instância global do SignalRManager
window.signalRManager = new SignalRManager();

// Registra no LazyLoader se disponível
if (window.lazyLoader) {
    window.lazyLoader.registerModule('SignalRManager', SignalRManager);
}

// Compatibilidade com código existente
window.getGlobalSignalRConnection = async function() {
    return await window.signalRManager.getGlobalConnection();
};

console.log('🎯 SignalRManager inicializado e pronto para uso!');