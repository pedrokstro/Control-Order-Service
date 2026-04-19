using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OrdemServicoMVC.Data;
using OrdemServicoMVC.Models;
using OrdemServicoMVC.ViewModels;
using OrdemServicoMVC.Services; // Para o serviço de cache
using OfficeOpenXml;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Text;

namespace OrdemServicoMVC.Controllers
{
    /// <summary>
    /// Controller responsável pela geração de relatórios de ordens de serviço
    /// Inclui funcionalidades de filtragem, exportação e cache para otimização
    /// </summary>
    [Authorize]
    public class RelatorioController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICacheService _cacheService; // Serviço de cache para otimização

        public RelatorioController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ICacheService cacheService) // Injeta o serviço de cache
        {
            _context = context;
            _userManager = userManager;
            _cacheService = cacheService; // Inicializa o serviço de cache
        }

        /// <summary>
        /// Exibe a página de filtros para geração de relatórios
        /// </summary>
        /// <returns>View com formulário de filtros</returns>
        // GET: Relatorio
        public async Task<IActionResult> Index()
        {
            var viewModel = new RelatorioFiltroViewModel();
            
            // Popula as listas para os dropdowns
            await PopularListasDropdown(viewModel);
            
            return View(viewModel);
        }

        /// <summary>
        /// Gera o relatório com base nos filtros aplicados
        /// </summary>
        /// <param name="filtros">Filtros aplicados pelo usuário</param>
        /// <returns>View com dados do relatório ou arquivo para download</returns>
        [HttpPost]
        public async Task<IActionResult> Gerar(RelatorioFiltroViewModel filtros)
        {
            if (!ModelState.IsValid)
            {
                await PopularListasDropdown(filtros);
                return View("Index", filtros);
            }

            // Valida se data inicial não é maior que data final
            if (filtros.DataInicio.HasValue && filtros.DataFim.HasValue && 
                filtros.DataInicio > filtros.DataFim)
            {
                ModelState.AddModelError("", "A data inicial não pode ser maior que a data final.");
                await PopularListasDropdown(filtros);
                return View("Index", filtros);
            }

            // Busca as ordens de serviço com base nos filtros
            var ordensQuery = await BuscarOrdensComFiltros(filtros);
            
            // Se for exportação, gera o arquivo
            if (!string.IsNullOrEmpty(filtros.TipoExportacao))
            {
                if (filtros.TipoExportacao.ToLower() == "excel")
                {
                    return await GerarExcel(ordensQuery, filtros);
                }
                else if (filtros.TipoExportacao.ToLower() == "pdf")
                {
                    return await GerarPdf(ordensQuery, filtros);
                }
            }

            // Caso contrário, exibe o relatório na tela
            var resultado = new RelatorioResultadoViewModel
            {
                OrdensServico = ordensQuery,
                Filtros = filtros,
                // Usa cache para estatísticas com chave baseada nos filtros
                Estatisticas = await _cacheService.GetEstatisticasAsync(
                    GerarChaveCache(filtros), 
                    () => CalcularEstatisticas(ordensQuery),
                    TimeSpan.FromMinutes(10)) // Cache por 10 minutos
            };

            await PopularListasDropdown(filtros);
            return View("Resultado", resultado);
        }

        /// <summary>
        /// Busca ordens de serviço aplicando os filtros especificados com otimizações de performance
        /// </summary>
        /// <param name="filtros">Filtros a serem aplicados</param>
        /// <returns>Lista de ordens de serviço filtradas</returns>
        private async Task<List<OrdemServico>> BuscarOrdensComFiltros(RelatorioFiltroViewModel filtros)
        {
            // Monta a query base com projeção otimizada - carrega apenas dados necessários
            IQueryable<OrdemServico> query = _context.OrdensServico
                .Include(o => o.UsuarioCriador)           // Dados do usuário que criou a ordem
                .Include(o => o.TecnicoResponsavel)       // Dados do técnico responsável
                .Include(o => o.Anexos)                   // Anexos da ordem
                .Include(o => o.Mensagens)                // Mensagens da ordem
                .AsNoTracking(); // Otimização: não rastreia mudanças para consultas read-only

            // Aplica filtro por data inicial
            if (filtros.DataInicio.HasValue)
            {
                query = query.Where(o => o.DataCriacao.Date >= filtros.DataInicio.Value.Date);
            }

            // Aplica filtro por data final
            if (filtros.DataFim.HasValue)
            {
                query = query.Where(o => o.DataCriacao.Date <= filtros.DataFim.Value.Date);
            }

            // Aplica filtro por técnico responsável
            if (!string.IsNullOrEmpty(filtros.TecnicoId))
            {
                if (filtros.TecnicoId == "sem_tecnico")
                {
                    query = query.Where(o => o.TecnicoResponsavelId == null);
                }
                else
                {
                    query = query.Where(o => o.TecnicoResponsavelId == filtros.TecnicoId);
                }
            }

            // Aplica filtro por loja
            if (!string.IsNullOrEmpty(filtros.Loja))
            {
                query = query.Where(o => o.UsuarioCriador != null && 
                    o.UsuarioCriador.NomeLoja.Contains(filtros.Loja));
            }

            // Aplica filtro por status
            if (filtros.Status.HasValue)
            {
                query = query.Where(o => (int)o.Status == filtros.Status.Value);
            }

            // Aplica filtro por prioridade
            if (filtros.Prioridade.HasValue)
            {
                query = query.Where(o => (int)o.Prioridade == filtros.Prioridade.Value);
            }

            // Ordena por data de criação (mais recentes primeiro)
            query = query.OrderByDescending(o => o.DataCriacao);

            return await query.ToListAsync();
        }

        /// <summary>
        /// Calcula estatísticas do relatório de forma otimizada
        /// </summary>
        /// <param name="ordens">Lista de ordens para calcular estatísticas</param>
        /// <returns>Objeto com estatísticas calculadas</returns>
        private async Task<RelatorioEstatisticasViewModel> CalcularEstatisticas(List<OrdemServico> ordens)
        {
            // Calcula estatísticas básicas em memória (mais eficiente para listas já carregadas)
            var estatisticas = new RelatorioEstatisticasViewModel
            {
                TotalOrdens = ordens.Count,
                OrdensAbertas = ordens.Count(o => o.Status == StatusEnum.Aberta),
                OrdensEmAndamento = ordens.Count(o => o.Status == StatusEnum.EmAndamento),
                OrdensConcluidas = ordens.Count(o => o.Status == StatusEnum.Concluida),
                OrdensPrioridadeAlta = ordens.Count(o => o.Prioridade == PrioridadeEnum.Alta),
                OrdensPrioridadeMedia = ordens.Count(o => o.Prioridade == PrioridadeEnum.Media),
                OrdensPrioridadeBaixa = ordens.Count(o => o.Prioridade == PrioridadeEnum.Baixa)
            };

            // Calcula tempo médio de conclusão de forma otimizada
            var ordensConcluidas = ordens.Where(o => o.Status == StatusEnum.Concluida && o.DataConclusao.HasValue);
            if (ordensConcluidas.Any())
            {
                // Usa LINQ otimizado para calcular média
                var tempos = ordensConcluidas.Select(o => (o.DataConclusao!.Value - o.DataCriacao).TotalDays);
                estatisticas.TempoMedioConclusao = Math.Round(tempos.Average(), 1);
            }

            // Encontra técnico mais ativo usando GroupBy otimizado
            var tecnicoMaisAtivo = ordens
                .Where(o => o.TecnicoResponsavel != null && o.Status == StatusEnum.Concluida)
                .GroupBy(o => o.TecnicoResponsavel!.UserName)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (tecnicoMaisAtivo != null)
            {
                estatisticas.TecnicoMaisAtivo = tecnicoMaisAtivo.Key;
                estatisticas.OrdensDoTecnicoMaisAtivo = tecnicoMaisAtivo.Count();
            }

            // Encontra loja mais ativa usando GroupBy otimizado
            var lojaMaisAtiva = ordens
                .Where(o => o.UsuarioCriador != null)
                .GroupBy(o => o.UsuarioCriador!.NomeLoja)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (lojaMaisAtiva != null)
            {
                estatisticas.LojaMaisAtiva = lojaMaisAtiva.Key;
                estatisticas.OrdensDaLojaMaisAtiva = lojaMaisAtiva.Count();
            }

            return estatisticas;
        }

        /// <summary>
        /// Gera arquivo Excel com os dados do relatório
        /// </summary>
        /// <param name="ordens">Lista de ordens para exportar</param>
        /// <param name="filtros">Filtros aplicados</param>
        /// <returns>Arquivo Excel para download</returns>
        private async Task<IActionResult> GerarExcel(List<OrdemServico> ordens, RelatorioFiltroViewModel filtros)
        {
            // Configura licença do EPPlus (modo não comercial)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Relatório de Ordens");

            // Cabeçalhos das colunas
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Título";
            worksheet.Cells[1, 3].Value = "Descrição";
            worksheet.Cells[1, 4].Value = "Status";
            worksheet.Cells[1, 5].Value = "Prioridade";
            worksheet.Cells[1, 6].Value = "Data Criação";
            worksheet.Cells[1, 7].Value = "Data Conclusão";
            worksheet.Cells[1, 8].Value = "Usuário Criador";
            worksheet.Cells[1, 9].Value = "Loja";
            worksheet.Cells[1, 10].Value = "Técnico Responsável";
            worksheet.Cells[1, 11].Value = "Observações";

            // Formata cabeçalhos
            using (var range = worksheet.Cells[1, 1, 1, 11])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Preenche dados das ordens
            for (int i = 0; i < ordens.Count; i++)
            {
                var ordem = ordens[i];
                var row = i + 2;

                worksheet.Cells[row, 1].Value = ordem.Id;
                worksheet.Cells[row, 2].Value = ordem.Titulo;
                worksheet.Cells[row, 3].Value = ordem.Descricao;
                worksheet.Cells[row, 4].Value = GetStatusText(ordem.Status);
                worksheet.Cells[row, 5].Value = GetPrioridadeText(ordem.Prioridade);
                worksheet.Cells[row, 6].Value = ordem.DataCriacao.ToString("dd/MM/yyyy às HH:mm");
                worksheet.Cells[row, 7].Value = ordem.DataConclusao?.ToString("dd/MM/yyyy às HH:mm") ?? "";
                worksheet.Cells[row, 8].Value = ordem.UsuarioCriador?.UserName ?? "";
                worksheet.Cells[row, 9].Value = ordem.UsuarioCriador?.NomeLoja ?? "";
                worksheet.Cells[row, 10].Value = ordem.TecnicoResponsavel?.UserName ?? "Não atribuído";
                worksheet.Cells[row, 11].Value = ordem.Observacoes ?? "";
            }

            // Ajusta largura das colunas
            worksheet.Cells.AutoFitColumns();

            // Gera nome do arquivo com data/hora
            var nomeArquivo = $"Relatorio_Ordens_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            // Retorna arquivo para download
            var stream = new MemoryStream(package.GetAsByteArray());
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nomeArquivo);
        }

        /// <summary>
        /// Gera arquivo PDF com os dados do relatório
        /// </summary>
        /// <param name="ordens">Lista de ordens para exportar</param>
        /// <param name="filtros">Filtros aplicados</param>
        /// <returns>Arquivo PDF para download</returns>
        private async Task<IActionResult> GerarPdf(List<OrdemServico> ordens, RelatorioFiltroViewModel filtros)
        {
            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
            var writer = PdfWriter.GetInstance(document, stream);

            document.Open();

            // Título do relatório
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
            var title = new Paragraph("Relatório de Ordens de Serviço", titleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 20
            };
            document.Add(title);

            // Informações dos filtros aplicados
            var filterFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var filterInfo = new StringBuilder("Filtros aplicados: ");
            
            if (filtros.DataInicio.HasValue)
                filterInfo.Append($"Data Inicial: {filtros.DataInicio.Value:dd/MM/yyyy} | ");
            if (filtros.DataFim.HasValue)
                filterInfo.Append($"Data Final: {filtros.DataFim.Value:dd/MM/yyyy} | ");
            if (!string.IsNullOrEmpty(filtros.TecnicoId))
                filterInfo.Append($"Técnico: {filtros.TecnicoId} | ");
            if (!string.IsNullOrEmpty(filtros.Loja))
                filterInfo.Append($"Loja: {filtros.Loja} | ");
            if (filtros.Status.HasValue)
                filterInfo.Append($"Status: {GetStatusText((StatusEnum)filtros.Status.Value)} | ");
            if (filtros.Prioridade.HasValue)
                filterInfo.Append($"Prioridade: {GetPrioridadeText((PrioridadeEnum)filtros.Prioridade.Value)} | ");

            var filterParagraph = new Paragraph(filterInfo.ToString().TrimEnd(' ', '|'), filterFont)
            {
                SpacingAfter = 15
            };
            document.Add(filterParagraph);

            // Tabela com dados das ordens
            var table = new PdfPTable(8) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 5, 20, 15, 10, 10, 15, 15, 10 });

            // Cabeçalhos da tabela
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);
            table.AddCell(new PdfPCell(new Phrase("ID", headerFont)) { BackgroundColor = new BaseColor(220, 220, 220) });
            table.AddCell(new PdfPCell(new Phrase("Solicitante", headerFont)) { BackgroundColor = new BaseColor(220, 220, 220) });
            table.AddCell(new PdfPCell(new Phrase("Status", headerFont)) { BackgroundColor = new BaseColor(220, 220, 220) });
            table.AddCell(new PdfPCell(new Phrase("Prioridade", headerFont)) { BackgroundColor = new BaseColor(220, 220, 220) });
            table.AddCell(new PdfPCell(new Phrase("Data Criação", headerFont)) { BackgroundColor = new BaseColor(220, 220, 220) });
            table.AddCell(new PdfPCell(new Phrase("Loja", headerFont)) { BackgroundColor = new BaseColor(220, 220, 220) });
            table.AddCell(new PdfPCell(new Phrase("Técnico", headerFont)) { BackgroundColor = new BaseColor(220, 220, 220) });
            table.AddCell(new PdfPCell(new Phrase("Conclusão", headerFont)) { BackgroundColor = new BaseColor(220, 220, 220) });

            // Dados das ordens
            var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);
            foreach (var ordem in ordens)
            {
                table.AddCell(new PdfPCell(new Phrase(ordem.Id.ToString(), cellFont)));
                table.AddCell(new PdfPCell(new Phrase(ordem.Titulo, cellFont)));
                table.AddCell(new PdfPCell(new Phrase(GetStatusText(ordem.Status), cellFont)));
                table.AddCell(new PdfPCell(new Phrase(GetPrioridadeText(ordem.Prioridade), cellFont)));
                table.AddCell(new PdfPCell(new Phrase(ordem.DataCriacao.ToString("dd/MM/yyyy"), cellFont)));
                table.AddCell(new PdfPCell(new Phrase(ordem.UsuarioCriador?.NomeLoja ?? "", cellFont)));
                table.AddCell(new PdfPCell(new Phrase(ordem.TecnicoResponsavel?.UserName ?? "Não atribuído", cellFont)));
                table.AddCell(new PdfPCell(new Phrase(ordem.DataConclusao?.ToString("dd/MM/yyyy") ?? "", cellFont)));
            }

            document.Add(table);

            // Rodapé com data de geração
            var footerFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 8);
            var footer = new Paragraph($"Relatório gerado em {DateTime.Now:dd/MM/yyyy às HH:mm}", footerFont)
            {
                Alignment = Element.ALIGN_RIGHT,
                SpacingBefore = 20
            };
            document.Add(footer);

            document.Close();

            // Gera nome do arquivo com data/hora
            var nomeArquivo = $"Relatorio_Ordens_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            return File(stream.ToArray(), "application/pdf", nomeArquivo);
        }

        /// <summary>
        /// Popula as listas de dropdown para os filtros
        /// </summary>
        /// <param name="viewModel">ViewModel a ser populado</param>
        private async Task PopularListasDropdown(RelatorioFiltroViewModel viewModel)
        {
            // Lista de técnicos
            var tecnicos = await _userManager.Users
                .Where(u => u.IsTecnico)
                .OrderBy(u => u.UserName)
                .ToListAsync();

            viewModel.TecnicosList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Todos os técnicos" },
                new SelectListItem { Value = "sem_tecnico", Text = "Sem técnico atribuído" }
            };
            viewModel.TecnicosList.AddRange(tecnicos.Select(t => new SelectListItem
            {
                Value = t.Id,
                Text = t.UserName ?? ""
            }));

            // Lista de lojas (apenas usuários de lojas, excluindo técnicos)
            var lojas = await _context.Users
                .Where(u => !string.IsNullOrEmpty(u.NomeLoja) && !u.IsTecnico)
                .Select(u => u.NomeLoja)
                .Distinct()
                .OrderBy(l => l)
                .ToListAsync();

            viewModel.LojasList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Todas as lojas" }
            };
            viewModel.LojasList.AddRange(lojas.Select(l => new SelectListItem
            {
                Value = l,
                Text = l
            }));

            // Lista de status
            viewModel.StatusList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Todos os status" },
                new SelectListItem { Value = "1", Text = "Aberta" },
                new SelectListItem { Value = "2", Text = "Em Andamento" },
                new SelectListItem { Value = "3", Text = "Concluída" }
            };

            // Lista de prioridades
            viewModel.PrioridadeList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Todas as prioridades" },
                new SelectListItem { Value = "1", Text = "Baixa" },
                new SelectListItem { Value = "2", Text = "Média" },
                new SelectListItem { Value = "3", Text = "Alta" }
            };
        }

        /// <summary>
        /// Converte enum de status para texto
        /// </summary>
        /// <param name="status">Status a ser convertido</param>
        /// <returns>Texto do status</returns>
        private string GetStatusText(StatusEnum status)
        {
            return status switch
            {
                StatusEnum.Aberta => "Aberta",
                StatusEnum.EmAndamento => "Em Andamento",
                StatusEnum.Concluida => "Concluída",
                _ => "Desconhecido"
            };
        }

        /// <summary>
        /// Converte enum de prioridade para texto
        /// </summary>
        /// <param name="prioridade">Prioridade a ser convertida</param>
        /// <returns>Texto da prioridade</returns>
        private string GetPrioridadeText(PrioridadeEnum prioridade)
        {
            return prioridade switch
            {
                PrioridadeEnum.Baixa => "Baixa",
                PrioridadeEnum.Media => "Média",
                PrioridadeEnum.Alta => "Alta",
                _ => "Desconhecido"
            };
        }

        /// <summary>
        /// Gera chave única para cache baseada nos filtros aplicados
        /// </summary>
        /// <param name="filtros">Filtros do relatório</param>
        /// <returns>Chave única para cache</returns>
        private string GerarChaveCache(RelatorioFiltroViewModel filtros)
        {
            // Cria uma chave única baseada nos filtros para garantir cache correto
            var chaveBuilder = new StringBuilder("relatorio_");
            
            // Adiciona data inicial se especificada
            if (filtros.DataInicio.HasValue)
                chaveBuilder.Append($"di_{filtros.DataInicio.Value:yyyyMMdd}_");
            
            // Adiciona data final se especificada
            if (filtros.DataFim.HasValue)
                chaveBuilder.Append($"df_{filtros.DataFim.Value:yyyyMMdd}_");
            
            // Adiciona técnico se especificado
            if (!string.IsNullOrEmpty(filtros.TecnicoId))
                chaveBuilder.Append($"tec_{filtros.TecnicoId}_");
            
            // Adiciona loja se especificada
            if (!string.IsNullOrEmpty(filtros.Loja))
                chaveBuilder.Append($"loja_{filtros.Loja.Replace(" ", "")}_");
            
            // Adiciona status se especificado
            if (filtros.Status.HasValue)
                chaveBuilder.Append($"st_{filtros.Status.Value}_");
            
            // Adiciona prioridade se especificada
            if (filtros.Prioridade.HasValue)
                chaveBuilder.Append($"pr_{filtros.Prioridade.Value}_");
            
            // Remove o último underscore se existir
            var chave = chaveBuilder.ToString().TrimEnd('_');
            
            return chave;
        }

        /// <summary>
        /// Exibe página de gráficos e análises visuais
        /// </summary>
        /// <param name="dataInicio">Data inicial do período</param>
        /// <param name="dataFim">Data final do período</param>
        /// <returns>View com gráficos</returns>
        [HttpGet]
        public async Task<IActionResult> Graficos(DateTime? dataInicio, DateTime? dataFim)
        {
            // Define período padrão (últimos 30 dias)
            var inicio = dataInicio ?? DateTime.Now.AddDays(-30);
            var fim = dataFim ?? DateTime.Now;
            
            // Busca ordens do período
            var ordens = await _context.OrdensServico
                .Include(o => o.UsuarioCriador)
                .Include(o => o.TecnicoResponsavel)
                .Where(o => o.DataCriacao >= inicio && o.DataCriacao <= fim)
                .AsNoTracking()
                .ToListAsync();
            
            // Calcula estatísticas com dados para gráficos
            var estatisticas = new RelatorioEstatisticasViewModel
            {
                TotalOrdens = ordens.Count,
                OrdensAbertas = ordens.Count(o => o.Status == StatusEnum.Aberta),
                OrdensEmAndamento = ordens.Count(o => o.Status == StatusEnum.EmAndamento),
                OrdensConcluidas = ordens.Count(o => o.Status == StatusEnum.Concluida),
                
                // Lojas mais ativas (top 10)
                LojasAtividade = ordens
                    .Where(o => o.UsuarioCriador != null)
                    .GroupBy(o => o.UsuarioCriador!.NomeLoja)
                    .Select(g => new LojaAtividadeViewModel
                    {
                        NomeLoja = g.Key,
                        TotalOrdens = g.Count(),
                        OrdensAbertas = g.Count(o => o.Status == StatusEnum.Aberta),
                        OrdensEmAndamento = g.Count(o => o.Status == StatusEnum.EmAndamento),
                        OrdensConcluidas = g.Count(o => o.Status == StatusEnum.Concluida),
                        PercentualTotal = ordens.Count > 0 ? (double)g.Count() / ordens.Count * 100 : 0
                    })
                    .OrderByDescending(l => l.TotalOrdens)
                    .Take(10)
                    .ToList(),
                
                // Problemas mais frequentes (análise de palavras-chave)
                ProblemasFrequentes = AnalisarProblemasFrequentes(ordens),
                
                // Atividade por setor
                SetoresAtividade = ordens
                    .GroupBy(o => o.Setor)
                    .Select(g => new SetorAtividadeViewModel
                    {
                        NomeSetor = g.Key.ToString().Replace("FrenteDeCaixa", "Frente de Caixa"),
                        TotalOrdens = g.Count(),
                        TempoMedioConclusao = g
                            .Where(o => o.DataConclusao.HasValue)
                            .Select(o => (o.DataConclusao!.Value - o.DataCriacao).TotalDays)
                            .DefaultIfEmpty(0)
                            .Average(),
                        PercentualTotal = ordens.Count > 0 ? (double)g.Count() / ordens.Count * 100 : 0
                    })
                    .OrderByDescending(s => s.TotalOrdens)
                    .ToList()
            };
            
            var viewModel = new RelatorioResultadoViewModel
            {
                Estatisticas = estatisticas,
                Filtros = new RelatorioFiltroViewModel
                {
                    DataInicio = inicio,
                    DataFim = fim
                }
            };
            
            return View(viewModel);
        }

        /// <summary>
        /// Gera um checklist em PDF para as ordens selecionadas
        /// </summary>
        /// <param name="ids">Lista de IDs das ordens selecionadas</param>
        /// <returns>Arquivo PDF para download</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GerarChecklist([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                return BadRequest("Nenhuma ordem selecionada.");
            }

            var ordens = await _context.OrdensServico
                .Include(o => o.UsuarioCriador)
                .Include(o => o.TecnicoResponsavel)
                .Where(o => ids.Contains(o.Id))
                .OrderBy(o => o.Prioridade) // Ordena por prioridade (Alta primeiro)
                .ThenBy(o => o.DataCriacao)
                .AsNoTracking()
                .ToListAsync();

            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4, 25, 25, 30, 30);
            var writer = PdfWriter.GetInstance(document, stream);

            document.Open();

            // Título
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
            var title = new Paragraph("Checklist de Execução - Ordens de Serviço", titleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 10
            };
            document.Add(title);

            var subTitleFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var subTitle = new Paragraph($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm} | Total: {ordens.Count} ordens", subTitleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 20
            };
            document.Add(subTitle);

            // Tabela
            var table = new PdfPTable(4) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 5, 60, 20, 15 }); // Checkbox, Detalhes, Local/Data, Prioridade

            // Cabeçalhos
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
            var cellHeader = new PdfPCell(new Phrase("OK", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER, BackgroundColor = new BaseColor(220, 220, 220), Padding = 5 };
            table.AddCell(cellHeader);
            table.AddCell(new PdfPCell(new Phrase("Detalhes da Ordem", headerFont)) { BackgroundColor = new BaseColor(220, 220, 220), Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase("Local / Data", headerFont)) { BackgroundColor = new BaseColor(220, 220, 220), Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase("Prioridade", headerFont)) { BackgroundColor = new BaseColor(220, 220, 220), Padding = 5 });

            var contentFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);
            var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);

            foreach (var ordem in ordens)
            {
                // Coluna Checkbox (Vazio para marcar)
                var cellCheck = new PdfPCell(new Phrase(" ", contentFont)) { FixedHeight = 40f }; // Altura fixa para facilitar a marcação
                table.AddCell(cellCheck);

                // Coluna Detalhes
                var detailsCell = new PdfPCell();
                detailsCell.Padding = 5;
                detailsCell.AddElement(new Paragraph($"#{ordem.Id} - {ordem.Titulo}", boldFont));
                detailsCell.AddElement(new Paragraph(ordem.Descricao, contentFont));
                if (!string.IsNullOrEmpty(ordem.TecnicoResponsavel?.UserName))
                {
                    detailsCell.AddElement(new Paragraph($"Técnico: {ordem.TecnicoResponsavel.UserName}", smallFont));
                }
                table.AddCell(detailsCell);

                // Coluna Local/Data
                var localCell = new PdfPCell();
                localCell.Padding = 5;
                localCell.AddElement(new Paragraph(ordem.UsuarioCriador?.NomeLoja ?? "N/A", contentFont));
                localCell.AddElement(new Paragraph(ordem.Setor.ToString(), smallFont));
                localCell.AddElement(new Paragraph(ordem.DataCriacao.ToString("dd/MM/yy HH:mm"), smallFont));
                table.AddCell(localCell);

                // Coluna Prioridade
                var prioridadeCell = new PdfPCell(new Phrase(GetPrioridadeText(ordem.Prioridade), boldFont));
                prioridadeCell.HorizontalAlignment = Element.ALIGN_CENTER;
                prioridadeCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                
                // Colorir background da prioridade (opcional, iTextSharp tem suas limitações de cores predefinidas ou RGB)
                if (ordem.Prioridade == PrioridadeEnum.Alta) prioridadeCell.BackgroundColor = new BaseColor(255, 200, 200); // Vermelho claro
                else if (ordem.Prioridade == PrioridadeEnum.Media) prioridadeCell.BackgroundColor = new BaseColor(255, 255, 200); // Amarelo claro
                else prioridadeCell.BackgroundColor = new BaseColor(200, 200, 255); // Azul claro
                
                table.AddCell(prioridadeCell);
            }

            document.Add(table);
            document.Close();

            var nomeArquivo = $"Checklist_Execucao_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(stream.ToArray(), "application/pdf", nomeArquivo);
        }

        /// <summary>
        /// Analisa problemas mais frequentes baseado em palavras-chave
        /// </summary>
        /// <param name="ordens">Lista de ordens</param>
        /// <returns>Lista de problemas frequentes</returns>
        private List<ProblemaFrequenteViewModel> AnalisarProblemasFrequentes(List<OrdemServico> ordens)
        {
            // Palavras irrelevantes (stopwords)
            var stopwords = new HashSet<string> 
            { 
                "o", "a", "de", "da", "do", "em", "para", "com", "por", "um", "uma",
                "os", "as", "dos", "das", "no", "na", "nos", "nas", "ao", "aos",
                "à", "às", "pelo", "pela", "pelos", "pelas", "este", "esta", "esse",
                "essa", "aquele", "aquela", "que", "qual", "quando", "onde", "como"
            };
            
            // Extrai e conta palavras-chave
            var palavrasContador = new Dictionary<string, List<OrdemServico>>();
            
            foreach (var ordem in ordens)
            {
                if (string.IsNullOrWhiteSpace(ordem.Descricao))
                    continue;
                
                // Extrai palavras relevantes
                var palavras = ordem.Descricao
                    .ToLower()
                    .Split(new[] { ' ', ',', '.', ';', ':', '\n', '\r', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(p => p.Length > 3 && !stopwords.Contains(p))
                    .Distinct();
                
                foreach (var palavra in palavras)
                {
                    if (!palavrasContador.ContainsKey(palavra))
                        palavrasContador[palavra] = new List<OrdemServico>();
                    
                    palavrasContador[palavra].Add(ordem);
                }
            }
            
            // Retorna top 10 problemas mais frequentes
            return palavrasContador
                .Where(kvp => kvp.Value.Count >= 2) // Mínimo 2 ocorrências
                .Select(kvp => new ProblemaFrequenteViewModel
                {
                    Descricao = char.ToUpper(kvp.Key[0]) + kvp.Key.Substring(1),
                    Quantidade = kvp.Value.Count,
                    PercentualTotal = ordens.Count > 0 ? (double)kvp.Value.Count / ordens.Count * 100 : 0,
                    LojasAfetadas = kvp.Value
                        .Where(o => o.UsuarioCriador != null)
                        .Select(o => o.UsuarioCriador!.NomeLoja)
                        .Distinct()
                        .ToList(),
                    SetorMaisAfetado = kvp.Value
                        .GroupBy(o => o.Setor)
                        .OrderByDescending(g => g.Count())
                        .Select(g => g.Key.ToString().Replace("FrenteDeCaixa", "Frente de Caixa"))
                        .FirstOrDefault() ?? "N/A"
                })
                .OrderByDescending(p => p.Quantidade)
                .Take(10)
                .ToList();
        }
    }
}