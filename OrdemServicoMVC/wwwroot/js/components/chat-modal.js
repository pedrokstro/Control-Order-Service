/**
 * Módulo Chat Modal - Carregamento Lazy
 * Componente de chat modal otimizado para carregamento sob demanda
 */

class ChatModal {
    constructor() {
        this.isInitialized = false;
        this.connection = null;
        this.modalElement = null;
        this.currentOrderId = null;
    }

    /**
     * Inicializa o componente de chat modal
     * @param {number} orderId - ID da ordem de serviço
     * @param {string} modalId - ID do modal (opcional)
     */
    async initialize(orderId, modalId = 'chatModal') {
        if (this.isInitialized) {
            console.log('💬 Chat modal já inicializado');
            return;
        }

        try {
            console.log('🚀 Inicializando chat modal...');
            
            // Define o ID da ordem atual
            this.currentOrderId = orderId;
            
            // Carrega dependências necessárias
            await this.loadDependencies();
            
            // Cria a estrutura do modal
            this.createModalStructure(modalId);
            
            // Inicializa SignalR para o chat
            await this.initializeSignalR();
            
            // Configura event listeners
            this.setupEventListeners();
            
            // Carrega mensagens existentes
            await this.loadExistingMessages();
            
            this.isInitialized = true;
            console.log('✅ Chat modal inicializado com sucesso!');
            
        } catch (error) {
            console.error('❌ Erro ao inicializar chat modal:', error);
            throw error;
        }
    }

    /**
     * Carrega as dependências necessárias para o chat
     */
    async loadDependencies() {
        console.log('📦 Carregando dependências do chat modal...');
        
        // Carrega SignalR se não estiver disponível
        if (typeof signalR === 'undefined') {
            await window.lazyLoader.loadScript(
                'https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js',
                'signalr'
            );
        }
        
        // Carrega Bootstrap se não estiver disponível
        if (typeof bootstrap === 'undefined') {
            await window.lazyLoader.loadScript('/lib/bootstrap/dist/js/bootstrap.bundle.min.js', 'bootstrap');
        }
        
        console.log('✅ Dependências carregadas!');
    }

    /**
     * Cria a estrutura HTML do modal de chat
     * @param {string} modalId - ID do modal
     */
    createModalStructure(modalId) {
        console.log('🏗️ Criando estrutura do modal de chat...');
        
        const modalHtml = `
            <div class="modal fade" id="${modalId}" tabindex="-1" aria-labelledby="${modalId}Label" aria-hidden="true">
                <div class="modal-dialog modal-lg">
                    <div class="modal-content">
                        <div class="modal-header bg-primary text-white">
                            <h5 class="modal-title" id="${modalId}Label">
                                <i class="bi bi-chat-dots me-2"></i>
                                Chat - Ordem de Serviço #${this.currentOrderId}
                            </h5>
                            <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body p-0">
                            <div class="d-flex flex-column" style="height: 500px;">
                                <!-- Área de mensagens -->
                                <div id="chat-messages-${modalId}" class="flex-grow-1 overflow-auto p-3 bg-light">
                                    <div class="text-center text-muted">
                                        <div class="spinner-border spinner-border-sm me-2" role="status">
                                            <span class="visually-hidden">Carregando...</span>
                                        </div>
                                        Carregando mensagens...
                                    </div>
                                </div>
                                
                                <!-- Área de digitação -->
                                <div class="border-top p-3 bg-white">
                                    <form id="chat-form-${modalId}" class="d-flex gap-2">
                                        <div class="flex-grow-1">
                                            <textarea id="message-input-${modalId}" 
                                                      class="form-control" 
                                                      rows="2" 
                                                      placeholder="Digite sua mensagem..."
                                                      style="resize: none;"></textarea>
                                        </div>
                                        <div class="d-flex flex-column gap-1">
                                            <input type="file" 
                                                   id="file-input-${modalId}" 
                                                   class="form-control form-control-sm" 
                                                   multiple 
                                                   accept="image/*,.pdf,.doc,.docx,.txt"
                                                   style="display: none;">
                                            <button type="button" 
                                                    id="attach-btn-${modalId}"
                                                    class="btn btn-outline-secondary btn-sm">
                                                <i class="bi bi-paperclip"></i>
                                            </button>
                                            <button type="submit" 
                                                    id="send-btn-${modalId}"
                                                    class="btn btn-primary btn-sm">
                                                <i class="bi bi-send"></i>
                                            </button>
                                        </div>
                                    </form>
                                    
                                    <!-- Preview de arquivos -->
                                    <div id="file-preview-${modalId}" class="mt-2"></div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        // Remove modal existente se houver
        const existingModal = document.getElementById(modalId);
        if (existingModal) {
            existingModal.remove();
        }
        
        // Adiciona o novo modal ao body
        document.body.insertAdjacentHTML('beforeend', modalHtml);
        
        // Armazena referência do modal
        this.modalElement = document.getElementById(modalId);
        
        console.log('✅ Estrutura do modal criada!');
    }

    /**
     * Inicializa a conexão SignalR para o chat
     */
    async initializeSignalR() {
        console.log('🔌 Inicializando SignalR para chat...');
        
        try {
            // Cria conexão SignalR
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl('/mensagemHub')
                .withAutomaticReconnect()
                .build();
            
            // Configura listeners de mensagens
            this.connection.on('ReceberMensagem', (mensagem) => {
                this.displayMessage(mensagem);
            });
            
            // Inicia a conexão
            await this.connection.start();
            
            // Entra no grupo da ordem de serviço
            await this.connection.invoke('JoinGroup', `ordem_${this.currentOrderId}`);
            
            console.log('✅ SignalR conectado para chat!');
            
        } catch (error) {
            console.error('❌ Erro ao conectar SignalR:', error);
            throw error;
        }
    }

    /**
     * Configura os event listeners do modal
     */
    setupEventListeners() {
        console.log('🎯 Configurando event listeners...');
        
        const modalId = this.modalElement.id;
        const messageInput = document.getElementById(`message-input-${modalId}`);
        const sendBtn = document.getElementById(`send-btn-${modalId}`);
        const attachBtn = document.getElementById(`attach-btn-${modalId}`);
        const fileInput = document.getElementById(`file-input-${modalId}`);
        const chatForm = document.getElementById(`chat-form-${modalId}`);
        
        // Event listener para envio de mensagem
        chatForm.addEventListener('submit', (e) => {
            e.preventDefault();
            this.sendMessage();
        });
        
        // Event listener para tecla Enter
        messageInput.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                this.sendMessage();
            }
        });
        
        // Event listener para botão de anexo
        attachBtn.addEventListener('click', () => {
            fileInput.click();
        });
        
        // Event listener para seleção de arquivos
        fileInput.addEventListener('change', () => {
            this.handleFileSelection();
        });
        
        // Event listener para limpeza ao fechar modal
        this.modalElement.addEventListener('hidden.bs.modal', () => {
            this.cleanup();
        });
        
        console.log('✅ Event listeners configurados!');
    }

    /**
     * Carrega mensagens existentes da ordem de serviço
     */
    async loadExistingMessages() {
        console.log('📥 Carregando mensagens existentes...');
        
        try {
            const response = await fetch(`/Mensagem/GetMensagens/${this.currentOrderId}`);
            
            if (response.ok) {
                const mensagens = await response.json();
                const messagesContainer = document.getElementById(`chat-messages-${this.modalElement.id}`);
                
                // Limpa o loading
                messagesContainer.innerHTML = '';
                
                // Exibe mensagens
                mensagens.forEach(mensagem => {
                    this.displayMessage(mensagem, false);
                });
                
                // Scroll para o final
                messagesContainer.scrollTop = messagesContainer.scrollHeight;
                
                console.log(`✅ ${mensagens.length} mensagens carregadas!`);
            } else {
                throw new Error('Erro ao carregar mensagens');
            }
            
        } catch (error) {
            console.error('❌ Erro ao carregar mensagens:', error);
            const messagesContainer = document.getElementById(`chat-messages-${this.modalElement.id}`);
            messagesContainer.innerHTML = '<div class="text-center text-danger">Erro ao carregar mensagens</div>';
        }
    }

    /**
     * Envia uma mensagem
     */
    async sendMessage() {
        const modalId = this.modalElement.id;
        const messageInput = document.getElementById(`message-input-${modalId}`);
        const sendBtn = document.getElementById(`send-btn-${modalId}`);
        
        const content = messageInput.value.trim();
        
        if (!content) {
            return;
        }
        
        try {
            console.log('📤 Enviando mensagem...');
            
            // Desabilita botão durante envio
            sendBtn.disabled = true;
            sendBtn.innerHTML = '<div class="spinner-border spinner-border-sm" role="status"></div>';
            
            const response = await fetch('/Mensagem/EnviarMensagem', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({
                    OrdemServicoId: this.currentOrderId,
                    Conteudo: content
                })
            });
            
            if (response.ok) {
                const result = await response.json();
                
                if (result.success) {
                    // Limpa o input
                    messageInput.value = '';
                    messageInput.focus();
                    console.log('✅ Mensagem enviada!');
                } else {
                    throw new Error(result.message || 'Erro ao enviar mensagem');
                }
            } else {
                throw new Error('Erro na requisição');
            }
            
        } catch (error) {
            console.error('❌ Erro ao enviar mensagem:', error);
            alert('Erro ao enviar mensagem. Tente novamente.');
        } finally {
            // Reabilita botão
            sendBtn.disabled = false;
            sendBtn.innerHTML = '<i class="bi bi-send"></i>';
        }
    }

    /**
     * Exibe uma mensagem no chat
     * @param {Object} mensagem - Objeto da mensagem
     * @param {boolean} animate - Se deve animar a entrada
     */
    displayMessage(mensagem, animate = true) {
        const messagesContainer = document.getElementById(`chat-messages-${this.modalElement.id}`);
        
        const messageDiv = document.createElement('div');
        messageDiv.className = `mb-3 ${animate ? 'fade-in' : ''}`;
        
        const isCurrentUser = mensagem.isCurrentUser || false;
        const alignClass = isCurrentUser ? 'text-end' : 'text-start';
        const bgClass = isCurrentUser ? 'bg-primary text-white' : 'bg-white border';
        
        messageDiv.innerHTML = `
            <div class="${alignClass}">
                <div class="d-inline-block p-3 rounded ${bgClass}" style="max-width: 80%; word-wrap: break-word;">
                    ${!isCurrentUser ? `<div class="fw-bold small mb-1">${mensagem.nomeUsuario}</div>` : ''}
                    <div style="white-space: pre-wrap;">${mensagem.conteudo}</div>
                    <div class="small mt-1 ${isCurrentUser ? 'text-white-50' : 'text-muted'}">
                        ${new Date(mensagem.dataEnvio).toLocaleString()}
                    </div>
                </div>
            </div>
        `;
        
        messagesContainer.appendChild(messageDiv);
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    /**
     * Manipula seleção de arquivos
     */
    handleFileSelection() {
        const modalId = this.modalElement.id;
        const fileInput = document.getElementById(`file-input-${modalId}`);
        const previewContainer = document.getElementById(`file-preview-${modalId}`);
        
        // Implementar preview de arquivos aqui
        console.log('📎 Arquivos selecionados:', fileInput.files.length);
    }

    /**
     * Mostra o modal
     */
    show() {
        if (this.modalElement) {
            const modal = new bootstrap.Modal(this.modalElement);
            modal.show();
        }
    }

    /**
     * Esconde o modal
     */
    hide() {
        if (this.modalElement) {
            const modal = bootstrap.Modal.getInstance(this.modalElement);
            if (modal) {
                modal.hide();
            }
        }
    }

    /**
     * Limpeza ao fechar o modal
     */
    cleanup() {
        console.log('🧹 Limpando recursos do chat modal...');
        
        // Sai do grupo SignalR
        if (this.connection) {
            this.connection.invoke('LeaveGroup', `ordem_${this.currentOrderId}`).catch(console.error);
        }
    }

    /**
     * Destrói o componente completamente
     */
    destroy() {
        console.log('💥 Destruindo chat modal...');
        
        // Para a conexão SignalR
        if (this.connection) {
            this.connection.stop().catch(console.error);
        }
        
        // Remove o modal do DOM
        if (this.modalElement) {
            this.modalElement.remove();
        }
        
        // Reset das propriedades
        this.isInitialized = false;
        this.connection = null;
        this.modalElement = null;
        this.currentOrderId = null;
    }
}

// Registra o módulo no LazyLoader
if (window.lazyLoader) {
    window.lazyLoader.registerModule('ChatModal', ChatModal);
}

// Exporta para uso direto
window.ChatModal = ChatModal;

console.log('💬 Módulo ChatModal carregado e registrado!');