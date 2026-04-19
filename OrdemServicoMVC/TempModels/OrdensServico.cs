using System;
using System.Collections.Generic;

namespace OrdemServicoMVC.TempModels;

public partial class OrdensServico
{
    public int Id { get; set; }

    public string Titulo { get; set; } = null!;

    public string Descricao { get; set; } = null!;

    public int Prioridade { get; set; }

    public int Status { get; set; }

    public DateTime DataCriacao { get; set; }

    public string? DataConclusao { get; set; }

    public string UsuarioCriadorId { get; set; } = null!;

    public string? TecnicoResponsavelId { get; set; }

    public string? Observacoes { get; set; }

    public int Setor { get; set; }

    public virtual ICollection<Anexo> Anexos { get; set; } = new List<Anexo>();

    public virtual ICollection<Mensagen> Mensagens { get; set; } = new List<Mensagen>();

    public virtual AspNetUser? TecnicoResponsavel { get; set; }

    public virtual AspNetUser UsuarioCriador { get; set; } = null!;
}
