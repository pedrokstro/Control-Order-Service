using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrdemServicoMVC.Models
{
    // Modelo para representar uma ordem de serviço
    public class OrdemServico
    {
        // Chave primária da ordem de serviço
        public int Id { get; set; }
        
        // Título da ordem de serviço
        [Required(ErrorMessage = "O título é obrigatório")]
        [StringLength(200, ErrorMessage = "O título deve ter no máximo 200 caracteres")]
        public string Titulo { get; set; } = string.Empty;
        
        // Descrição detalhada da ordem de serviço
        [Required(ErrorMessage = "A descrição é obrigatória")]
        [StringLength(1000, ErrorMessage = "A descrição deve ter no máximo 1000 caracteres")]
        public string Descricao { get; set; } = string.Empty;
        
        // Prioridade da ordem (Alta, Média, Baixa)
        [Required(ErrorMessage = "A prioridade é obrigatória")]
        public PrioridadeEnum Prioridade { get; set; }
        
        // Status da ordem (Aberta, Em Andamento, Concluída)
        public StatusEnum Status { get; set; } = StatusEnum.Aberta;
        
        // Data de criação da ordem (horário brasileiro UTC-3)
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow.AddHours(-3);
        
        // Data de conclusão da ordem (opcional)
        public DateTime? DataConclusao { get; set; }
        
        // ID do usuário que criou a ordem
        [Required]
        public string UsuarioCriadorId { get; set; } = string.Empty;
        
        // Navegação para o usuário criador
        [ForeignKey("UsuarioCriadorId")]
        public virtual ApplicationUser? UsuarioCriador { get; set; }
        
        // ID do técnico responsável (opcional)
        public string? TecnicoResponsavelId { get; set; }
        
        // Navegação para o técnico responsável
        [ForeignKey("TecnicoResponsavelId")]
        public virtual ApplicationUser? TecnicoResponsavel { get; set; }
        
        // Setor responsável pela ordem de serviço
        [Required(ErrorMessage = "O setor é obrigatório")]
        public SetorEnum Setor { get; set; }
        
        // Observações adicionais
        [StringLength(500, ErrorMessage = "As observações devem ter no máximo 500 caracteres")]
        public string? Observacoes { get; set; }
        
        // Coleção de mensagens relacionadas a esta ordem de serviço
        public virtual ICollection<Mensagem> Mensagens { get; set; } = new List<Mensagem>();
        
        // Coleção de anexos relacionados a esta ordem de serviço
        public virtual ICollection<Anexo> Anexos { get; set; } = new List<Anexo>();
    }
    
    // Enum para definir as prioridades possíveis
    public enum PrioridadeEnum
    {
        Baixa = 1,
        Media = 2,
        Alta = 3
    }
    
    // Enum para definir os status possíveis
    public enum StatusEnum
    {
        Aberta = 1,
        EmAndamento = 2,
        Concluida = 3
    }
    
    // Enum para definir os setores possíveis
    public enum SetorEnum
    {
        FrenteDeCaixa = 1,
        Financeiro = 2,
        Faturamento = 3,
        Administrativo = 4,
        Rh = 5,
        Prevencao = 6,
        Gerencia = 7,
        Contabilidade = 8,
        Patrimonio = 9,
        Compras = 10  // Novo setor adicionado
    }
}