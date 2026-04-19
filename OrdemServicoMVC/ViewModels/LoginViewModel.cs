using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace OrdemServicoMVC.ViewModels
{
    // ViewModel para a página de login
    public class LoginViewModel
    {
        // Loja/usuário selecionado no dropdown
        [Required(ErrorMessage = "Selecione uma loja")]
        [Display(Name = "Loja")]
        public string LojaUsuario { get; set; } = string.Empty;
        
        // Senha do usuário
        [Required(ErrorMessage = "A senha é obrigatória")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Senha { get; set; } = string.Empty;
        
        // Opção para lembrar o login
        [Display(Name = "Lembrar-me")]
        public bool LembrarMe { get; set; }
        
        // Lista de lojas para o dropdown
        public List<SelectListItem> Lojas { get; set; } = new List<SelectListItem>();
    }
}