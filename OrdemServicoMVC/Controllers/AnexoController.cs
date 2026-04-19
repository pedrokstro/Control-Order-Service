using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdemServicoMVC.Data;
using OrdemServicoMVC.Models;

namespace OrdemServicoMVC.Controllers
{
    [Authorize]
    public class AnexoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AnexoController> _logger;
        
        // Tipos de arquivo permitidos (imagens, vídeos e pdf)
        private readonly string[] _tiposPermitidos = { 
            "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp", "image/bmp",
            "video/mp4", "video/webm", "video/quicktime",
            "application/pdf"
        };
        
        // Tamanho máximo do arquivo (20MB)
        private const long _tamanhoMaximo = 20 * 1024 * 1024;

        public AnexoController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<AnexoController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Upload de anexo para ordem de serviço
        [HttpPost]
        public async Task<IActionResult> UploadOrdemServico(int ordemServicoId, IFormFile arquivo)
        {
            try
            {
                if (arquivo == null || arquivo.Length == 0)
                {
                    return Json(new { sucesso = false, mensagem = "Nenhum arquivo foi selecionado." });
                }

                // Verificar se o usuário tem permissão para acessar a ordem
                var ordem = await _context.OrdensServico
                    .FirstOrDefaultAsync(o => o.Id == ordemServicoId);

                if (ordem == null)
                {
                    return Json(new { sucesso = false, mensagem = "Ordem de serviço não encontrada." });
                }

                var usuarioAtual = await _userManager.GetUserAsync(User);
                var isAdmin = User.IsInRole("Admin");

                // Verificar permissões
                if (!isAdmin && ordem.UsuarioCriadorId != usuarioAtual.Id && ordem.TecnicoResponsavelId != usuarioAtual.Id)
                {
                    return Json(new { sucesso = false, mensagem = "Você não tem permissão para anexar arquivos nesta ordem." });
                }

                // Validar arquivo
                var resultadoValidacao = ValidarArquivo(arquivo);
                if (!resultadoValidacao.valido)
                {
                    return Json(new { sucesso = false, mensagem = resultadoValidacao.mensagem });
                }

                // Criar anexo
                var anexo = await CriarAnexo(arquivo, usuarioAtual.Id, ordemServicoId: ordemServicoId);
                
                return Json(new { 
                    sucesso = true, 
                    mensagem = "Arquivo anexado com sucesso!",
                    anexoId = anexo.Id,
                    nomeArquivo = anexo.NomeArquivo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer upload de anexo para ordem de serviço {OrdemServicoId}", ordemServicoId);
                return Json(new { sucesso = false, mensagem = "Erro interno do servidor." });
            }
        }

        // Upload de anexo para mensagem
        [HttpPost]
        public async Task<IActionResult> UploadMensagem(int mensagemId, IFormFile arquivo)
        {
            try
            {
                if (arquivo == null || arquivo.Length == 0)
                {
                    return Json(new { sucesso = false, mensagem = "Nenhum arquivo foi selecionado." });
                }

                // Verificar se o usuário tem permissão para acessar a mensagem
                var mensagem = await _context.Mensagens
                    .Include(m => m.OrdemServico)
                    .FirstOrDefaultAsync(m => m.Id == mensagemId);

                if (mensagem == null)
                {
                    return Json(new { sucesso = false, mensagem = "Mensagem não encontrada." });
                }

                var usuarioAtual = await _userManager.GetUserAsync(User);
                var isAdmin = User.IsInRole("Admin");

                // Verificar permissões
                if (!isAdmin && 
                    mensagem.OrdemServico.UsuarioCriadorId != usuarioAtual.Id && 
                    mensagem.OrdemServico.TecnicoResponsavelId != usuarioAtual.Id)
                {
                    return Json(new { sucesso = false, mensagem = "Você não tem permissão para anexar arquivos nesta conversa." });
                }

                // Validar arquivo
                var resultadoValidacao = ValidarArquivo(arquivo);
                if (!resultadoValidacao.valido)
                {
                    return Json(new { sucesso = false, mensagem = resultadoValidacao.mensagem });
                }

                // Criar anexo
                var anexo = await CriarAnexo(arquivo, usuarioAtual.Id, mensagemId: mensagemId);
                
                return Json(new { 
                    sucesso = true, 
                    mensagem = "Arquivo anexado com sucesso!",
                    anexoId = anexo.Id,
                    nomeArquivo = anexo.NomeArquivo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer upload de anexo para mensagem {MensagemId}", mensagemId);
                return Json(new { sucesso = false, mensagem = "Erro interno do servidor." });
            }
        }

        // Download de anexo
        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            try
            {
                var anexo = await _context.Anexos
                    .Include(a => a.OrdemServico)
                    .Include(a => a.Mensagem)
                        .ThenInclude(m => m.OrdemServico)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (anexo == null)
                {
                    return NotFound("Anexo não encontrado.");
                }

                var usuarioAtual = await _userManager.GetUserAsync(User);
                var isAdmin = User.IsInRole("Admin");

                // Verificar permissões
                OrdemServico? ordem = anexo.OrdemServico ?? anexo.Mensagem?.OrdemServico;
                if (ordem == null)
                {
                    return NotFound("Ordem de serviço não encontrada.");
                }

                if (!isAdmin && 
                    ordem.UsuarioCriadorId != usuarioAtual.Id && 
                    ordem.TecnicoResponsavelId != usuarioAtual.Id)
                {
                    return Forbid("Você não tem permissão para acessar este anexo.");
                }

                return File(anexo.DadosArquivo, anexo.TipoMime, anexo.NomeArquivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer download do anexo {AnexoId}", id);
                return StatusCode(500, "Erro interno do servidor.");
            }
        }

        // Excluir anexo
        [HttpPost]
        public async Task<IActionResult> Excluir(int id)
        {
            try
            {
                var anexo = await _context.Anexos
                    .Include(a => a.OrdemServico)
                    .Include(a => a.Mensagem)
                        .ThenInclude(m => m.OrdemServico)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (anexo == null)
                {
                    return Json(new { sucesso = false, mensagem = "Anexo não encontrado." });
                }

                var usuarioAtual = await _userManager.GetUserAsync(User);
                var isAdmin = User.IsInRole("Admin");

                // Verificar permissões (apenas o usuário que fez upload ou admin pode excluir)
                if (!isAdmin && anexo.UsuarioId != usuarioAtual.Id)
                {
                    return Json(new { sucesso = false, mensagem = "Você não tem permissão para excluir este anexo." });
                }

                _context.Anexos.Remove(anexo);
                await _context.SaveChangesAsync();

                return Json(new { sucesso = true, mensagem = "Anexo excluído com sucesso!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir anexo {AnexoId}", id);
                return Json(new { sucesso = false, mensagem = "Erro interno do servidor." });
            }
        }

        // Métodos auxiliares
        
        /// <summary>
        /// Valida se o arquivo enviado atende aos critérios de segurança e formato
        /// Verifica tamanho máximo (5MB) e tipos de arquivo permitidos (apenas imagens)
        /// </summary>
        /// <param name="arquivo">Arquivo a ser validado</param>
        /// <returns>Tupla indicando se é válido e mensagem de erro se aplicável</returns>
        private (bool valido, string mensagem) ValidarArquivo(IFormFile arquivo)
        {
            // Verificar tamanho máximo permitido
            if (arquivo.Length > _tamanhoMaximo)
            {
                return (false, $"O arquivo deve ter no máximo {_tamanhoMaximo / (1024 * 1024)}MB.");
            }

            // Verificar se o tipo de arquivo está na lista de tipos permitidos
            if (!_tiposPermitidos.Contains(arquivo.ContentType.ToLower()))
            {
                return (false, "Tipo de arquivo não permitido. Apenas imagens, vídeos (MP4/WebM) e PDF são aceitos.");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Cria um novo anexo no banco de dados a partir do arquivo enviado
        /// Converte o arquivo para array de bytes e associa à ordem de serviço ou mensagem
        /// </summary>
        /// <param name="arquivo">Arquivo a ser salvo</param>
        /// <param name="usuarioId">ID do usuário que está fazendo o upload</param>
        /// <param name="ordemServicoId">ID da ordem de serviço (opcional)</param>
        /// <param name="mensagemId">ID da mensagem (opcional)</param>
        /// <returns>Objeto Anexo criado e salvo no banco</returns>
        private async Task<Anexo> CriarAnexo(IFormFile arquivo, string usuarioId, int? ordemServicoId = null, int? mensagemId = null)
        {
            // Converte o arquivo para array de bytes usando MemoryStream
            using var memoryStream = new MemoryStream();
            await arquivo.CopyToAsync(memoryStream);

            // Cria o objeto Anexo com os dados do arquivo
            var anexo = new Anexo
            {
                NomeArquivo = arquivo.FileName,
                TipoMime = arquivo.ContentType,
                TamanhoBytes = arquivo.Length,
                DadosArquivo = memoryStream.ToArray(), // Converte para bytes
                UsuarioId = usuarioId,
                OrdemServicoId = ordemServicoId,
                MensagemId = mensagemId
            };

            // Salva o anexo no banco de dados
            _context.Anexos.Add(anexo);
            await _context.SaveChangesAsync();

            return anexo;
        }
    }
}