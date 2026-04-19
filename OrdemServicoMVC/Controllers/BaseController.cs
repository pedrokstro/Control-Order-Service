using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OrdemServicoMVC.Models;

namespace OrdemServicoMVC.Controllers
{
    /// <summary>
    /// Controller base que fornece funcionalidades comuns para todos os controllers
    /// </summary>
    public abstract class BaseController : Controller
    {
        private readonly AppSettings _appSettings;

        /// <summary>
        /// Construtor que injeta as configurações da aplicação
        /// </summary>
        /// <param name="appSettings">Configurações da aplicação</param>
        protected BaseController(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        /// <summary>
        /// Método executado antes de cada action para configurar dados comuns
        /// </summary>
        /// <param name="context">Contexto da action</param>
        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            // Adiciona a versão da aplicação no ViewBag para uso em todas as views
            ViewBag.AppVersion = _appSettings.Version;
            ViewBag.ApplicationName = _appSettings.ApplicationName;
            
            base.OnActionExecuting(context);
        }
    }
}