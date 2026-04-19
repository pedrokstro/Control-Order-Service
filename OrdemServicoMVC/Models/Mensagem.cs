using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrdemServicoMVC.Models
{
    // Modelo para representar mensagens do chat de uma ordem de serviço
    public class Mensagem
    {
        // Chave primária da mensagem
        public int Id { get; set; }
        
        // ID da ordem de serviço relacionada
        [Required]
        public int OrdemServicoId { get; set; }
        
        // Navegação para a ordem de serviço
        [ForeignKey("OrdemServicoId")]
        public virtual OrdemServico? OrdemServico { get; set; }
        
        // ID do usuário que enviou a mensagem
        [Required]
        public string UsuarioId { get; set; } = string.Empty;
        
        // Navegação para o usuário
        [ForeignKey("UsuarioId")]
        public virtual ApplicationUser? Usuario { get; set; }
        
        // Conteúdo da mensagem
        [Required(ErrorMessage = "O conteúdo da mensagem é obrigatório")]
        [StringLength(1000, ErrorMessage = "A mensagem deve ter no máximo 1000 caracteres")]
        public string Conteudo { get; set; } = string.Empty;
        
        // Data e hora do envio da mensagem
        // Data de envio da mensagem (horário brasileiro UTC-3)
    public DateTime DataEnvio { get; set; } = DateTime.UtcNow.AddHours(-3);
        
        // Indica se a mensagem foi lida
        public bool Lida { get; set; } = false;
        
        // Coleção de anexos relacionados a esta mensagem
        public virtual ICollection<Anexo> Anexos { get; set; } = new List<Anexo>();
    }
}