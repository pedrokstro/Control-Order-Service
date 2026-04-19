using Microsoft.AspNetCore.Mvc.Rendering;
using OrdemServicoMVC.Models;
using System.ComponentModel.DataAnnotations;

namespace OrdemServicoMVC.ViewModels
{
    // ViewModel para a página principal de ordens de serviço
    public class OrdemServicoIndexViewModel
    {
        public List<OrdemServico> OrdensServico { get; set; } = new List<OrdemServico>();
        public int PaginaAtual { get; set; }
        public int TotalPaginas { get; set; }
        public int TotalItens { get; set; }
        
        // Propriedades auxiliares para paginação
        public bool HasPreviousPage => PaginaAtual > 1;
        public bool HasNextPage => PaginaAtual < TotalPaginas;
        
        // Listas para dropdowns (apenas para admin)
        public List<SelectListItem> StatusList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> TecnicosList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> LojasList { get; set; } = new List<SelectListItem>();

        /// <summary>
        /// Mapa de OrdemId → total de anexos (diretos na OS + via mensagens do chat).
        /// Usado para exibir o badge de anexos na listagem sem precisar carregar os dados binários.
        /// </summary>
        public Dictionary<int, int> AnexosCount { get; set; } = new Dictionary<int, int>();

        /// <summary>
        /// Mapa de OrdemId → total de mensagens não lidas.
        /// </summary>
        public Dictionary<int, int> MensagensNaoLidasCount { get; set; } = new Dictionary<int, int>();
    }

    // ViewModel para criação de ordem de serviço
    public class OrdemServicoCreateViewModel
    {
        [Required(ErrorMessage = "O título é obrigatório")]
        [StringLength(200, ErrorMessage = "O título deve ter no máximo 200 caracteres")]
        [Display(Name = "Título")]
        public string Titulo { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "A descrição é obrigatória")]
        [StringLength(1000, ErrorMessage = "A descrição deve ter no máximo 1000 caracteres")]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; } = string.Empty;
        
        // Prioridade opcional - apenas administradores podem definir
        [Display(Name = "Prioridade")]
        public PrioridadeEnum? Prioridade { get; set; }
        
        [Required(ErrorMessage = "O setor é obrigatório")]
        [Display(Name = "Setor")]
        public SetorEnum Setor { get; set; }
        
        [Display(Name = "Técnico Responsável")]
        public string? TecnicoResponsavelId { get; set; }
        
        [StringLength(500, ErrorMessage = "As observações devem ter no máximo 500 caracteres")]
        [Display(Name = "Observações")]
        public string? Observacoes { get; set; }
        
        // Lista de técnicos para seleção (apenas para admin)
        public List<SelectListItem> Tecnicos { get; set; } = new List<SelectListItem>();
    }

    // ViewModel para edição de ordem de serviço
    public class OrdemServicoEditViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "O título é obrigatório")]
        [StringLength(200, ErrorMessage = "O título deve ter no máximo 200 caracteres")]
        [Display(Name = "Título")]
        public string Titulo { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "A descrição é obrigatória")]
        [StringLength(1000, ErrorMessage = "A descrição deve ter no máximo 1000 caracteres")]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "A prioridade é obrigatória")]
        [Display(Name = "Prioridade")]
        public PrioridadeEnum Prioridade { get; set; }
        
        [Required(ErrorMessage = "O status é obrigatório")]
        [Display(Name = "Status")]
        public StatusEnum Status { get; set; }
        
        // Setor da ordem de serviço
        [Required(ErrorMessage = "O setor é obrigatório")]
        [Display(Name = "Setor")]
        public SetorEnum Setor { get; set; }
        
        [Display(Name = "Técnico Responsável")]
        public string? TecnicoResponsavelId { get; set; }
        
        [StringLength(500, ErrorMessage = "As observações devem ter no máximo 500 caracteres")]
        [Display(Name = "Observações")]
        public string? Observacoes { get; set; }
        
        // Propriedades para exibição de informações históricas
        public DateTime DataCriacao { get; set; }
        public DateTime? DataConclusao { get; set; }
        
        // Lista de técnicos para seleção
        public List<SelectListItem> Tecnicos { get; set; } = new List<SelectListItem>();
    }
}