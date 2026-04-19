using System;
using System.Collections.Generic;

namespace OrdemServicoMVC.TempModels;

public partial class AspNetUser
{
    public string Id { get; set; } = null!;

    public string? NomeLoja { get; set; }

    public int IsAdmin { get; set; }

    public DateTime DataCriacao { get; set; }

    public string? UserName { get; set; }

    public string? NormalizedUserName { get; set; }

    public string? Email { get; set; }

    public string? NormalizedEmail { get; set; }

    public int EmailConfirmed { get; set; }

    public string? PasswordHash { get; set; }

    public string? SecurityStamp { get; set; }

    public string? ConcurrencyStamp { get; set; }

    public string? PhoneNumber { get; set; }

    public int PhoneNumberConfirmed { get; set; }

    public int TwoFactorEnabled { get; set; }

    public string? LockoutEnd { get; set; }

    public int LockoutEnabled { get; set; }

    public int AccessFailedCount { get; set; }

    public int IsTecnico { get; set; }

    public virtual ICollection<Anexo> Anexos { get; set; } = new List<Anexo>();

    public virtual ICollection<AspNetUserClaim> AspNetUserClaims { get; set; } = new List<AspNetUserClaim>();

    public virtual ICollection<AspNetUserLogin> AspNetUserLogins { get; set; } = new List<AspNetUserLogin>();

    public virtual ICollection<AspNetUserToken> AspNetUserTokens { get; set; } = new List<AspNetUserToken>();

    public virtual ICollection<Mensagen> Mensagens { get; set; } = new List<Mensagen>();

    public virtual ICollection<OrdensServico> OrdensServicoTecnicoResponsavels { get; set; } = new List<OrdensServico>();

    public virtual ICollection<OrdensServico> OrdensServicoUsuarioCriadors { get; set; } = new List<OrdensServico>();

    public virtual ICollection<AspNetRole> Roles { get; set; } = new List<AspNetRole>();
}
