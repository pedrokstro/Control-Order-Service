/**
 * BRUTALIST ANIMATIONS SYSTEM
 * Scroll-triggered reveals, micro-interactions, spring physics
 */

(function () {
    'use strict';

    const BrutalistAnimations = {
        // Configuration
        config: {
            threshold: 0,
            rootMargin: '100px 0px 0px 0px', // Pre-load before it enters viewport
            staggerDelay: 50
        },

        // Initialize all animations
        init() {
            this.setupScrollReveal();
            this.setupMicroInteractions();
            this.setupHoverEffects();
            this.observeNewElements();

            // Extra Safety: Ensure content is visible on window load
            window.addEventListener('load', () => {
                setTimeout(() => {
                    document.querySelectorAll('.brutal-reveal-hidden').forEach(el => {
                        el.classList.add('brutal-revealed');
                        el.classList.remove('brutal-reveal-hidden');
                    });
                }, 100);
            });
        },

        // Scroll-triggered reveal animations
        setupScrollReveal() {
            const observer = new IntersectionObserver((entries) => {
                entries.forEach((entry, index) => {
                    if (entry.isIntersecting) {
                        setTimeout(() => {
                            entry.target.classList.add('brutal-revealed');
                            // Remove hidden class after animation to prevent layout issues
                            setTimeout(() => {
                                entry.target.classList.remove('brutal-reveal-hidden');
                            }, 700);
                        }, index * this.config.staggerDelay);
                        observer.unobserve(entry.target);
                    }
                });
            }, {
                threshold: this.config.threshold,
                rootMargin: this.config.rootMargin
            });

            // Observe elements with reveal classes
            document.querySelectorAll('[data-brutal-reveal]').forEach(el => {
                el.classList.add('brutal-reveal-hidden');
                observer.observe(el);
            });

            // SAFETY NET: Force reveal after 300ms in case observer fails or page is cached
            setTimeout(() => {
                document.querySelectorAll('.brutal-reveal-hidden').forEach(el => {
                    el.classList.add('brutal-revealed');
                    el.classList.remove('brutal-reveal-hidden');
                });
            }, 300);
        },

        // Micro-interactions for buttons and interactive elements
        setupMicroInteractions() {
            // Button press effect
            document.addEventListener('click', (e) => {
                const btn = e.target.closest('.brutal-btn, .modern-btn, .btn');
                if (btn && !btn.disabled) {
                    btn.classList.add('brutal-pressed');
                    setTimeout(() => btn.classList.remove('brutal-pressed'), 150);
                }
            });

            // Card hover depth effect
            document.querySelectorAll('.brutal-card, .modern-card, .order-detail-card').forEach(card => {
                card.addEventListener('mouseenter', function () {
                    this.style.transform = 'translate(-2px, -2px)';
                });

                card.addEventListener('mouseleave', function () {
                    this.style.transform = 'translate(0, 0)';
                });
            });
        },

        // Enhanced hover effects with spring physics
        setupHoverEffects() {
            // Table row interactions
            document.querySelectorAll('.brutal-table tbody tr, .modern-table tbody tr').forEach(row => {
                row.addEventListener('mouseenter', function () {
                    this.style.transition = 'transform 200ms cubic-bezier(0.68, -0.55, 0.265, 1.55)';
                    this.style.transform = 'translateX(4px)';
                });

                row.addEventListener('mouseleave', function () {
                    this.style.transform = 'translateX(0)';
                });
            });

            // Badge pulse on hover
            document.querySelectorAll('.brutal-badge, .os-badge, .badge').forEach(badge => {
                badge.addEventListener('mouseenter', function () {
                    this.style.animation = 'brutal-pulse 0.6s ease-in-out';
                });

                badge.addEventListener('animationend', function () {
                    this.style.animation = '';
                });
            });
        },

        // Observe DOM for dynamically added elements
        observeNewElements() {
            const observer = new MutationObserver((mutations) => {
                mutations.forEach((mutation) => {
                    mutation.addedNodes.forEach((node) => {
                        if (node.nodeType === 1) {
                            // Re-apply animations to new elements
                            if (node.hasAttribute('data-brutal-reveal')) {
                                node.classList.add('brutal-revealed');
                            }

                            // Apply micro-interactions to new cards
                            if (node.classList.contains('brutal-card') ||
                                node.classList.contains('modern-card')) {
                                this.applyCardHover(node);
                            }
                        }
                    });
                });
            });

            observer.observe(document.body, {
                childList: true,
                subtree: true
            });
        },

        // Apply card hover effect to a specific element
        applyCardHover(card) {
            card.addEventListener('mouseenter', function () {
                this.style.transform = 'translate(-2px, -2px)';
            });

            card.addEventListener('mouseleave', function () {
                this.style.transform = 'translate(0, 0)';
            });
        },

        // Stagger animation for lists
        staggerReveal(selector, delay = 100) {
            const elements = document.querySelectorAll(selector);
            elements.forEach((el, index) => {
                setTimeout(() => {
                    el.classList.add('brutal-animate-slide-in');
                }, index * delay);
            });
        },

        // Parallax effect for backgrounds
        setupParallax() {
            const parallaxElements = document.querySelectorAll('[data-parallax]');

            window.addEventListener('scroll', () => {
                const scrolled = window.pageYOffset;

                parallaxElements.forEach(el => {
                    const speed = el.dataset.parallax || 0.5;
                    el.style.transform = `translateY(${scrolled * speed}px)`;
                });
            });
        },

        // Loading state animation
        showLoading(element) {
            element.classList.add('brutal-loading');
            element.style.pointerEvents = 'none';
            element.style.opacity = '0.6';
        },

        hideLoading(element) {
            element.classList.remove('brutal-loading');
            element.style.pointerEvents = '';
            element.style.opacity = '';
        },

        // Toast notification with Premium/Clean style
        showToast(message, type = 'info') {
            const toast = document.createElement('div');
            toast.className = `modern-toast modern-toast--${type}`;
            
            let icon = 'bi-info-circle-fill';
            if (type === 'success') icon = 'bi-check-circle-fill';
            if (type === 'danger') icon = 'bi-exclamation-triangle-fill';
            if (type === 'warning') icon = 'bi-exclamation-circle-fill';

            toast.innerHTML = `
                <div class="modern-toast__icon">
                    <i class="bi ${icon}"></i>
                </div>
                <div class="modern-toast__content">
                    <div class="modern-toast__title">${type.toUpperCase()}</div>
                    <div class="modern-toast__message">${message}</div>
                </div>
                <button class="modern-toast__close" onclick="this.parentElement.classList.remove('modern-toast--show'); setTimeout(() => this.parentElement.remove(), 400);">
                    <i class="bi bi-x"></i>
                </button>
                <div class="modern-toast__progress"></div>
            `;

            document.body.appendChild(toast);

            setTimeout(() => toast.classList.add('modern-toast--show'), 10);

            // Auto removal after 4s
            setTimeout(() => {
                if (toast.parentElement) {
                    toast.classList.remove('modern-toast--show');
                    setTimeout(() => toast.remove(), 400);
                }
            }, 4000);
        }
    };

    // Auto-initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => BrutalistAnimations.init());
    } else {
        BrutalistAnimations.init();
    }

    // Expose to global scope
    window.BrutalistAnimations = BrutalistAnimations;

})();

// CSS for reveal animations and MODERN TOAST
const revealStyles = document.createElement('style');
revealStyles.textContent = `
    .brutal-reveal-hidden {
        opacity: 0;
        transform: translateY(30px);
    }
    
    .brutal-revealed {
        opacity: 1;
        transform: translateY(0);
        transition: opacity 0.6s cubic-bezier(0.68, -0.55, 0.265, 1.55),
                    transform 0.6s cubic-bezier(0.68, -0.55, 0.265, 1.55);
    }
    
    .brutal-pressed {
        transform: translate(2px, 2px) !important;
        box-shadow: none !important;
    }
    
    /* MODERN TOAST STYLES */
    .modern-toast {
        position: fixed;
        bottom: 30px;
        right: 30px;
        min-width: 320px;
        max-width: 450px;
        background: rgba(255, 255, 255, 0.95);
        backdrop-filter: blur(10px);
        -webkit-backdrop-filter: blur(10px);
        border-radius: 12px;
        padding: 16px;
        display: flex;
        align-items: center;
        gap: 15px;
        box-shadow: 0 15px 35px rgba(0,0,0,0.1), 0 5px 15px rgba(0,0,0,0.05);
        transform: translateX(120%);
        transition: transform 0.4s cubic-bezier(0.19, 1, 0.22, 1);
        z-index: 999999;
        font-family: 'Outfit', 'Inter', -apple-system, system-ui, sans-serif;
        border: 1px solid rgba(255, 255, 255, 0.2);
        overflow: hidden;
    }
    
    .modern-toast--show {
        transform: translateX(0);
    }
    
    .modern-toast__icon {
        width: 40px;
        height: 40px;
        border-radius: 10px;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 1.25rem;
        flex-shrink: 0;
    }
    
    .modern-toast__content {
        flex-grow: 1;
    }
    
    .modern-toast__title {
        font-size: 0.75rem;
        font-weight: 800;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        margin-bottom: 2px;
    }
    
    .modern-toast__message {
        font-size: 0.875rem;
        color: #4b5563;
        line-height: 1.4;
    }
    
    .modern-toast__close {
        background: transparent;
        border: none;
        color: #9ca3af;
        cursor: pointer;
        padding: 4px;
        font-size: 1.25rem;
        line-height: 1;
        transition: color 0.2s;
        border-radius: 6px;
    }
    
    .modern-toast__close:hover {
        color: #111827;
        background: rgba(0,0,0,0.05);
    }
    
    .modern-toast__progress {
        position: absolute;
        bottom: 0;
        left: 0;
        height: 3px;
        width: 100%;
        background: rgba(0,0,0,0.1);
    }
    
    .modern-toast__progress::after {
        content: '';
        position: absolute;
        bottom: 0;
        left: 0;
        height: 100%;
        width: 100%;
        background: var(--toast-color, #3b82f6);
        animation: toast-progress 4s linear forwards;
    }
    
    @keyframes toast-progress {
        from { width: 100%; }
        to { width: 0%; }
    }
    
    /* THEMES */
    .modern-toast--info {
        --toast-color: #3b82f6;
        border-left: 4px solid #3b82f6;
    }
    .modern-toast--info .modern-toast__icon {
        background: rgba(59, 130, 246, 0.1);
        color: #3b82f6;
    }
    .modern-toast--info .modern-toast__title { color: #3b82f6; }
    
    .modern-toast--success {
        --toast-color: #10b981;
        border-left: 4px solid #10b981;
    }
    .modern-toast--success .modern-toast__icon {
        background: rgba(16, 185, 129, 0.1);
        color: #10b981;
    }
    .modern-toast--success .modern-toast__title { color: #10b981; }
    
    .modern-toast--danger {
        --toast-color: #ef4444;
        border-left: 4px solid #ef4444;
    }
    .modern-toast--danger .modern-toast__icon {
        background: rgba(239, 68, 68, 0.1);
        color: #ef4444;
    }
    .modern-toast--danger .modern-toast__title { color: #ef4444; }
    
    .modern-toast--warning {
        --toast-color: #f59e0b;
        border-left: 4px solid #f59e0b;
    }
    .modern-toast--warning .modern-toast__icon {
        background: rgba(245, 158, 11, 0.1);
        color: #f59e0b;
    }
    .modern-toast--warning .modern-toast__title { color: #f59e0b; }
`;
document.head.appendChild(revealStyles);
