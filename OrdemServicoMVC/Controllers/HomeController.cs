using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OrdemServicoMVC.Models;

namespace OrdemServicoMVC.Controllers;

/// <summary>
/// Controller responsável pelas páginas principais do sistema
/// Gerencia a página inicial, política de privacidade e tratamento de erros
/// </summary>
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Exibe a página inicial do sistema
    /// </summary>
    /// <returns>View da página inicial</returns>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Exibe a página de política de privacidade
    /// </summary>
    /// <returns>View da política de privacidade</returns>
    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// Exibe a página de erro do sistema
    /// Configurado para não fazer cache da página de erro
    /// Coleta informações detalhadas sobre o erro para diagnóstico
    /// </summary>
    /// <returns>View de erro com informações de rastreamento e diagnóstico</returns>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        // Coleta informações sobre o erro
        var errorViewModel = new ErrorViewModel 
        { 
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            RequestPath = HttpContext.Request.Path,
            StatusCode = HttpContext.Response.StatusCode,
            ShowDetails = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()
        };
        
        // Tenta obter informações adicionais do erro se disponível
        var exceptionFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (exceptionFeature != null)
        {
            // Log do erro para análise posterior
            _logger.LogError(exceptionFeature.Error, 
                "Erro capturado na página de erro. RequestId: {RequestId}, Path: {Path}", 
                errorViewModel.RequestId, 
                errorViewModel.RequestPath);
                
            // Em desenvolvimento, inclui a mensagem do erro
            if (errorViewModel.ShowDetails)
            {
                errorViewModel.ErrorMessage = exceptionFeature.Error.Message;
            }
        }
        
        return View(errorViewModel);
    }
}
