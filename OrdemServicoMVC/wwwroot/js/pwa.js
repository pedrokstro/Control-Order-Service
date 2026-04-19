(() => {
    const state = {
        deferredPrompt: null,
        swRegistration: null,
        toastTimeouts: []
    };

    document.addEventListener('DOMContentLoaded', () => {
        registerServiceWorker();
        setupInstallPrompt();
        handleConnectionBanner();
        highlightMobileNavigation();
        setupVisibilityHandlers();
    });

    function registerServiceWorker() {
        if (!('serviceWorker' in navigator)) {
            console.warn('ServiceWorker não suportado neste navegador.');
            return;
        }

        window.addEventListener('load', () => {
            navigator.serviceWorker.register('/service-worker.js')
                .then((registration) => {
                    state.swRegistration = registration;
                    console.log('[PWA] Service worker registrado:', registration.scope);

                    registration.addEventListener('updatefound', () => {
                        const newWorker = registration.installing;
                        if (!newWorker) {
                            return;
                        }

                        newWorker.addEventListener('statechange', () => {
                            if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                                showToast('Atualização disponível', 'Recarregar', () => {
                                    if (registration.waiting) {
                                        registration.waiting.postMessage({ type: 'SKIP_WAITING' });
                                    }
                                });
                            }
                        });
                    });
                })
                .catch((error) => console.error('[PWA] Falha ao registrar service worker:', error));

            navigator.serviceWorker.addEventListener('controllerchange', () => {
                console.log('[PWA] Novo service worker ativo, recarregando...');
                window.location.reload();
            });
        });
    }

    function setupInstallPrompt() {
        const installButton = document.getElementById('pwaInstallButton');
        if (!installButton) {
            return;
        }

        window.addEventListener('beforeinstallprompt', (event) => {
            event.preventDefault();
            state.deferredPrompt = event;
            installButton.classList.remove('d-none');
            installButton.setAttribute('aria-hidden', 'false');
        });

        window.addEventListener('appinstalled', () => {
            showToast('Aplicativo instalado com sucesso!');
            hideInstallButton();
        });

        installButton.addEventListener('click', async () => {
            if (!state.deferredPrompt) {
                showToast('Instalação não disponível agora. Tente novamente mais tarde.');
                return;
            }

            installButton.disabled = true;
            state.deferredPrompt.prompt();

            const { outcome } = await state.deferredPrompt.userChoice;
            console.log('[PWA] Resultado da instalação:', outcome);

            if (outcome === 'accepted') {
                showToast('Obrigado por instalar o COS!');
                hideInstallButton();
            } else {
                installButton.disabled = false;
                showToast('Instalação cancelada.');
            }

            state.deferredPrompt = null;
        });

        function hideInstallButton() {
            installButton.classList.add('d-none');
            installButton.setAttribute('aria-hidden', 'true');
        }
    }

    function handleConnectionBanner() {
        const banner = document.getElementById('connectionStatusBanner');
        if (!banner) {
            return;
        }

        const showBanner = (message, isError = false) => {
            banner.textContent = '';
            const icon = document.createElement('i');
            icon.classList.add('me-2', 'bi', isError ? 'bi-wifi-off' : 'bi-wifi');
            banner.appendChild(icon);
            banner.append(message);
            banner.classList.toggle('connection-banner--online', !isError);
            banner.classList.toggle('connection-banner--offline', isError);
            banner.classList.add('is-visible');

            if (!isError) {
                setTimeout(() => banner.classList.remove('is-visible'), 2500);
            }
        };

        const updateStatus = () => {
            if (!navigator.onLine) {
                showBanner('Sem conexão. Tentando reconectar...', true);
            } else {
                // Remove o banner quando conectado
                banner.classList.remove('is-visible');
            }
        };

        window.addEventListener('online', updateStatus);
        window.addEventListener('offline', updateStatus);
    }

    function highlightMobileNavigation() {
        const items = document.querySelectorAll('.mobile-nav-item');
        if (!items.length) {
            return;
        }

        const path = window.location.pathname.toLowerCase();

        items.forEach((item) => {
            const route = (item.dataset.navRoute || '').toLowerCase();
            if (!route) {
                return;
            }

            if (route === '/' && path.length === 1) {
                item.classList.add('is-active');
                return;
            }

            if (route !== '/' && path.startsWith(route)) {
                item.classList.add('is-active');
            } else {
                item.classList.remove('is-active');
            }
        });
    }

    function setupVisibilityHandlers() {
        document.addEventListener('visibilitychange', () => {
            if (document.visibilityState === 'visible') {
                highlightMobileNavigation();
            }
        });
    }

    function showToast(message, actionLabel, actionCallback) {
        const container = document.getElementById('pwaToastContainer');
        if (!container) {
            return;
        }

        const toast = document.createElement('div');
        toast.className = 'pwa-toast';
        toast.setAttribute('role', 'status');
        toast.innerHTML = `
            <span class="pwa-toast__message">${message}</span>
        `;

        if (actionLabel && typeof actionCallback === 'function') {
            const actionButton = document.createElement('button');
            actionButton.className = 'pwa-toast__action';
            actionButton.type = 'button';
            actionButton.textContent = actionLabel;
            actionButton.addEventListener('click', () => {
                actionCallback();
                dismissToast(toast);
            });
            toast.appendChild(actionButton);
        }

        container.appendChild(toast);

        const timeoutId = setTimeout(() => dismissToast(toast), 5000);
        state.toastTimeouts.push(timeoutId);
    }

    function dismissToast(toastElement) {
        toastElement.classList.add('is-hiding');
        setTimeout(() => toastElement.remove(), 300);
    }
})();
