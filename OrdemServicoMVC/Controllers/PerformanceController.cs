using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdemServicoMVC.Services;

namespace OrdemServicoMVC.Controllers
{
    /// <summary>
    /// Controller para testes de performance das otimizações implementadas
    /// Permite aos administradores verificar a eficácia das melhorias
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class PerformanceController : Controller
    {
        private readonly IPerformanceTestService _performanceTestService;
        private readonly ILogger<PerformanceController> _logger;

        public PerformanceController(
            IPerformanceTestService performanceTestService,
            ILogger<PerformanceController> logger)
        {
            _performanceTestService = performanceTestService;
            _logger = logger;
        }

        /// <summary>
        /// Exibe a página principal de testes de performance
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Executa teste de performance dos relatórios com cache
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> TestarRelatorios()
        {
            try
            {
                _logger.LogInformation("Iniciando teste de performance de relatórios");
                
                // Executa o teste de performance
                var resultado = await _performanceTestService.TestarPerformanceRelatorios();
                
                // Retorna resultado em JSON para exibição via AJAX
                return Json(new
                {
                    sucesso = resultado.Sucesso,
                    testeName = resultado.TesteName,
                    primeiraExecucao = resultado.PrimeiraExecucaoMs,
                    segundaExecucao = resultado.SegundaExecucaoMs,
                    registrosProcessados = resultado.RegistrosProcessados,
                    melhoriaPercentual = resultado.MelhoriaPercentual,
                    resumo = resultado.ResumoPerformance,
                    erro = resultado.ErroMensagem
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante teste de performance de relatórios");
                return Json(new
                {
                    sucesso = false,
                    erro = "Erro interno durante o teste: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Executa teste de performance das consultas com índices compostos
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> TestarConsultas()
        {
            try
            {
                _logger.LogInformation("Iniciando teste de performance de consultas");
                
                // Executa o teste de performance
                var resultado = await _performanceTestService.TestarPerformanceConsultas();
                
                // Retorna resultado em JSON para exibição via AJAX
                return Json(new
                {
                    sucesso = resultado.Sucesso,
                    testeName = resultado.TesteName,
                    primeiraExecucao = resultado.PrimeiraExecucaoMs,
                    segundaExecucao = resultado.SegundaExecucaoMs,
                    terceiraExecucao = resultado.TerceiraExecucaoMs,
                    registrosProcessados = resultado.RegistrosProcessados,
                    resumo = resultado.ResumoPerformance,
                    erro = resultado.ErroMensagem
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante teste de performance de consultas");
                return Json(new
                {
                    sucesso = false,
                    erro = "Erro interno durante o teste: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Executa todos os testes de performance
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> TestarTodos()
        {
            try
            {
                _logger.LogInformation("Iniciando todos os testes de performance");
                
                // Executa ambos os testes
                var resultadoRelatorios = await _performanceTestService.TestarPerformanceRelatorios();
                var resultadoConsultas = await _performanceTestService.TestarPerformanceConsultas();
                
                // Retorna resultados combinados
                return Json(new
                {
                    sucesso = resultadoRelatorios.Sucesso && resultadoConsultas.Sucesso,
                    relatorios = new
                    {
                        sucesso = resultadoRelatorios.Sucesso,
                        testeName = resultadoRelatorios.TesteName,
                        primeiraExecucao = resultadoRelatorios.PrimeiraExecucaoMs,
                        segundaExecucao = resultadoRelatorios.SegundaExecucaoMs,
                        registrosProcessados = resultadoRelatorios.RegistrosProcessados,
                        melhoriaPercentual = resultadoRelatorios.MelhoriaPercentual,
                        resumo = resultadoRelatorios.ResumoPerformance,
                        erro = resultadoRelatorios.ErroMensagem
                    },
                    consultas = new
                    {
                        sucesso = resultadoConsultas.Sucesso,
                        testeName = resultadoConsultas.TesteName,
                        primeiraExecucao = resultadoConsultas.PrimeiraExecucaoMs,
                        segundaExecucao = resultadoConsultas.SegundaExecucaoMs,
                        terceiraExecucao = resultadoConsultas.TerceiraExecucaoMs,
                        registrosProcessados = resultadoConsultas.RegistrosProcessados,
                        resumo = resultadoConsultas.ResumoPerformance,
                        erro = resultadoConsultas.ErroMensagem
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante execução de todos os testes");
                return Json(new
                {
                    sucesso = false,
                    erro = "Erro interno durante os testes: " + ex.Message
                });
            }
        }
    }
}