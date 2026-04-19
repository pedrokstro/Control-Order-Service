using Microsoft.AspNetCore.Mvc.Rendering;
using OrdemServicoMVC.Models;
using System.ComponentModel.DataAnnotations;

namespace OrdemServicoMVC.ViewModels
{
    // ViewModel para filtros de relatórios personalizáveis
    public class RelatorioFiltroViewModel
    {
        // Filtro por período - data inicial
        [Display(Name = "Data Inicial")]
        [DataType(DataType.Date)]
        public DateTime? DataInicio { get; set; }
        
        // Filtro por período - data final
        [Display(Name = "Data Final")]
        [DataType(DataType.Date)]
        public DateTime? DataFim { get; set; }
        
        // Filtro por técnico responsável
        [Display(Name = "Técnico")]
        public string? TecnicoId { get; set; }
        
        // Filtro por loja
        [Display(Name = "Loja")]
        public string? Loja { get; set; }
        
        // Filtro por status da ordem
        [Display(Name = "Status")]
        public int? Status { get; set; }
        
        // Filtro por prioridade
        [Display(Name = "Prioridade")]
        public int? Prioridade { get; set; }
        
        // Tipo de exportação (Excel ou PDF)
        [Display(Name = "Formato de Exportação")]
        public string TipoExportacao { get; set; } = "excel";
        
        // Listas para dropdowns
        public List<SelectListItem> TecnicosList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> LojasList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> StatusList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> PrioridadeList { get; set; } = new List<SelectListItem>();
    }
    
    // ViewModel para exibir dados do relatório
    public class RelatorioResultadoViewModel
    {
        // Lista de ordens de serviço filtradas
        public List<OrdemServico> OrdensServico { get; set; } = new List<OrdemServico>();
        
        // Filtros aplicados (para exibição)
        public RelatorioFiltroViewModel Filtros { get; set; } = new RelatorioFiltroViewModel();
        
        // Estatísticas do relatório
        public RelatorioEstatisticasViewModel Estatisticas { get; set; } = new RelatorioEstatisticasViewModel();
    }
    
    // ViewModel para estatísticas do relatório
    public class RelatorioEstatisticasViewModel
    {
        // Total de ordens no período
        public int TotalOrdens { get; set; }
        
        // Ordens por status
        public int OrdensAbertas { get; set; }
        public int OrdensEmAndamento { get; set; }
        public int OrdensConcluidas { get; set; }
        
        // Ordens por prioridade
        public int OrdensPrioridadeAlta { get; set; }
        public int OrdensPrioridadeMedia { get; set; }
        public int OrdensPrioridadeBaixa { get; set; }
        
        // Tempo médio de conclusão (em dias)
        public double? TempoMedioConclusao { get; set; }
        
        // Técnico com mais ordens concluídas
        public string? TecnicoMaisAtivo { get; set; }
        public int OrdensDoTecnicoMaisAtivo { get; set; }
        
        // Loja com mais ordens
        public string? LojaMaisAtiva { get; set; }
        public int OrdensDaLojaMaisAtiva { get; set; }
        
        // Dados para gráficos
        public List<LojaAtividadeViewModel> LojasAtividade { get; set; } = new List<LojaAtividadeViewModel>();
        public List<ProblemaFrequenteViewModel> ProblemasFrequentes { get; set; } = new List<ProblemaFrequenteViewModel>();
        public List<SetorAtividadeViewModel> SetoresAtividade { get; set; } = new List<SetorAtividadeViewModel>();
    }
    
    // ViewModel para atividade por loja (gráfico)
    public class LojaAtividadeViewModel
    {
        public string NomeLoja { get; set; } = string.Empty;
        public int TotalOrdens { get; set; }
        public int OrdensAbertas { get; set; }
        public int OrdensEmAndamento { get; set; }
        public int OrdensConcluidas { get; set; }
        public double PercentualTotal { get; set; }
    }
    
    // ViewModel para problemas mais frequentes (gráfico)
    public class ProblemaFrequenteViewModel
    {
        public string Descricao { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public double PercentualTotal { get; set; }
        public List<string> LojasAfetadas { get; set; } = new List<string>();
        public string SetorMaisAfetado { get; set; } = string.Empty;
    }
    
    // ViewModel para atividade por setor (gráfico)
    public class SetorAtividadeViewModel
    {
        public string NomeSetor { get; set; } = string.Empty;
        public int TotalOrdens { get; set; }
        public double TempoMedioConclusao { get; set; }
        public double PercentualTotal { get; set; }
    }
    
    // ViewModel para dados de gráficos (formato Chart.js)
    public class GraficoDataViewModel
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<int> Data { get; set; } = new List<int>();
        public List<string> BackgroundColors { get; set; } = new List<string>();
        public List<string> BorderColors { get; set; } = new List<string>();
    }
}