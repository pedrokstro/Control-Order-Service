using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OrdemServicoMVC.Data;
using OrdemServicoMVC.Models;
using OrdemServicoMVC.Hubs;
using OrdemServicoMVC.ViewModels;

namespace OrdemServicoMVC.Controllers
{
    /// <summary>
    /// Controller responsável pelo gerenciamento de mensagens e chat das ordens de serviço
    /// Permite visualizar conversas e enviar mensagens entre usuários e técnicos
    /// </summary>
    [Authorize] // Exige que o usuário esteja autenticado para acessar este controller
    [Route("[controller]")] // Define o prefixo de rota com o nome do controller (Mensagem), resultando em /Mensagem/...
    public class MensagemController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<ChatHub> _hubContext; // Contexto do SignalR para envio de notificações

        public MensagemController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IHubContext<ChatHub> hubContext)
        {
            _context = context; // Injeta o contexto do banco de dados
            _userManager = userManager; // Injeta o gerenciador de usuários
            _hubContext = hubContext; // Injeta o contexto do SignalR
        }

        /// <summary>
        /// Exibe a interface de chat para uma ordem de serviço específica
        /// Carrega todas as mensagens, marca mensagens como lidas e verifica permissões
        /// </summary>
        /// <param name="id">ID da ordem de serviço</param>
        /// <returns>View do chat com mensagens da ordem de serviço</returns>
        // GET: Mensagem/Chat/5
        [HttpGet("Chat/{id:int}")]
        public async Task<IActionResult> Chat(int id)
        {
            var ordemServico = await _context.OrdensServico
                .Include(o => o.UsuarioCriador)
                .Include(o => o.TecnicoResponsavel)
                .Include(o => o.Mensagens)
                    .ThenInclude(m => m.Usuario)
                .Include(o => o.Mensagens)
                    .ThenInclude(m => m.Anexos)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (ordemServico == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            // Verificar se o usuário tem permissão para ver o chat
            if (!isAdmin && ordemServico.UsuarioCriadorId != currentUser?.Id && ordemServico.TecnicoResponsavelId != currentUser?.Id)
            {
                return Forbid();
            }

            // Marcar mensagens como lidas
            var mensagensNaoLidas = ordemServico.Mensagens
                .Where(m => !m.Lida && m.UsuarioId != currentUser?.Id)
                .ToList();

            foreach (var mensagem in mensagensNaoLidas)
            {
                mensagem.Lida = true;
            }

            if (mensagensNaoLidas.Any())
            {
                await _context.SaveChangesAsync();
            }

            return View(ordemServico);
        }

        /// <summary>
        /// Processa o envio de uma nova mensagem no chat da ordem de serviço
        /// Valida permissões, cria a mensagem e processa anexos se houver
        /// </summary>
        /// <param name="ordemServicoId">ID da ordem de serviço</param>
        /// <param name="conteudo">Texto da mensagem</param>
        /// <param name="anexos">Lista de arquivos anexados (opcional)</param>
        /// <returns>Redirecionamento para o chat</returns>
        // POST: Mensagem/EnviarMensagem
        [HttpPost("EnviarMensagem")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarMensagem(int ordemServicoId, string conteudo, List<IFormFile> anexos)
        {
            // Validar se há conteúdo de texto ou anexos (pelo menos um é obrigatório)
            if (string.IsNullOrWhiteSpace(conteudo) && (anexos == null || !anexos.Any()))
            {
                TempData["ErrorMessage"] = "Digite uma mensagem ou anexe uma imagem.";
                return RedirectToAction("Chat", new { id = ordemServicoId });
            }

            // Verificar se a ordem de serviço existe
            var ordemServico = await _context.OrdensServico.FindAsync(ordemServicoId);
            if (ordemServico == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            // Verificar se o usuário tem permissão para enviar mensagem nesta ordem
            if (!isAdmin && ordemServico.UsuarioCriadorId != currentUser?.Id && ordemServico.TecnicoResponsavelId != currentUser?.Id)
            {
                return Forbid();
            }

            // Criar nova mensagem
            var mensagem = new Mensagem
            {
                OrdemServicoId = ordemServicoId,
                UsuarioId = currentUser!.Id,
                Conteudo = string.IsNullOrWhiteSpace(conteudo) ? "" : conteudo.Trim(),
                // Define data de envio com horário brasileiro (UTC-3)
                DataEnvio = DateTime.UtcNow.AddHours(-3),
                Lida = false // Mensagem inicia como não lida
            };

            // Salvar mensagem no banco de dados
            _context.Mensagens.Add(mensagem);
            await _context.SaveChangesAsync();

            // Processar anexos se houver arquivos enviados
            if (anexos != null && anexos.Any())
            {
                await ProcessarAnexosMensagem(anexos, currentUser.Id, mensagem.Id);
            }

            // Enviar notificação em tempo real via SignalR para todos os usuários conectados
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", 
                currentUser.UserName ?? "Usuário", 
                mensagem.Conteudo, 
                mensagem.DataEnvio.ToString("dd/MM/yyyy às HH:mm"),
                ordemServicoId);

            // Notificar sobre mensagens não lidas para usuários específicos
            await NotificarMensagensNaoLidas(ordemServicoId, currentUser.Id);

            TempData["SuccessMessage"] = "Mensagem enviada com sucesso!";
            return RedirectToAction("Chat", new { id = ordemServicoId });
        }

        // GET: Mensagem/ContarNaoLidas/{id:int}
        [HttpGet("ContarNaoLidas/{id:int}")]
        public async Task<IActionResult> ContarNaoLidas(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            var count = await _context.Mensagens
                .Where(m => m.OrdemServicoId == id && !m.Lida && m.UsuarioId != currentUser!.Id)
                .CountAsync();

            return Json(new { count });
        }

        /// <summary>
        /// Conta o total de mensagens não lidas do usuário atual em todas as ordens de serviço
        /// Usado para exibir o badge de notificação global no layout
        /// </summary>
        /// <returns>JSON com o total de mensagens não lidas</returns>
        // GET: Mensagem/ContarNaoLidas
        [HttpGet("ContarNaoLidas")]
        public async Task<IActionResult> ContarNaoLidas()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            
            // Busca todas as ordens de serviço que o usuário tem acesso
            var ordensAcessiveis = _context.OrdensServico
                .AsNoTracking()
                .AsQueryable();
            
            if (!isAdmin)
            {
                // Se não for admin, só pode ver ordens que criou ou que é técnico responsável
                ordensAcessiveis = ordensAcessiveis.Where(o => 
                    o.UsuarioCriadorId == currentUser!.Id || 
                    o.TecnicoResponsavelId == currentUser!.Id);
            }
            
            // Conta mensagens não lidas nas ordens acessíveis (exceto as próprias mensagens)
            var ordensIdsQuery = ordensAcessiveis.Select(o => o.Id);

            var count = await _context.Mensagens
                .AsNoTracking()
                .Where(m => ordensIdsQuery.Contains(m.OrdemServicoId) && 
                           !m.Lida && 
                           m.UsuarioId != currentUser!.Id)
                .CountAsync();

            return Json(new { count });
        }
        
        /// <summary>
        /// Retorna uma lista resumida das ordens com mensagens não lidas para o usuário atual
        /// Usado pelo Floating Action Button para facilitar o acesso rápido ao chat
        /// </summary>
        /// <returns>JSON com as ordens e a quantidade de mensagens não lidas</returns>
        [HttpGet("OrdensNaoLidas")]
        public async Task<IActionResult> OrdensNaoLidas()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userId = currentUser?.Id;

            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { ordens = Array.Empty<object>() });
            }

            var isAdmin = User.IsInRole("Admin");

            var ordensQuery = _context.OrdensServico
                .AsNoTracking()
                .AsQueryable();

            if (!isAdmin)
            {
                ordensQuery = ordensQuery.Where(o =>
                    o.UsuarioCriadorId == userId ||
                    o.TecnicoResponsavelId == userId);
            }

            var ordens = await ordensQuery
                .Select(o => new
                {
                    o.Id,
                    o.Titulo,
                    Loja = o.UsuarioCriador != null ? o.UsuarioCriador.NomeLoja : "N/D",
                    Tecnico = o.TecnicoResponsavel != null ? o.TecnicoResponsavel.NomeLoja : "Não atribuído",
                    Unread = o.Mensagens
                        .Where(m => !m.Lida && m.UsuarioId != userId)
                        .Count()
                })
                .Where(o => o.Unread > 0)
                .OrderByDescending(o => o.Unread)
                .ThenBy(o => o.Titulo)
                .Take(8)
                .ToListAsync();

            return Json(new { ordens });
        }

        [HttpGet("Painel")]
        public async Task<IActionResult> Painel()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = currentUser.Id;
            var isAdmin = User.IsInRole("Admin");

            var ordensQuery = _context.OrdensServico
                .AsNoTracking()
                .AsQueryable();

            if (!isAdmin)
            {
                ordensQuery = ordensQuery.Where(o =>
                    o.UsuarioCriadorId == userId || o.TecnicoResponsavelId == userId);
            }

            var conversas = await ordensQuery
                .Select(o => new PainelMensagemItemViewModel
                {
                    OrdemId = o.Id,
                    Titulo = o.Titulo ?? "(Sem título)",
                    Loja = o.UsuarioCriador != null ? o.UsuarioCriador.NomeLoja : "N/D",
                    Tecnico = o.TecnicoResponsavel != null ? o.TecnicoResponsavel.UserName : "Não atribuído",
                    Prioridade = o.Prioridade.ToString(),
                    Status = o.Status.ToString(),
                    UltimaMensagem = o.Mensagens
                        .OrderByDescending(m => m.DataEnvio)
                        .Select(m => m.DataEnvio)
                        .FirstOrDefault(),
                    UltimoAutor = o.Mensagens
                        .OrderByDescending(m => m.DataEnvio)
                        .Select(m => m.Usuario.NomeLoja ?? m.Usuario.UserName)
                        .FirstOrDefault() ?? "-",
                    UltimaMensagemPreview = o.Mensagens
                        .OrderByDescending(m => m.DataEnvio)
                        .Select(m => string.IsNullOrEmpty(m.Conteudo) ? "[Anexo]" : m.Conteudo)
                        .FirstOrDefault() ?? "Sem mensagens",
                    NaoLidas = o.Mensagens
                        .Count(m => !m.Lida && m.UsuarioId != userId)
                })
                .OrderByDescending(c => c.NaoLidas)
                .ThenByDescending(c => c.UltimaMensagem)
                .ToListAsync();

            var viewModel = new PainelMensagensViewModel
            {
                Conversas = conversas
            };

            return View(viewModel);
        }

        /// <summary>
        /// Método auxiliar para processar e salvar anexos de uma mensagem
        /// Valida tipo e tamanho dos arquivos antes de salvar no banco
        /// </summary>
        /// <param name="anexos">Lista de arquivos a serem processados</param>
        /// <param name="usuarioId">ID do usuário que está enviando os anexos</param>
        /// <param name="mensagemId">ID da mensagem à qual os anexos serão associados</param>
        private async Task ProcessarAnexosMensagem(List<IFormFile> anexos, string usuarioId, int mensagemId)
        {
            // Define tipos de arquivo permitidos (imagens, vídeos e pdf)
            var tiposPermitidos = new[] { 
                "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp", "image/bmp",
                "video/mp4", "video/webm", "video/quicktime",
                "application/pdf"
            };
            const long tamanhoMaximo = 20 * 1024 * 1024; // Limite de 20MB por arquivo
            
            // Processa cada arquivo enviado
            foreach (var arquivo in anexos)
            {
                if (arquivo != null && arquivo.Length > 0)
                {
                    // Validar se o tipo de arquivo é permitido
                    if (!tiposPermitidos.Contains(arquivo.ContentType.ToLower()))
                    {
                        continue; // Pula arquivos com tipo não permitido
                    }
                    
                    // Validar se o tamanho está dentro do limite
                    if (arquivo.Length > tamanhoMaximo)
                    {
                        continue; // Pula arquivos muito grandes
                    }
                    
                    // Converter arquivo para array de bytes e criar anexo
                    using var memoryStream = new MemoryStream();
                    await arquivo.CopyToAsync(memoryStream);
                    
                    var anexo = new Anexo
                    {
                        NomeArquivo = arquivo.FileName,
                        TipoMime = arquivo.ContentType,
                        TamanhoBytes = arquivo.Length,
                        DadosArquivo = memoryStream.ToArray(), // Converte para bytes
                        UsuarioId = usuarioId,
                        MensagemId = mensagemId
                    };
                    
                    _context.Anexos.Add(anexo);
                }
            }
            
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Método auxiliar para notificar usuários sobre mensagens não lidas via SignalR
        /// Calcula o número de mensagens não lidas e envia notificação em tempo real
        /// </summary>
        /// <param name="ordemServicoId">ID da ordem de serviço</param>
        /// <param name="remetenteId">ID do usuário que enviou a mensagem</param>
        private async Task NotificarMensagensNaoLidas(int ordemServicoId, string remetenteId)
        {
            // Busca a ordem de serviço com os usuários relacionados (sem tracking)
            var ordemServico = await _context.OrdensServico
                .AsNoTracking()
                .Select(o => new { o.Id, o.UsuarioCriadorId, o.TecnicoResponsavelId })
                .FirstOrDefaultAsync(o => o.Id == ordemServicoId);

            if (ordemServico == null) return;

            // Lista de usuários que devem receber notificação (exceto o remetente)
            var usuariosParaNotificar = new List<string>();
            
            if (ordemServico.UsuarioCriadorId != remetenteId)
                usuariosParaNotificar.Add(ordemServico.UsuarioCriadorId);
                
            if (!string.IsNullOrEmpty(ordemServico.TecnicoResponsavelId) && 
                ordemServico.TecnicoResponsavelId != remetenteId)
                usuariosParaNotificar.Add(ordemServico.TecnicoResponsavelId);

            if (!usuariosParaNotificar.Any()) return;

            // Busca contagem de não lidas para esta ordem para cada usuário em uma única query
            var countPorOrdem = await _context.Mensagens
                .AsNoTracking()
                .Where(m => m.OrdemServicoId == ordemServicoId && !m.Lida)
                .GroupBy(m => m.UsuarioId)
                .Select(g => new { UsuarioId = g.Key, Count = g.Count() })
                .ToListAsync();

            // Total de mensagens não lidas nesta ordem (excluindo cada usuário)
            var totalNaoLidas = await _context.Mensagens
                .AsNoTracking()
                .Where(m => m.OrdemServicoId == ordemServicoId && !m.Lida)
                .CountAsync();

            // Busca contagem global para cada usuário em paralelo
            var notificacoes = new List<Task>();
            foreach (var usuarioId in usuariosParaNotificar)
            {
                var countLocal = totalNaoLidas - (countPorOrdem.FirstOrDefault(c => c.UsuarioId == usuarioId)?.Count ?? 0);

                notificacoes.Add(
                    _hubContext.Clients.Group($"User_{usuarioId}")
                        .SendAsync("UnreadMessagesCount", new { ordemServicoId, count = countLocal })
                );

                // Contador global — usa subquery para performance
                var countGlobalTask = _context.Mensagens
                    .AsNoTracking()
                    .Where(m => _context.OrdensServico
                        .Where(o => o.UsuarioCriadorId == usuarioId || o.TecnicoResponsavelId == usuarioId)
                        .Any(o => o.Id == m.OrdemServicoId) && 
                        !m.Lida && 
                        m.UsuarioId != usuarioId)
                    .CountAsync();

                notificacoes.Add(countGlobalTask.ContinueWith(t =>
                    _hubContext.Clients.Group($"User_{usuarioId}")
                        .SendAsync("GlobalUnreadMessagesCount", new { count = t.Result })
                ).Unwrap());
            }

            await Task.WhenAll(notificacoes);
        }

        /// <summary>
        /// Processa o envio de uma nova mensagem via AJAX (com ou sem anexos)
        /// Valida permissões, cria a mensagem, processa anexos e retorna JSON
        /// </summary>
        /// <summary>
        /// Método de teste para envio de mensagens sem autenticação (APENAS PARA DESENVOLVIMENTO)
        /// </summary>
        [HttpPost("EnviarMensagemAjaxTeste")]
        [AllowAnonymous] // Permite acesso sem autenticação para teste
        public async Task<IActionResult> EnviarMensagemAjaxTeste(int ordemServicoId, string conteudo, List<IFormFile> anexos)
        {
            try
            {
                // Validar se há conteúdo de texto ou anexos (pelo menos um é obrigatório)
                if (string.IsNullOrWhiteSpace(conteudo) && (anexos == null || !anexos.Any()))
                {
                    return Json(new { success = false, message = "Digite uma mensagem ou anexe uma imagem." });
                }

                // Retornar sucesso simulado para teste
                return Json(new 
                { 
                    success = true, 
                    message = "Mensagem de teste enviada com sucesso!",
                    mensagem = new
                    {
                        id = 999,
                        conteudo = conteudo,
                        dataEnvio = DateTime.Now.ToString("dd/MM/yyyy às HH:mm"),
                        usuario = new
                        {
                            id = "test-user",
                            userName = "UsuarioTeste",
                            nomeLoja = "Loja Teste"
                        },
                        anexos = new List<object>()
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erro interno do servidor. Tente novamente." });
            }
        }

        /// <param name="ordemServicoId">ID da ordem de serviço</param>
        /// <param name="conteudo">Texto da mensagem</param>
        /// <param name="anexos">Lista de arquivos anexados (opcional)</param>
        /// <returns>JSON com sucesso/erro e dados da mensagem</returns>
        [HttpPost("EnviarMensagemAjax")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarMensagemAjax(int ordemServicoId, string conteudo, List<IFormFile> anexos)
        {
            try
            {
                // Validar se há conteúdo de texto ou anexos (pelo menos um é obrigatório)
                if (string.IsNullOrWhiteSpace(conteudo) && (anexos == null || !anexos.Any()))
                {
                    return Json(new { success = false, message = "Digite uma mensagem ou anexe uma imagem." });
                }

                // Verificar se a ordem de serviço existe
                var ordemServico = await _context.OrdensServico.FindAsync(ordemServicoId);
                if (ordemServico == null)
                {
                    return Json(new { success = false, message = "Ordem de serviço não encontrada." });
                }

                var currentUser = await _userManager.GetUserAsync(User);
                var isAdmin = User.IsInRole("Admin");

                // Verificar se o usuário tem permissão para enviar mensagem nesta ordem
                if (!isAdmin && ordemServico.UsuarioCriadorId != currentUser?.Id && ordemServico.TecnicoResponsavelId != currentUser?.Id)
                {
                    return Json(new { success = false, message = "Você não tem permissão para enviar mensagens nesta ordem." });
                }

                // Criar nova mensagem
                var mensagem = new Mensagem
                {
                    OrdemServicoId = ordemServicoId,
                    UsuarioId = currentUser!.Id,
                    Conteudo = string.IsNullOrWhiteSpace(conteudo) ? "" : conteudo.Trim(),
                    // Define data de envio com horário brasileiro (UTC-3)
                    DataEnvio = DateTime.UtcNow.AddHours(-3),
                    Lida = false // Mensagem inicia como não lida
                };

                // Salvar mensagem no banco de dados
                _context.Mensagens.Add(mensagem);
                await _context.SaveChangesAsync();

                // Processar anexos se houver arquivos enviados
                if (anexos != null && anexos.Any())
                {
                    await ProcessarAnexosMensagem(anexos, currentUser.Id, mensagem.Id);
                }

                // Carregar dados do usuário e anexos para retornar na resposta
                await _context.Entry(mensagem)
                    .Reference(m => m.Usuario)
                    .LoadAsync();
                    
                await _context.Entry(mensagem)
                    .Collection(m => m.Anexos)
                    .LoadAsync();

                // Enviar notificação em tempo real via SignalR para o grupo específico da ordem de serviço
                await _hubContext.Clients.Group($"OrdemServico_{ordemServicoId}")
                    .SendAsync("ReceberMensagem", new
                    {
                        id = mensagem.Id,
                        conteudo = mensagem.Conteudo,
                        dataEnvio = mensagem.DataEnvio,
                        ordemServicoId = ordemServicoId,
                        usuario = new
                        {
                            id = mensagem.Usuario.Id,
                            userName = mensagem.Usuario.UserName,
                            nomeLoja = mensagem.Usuario.NomeLoja
                        },
                        anexos = mensagem.Anexos?.Select(a => new
                        {
                            id = a.Id,
                            nomeArquivo = a.NomeArquivo,
                            tipoArquivo = a.TipoMime,
                            isImagem = a.TipoMime?.StartsWith("image/") == true
                        }).ToList()
                    });

                // Notificar sobre mensagens não lidas para usuários específicos
                await NotificarMensagensNaoLidas(ordemServicoId, currentUser.Id);

                // Retornar dados da mensagem para atualizar a interface
                return Json(new 
                { 
                    success = true, 
                    message = "Mensagem enviada com sucesso!",
                    mensagem = new
                    {
                        id = mensagem.Id,
                        conteudo = mensagem.Conteudo,
                        dataEnvio = mensagem.DataEnvio.ToString("dd/MM/yyyy às HH:mm"),
                        usuario = new
                        {
                            id = mensagem.Usuario.Id,
                            userName = mensagem.Usuario.UserName,
                            nomeLoja = mensagem.Usuario.NomeLoja
                        },
                        anexos = mensagem.Anexos?.Select(a => new
                        {
                            id = a.Id,
                            nomeArquivo = a.NomeArquivo,
                            tipoArquivo = a.TipoMime,
                            isImagem = a.TipoMime?.StartsWith("image/") == true
                        }).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                // Log do erro (se necessário)
                return Json(new { success = false, message = "Erro interno do servidor. Tente novamente." });
            }
        }
    }
}