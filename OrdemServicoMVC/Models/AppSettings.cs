using System.ComponentModel.DataAnnotations;

namespace OrdemServicoMVC.Models
{
    /// <summary>
    /// Classe para mapear as configurações da aplicação do appsettings.json
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Versão atual da aplicação
        /// </summary>
        [Required]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Nome da aplicação
        /// </summary>
        [Required]
        public string ApplicationName { get; set; } = string.Empty;
    }
}