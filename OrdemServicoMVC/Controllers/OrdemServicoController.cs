using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdemServicoMVC.Data;
using OrdemServicoMVC.Models;
using OrdemServicoMVC.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR; // Para notificações em tempo real
using OrdemServicoMVC.Hubs; // Para acessar o ChatHub
using OrdemServicoMVC.Scripts;
using Microsoft.Extensions.Options; // Para acessar as configurações
using Microsoft.Extensions.Caching.Memory; // Para cache em memória
using System.Security.Claims; // Para acessar os dados do usuário logado via Claims

namespace OrdemServicoMVC.Controllers
{
    // Controller responsável pelo gerenciamento das ordens de serviço
    [Authorize]
    public class OrdemServicoController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<OrdemServicoController> _logger;
        private readonly IHubContext<ChatHub> _hubContext; // Para enviar notificações SignalR
        private readonly IMemoryCache _cache; // Para otimização de performance via cache
        private const int PageSize = 50; // Número de itens por página (aumentado para mostrar mais ordens)

        public OrdemServicoController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<OrdemServicoController> logger,
            IHubContext<ChatHub> hubContext, // Injeta o contexto do SignalR
            IOptions<AppSettings> appSettings,
            IMemoryCache cache) // Injeta o cache em memória
            : base(appSettings) // Chama o construtor da classe base
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _hubContext = hubContext; // Inicializa o contexto do SignalR
            _cache = cache;
        }

        /// <summary>
        /// Exibe a lista paginada de ordens de serviço com filtros opcionais
        /// Para usuários comuns: mostra apenas suas próprias ordens
        /// Para administradores: mostra todas as ordens com opções de filtro
        /// </summary>
        /// <param name="page">Número da página para paginação</param>
        /// <param name="status">Filtro por status (apenas admin)</param>
        /// <param name="prioridade">Filtro por prioridade (apenas admin)</param>
        /// <param name="tecnico">Filtro por técnico responsável (apenas admin)</param>
        /// <param name="loja">Filtro por nome da loja (apenas admin)</param>
        /// <param name="dataInicio">Filtro por data inicial (apenas admin)</param>
        /// <param name="dataFim">Filtro por data final (apenas admin)</param>
        /// <returns>View com lista paginada de ordens de serviço</returns>
        // GET: OrdemServico
        public async Task<IActionResult> Index(int page = 1, int? status = null, int? prioridade = null, 
            string? tecnico = null, string? loja = null, int? setor = null, DateTime? dataInicio = null, DateTime? dataFim = null, string? busca = null)
        {
            // Obtém o usuário atual autenticado
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Monta a query base incluindo apenas as entidades necessárias para a listagem
            IQueryable<OrdemServico> query = _context.OrdensServico
                .AsNoTracking()                              // Otimização: sem tracking para queries de leitura
                .Include(o => o.UsuarioCriador)           // Dados do usuário que criou a ordem
                .Include(o => o.TecnicoResponsavel);      // Dados do técnico responsável

            // Aplica filtros baseados no perfil do usuário
            if (!User.IsInRole("Admin"))
            {
                // Usuários comuns veem apenas suas próprias ordens
                query = query.Where(o => o.UsuarioCriadorId == currentUser.Id);
            }

            // Filtro de busca global (disponível para todos os usuários)
            if (!string.IsNullOrWhiteSpace(busca))
            {
                var buscaLower = busca.ToLower().Trim();
                query = query.Where(o => 
                    o.Id.ToString().Contains(buscaLower) ||
                    o.Descricao.ToLower().Contains(buscaLower) ||
                    (o.UsuarioCriador != null && o.UsuarioCriador.UserName != null && o.UsuarioCriador.UserName.ToLower().Contains(buscaLower)) ||
                    (o.UsuarioCriador != null && o.UsuarioCriador.NomeLoja != null && o.UsuarioCriador.NomeLoja.ToLower().Contains(buscaLower)) ||
                    (o.TecnicoResponsavel != null && o.TecnicoResponsavel.UserName != null && o.TecnicoResponsavel.UserName.ToLower().Contains(buscaLower))
                );
            }

            if (User.IsInRole("Admin"))
            {
                // Administradores podem aplicar filtros avançados
                
                // Filtro por status da ordem
                if (status.HasValue)
                {
                    query = query.Where(o => (int)o.Status == status.Value);
                }

                // Filtro por prioridade da ordem
                if (prioridade.HasValue)
                {
                    query = query.Where(o => (int)o.Prioridade == prioridade.Value);
                }

                // Filtro por técnico responsável
                if (!string.IsNullOrEmpty(tecnico))
                {
                    if (tecnico == "sem_tecnico")
                    {
                        // Filtra ordens sem técnico atribuído (TecnicoResponsavelId é null)
                        query = query.Where(o => o.TecnicoResponsavelId == null);
                    }
                    else
                    {
                        // Filtra ordens com técnico específico
                        query = query.Where(o => o.TecnicoResponsavelId == tecnico);
                    }
                }

                // Filtro por nome da loja do usuário criador
                if (!string.IsNullOrEmpty(loja))
                {
                    query = query.Where(o => o.UsuarioCriador != null && 
                        o.UsuarioCriador.NomeLoja.Contains(loja));
                }

                // Filtro por setor da ordem de serviço
                if (setor.HasValue)
                {
                    query = query.Where(o => (int)o.Setor == setor.Value);
                }

                // Filtro por data de criação (início do período)
                if (dataInicio.HasValue)
                {
                    query = query.Where(o => o.DataCriacao.Date >= dataInicio.Value.Date);
                }

                // Filtro por data de criação (fim do período)
                if (dataFim.HasValue)
                {
                    query = query.Where(o => o.DataCriacao.Date <= dataFim.Value.Date);
                }
            }

            List<OrdemServico> ordens;
            int totalItems;
            int totalPages;

            // Verifica se há qualquer filtro ativo
            bool temFiltroAtivo = status.HasValue || 
                                 prioridade.HasValue || 
                                 !string.IsNullOrEmpty(tecnico) || 
                                 !string.IsNullOrEmpty(loja) || 
                                 dataInicio.HasValue || 
                                 dataFim.HasValue ||
                                 !string.IsNullOrWhiteSpace(busca);

            bool isAdmin = User.IsInRole("Admin");

            Func<IQueryable<OrdemServico>, IOrderedQueryable<OrdemServico>> ordenar = queryOrdenar =>
                queryOrdenar.OrderByDescending(o => o.DataCriacao);

            if (!temFiltroAtivo) // Aplica a lógica de separação para todos os usuários
            {
                // Sem filtros ativos: separa ordens ativas (página 1) das concluídas (páginas 2+)
                var ordensAtivas = ordenar(query.Where(o => o.Status != StatusEnum.Concluida));
                var ordensConcluidas = ordenar(query.Where(o => o.Status == StatusEnum.Concluida));

                // Conta total de ordens ativas e concluídas
                var totalOrdensAtivas = await ordensAtivas.CountAsync();
                var totalOrdensConcluidas = await ordensConcluidas.CountAsync();

                // Se não há ordens ativas, a página 1 mostra as primeiras ordens concluídas
                if (totalOrdensAtivas == 0)
                {
                    // Sem ordens ativas: paginação normal das ordens concluídas
                    ordens = await ordensConcluidas
                        .Skip((page - 1) * PageSize)
                        .Take(PageSize)
                        .ToListAsync();
                    
                    // Calcula total de páginas apenas com base nas ordens concluídas
                    totalPages = (int)Math.Ceiling(totalOrdensConcluidas / (double)PageSize);
                    // Garante pelo menos 1 página mesmo sem ordens
                    if (totalPages == 0) totalPages = 1;
                }
                else
                {
                    // Há ordens ativas: aplica separação normal
                    if (page == 1)
                    {
                        // Página 1: apenas ordens ativas (até 20)
                        ordens = await ordensAtivas.Take(PageSize).ToListAsync();
                    }
                    else
                    {
                        // Páginas 2+: apenas ordens concluídas
                        // Para página 2, queremos os primeiros 20 itens (skip 0)
                        // Para página 3, queremos os próximos 20 itens (skip 20), etc.
                        var skipCount = (page - 2) * PageSize; // page-2 porque página 2 = primeira página de concluídas
                        ordens = await ordensConcluidas
                            .Skip(skipCount)
                            .Take(PageSize)
                            .ToListAsync();
                    }
                    
                    // Calcula total de páginas: 1 para ativas + páginas para concluídas
                    var paginasConcluidas = (int)Math.Ceiling(totalOrdensConcluidas / (double)PageSize);
                    totalPages = 1 + paginasConcluidas;
                }
                
                totalItems = totalOrdensAtivas + totalOrdensConcluidas;
            }
            else
            {
                // Com filtros ativos ou usuário comum: paginação normal
                query = ordenar(query);
                
                // Conta total de itens filtrados
                totalItems = await query.CountAsync();
                
                // Aplica paginação normal
                ordens = await query
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();
                
                // Calcula total de páginas normalmente
                totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
            }

            var viewModel = new OrdemServicoIndexViewModel
            {
                OrdensServico = ordens,
                PaginaAtual = page,
                TotalPaginas = totalPages,
                TotalItens = totalItems
            };

            // Calcula a contagem de anexos para as ordens da página atual
            // (inclui anexos diretos da OS + anexos enviados pelo chat/mensagens)
            if (ordens.Any())
            {
                var ordemIds = ordens.Select(o => o.Id).ToList();

                // Anexos diretos da ordem de serviço
                var anexosDiretos = await _context.Anexos
                    .AsNoTracking()
                    .Where(a => a.OrdemServicoId != null && ordemIds.Contains(a.OrdemServicoId.Value))
                    .GroupBy(a => a.OrdemServicoId!.Value)
                    .Select(g => new { OrdemId = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Anexos enviados via chat (mensagens da ordem)
                var anexosChat = await _context.Anexos
                    .AsNoTracking()
                    .Where(a => a.MensagemId != null && a.Mensagem != null && ordemIds.Contains(a.Mensagem.OrdemServicoId))
                    .GroupBy(a => a.Mensagem!.OrdemServicoId)
                    .Select(g => new { OrdemId = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Mescla as duas contagens
                var contagem = new Dictionary<int, int>();
                foreach (var item in anexosDiretos)
                    contagem[item.OrdemId] = item.Count;
                foreach (var item in anexosChat)
                {
                    if (contagem.ContainsKey(item.OrdemId))
                        contagem[item.OrdemId] += item.Count;
                    else
                        contagem[item.OrdemId] = item.Count;
                }
                viewModel.AnexosCount = contagem;

                // Calcula a contagem de mensagens não lidas para o usuário atual
                var currentUserId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                var mensagensNaoLidas = await _context.Mensagens
                    .AsNoTracking()
                    .Where(m => ordemIds.Contains(m.OrdemServicoId) && !m.Lida && m.UsuarioId != currentUserId)
                    .GroupBy(m => m.OrdemServicoId)
                    .Select(g => new { OrdemId = g.Key, Count = g.Count() })
                    .ToListAsync();
                
                var contagemMensagens = new Dictionary<int, int>();
                foreach (var item in mensagensNaoLidas)
                {
                    contagemMensagens[item.OrdemId] = item.Count;
                }
                viewModel.MensagensNaoLidasCount = contagemMensagens;
            }

            // Se for admin, popula as listas para os dropdowns
            if (User.IsInRole("Admin"))
            {
                // Lista de status
                viewModel.StatusList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "1", Text = "Aberta" },
                    new SelectListItem { Value = "2", Text = "Em Andamento" },
                    new SelectListItem { Value = "3", Text = "Concluída" }
                };

                // Lista de técnicos
                viewModel.TecnicosList = await GetTecnicosSelectList();
                
                // Lista de lojas
                viewModel.LojasList = await GetLojasSelectList();
                
                // Preserva os valores atuais dos filtros na ViewBag para os links de paginação
                ViewBag.CurrentStatus = status;
            ViewBag.CurrentPrioridade = prioridade;
            ViewBag.CurrentTecnico = tecnico;
            ViewBag.CurrentLoja = loja;
            ViewBag.CurrentSetor = setor; // Preserva o filtro de setor selecionado
            ViewBag.CurrentDataInicio = dataInicio?.ToString("yyyy-MM-dd");
            ViewBag.CurrentDataFim = dataFim?.ToString("yyyy-MM-dd");
            ViewBag.CurrentBusca = busca;
            }

            ViewBag.ShowOrderBackButton = true;
            return View(viewModel);
        }

        // GET: OrdemServico/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ordemServico = await _context.OrdensServico
                .AsNoTracking() // Otimização: sem tracking para queries de leitura
                .Include(o => o.UsuarioCriador)
                .Include(o => o.TecnicoResponsavel)
                .Include(o => o.Anexos)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ordemServico == null)
            {
                return NotFound();
            }

            // Verifica se o usuário tem permissão para ver esta ordem
            // Permite acesso para: Admins, Criador da ordem, Técnico responsável
            var currentUser = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && 
                ordemServico.UsuarioCriadorId != currentUser?.Id && 
                ordemServico.TecnicoResponsavelId != currentUser?.Id)
            {
                return Forbid();
            }

            return View(ordemServico);
        }

        /// <summary>
        /// Retorna os detalhes da ordem de serviço em formato parcial para exibição em modal
        /// </summary>
        /// <param name="id">ID da ordem de serviço</param>
        /// <returns>View parcial com detalhes da ordem</returns>
        // GET: OrdemServico/DetailsModal/5
        public async Task<IActionResult> DetailsModal(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Inclui entidades relacionadas, incluindo mensagens para o chat
            var ordemServico = await _context.OrdensServico
                .AsNoTracking() // Otimização: sem tracking para queries de leitura
                .Include(o => o.UsuarioCriador)
                .Include(o => o.TecnicoResponsavel)
                .Include(o => o.Anexos)
                .Include(o => o.Mensagens) // Inclui mensagens para o chat
                    .ThenInclude(m => m.Usuario) // Inclui dados do usuário de cada mensagem
                .Include(o => o.Mensagens)
                    .ThenInclude(m => m.Anexos) // Inclui anexos das mensagens
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ordemServico == null)
            {
                return NotFound();
            }

            // Verifica se o usuário tem permissão para ver esta ordem
            var currentUser = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && 
                ordemServico.UsuarioCriadorId != currentUser?.Id && 
                ordemServico.TecnicoResponsavelId != currentUser?.Id)
            {
                return Forbid();
            }

            // Ordena as mensagens por data de envio (mais antigas primeiro)
            if (ordemServico.Mensagens != null && ordemServico.Mensagens.Any())
            {
                ordemServico.Mensagens = ordemServico.Mensagens
                    .OrderBy(m => m.DataEnvio)
                    .ToList();
            }

            // Passa o ID do usuário atual para a view para identificar mensagens próprias
            ViewBag.CurrentUserId = currentUser?.Id;

            // Retorna uma view parcial sem layout para o modal
            return PartialView("_DetailsModal", ordemServico);
        }

        // GET: OrdemServico/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new OrdemServicoCreateViewModel();
            
            // Se for admin, pode atribuir técnico
            if (User.IsInRole("Admin"))
            {
                viewModel.Tecnicos = await GetTecnicosSelectList();
            }

            return View(viewModel);
        }

        // POST: OrdemServico/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrdemServicoCreateViewModel viewModel, List<IFormFile> anexos)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Definir prioridade baseada no tipo de usuário
                PrioridadeEnum prioridadeFinal;
                if (User.IsInRole("Admin") && viewModel.Prioridade.HasValue)
                {
                    // Admin pode definir prioridade personalizada
                    prioridadeFinal = viewModel.Prioridade.Value;
                }
                else
                {
                    // Usuários normais recebem prioridade padrão (Média)
                    prioridadeFinal = PrioridadeEnum.Media;
                }

                var ordemServico = new OrdemServico
                {
                    Titulo = viewModel.Titulo,
                    Descricao = viewModel.Descricao,
                    Prioridade = prioridadeFinal, // Usa a prioridade definida pela lógica acima
                    Setor = viewModel.Setor, // Mapeia o setor selecionado no formulário
                    UsuarioCriadorId = currentUser.Id,
                    TecnicoResponsavelId = viewModel.TecnicoResponsavelId,
                    Observacoes = viewModel.Observacoes
                };

                _context.Add(ordemServico);
                await _context.SaveChangesAsync();
                
                // Processar anexos se houver
                if (anexos != null && anexos.Count > 0)
                {
                    await ProcessarAnexos(anexos, currentUser.Id, ordemServico.Id);
                }
                
                // Enviar notificação SignalR para todos os usuários sobre nova ordem
                await _hubContext.Clients.All.SendAsync("NewOrderCreated", new {
                    id = ordemServico.Id,
                    titulo = ordemServico.Titulo,
                    prioridade = ordemServico.Prioridade.ToString(),
                    criador = currentUser.UserName,
                    nomeLoja = currentUser.NomeLoja
                });
                
                _logger.LogInformation($"Nova ordem de serviço criada: {ordemServico.Id} por {currentUser.UserName}");
                
                TempData["SuccessMessage"] = "Ordem de serviço criada com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            // Se chegou aqui, algo falhou, reexibe o formulário
            if (User.IsInRole("Admin"))
            {
                viewModel.Tecnicos = await GetTecnicosSelectList();
            }
            
            return View(viewModel);
        }

        // GET: OrdemServico/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ordemServico = await _context.OrdensServico.FindAsync(id);
            if (ordemServico == null)
            {
                return NotFound();
            }

            var viewModel = new OrdemServicoEditViewModel
            {
                Id = ordemServico.Id,
                Titulo = ordemServico.Titulo,
                Descricao = ordemServico.Descricao,
                Prioridade = ordemServico.Prioridade,
                Status = ordemServico.Status,
                Setor = ordemServico.Setor, // Mapeia o setor da ordem de serviço
                TecnicoResponsavelId = ordemServico.TecnicoResponsavelId,
                Observacoes = ordemServico.Observacoes,
                DataCriacao = ordemServico.DataCriacao, // Mapeia a data de criação
                DataConclusao = ordemServico.DataConclusao, // Mapeia a data de conclusão
                Tecnicos = await GetTecnicosSelectList()
            };

            return View(viewModel);
        }

        // POST: OrdemServico/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, OrdemServicoEditViewModel viewModel, List<IFormFile> anexos)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var ordemServico = await _context.OrdensServico.FindAsync(id);
                    if (ordemServico == null)
                    {
                        return NotFound();
                    }

                    // Validação: não permite marcar como concluída sem técnico atribuído
                    if (viewModel.Status == StatusEnum.Concluida && string.IsNullOrEmpty(viewModel.TecnicoResponsavelId))
                    {
                        ModelState.AddModelError("Status", "Não é possível marcar a ordem como concluída sem atribuir um técnico responsável.");
                        viewModel.Tecnicos = await GetTecnicosSelectList();
                        return View(viewModel);
                    }

                    // Atualiza os campos
                    ordemServico.Titulo = viewModel.Titulo;
                    ordemServico.Descricao = viewModel.Descricao;
                    ordemServico.Prioridade = viewModel.Prioridade;
                    ordemServico.Setor = viewModel.Setor; // Atualiza o setor da ordem de serviço
                    ordemServico.TecnicoResponsavelId = viewModel.TecnicoResponsavelId;
                    ordemServico.Observacoes = viewModel.Observacoes;
                    
                    // Se o status mudou para concluído, define a data de conclusão
                    if (ordemServico.Status != viewModel.Status && viewModel.Status == StatusEnum.Concluida)
                    {
                        // Define data de conclusão com horário brasileiro (UTC-3)
                ordemServico.DataConclusao = DateTime.UtcNow.AddHours(-3);
                    }
                    else if (viewModel.Status != StatusEnum.Concluida)
                    {
                        ordemServico.DataConclusao = null;
                    }
                    
                    ordemServico.Status = viewModel.Status;

                    _context.Update(ordemServico);
                    await _context.SaveChangesAsync();
                    
                    // Processar novos anexos se houver
                    if (anexos != null && anexos.Any())
                    {
                        var currentUserId = (await _userManager.GetUserAsync(User))?.Id ?? "";
                        await ProcessarAnexos(anexos, currentUserId, id);
                    }
                    
                    // Enviar notificação SignalR sobre atualização da ordem
                    await _hubContext.Clients.All.SendAsync("OrderStatusUpdated", new {
                        id = ordemServico.Id,
                        titulo = ordemServico.Titulo,
                        status = ordemServico.Status.ToString(),
                        tecnicoId = ordemServico.TecnicoResponsavelId,
                        dataConclusao = ordemServico.DataConclusao?.ToString("dd/MM/yyyy HH:mm")
                    });
                    
                    _logger.LogInformation($"Ordem de serviço {id} atualizada por {User.Identity?.Name}");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrdemServicoExists(viewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            
            viewModel.Tecnicos = await GetTecnicosSelectList();
            return View(viewModel);
        }

        // GET: OrdemServico/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ordemServico = await _context.OrdensServico
                .Include(o => o.UsuarioCriador)
                .Include(o => o.TecnicoResponsavel)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (ordemServico == null)
            {
                return NotFound();
            }

            return View(ordemServico);
        }

        // POST: OrdemServico/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ordemServico = await _context.OrdensServico.FindAsync(id);
            if (ordemServico != null)
            {
                _context.OrdensServico.Remove(ordemServico);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Ordem de serviço {id} excluída por {User.Identity?.Name}");
            }

            return RedirectToAction(nameof(Index));
        }

        // Método auxiliar para verificar se uma ordem existe
        private bool OrdemServicoExists(int id)
        {
            return _context.OrdensServico.Any(e => e.Id == id);
        }

        // Método para atualizar status, técnico e prioridade via AJAX
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AtualizarStatusTecnico(int id, int? status, string? tecnicoId, int? prioridade)
        {
            try
            {
                var ordem = await _context.OrdensServico.FindAsync(id);
                if (ordem == null)
                {
                    return Json(new { success = false, message = "Ordem não encontrada" });
                }

                bool alterado = false;

                // Atualiza status se fornecido
                if (status.HasValue && Enum.IsDefined(typeof(StatusEnum), status.Value))
                {
                    var novoStatus = (StatusEnum)status.Value;
                    
                    // Validação: não permite marcar como concluída sem técnico atribuído
                    if (novoStatus == StatusEnum.Concluida && string.IsNullOrEmpty(ordem.TecnicoResponsavelId) && string.IsNullOrEmpty(tecnicoId))
                    {
                        return Json(new { success = false, message = "Não é possível marcar a ordem como concluída sem atribuir um técnico responsável." });
                    }
                    
                    ordem.Status = novoStatus;
                    alterado = true;

                    // Se status for concluído, define data de conclusão
                    if (ordem.Status == StatusEnum.Concluida && !ordem.DataConclusao.HasValue)
                    {
                        // Define data de conclusão com horário brasileiro (UTC-3)
                ordem.DataConclusao = DateTime.UtcNow.AddHours(-3);
                    }
                    // Se status não for concluído, remove data de conclusão
                    else if (ordem.Status != StatusEnum.Concluida)
                    {
                        ordem.DataConclusao = null;
                    }
                }

                // Atualiza técnico se fornecido
                if (tecnicoId != null)
                {
                    ordem.TecnicoResponsavelId = string.IsNullOrEmpty(tecnicoId) ? null : tecnicoId;
                    alterado = true;
                }

                // Atualiza prioridade se fornecida
                if (prioridade.HasValue && Enum.IsDefined(typeof(PrioridadeEnum), prioridade.Value))
                {
                    var novaPrioridade = (PrioridadeEnum)prioridade.Value;
                    ordem.Prioridade = novaPrioridade;
                    alterado = true;
                }

                if (alterado)
                {
                    await _context.SaveChangesAsync();
                    
                    // Determina qual campo foi alterado para a notificação
                    string campoAlterado = "";
                    string valorAlterado = "";
                    
                    if (status.HasValue)
                    {
                        campoAlterado = "status";
                        valorAlterado = ordem.Status.ToString();
                    }
                    else if (prioridade.HasValue)
                    {
                        campoAlterado = "prioridade";
                        valorAlterado = ordem.Prioridade.ToString();
                    }
                    else if (tecnicoId != null)
                    {
                        campoAlterado = "tecnico";
                        valorAlterado = ordem.TecnicoResponsavelId ?? "Nenhum";
                    }
                    
                    // Enviar notificação SignalR sobre atualização via AJAX
                    await _hubContext.Clients.All.SendAsync("OrderStatusUpdated", new {
                        id = ordem.Id,
                        titulo = ordem.Titulo,
                        status = ordem.Status.ToString(),
                        prioridade = ordem.Prioridade.ToString(),
                        tecnicoId = ordem.TecnicoResponsavelId,
                        dataConclusao = ordem.DataConclusao?.ToString("dd/MM/yyyy HH:mm"),
                        campoAlterado = campoAlterado,
                        valorAlterado = valorAlterado
                    });
                    
                    _logger.LogInformation($"Ordem {id} atualizada por {User.Identity?.Name}");
                }

                return Json(new { success = true, message = "Atualizado com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao atualizar ordem {id}");
                return Json(new { success = false, message = "Erro interno do servidor" });
            }
        }

        // Método auxiliar para obter lista de técnicos com cache
private async Task<List<SelectListItem>> GetTecnicosSelectList()
{
    const string cacheKey = "TecnicosSelectList";
    
    if (!_cache.TryGetValue(cacheKey, out List<SelectListItem>? tecnicos) || tecnicos == null)
    {
        // Projeta diretamente na query do banco, evitando carregar todos os usuários em memória
        tecnicos = await _userManager.Users
            .AsNoTracking()
            .Where(u => u.IsTecnico)
            .OrderBy(u => u.NomeLoja)
            .Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = u.NomeLoja
            })
            .ToListAsync();

        tecnicos.Insert(0, new SelectListItem
        {
            Value = "",
            Text = "Nenhum técnico atribuído"
        });

        // Configura o cache por 15 minutos
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(15))
            .SetSlidingExpiration(TimeSpan.FromMinutes(5))
            .SetSize(1); // Obrigatório quando SizeLimit está definido no Program.cs
            
        _cache.Set(cacheKey, tecnicos, cacheOptions);
        _logger.LogInformation("Cache de técnicos populado.");
    }

    return tecnicos;
}

        // Método auxiliar para obter lista de lojas com cache
private async Task<List<SelectListItem>> GetLojasSelectList()
{
    const string cacheKey = "LojasSelectList";
    
    if (!_cache.TryGetValue(cacheKey, out List<SelectListItem>? lojas) || lojas == null)
    {
        // Projeta diretamente na query do banco, evitando carregar todos os usuários em memória
        lojas = await _userManager.Users
            .AsNoTracking()
            .Where(u => !u.IsTecnico && !u.IsAdmin)
            .OrderBy(u => u.NomeLoja)
            .Select(u => new SelectListItem
            {
                Value = u.NomeLoja,
                Text = u.NomeLoja
            })
            .ToListAsync();

        // Reordena em memória para ordenação numérica das lojas
        lojas = lojas
            .OrderBy(x => {
                if (x.Text != null && x.Text.StartsWith("LOJA "))
                {
                    var parts = x.Text.Split(' ');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int lojaNum))
                    {
                        return $"1{lojaNum:D3}";
                    }
                }
                return "2" + x.Text;
            })
            .ToList();

        lojas.Insert(0, new SelectListItem
        {
            Value = "",
            Text = "Todas as lojas"
        });

        // Configura o cache por 15 minutos
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(15))
            .SetSlidingExpiration(TimeSpan.FromMinutes(5))
            .SetSize(1); // Obrigatório quando SizeLimit está definido no Program.cs
            
        _cache.Set(cacheKey, lojas, cacheOptions);
        _logger.LogInformation("Cache de lojas populado.");
    }

    return lojas;
}
        
        // Método auxiliar para processar anexos
        private async Task ProcessarAnexos(List<IFormFile> anexos, string usuarioId, int? ordemServicoId = null, int? mensagemId = null)
        {
            // Tipos de arquivo permitidos (imagens, vídeos e pdf)
            var tiposPermitidos = new[] { 
                "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp", "image/bmp",
                "video/mp4", "video/webm", "video/quicktime",
                "application/pdf"
            };
            const long tamanhoMaximo = 20 * 1024 * 1024; // 20MB
            
            foreach (var arquivo in anexos)
            {
                if (arquivo.Length > 0 && arquivo.Length <= 20 * 1024 * 1024)
                {
                    // Aloca o buffer uma única vez para o tamanho do arquivo
                    byte[] dados;
                    using (var stream = arquivo.OpenReadStream())
                    {
                        dados = new byte[arquivo.Length];
                        await stream.ReadAsync(dados, 0, (int)arquivo.Length);
                    }
                    
                    var anexo = new Anexo
                    {
                        NomeArquivo = arquivo.FileName,
                        TipoMime = arquivo.ContentType,
                        TamanhoBytes = arquivo.Length,
                        DadosArquivo = dados,
                        UsuarioId = usuarioId,
                        OrdemServicoId = ordemServicoId,
                        MensagemId = mensagemId
                    };
                    
                    _context.Anexos.Add(anexo);
                }
            }
            
            await _context.SaveChangesAsync();
        }

        /// <summary>
/// Obtém estatísticas de performance das ordens de serviço
/// Disponível apenas para administradores
/// Otimizado: executa uma única query ao banco para todas as estatísticas
/// </summary>
/// <returns>JSON com estatísticas das ordens abertas</returns>
[HttpGet]
[Authorize(Roles = "Admin")] // Apenas administradores podem acessar
public async Task<IActionResult> GetEstatisticas()
{
    try
    {
        // Query única que calcula todas as estatísticas de uma vez
        var stats = await _context.OrdensServico
            .AsNoTracking()
            .GroupBy(_ => 1) // Agrupa tudo em um único grupo
            .Select(g => new
            {
                TotalOrdensAbertas = g.Count(o => o.Status != StatusEnum.Concluida),
                OrdensConcluidas = g.Count(o => o.Status == StatusEnum.Concluida),
                StatusAberta = g.Count(o => o.Status == StatusEnum.Aberta),
                StatusEmAndamento = g.Count(o => o.Status == StatusEnum.EmAndamento),
                PrioridadeAlta = g.Count(o => o.Status != StatusEnum.Concluida && o.Prioridade == PrioridadeEnum.Alta),
                PrioridadeMedia = g.Count(o => o.Status != StatusEnum.Concluida && o.Prioridade == PrioridadeEnum.Media),
                PrioridadeBaixa = g.Count(o => o.Status != StatusEnum.Concluida && o.Prioridade == PrioridadeEnum.Baixa)
            })
            .FirstOrDefaultAsync();

        var estatisticas = new
        {
            TotalOrdensAbertas = stats?.TotalOrdensAbertas ?? 0,
            OrdensPorStatus = new[]
            {
                new { Status = "Aberta", Quantidade = stats?.StatusAberta ?? 0 },
                new { Status = "EmAndamento", Quantidade = stats?.StatusEmAndamento ?? 0 }
            },
            OrdensPorPrioridade = new[]
            {
                new { Prioridade = "Alta", Quantidade = stats?.PrioridadeAlta ?? 0 },
                new { Prioridade = "Media", Quantidade = stats?.PrioridadeMedia ?? 0 },
                new { Prioridade = "Baixa", Quantidade = stats?.PrioridadeBaixa ?? 0 }
            },
            OrdensConcluidas = stats?.OrdensConcluidas ?? 0,
            UltimaAtualizacao = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
        };

        return Json(estatisticas);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Erro ao obter estatísticas das ordens de serviço");
        return StatusCode(500, new { erro = "Erro interno do servidor" });
    }
}

        /// <summary>
        /// Endpoint para limpar todas as ordens de serviço do sistema
        /// Apenas administradores podem executar esta ação
        /// Remove todas as ordens, mensagens e anexos relacionados
        /// </summary>
        /// <returns>Resultado da operação de limpeza</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LimparTodasOrdens(string? senha)
        {
            try
            {
                // Verifica a senha de confirmação
                if (string.IsNullOrEmpty(senha) || senha != "4rfv-bgt5-6yhn-mju7")
                {
                    _logger.LogWarning($"Tentativa de limpeza falhou: Senha incorreta. Usuário: {User.Identity?.Name}");
                    return Json(new { 
                        success = false, 
                        message = "Senha de confirmação incorreta. A operação foi cancelada."
                    });
                }

                // Cria uma instância da classe de limpeza
                var limpeza = new ExecutarLimpeza(_context);
                
                // Executa a limpeza completa com reset de contadores
                int totalRemovidos = await limpeza.LimparEResetarContadoresAsync();
                
                // Log da operação para auditoria
                _logger.LogWarning($"Limpeza completa executada por {User.Identity?.Name}. Total de registros removidos: {totalRemovidos}");
                
                // Envia notificação SignalR para todos os usuários sobre a limpeza
                await _hubContext.Clients.All.SendAsync("SystemCleaned", new {
                    message = "Todas as ordens de serviço foram removidas do sistema",
                    totalRemovidos = totalRemovidos,
                    executadoPor = User.Identity?.Name,
                    dataExecucao = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
                });
                
                return Json(new { 
                    success = true, 
                    message = $"Limpeza concluída com sucesso! {totalRemovidos} registros foram removidos.",
                    totalRemovidos = totalRemovidos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro durante a limpeza das ordens de serviço executada por {User.Identity?.Name}");
                return Json(new { 
                    success = false, 
                    message = $"Erro durante a limpeza: {ex.Message}"
                });
            }
        }
    }
}