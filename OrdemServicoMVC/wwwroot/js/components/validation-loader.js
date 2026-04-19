/**
 * Carregador de Scripts de Validação - Lazy Loading
 * Carrega scripts de validação jQuery apenas quando necessário
 */

class ValidationLoader {
    constructor() {
        this.isLoaded = false;
        this.loadingPromise = null;
        this.validationScripts = [
            {
                url: '/lib/jquery-validation/dist/jquery.validate.min.js',
                id: 'jquery-validation'
            },
            {
                url: '/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js',
                id: 'jquery-validation-unobtrusive'
            }
        ];
    }

    /**
     * Carrega os scripts de validação sob demanda
     * @returns {Promise} - Promise que resolve quando scripts estão carregados
     */
    async loadValidationScripts() {
        // Se já carregado, retorna imediatamente
        if (this.isLoaded) {
            console.log('✅ Scripts de validação já carregados');
            return Promise.resolve();
        }

        // Se já está carregando, retorna a promise existente
        if (this.loadingPromise) {
            console.log('⏳ Aguardando carregamento de validação em progresso...');
            return this.loadingPromise;
        }

        // Inicia o carregamento
        this.loadingPromise = this.performLoad();
        
        try {
            await this.loadingPromise;
            this.isLoaded = true;
            console.log('✅ Scripts de validação carregados com sucesso!');
        } catch (error) {
            console.error('❌ Erro ao carregar scripts de validação:', error);
            this.loadingPromise = null;
            throw error;
        }

        return this.loadingPromise;
    }

    /**
     * Executa o carregamento dos scripts
     * @returns {Promise} - Promise de carregamento
     */
    async performLoad() {
        console.log('📦 Carregando scripts de validação...');

        // Verifica se jQuery está disponível
        if (typeof $ === 'undefined') {
            throw new Error('jQuery não está disponível. Scripts de validação requerem jQuery.');
        }

        // Carrega scripts em sequência usando LazyLoader
        if (window.lazyLoader) {
            await window.lazyLoader.loadScripts(this.validationScripts);
        } else {
            // Fallback para carregamento manual
            for (const script of this.validationScripts) {
                await this.loadScriptManual(script.url);
            }
        }

        // Configura validação padrão
        this.setupDefaultValidation();
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
     * Configura validação padrão para formulários
     */
    setupDefaultValidation() {
        console.log('⚙️ Configurando validação padrão...');

        // Configurações globais de validação
        if ($.validator) {
            // Configurações de mensagens em português
            $.validator.setDefaults({
                errorClass: 'is-invalid',
                validClass: 'is-valid',
                errorElement: 'div',
                errorPlacement: function(error, element) {
                    // Adiciona classe Bootstrap para feedback
                    error.addClass('invalid-feedback');
                    
                    // Posicionamento específico baseado no tipo de input
                    if (element.parent('.input-group').length) {
                        error.insertAfter(element.parent());
                    } else {
                        error.insertAfter(element);
                    }
                },
                highlight: function(element) {
                    // Adiciona classe de erro do Bootstrap
                    $(element).addClass('is-invalid').removeClass('is-valid');
                },
                unhighlight: function(element) {
                    // Remove classe de erro e adiciona classe de sucesso
                    $(element).removeClass('is-invalid').addClass('is-valid');
                }
            });

            // Mensagens customizadas em português
            $.validator.addMethod('cpf', function(value, element) {
                return this.optional(element) || this.validateCPF(value);
            }, 'Por favor, insira um CPF válido.');

            $.validator.addMethod('cnpj', function(value, element) {
                return this.optional(element) || this.validateCNPJ(value);
            }, 'Por favor, insira um CNPJ válido.');

            // Adiciona método de validação de CPF
            $.validator.prototype.validateCPF = function(cpf) {
                cpf = cpf.replace(/[^\d]+/g, '');
                if (cpf.length !== 11 || /^(\d)\1+$/.test(cpf)) return false;
                
                let sum = 0;
                for (let i = 0; i < 9; i++) {
                    sum += parseInt(cpf.charAt(i)) * (10 - i);
                }
                let remainder = (sum * 10) % 11;
                if (remainder === 10 || remainder === 11) remainder = 0;
                if (remainder !== parseInt(cpf.charAt(9))) return false;
                
                sum = 0;
                for (let i = 0; i < 10; i++) {
                    sum += parseInt(cpf.charAt(i)) * (11 - i);
                }
                remainder = (sum * 10) % 11;
                if (remainder === 10 || remainder === 11) remainder = 0;
                return remainder === parseInt(cpf.charAt(10));
            };

            // Adiciona método de validação de CNPJ
            $.validator.prototype.validateCNPJ = function(cnpj) {
                cnpj = cnpj.replace(/[^\d]+/g, '');
                if (cnpj.length !== 14) return false;
                
                // Validação do CNPJ
                let length = cnpj.length - 2;
                let numbers = cnpj.substring(0, length);
                let digits = cnpj.substring(length);
                let sum = 0;
                let pos = length - 7;
                
                for (let i = length; i >= 1; i--) {
                    sum += numbers.charAt(length - i) * pos--;
                    if (pos < 2) pos = 9;
                }
                
                let result = sum % 11 < 2 ? 0 : 11 - sum % 11;
                if (result !== parseInt(digits.charAt(0))) return false;
                
                length = length + 1;
                numbers = cnpj.substring(0, length);
                sum = 0;
                pos = length - 7;
                
                for (let i = length; i >= 1; i--) {
                    sum += numbers.charAt(length - i) * pos--;
                    if (pos < 2) pos = 9;
                }
                
                result = sum % 11 < 2 ? 0 : 11 - sum % 11;
                return result === parseInt(digits.charAt(1));
            };

            console.log('✅ Validação padrão configurada!');
        }
    }

    /**
     * Inicializa validação para um formulário específico
     * @param {string|jQuery} formSelector - Seletor ou elemento jQuery do formulário
     * @param {Object} options - Opções de validação customizadas
     */
    async initializeFormValidation(formSelector, options = {}) {
        // Carrega scripts se necessário
        await this.loadValidationScripts();

        const $form = $(formSelector);
        
        if ($form.length === 0) {
            console.warn(`Formulário não encontrado: ${formSelector}`);
            return;
        }

        console.log(`🎯 Inicializando validação para: ${formSelector}`);

        // Configurações padrão mescladas com opções customizadas
        const validationOptions = {
            // Configurações padrão
            submitHandler: function(form) {
                // Handler padrão de submit
                console.log('📤 Formulário válido, enviando...');
                form.submit();
            },
            invalidHandler: function(event, validator) {
                // Handler para formulário inválido
                console.log('❌ Formulário contém erros de validação');
                
                // Foca no primeiro campo com erro
                if (validator.errorList.length > 0) {
                    $(validator.errorList[0].element).focus();
                }
            },
            // Mescla com opções customizadas
            ...options
        };

        // Inicializa validação no formulário
        $form.validate(validationOptions);

        console.log(`✅ Validação inicializada para: ${formSelector}`);
    }

    /**
     * Detecta e inicializa validação automaticamente para formulários com atributos de validação
     */
    async autoInitializeValidation() {
        console.log('🔍 Detectando formulários que precisam de validação...');

        // Carrega scripts se necessário
        await this.loadValidationScripts();

        // Procura por formulários com atributos de validação
        const $formsWithValidation = $('form').filter(function() {
            return $(this).find('[data-val="true"]').length > 0;
        });

        if ($formsWithValidation.length > 0) {
            console.log(`📋 Encontrados ${$formsWithValidation.length} formulários com validação`);

            // Inicializa validação para cada formulário encontrado
            $formsWithValidation.each((index, form) => {
                const formId = form.id || `form-${index}`;
                console.log(`⚙️ Inicializando validação automática para: ${formId}`);
                
                // Usa validação unobtrusive se disponível
                if ($.validator && $.validator.unobtrusive) {
                    $.validator.unobtrusive.parse(form);
                }
            });

            console.log('✅ Validação automática inicializada!');
        } else {
            console.log('ℹ️ Nenhum formulário com validação encontrado');
        }
    }

    /**
     * Verifica se os scripts de validação estão carregados
     * @returns {boolean} - True se carregados
     */
    isValidationLoaded() {
        return this.isLoaded && typeof $.validator !== 'undefined';
    }

    /**
     * Remove validação de um formulário
     * @param {string|jQuery} formSelector - Seletor do formulário
     */
    removeValidation(formSelector) {
        const $form = $(formSelector);
        
        if ($form.length > 0 && $form.data('validator')) {
            $form.removeData('validator');
            $form.find('.is-invalid, .is-valid').removeClass('is-invalid is-valid');
            $form.find('.invalid-feedback').remove();
            
            console.log(`🗑️ Validação removida de: ${formSelector}`);
        }
    }
}

// Cria instância global do ValidationLoader
window.validationLoader = new ValidationLoader();

// Registra no LazyLoader se disponível
if (window.lazyLoader) {
    window.lazyLoader.registerModule('ValidationLoader', ValidationLoader);
}

// Função de conveniência global
window.loadValidation = async function(formSelector, options) {
    return await window.validationLoader.initializeFormValidation(formSelector, options);
};

// Auto-inicialização quando DOM está pronto
$(document).ready(function() {
    // Detecta automaticamente formulários que precisam de validação
    // Usa timeout para não bloquear o carregamento inicial
    setTimeout(() => {
        window.validationLoader.autoInitializeValidation();
    }, 500);
});

console.log('📝 ValidationLoader inicializado e pronto para uso!');