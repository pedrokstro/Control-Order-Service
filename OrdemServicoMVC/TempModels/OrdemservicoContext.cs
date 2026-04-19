using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace OrdemServicoMVC.TempModels;

public partial class OrdemservicoContext : DbContext
{
    public OrdemservicoContext()
    {
    }

    public OrdemservicoContext(DbContextOptions<OrdemservicoContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Anexo> Anexos { get; set; }

    public virtual DbSet<AspNetRole> AspNetRoles { get; set; }

    public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; }

    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }

    public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }

    public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; }

    public virtual DbSet<EfmigrationsLock> EfmigrationsLocks { get; set; }

    public virtual DbSet<Mensagen> Mensagens { get; set; }

    public virtual DbSet<OrdensServico> OrdensServicos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlite("Data Source=ordemservico.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Anexo>(entity =>
        {
            entity.HasIndex(e => e.DataUpload, "IX_Anexos_DataUpload");

            entity.HasIndex(e => e.MensagemId, "IX_Anexos_MensagemId");

            entity.HasIndex(e => e.OrdemServicoId, "IX_Anexos_OrdemServicoId");

            entity.HasIndex(e => e.UsuarioId, "IX_Anexos_UsuarioId");

            entity.HasOne(d => d.Mensagem).WithMany(p => p.Anexos)
                .HasForeignKey(d => d.MensagemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.OrdemServico).WithMany(p => p.Anexos)
                .HasForeignKey(d => d.OrdemServicoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Usuario).WithMany(p => p.Anexos)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AspNetRole>(entity =>
        {
            entity.HasIndex(e => e.NormalizedName, "RoleNameIndex").IsUnique();
        });

        modelBuilder.Entity<AspNetRoleClaim>(entity =>
        {
            entity.HasIndex(e => e.RoleId, "IX_AspNetRoleClaims_RoleId");

            entity.HasOne(d => d.Role).WithMany(p => p.AspNetRoleClaims).HasForeignKey(d => d.RoleId);
        });

        modelBuilder.Entity<AspNetUser>(entity =>
        {
            entity.HasIndex(e => e.NormalizedEmail, "EmailIndex");

            entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex").IsUnique();

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "AspNetUserRole",
                    r => r.HasOne<AspNetRole>().WithMany().HasForeignKey("RoleId"),
                    l => l.HasOne<AspNetUser>().WithMany().HasForeignKey("UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("AspNetUserRoles");
                        j.HasIndex(new[] { "RoleId" }, "IX_AspNetUserRoles_RoleId");
                    });
        });

        modelBuilder.Entity<AspNetUserClaim>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_AspNetUserClaims_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserClaims).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetUserLogin>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

            entity.HasIndex(e => e.UserId, "IX_AspNetUserLogins_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserLogins).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetUserToken>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserTokens).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<EfmigrationsLock>(entity =>
        {
            entity.ToTable("__EFMigrationsLock");

            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<Mensagen>(entity =>
        {
            entity.HasIndex(e => e.DataEnvio, "IX_Mensagens_DataEnvio");

            entity.HasIndex(e => e.OrdemServicoId, "IX_Mensagens_OrdemServicoId");

            entity.HasIndex(e => e.UsuarioId, "IX_Mensagens_UsuarioId");

            entity.HasOne(d => d.OrdemServico).WithMany(p => p.Mensagens).HasForeignKey(d => d.OrdemServicoId);

            entity.HasOne(d => d.Usuario).WithMany(p => p.Mensagens)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrdensServico>(entity =>
        {
            entity.ToTable("OrdensServico");

            entity.HasIndex(e => e.DataCriacao, "IX_OrdensServico_DataCriacao");

            entity.HasIndex(e => e.Status, "IX_OrdensServico_Status");

            entity.HasIndex(e => e.TecnicoResponsavelId, "IX_OrdensServico_TecnicoResponsavelId");

            entity.HasIndex(e => e.UsuarioCriadorId, "IX_OrdensServico_UsuarioCriadorId");

            entity.HasOne(d => d.TecnicoResponsavel).WithMany(p => p.OrdensServicoTecnicoResponsavels)
                .HasForeignKey(d => d.TecnicoResponsavelId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.UsuarioCriador).WithMany(p => p.OrdensServicoUsuarioCriadors)
                .HasForeignKey(d => d.UsuarioCriadorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
