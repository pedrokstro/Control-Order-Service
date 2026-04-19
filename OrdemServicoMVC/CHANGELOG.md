# Changelog - Control Order Service (COS)

## [2.3.27] - 2026-04-19
### UI / UX
- **Mobile Navbar:** O botão de "Sair" e "Perfil" tiveram suas posições invertidas na barra de navegação inferior mobile para condizer com o padrão ergonômico onde "Perfil" fica antes e o "Sair" fica no final, com coloração destacada de atenção.

## [2.3.26] - 2026-04-19
### Funcionalidades
- **Login por Biometria/Digital:** Adicionada a opção "Usar Digital" diretamente na tela de Login. Através da API WebAuthn do navegador, usuários podem autenticar com Leitor de Impressão Digital (Android/iOS/Estações) sem precisar digitar a senha ou escolher a loja novamente. Totalmente sem senha (passwordless local shim).

## [2.3.25] - 2026-04-19
### UI / UX & Funcionalidades
- **Câmera Nativa:** Novo atalho "Usar Câmera" implementado no formulário de Nova Ordem, visível de forma exclusiva no mobile (`capture="environment"`). A lógica de envio também foi refatorada e agora permite tirar múltiplas fotos ou vídeos consecutivos sem que o envio sobrescreva as mídias selecionadas anteriormente.
- **Mobile Navbar:** Adicionado efeito de Glassmorphism (blur e semi-transparência) na navbar inferior e corrigido o alinhamento central do botão "Nova" com as demais labels de navegação.
- **Bottom Sheet Global:** O sistema de seleção via Bottom Sheet (Offcanvas), antes restrito à tela inicial, foi generalizado como padrão para todos os elementos `.form-select-modern` e `<select>` da interface mobile através do `site.js`.
- **Limpeza de UI Mobile:** Ocultados cabaçalhos, filtros avançados, e FABs de mensagens que causavam layout poluído em dispositivos menores.

## [2.3.22] - 2026-04-01
### Fixed
- **Instant Loading Feedback:** O `showLoading` agora é disparado como a primeira ação no evento `change` dos inputs de arquivo, garantindo que o overlay apareça no momento exato da seleção.
- **Cache Bypass:** Renomeação do ID do overlay (de `globalLoadingOverlay` para `globalAppLoader`) para forçar o navegador e o Service Worker a carregarem a nova estrutura sem spinner (circular).
- **Consistência de UX:** Garantia de que a barra de progresso e o contador de porcentagem sejam os únicos elementos visuais de carregamento durante o processamento de anexos.
- **Correção de "Seconds After":** Removido o delay percebido entre a escolha dos arquivos e o aparecimento do overlay.

## [2.3.21] - 2026-04-01
### Improved
- **Refatoração do Overlay de Carregamento:** Remoção completa de spinners e animações circulares.
- **Design Linear:** Implementada barra de progresso linear com animação *shimmer* e tipografia moderna.
- **Feedback de Porcentagem:** Adicionado contador de porcentagem centralizado.

## [2.3.20] - 2026-04-01
### Performance
- **Processamento Assíncrono:** Refatoração da lógica de validação de anexos para utilizar `async/await` e `setTimeout`, evitando o travamento da interface.
- **Simulação de Progresso:** Adicionado progresso simulado para submissões de formulários `multipart/form-data`.
