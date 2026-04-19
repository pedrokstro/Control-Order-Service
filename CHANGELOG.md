# Changelog - Control Order Service (COS)

## [2.3.23] - 2026-04-01
### Fixed
- **Performance de Anexos:** Otimizado o loop de processamento de anexos. O tempo de processamento caiu de segundos para milissegundos através da implementação de `DocumentFragment` e redução do delay de UI.
- **Renderização em Lote:** Previews de arquivos agora são renderizados de uma vez só, evitando reflows custosos no navegador.

## [2.3.22] - 2026-04-01
### Corrigido
- **Visibilidade da Barra de Progresso**: Refatorada a lógica de processamento de anexos para ser **assíncrona**. Isso garante que o navegador tenha tempo de renderizar cada degrau da barra de porcentagem (0-100%) enquanto prepara os previews. Antes, a barra "pulava" instantaneamente para 100%.
- **Simulação de Progresso no Upload**: Adicionada uma simulação inteligente de barra de carregamento durante o envio de formulários (salvamento da OS e chat). A barra agora se move de forma fluida até 90% enquanto os arquivos estão sendo transmitidos para o servidor, fornecendo um feedback visual contínuo.

## [2.3.19] - 2026-04-01
### Adicionado
- **Barra de Progresso com Porcentagem**: O overlay de carregamento global agora exibe uma barra linear com contagem real de 0% a 100%.
- **Feedback Granular na Seleção de Anexos**: A porcentagem é atualizada conforme cada arquivo é validado e processado, dando feedback instantâneo ao usuário.
- **Melhoria Visual**: Loader mais espesso e texto de porcentagem em destaque azul para melhor visibilidade.

## [2.3.18] - 2026-04-01
### Adicionado
- **Novo Overlay de Carregamento Global**: Implementado um sistema de feedback visual premium com animações de pulso e uma **barra de progresso** (indeterminada). Isso garante que o usuário saiba que o sistema está trabalhando durante uploads pesados.
- **Feedback na Seleção de Arquivos**: O loader agora é exibido instantaneamente ao selecionar múltiplos anexos, informando que o sistema está "Processando Anexos". Isso elimina a sensação de "travamento" ao carregar muitos vídeos ou fotos de uma vez.
- **Mensagens Dinâmicas**: O overlay agora suporta títulos e descrições personalizadas que mudam conforme o contexto da operação.

## [2.3.17] - 2026-04-01
### Melhorado
- **Performance de Preview (ObjectURL)**: Substituição do `FileReader` por `URL.createObjectURL` nos previews de anexos. Esta mudança técnica elimina os congelamentos de interface ao manipular múltiplos arquivos pesados (vídeos/fotos).
- **Fluidez na Remoção**: Adicionada proteção contra propagação de eventos e otimizada a reconstrução da lista de anexos, garantindo uma resposta instantânea ao remover itens selecionados.

## [2.3.16] - 2026-04-01
### Corrigido
- **Razor Syntax**: Corrigido erro de "Contexto Atual" no `_Layout.cshtml` ao escapar diretivas de estilo que entravam em conflito com o motor Razor (.NET).
- **Consistência de Dados**: Ajustada a assinatura do método `ProcessarAnexos` para garantir o vínculo correto entre anexos e mensagens do chat.

## [2.3.15] - 2026-04-01
### Adicionado
- **Deduplicação de Anexos**: Implementada detecção inteligente de arquivos duplicados durante a seleção. O sistema agora bloqueia arquivos com o mesmo nome e tamanho em uma única remessa, garantindo ordens de serviço limpas e organizadas.
- **Alertas de Duplicidade**: Usuários recebem um toast informativo ("Arquivo já selecionado") ao tentarem adicionar mídias repetidas.

## [2.3.14] - 2026-04-01
### Melhorado
- **Global Loading Overlay**: Implementado um sistema de carregamento global para operações que envolvem upload de arquivos pesados. Isso fornece feedback visual imediato ao usuário, eliminando a sensação de "travamento" durante envios de até 20MB.
- **Otimização de Upload (Server-Side)**: Refatorada a lógica de processamento de anexos (`ProcessarAnexos`) para eliminar alocações redundantes de memória, acelerando a gravação de arquivos grandes no banco de dados.

## [2.3.13] - 2026-04-01
### Melhorado
- **Redesign de Notificações (Premium Toast)**: O antigo design "Brutalist" foi substituído por um sistema moderno de notificações com **Glassmorphism**, ícones dinâmicos do Bootstrap e barra de progresso animada.
- **Identidade Visual**: As notificações agora utilizam fontes modernas (Outfit/Inter) e cores refinadas que se integram perfeitamente ao tema premium do COS.


## [2.3.12] - 2026-04-01
### Adicionado
- **Validação Reativa de Anexos**: Implementada validação instantânea no lado do cliente (JavaScript) para arquivos anexados nas telas de criação, edição e chat.
- **Notificações de Erro (Brutalist Toasts)**: O sistema agora exibe alertas visuais imediatos caso o usuário tente anexar arquivos com formatos não suportados ou que excedam o limite de 20MB.
- **Filtragem de Seleção**: Arquivos que não atendem aos critérios são automaticamente descartados da seleção, garantindo que o formulário contenha apenas dados válidos.

## [2.3.11] - 2026-04-01
### Adicionado
- **Suporte Multimídia Expandido**: Sistema agora aceita anexos em vídeo (MP4, WebM, MOV) e documentos PDF, além de imagens.
- **Player de Vídeo Integrado**: Visualização de vídeos diretamente no chat e no modal de detalhes da ordem de serviço.
- **Preview de PDF**: Integração de visualizador de PDF no Lightbox de detalhes.
- **Novos Limites**: Limite de tamanho de anexos aumentado de 5MB para 20MB para suportar vídeos técnicos.
- **Edição com Anexos**: Adicionada funcionalidade de upload de novos arquivos diretamente pela tela de edição de ordens de serviço.

### Melhorado
- **Interface de Upload**: Preview inteligente que identifica e exibe ícones específicos para cada tipo de arquivo (Imagem, Vídeo, PDF).
- **Lightbox Genérico**: O visualizador de anexos agora adapta seu layout dependendo se o conteúdo é uma imagem, vídeo ou documento.


## [2.3.10] - 2026-03-27
### Corrigido
- **Erro Crítico de Inicialização:** Corrigido o `InvalidOperationException` causado pela definição de `SizeLimit` no `IMemoryCache` sem especificar o `Size` nas entradas individuais.
- **Segurança de Thread/Acesso:** Migrado para obtenção de ID do usuário via `Claims` ao invés de desreferenciar `currentUser` na query de mensagens não lidas, prevenindo `NullReferenceException` intermitente no SQL Server.

## [2.3.9] - 2026-03-27
### Otimizado
- **Desempenho de Banco de Dados (Listagem):**
  - Removido o carregamento antecipado (*Eager Loading*) de `Anexos` na query principal. Isso evita que gigabytes de dados binários sejam transferidos do SQL Server para a memória do servidor sem necessidade na listagem.
  - Implementado **Caching em Memória** para as listas de técnicos e lojas (15 min), reduzindo hits redundantes ao banco.
  - Substituída a lógica de "Mensagens Não Lidas" na View (que gerava o problema N+1 queries) por um dicionário pré-calculado de forma eficiente na Controller.
- **Fluidez e UX:**
  - Implementado **Skeleton Screen** moderno no carregamento do modal de detalhes, eliminando o spinner estático por uma percepção de velocidade maior.
  - Refinamento das notificações (Toasts) com design mais premium e bordas coloridas sutis.
  - Melhora significativa no tempo de resposta (TTFB) da página inicial.

## [2.3.8] — 2026-03-26
### Melhorado
- **Layout Compacto no Modal de Detalhes:**
  - Reduzido o espaçamento vertical entre as seções para melhor visualização em telas menores.
  - Implementados estilos mais densos para a grade de informações da ordem.
  - Reduzidas as margens e paddings de cabeçalhos e rodapés no modal.
  - Adicionada barra de rolagem interna (`modal-dialog-scrollable`) para evitar que o modal extrapole a altura da tela.
  - Redimensionados os badges e ícones dentro do modal para uma estética mais equilibrada.

## [2.3.7] — 2026-03-26
### Adicionado
- **Galeria de anexos com lightbox** no modal de detalhes da ordem de serviço:
  - Miniaturas clicáveis com overlay de zoom ao passar o mouse
  - Lightbox fullscreen para visualização ampliada das imagens
  - Botão de download dentro do lightbox
  - Suporte a tecla ESC para fechar o lightbox
  - Mensagem de "Nenhum anexo" quando a ordem não tem arquivos
- **Badge de anexos** visível na coluna principal da tabela desktop (admin):
  - Destaque laranja com ícone de clipe e contagem de anexos
  - Clique no badge abre diretamente o modal de detalhes
  - Contagem inclui **tanto anexos diretos quanto via mensagens/chat**

### Corrigido
- **Bug crítico:** input de upload de arquivos no formulário `Create.cshtml` usava `name="imagens"` mas o controller esperava `name="anexos"` — os arquivos **nunca chegavam ao servidor**
- Badge de anexos agora usa `AnexosCount` do ViewModel (query eficiente sem carregar dados binários) em vez de depender do `.Include(o => o.Anexos)`

---

## [2.3.6] — anterior
### Histórico
- Versão anterior estável com otimizações de UI mobile e correções de datas.
