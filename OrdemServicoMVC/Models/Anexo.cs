using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrdemServicoMVC.Models
{
    // Modelo para representar anexos/arquivos
    public class Anexo
    {
        // Chave primária do anexo
        public int Id { get; set; }
        
        // Nome original do arquivo
        [Required(ErrorMessage = "O nome do arquivo é obrigatório")]
        [StringLength(255, ErrorMessage = "O nome do arquivo deve ter no máximo 255 caracteres")]
        public string NomeArquivo { get; set; } = string.Empty;
        
        // Tipo MIME do arquivo
        [Required]
        [StringLength(100)]
        public string TipoMime { get; set; } = string.Empty;
        
        // Tamanho do arquivo em bytes
        public long TamanhoBytes { get; set; }
        
        // Dados binários do arquivo
        [Required]
        public byte[] DadosArquivo { get; set; } = Array.Empty<byte>();
        
        // Data de upload
        // Data de upload do anexo (horário brasileiro UTC-3)
    public DateTime DataUpload { get; set; } = DateTime.UtcNow.AddHours(-3);
        
        // ID da ordem de serviço (opcional - para anexos da ordem)
        public int? OrdemServicoId { get; set; }
        
        // Navegação para a ordem de serviço
        [ForeignKey("OrdemServicoId")]
        public virtual OrdemServico? OrdemServico { get; set; }
        
        // ID da mensagem (opcional - para anexos do chat)
        public int? MensagemId { get; set; }
        
        // Navegação para a mensagem
        [ForeignKey("MensagemId")]
        public virtual Mensagem? Mensagem { get; set; }
        
        // ID do usuário que fez o upload
        [Required]
        public string UsuarioId { get; set; } = string.Empty;
        
        // Navegação para o usuário
        [ForeignKey("UsuarioId")]
        public virtual ApplicationUser? Usuario { get; set; }
    }
}