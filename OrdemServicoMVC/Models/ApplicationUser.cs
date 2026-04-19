using Microsoft.AspNetCore.Identity;

namespace OrdemServicoMVC.Models
{
    // Modelo de usuário personalizado que herda de IdentityUser
    public class ApplicationUser : IdentityUser
    {
        // Nome da loja do usuário
        public string? NomeLoja { get; set; }
        
        // Indica se o usuário é administrador
        public bool IsAdmin { get; set; }
        
        // Indica se o usuário é um técnico
        public bool IsTecnico { get; set; }
        
        // Data de criação do usuário (horário brasileiro UTC-3)
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow.AddHours(-3);
    }
}