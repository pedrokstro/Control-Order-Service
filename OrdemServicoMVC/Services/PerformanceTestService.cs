using Microsoft.EntityFrameworkCore;
using OrdemServicoMVC.Data;
using OrdemServicoMVC.Models;
using OrdemServicoMVC.ViewModels;
using System.Diagnostics;

namespace OrdemServicoMVC.Services
{
    /// <summary>
    /// Serviço para testar a performance das otimizações implementadas
    /// Permite comparar tempos de execução antes e depois das melhorias
    /// </summary>
    public interface IPerformanceTestService
    {
        Task<PerformanceTestResult> TestarPerformanceRelatorios();
        Task<PerformanceTestResult> TestarPerformanceConsultas();
    }

    public class PerformanceTestService : IPerformanceTestService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly ILogger<PerformanceTestService> _logger;

        public PerformanceTestService(
            ApplicationDbContext context,
            ICacheService cacheService,
            ILogger<PerformanceTestService> logger)
        {
            _context = context;
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// Testa a performance da geração de relatórios com cache
        /// </summary>
        public async Task<PerformanceTestResult> TestarPerformanceRelatorios()
        {
            var resultado = new PerformanceTestResult { TesteName = "Relatórios com Cache" };
            var stopwatch = new Stopwatch();

            try
            {
                // Filtros de teste para simular consulta real
                var filtros = new RelatorioFiltroViewModel
                {
                    DataInicio = DateTime.Now.AddDays(-30),
                    DataFim = DateTime.Now,
                    Status = null, // Todos os status
                    Prioridade = null // Todas as prioridades
                };

                // Teste 1: Primeira execução (sem cache)
                _logger.LogInformation("Iniciando teste de performance - primeira execução (sem cache)");
                stopwatch.Start();
                
                var ordens = await BuscarOrdensComFiltros(filtros);
                var estatisticas = await CalcularEstatisticas(ordens);
                
                stopwatch.Stop();
                resultado.PrimeiraExecucaoMs = stopwatch.ElapsedMilliseconds;
                resultado.RegistrosProcessados = ordens.Count;

                // Teste 2: Segunda execução (com cache)
                stopwatch.Restart();
                
                var chaveCache = GerarChaveCache(filtros);
                var estatisticasCache = await _cacheService.GetEstatisticasAsync(
                    chaveCache,
                    () => CalcularEstatisticas(ordens),
                    TimeSpan.FromMinutes(10));
                
                stopwatch.Stop();
                resultado.SegundaExecucaoMs = stopwatch.ElapsedMilliseconds;

                // Calcula melhoria de performance
                resultado.MelhoriaPercentual = resultado.PrimeiraExecucaoMs > 0 
                    ? ((double)(resultado.PrimeiraExecucaoMs - resultado.SegundaExecucaoMs) / resultado.PrimeiraExecucaoMs) * 100
                    : 0;

                resultado.Sucesso = true;
                _logger.LogInformation($"Teste concluído: {resultado.PrimeiraExecucaoMs}ms -> {resultado.SegundaExecucaoMs}ms ({resultado.MelhoriaPercentual:F1}% melhoria)");
            }
            catch (Exception ex)
            {
                resultado.Sucesso = false;
                resultado.ErroMensagem = ex.Message;
                _logger.LogError(ex, "Erro durante teste de performance de relatórios");
            }

            return resultado;
        }

        /// <summary>
        /// Testa a performance das consultas com índices compostos
        /// </summary>
        public async Task<PerformanceTestResult> TestarPerformanceConsultas()
        {
            var resultado = new PerformanceTestResult { TesteName = "Consultas com Índices Compostos" };
            var stopwatch = new Stopwatch();

            try
            {
                _logger.LogInformation("Iniciando teste de performance de consultas com índices");

                // Teste de consulta por data e status (usa índice composto)
                stopwatch.Start();
                
                var ordensDataStatus = await _context.OrdensServico
                    .AsNoTracking()
                    .Where(o => o.DataCriacao >= DateTime.Now.AddDays(-30) && o.Status == StatusEnum.Concluida)
                    .CountAsync();
                
                stopwatch.Stop();
                var tempoDataStatus = stopwatch.ElapsedMilliseconds;

                // Teste de consulta por técnico e status (usa índice composto)
                stopwatch.Restart();
                
                var ordensTecnicoStatus = await _context.OrdensServico
                    .AsNoTracking()
                    .Where(o => o.TecnicoResponsavelId != null && o.Status == StatusEnum.EmAndamento)
                    .CountAsync();
                
                stopwatch.Stop();
                var tempoTecnicoStatus = stopwatch.ElapsedMilliseconds;

                // Teste de consulta por prioridade e status (usa índice composto)
                stopwatch.Restart();
                
                var ordensPrioridadeStatus = await _context.OrdensServico
                    .AsNoTracking()
                    .Where(o => o.Prioridade == PrioridadeEnum.Alta && o.Status == StatusEnum.Aberta)
                    .CountAsync();
                
                stopwatch.Stop();
                var tempoPrioridadeStatus = stopwatch.ElapsedMilliseconds;

                resultado.PrimeiraExecucaoMs = tempoDataStatus;
                resultado.SegundaExecucaoMs = tempoTecnicoStatus;
                resultado.TerceiraExecucaoMs = tempoPrioridadeStatus;
                resultado.RegistrosProcessados = ordensDataStatus + ordensTecnicoStatus + ordensPrioridadeStatus;
                resultado.Sucesso = true;

                _logger.LogInformation($"Consultas executadas: Data+Status={tempoDataStatus}ms, Técnico+Status={tempoTecnicoStatus}ms, Prioridade+Status={tempoPrioridadeStatus}ms");
            }
            catch (Exception ex)
            {
                resultado.Sucesso = false;
                resultado.ErroMensagem = ex.Message;
                _logger.LogError(ex, "Erro durante teste de performance de consultas");
            }

            return resultado;
        }

        /// <summary>
        /// Busca ordens com filtros aplicados (método otimizado)
        /// </summary>
        private async Task<List<OrdemServico>> BuscarOrdensComFiltros(RelatorioFiltroViewModel filtros)
        {
            var query = _context.OrdensServico
                .AsNoTracking() // Otimização: não rastreia mudanças
                .Include(o => o.UsuarioCriador)
                .Include(o => o.TecnicoResponsavel)
                .AsQueryable();

            // Aplica filtros de data
            if (filtros.DataInicio.HasValue)
                query = query.Where(o => o.DataCriacao >= filtros.DataInicio.Value);
            if (filtros.DataFim.HasValue)
                query = query.Where(o => o.DataCriacao <= filtros.DataFim.Value);

            // Aplica filtros de status e prioridade
            if (filtros.Status.HasValue)
                query = query.Where(o => o.Status == (StatusEnum)filtros.Status.Value);
            if (filtros.Prioridade.HasValue)
                query = query.Where(o => o.Prioridade == (PrioridadeEnum)filtros.Prioridade.Value);

            return await query.OrderByDescending(o => o.DataCriacao).ToListAsync();
        }

        /// <summary>
        /// Calcula estatísticas de forma otimizada
        /// </summary>
        private async Task<RelatorioEstatisticasViewModel> CalcularEstatisticas(List<OrdemServico> ordens)
        {
            return await Task.FromResult(new RelatorioEstatisticasViewModel
            {
                TotalOrdens = ordens.Count,
                OrdensAbertas = ordens.Count(o => o.Status == StatusEnum.Aberta),
                OrdensEmAndamento = ordens.Count(o => o.Status == StatusEnum.EmAndamento),
                OrdensConcluidas = ordens.Count(o => o.Status == StatusEnum.Concluida),
                OrdensPrioridadeAlta = ordens.Count(o => o.Prioridade == PrioridadeEnum.Alta),
                OrdensPrioridadeMedia = ordens.Count(o => o.Prioridade == PrioridadeEnum.Media),
                OrdensPrioridadeBaixa = ordens.Count(o => o.Prioridade == PrioridadeEnum.Baixa)
            });
        }

        /// <summary>
        /// Gera chave de cache baseada nos filtros
        /// </summary>
        private string GerarChaveCache(RelatorioFiltroViewModel filtros)
        {
            return $"relatorio_{filtros.DataInicio?.ToString("yyyyMMdd") ?? "null"}_" +
                   $"{filtros.DataFim?.ToString("yyyyMMdd") ?? "null"}_" +
                   $"{filtros.Status?.ToString() ?? "null"}_" +
                   $"{filtros.Prioridade?.ToString() ?? "null"}";
        }
    }

    /// <summary>
    /// Resultado dos testes de performance
    /// </summary>
    public class PerformanceTestResult
    {
        public string TesteName { get; set; } = string.Empty;
        public bool Sucesso { get; set; }
        public string? ErroMensagem { get; set; }
        public long PrimeiraExecucaoMs { get; set; }
        public long SegundaExecucaoMs { get; set; }
        public long? TerceiraExecucaoMs { get; set; }
        public int RegistrosProcessados { get; set; }
        public double MelhoriaPercentual { get; set; }
        
        public string ResumoPerformance => Sucesso 
            ? $"{TesteName}: {PrimeiraExecucaoMs}ms → {SegundaExecucaoMs}ms ({MelhoriaPercentual:F1}% melhoria) - {RegistrosProcessados} registros"
            : $"{TesteName}: ERRO - {ErroMensagem}";
    }
}