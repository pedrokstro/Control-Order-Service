using System;
using System.Collections.Generic;

namespace OrdemServicoMVC.TempModels;

public partial class Anexo
{
    public int Id { get; set; }

    public string NomeArquivo { get; set; } = null!;

    public string TipoMime { get; set; } = null!;

    public int TamanhoBytes { get; set; }

    public byte[] DadosArquivo { get; set; } = null!;

    public string DataUpload { get; set; } = null!;

    public int? OrdemServicoId { get; set; }

    public int? MensagemId { get; set; }

    public string UsuarioId { get; set; } = null!;

    public virtual Mensagen? Mensagem { get; set; }

    public virtual OrdensServico? OrdemServico { get; set; }

    public virtual AspNetUser Usuario { get; set; } = null!;
}
