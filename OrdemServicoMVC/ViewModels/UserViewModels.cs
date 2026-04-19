using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OrdemServicoMVC.ViewModels
{
    // ViewModel para exibição de usuários na listagem
    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        
        [Display(Name = "Nome de Usuário")]
        public string UserName { get; set; } = string.Empty;
        
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;
        
        [Display(Name = "Nome da Loja")]
        public string NomeLoja { get; set; } = string.Empty;
        
        [Display(Name = "Administrador")]
        public bool IsAdmin { get; set; }
        
        [Display(Name = "Data de Criação")]
        public DateTime DataCriacao { get; set; }
        
        [Display(Name = "E-mail Confirmado")]
        public bool EmailConfirmed { get; set; }
        
        [Display(Name = "Perfis")]
        public string Roles { get; set; } = string.Empty;
    }

    // ViewModel para criação de usuários
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "O nome de usuário é obrigatório")]
        [StringLength(50, ErrorMessage = "O nome de usuário deve ter no máximo 50 caracteres")]
        [Display(Name = "Nome de Usuário")]
        public string UserName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "O e-mail é obrigatório")]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "O nome da loja é obrigatório")]
        [StringLength(100, ErrorMessage = "O nome da loja deve ter no máximo 100 caracteres")]
        [Display(Name = "Nome da Loja")]
        public string NomeLoja { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "A senha é obrigatória")]
        [StringLength(100, MinimumLength = 4, ErrorMessage = "A senha deve ter entre 4 e 100 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Senha { get; set; } = string.Empty;
        
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Senha")]
        [Compare("Senha", ErrorMessage = "A senha e a confirmação não coincidem")]
        public string ConfirmarSenha { get; set; } = string.Empty;
        
        [Display(Name = "É Administrador?")]
        public bool IsAdmin { get; set; }
        
        [Display(Name = "E-mail Confirmado")]
        public bool EmailConfirmed { get; set; }
    }

    // ViewModel para edição de usuários
    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "O nome de usuário é obrigatório")]
        [StringLength(50, ErrorMessage = "O nome de usuário deve ter no máximo 50 caracteres")]
        [Display(Name = "Nome de Usuário")]
        public string UserName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "O e-mail é obrigatório")]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "O nome da loja é obrigatório")]
        [StringLength(100, ErrorMessage = "O nome da loja deve ter no máximo 100 caracteres")]
        [Display(Name = "Nome da Loja")]
        public string NomeLoja { get; set; } = string.Empty;
        
        [Display(Name = "É Administrador?")]
        public bool IsAdmin { get; set; }
        
        [Display(Name = "E-mail Confirmado")]
        public bool EmailConfirmed { get; set; }
    }

    // ViewModel para alteração de senha
    public class ChangePasswordViewModel
    {
        public string UserId { get; set; } = string.Empty;
        
        [Display(Name = "Usuário")]
        public string UserName { get; set; } = string.Empty;
        
        [Display(Name = "Loja")]
        public string NomeLoja { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "A nova senha é obrigatória")]
        [StringLength(100, MinimumLength = 4, ErrorMessage = "A senha deve ter entre 4 e 100 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Nova Senha")]
        public string NovaSenha { get; set; } = string.Empty;
        
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Nova Senha")]
        [Compare("NovaSenha", ErrorMessage = "A senha e a confirmação não coincidem")]
        public string ConfirmarNovaSenha { get; set; } = string.Empty;
    }

    // ViewModel para exibição de perfil do usuário
    public class UserProfileViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool IsTecnico { get; set; }
        public string NomeLoja { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? MemberSince { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
    }
}