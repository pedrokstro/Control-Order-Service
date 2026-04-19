using System;
using System.Collections.Generic;
using System.Linq;

namespace OrdemServicoMVC.ViewModels
{
    public class PainelMensagensViewModel
    {
        public List<PainelMensagemItemViewModel> Conversas { get; set; } = new();

        public int TotalNaoLidas => Conversas.Sum(c => c.NaoLidas);
    }

    public class PainelMensagemItemViewModel
    {
        public int OrdemId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Loja { get; set; } = string.Empty;
        public string Tecnico { get; set; } = "Não atribuído";
        public string Prioridade { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? UltimaMensagem { get; set; }
        public string UltimoAutor { get; set; } = string.Empty;
        public string UltimaMensagemPreview { get; set; } = string.Empty;
        public int NaoLidas { get; set; }
    }
}
