using System;
using System.Collections.Generic;

namespace OrdemServicoMVC.TempModels;

public partial class Mensagen
{
    public int Id { get; set; }

    public int OrdemServicoId { get; set; }

    public string UsuarioId { get; set; } = null!;

    public string Conteudo { get; set; } = null!;

    public string DataEnvio { get; set; } = null!;

    public int Lida { get; set; }

    public virtual ICollection<Anexo> Anexos { get; set; } = new List<Anexo>();

    public virtual OrdensServico OrdemServico { get; set; } = null!;

    public virtual AspNetUser Usuario { get; set; } = null!;
}
