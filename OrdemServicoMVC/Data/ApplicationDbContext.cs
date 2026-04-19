using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OrdemServicoMVC.Models;

namespace OrdemServicoMVC.Data
{
    // Contexto do banco de dados que herda de IdentityDbContext
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        // DbSet para as ordens de serviço
        public DbSet<OrdemServico> OrdensServico { get; set; }
        
        // DbSet para as mensagens
        public DbSet<Mensagem> Mensagens { get; set; }
        
        // DbSet para os anexos
        public DbSet<Anexo> Anexos { get; set; }
        
        // Configurações adicionais do modelo
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            // Detecta se estamos usando SQL Server para ajustar comportamento de cascade
            var isSqlServer = Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer";
            
            // Configuração da entidade OrdemServico
            builder.Entity<OrdemServico>(entity =>
            {
                // Configuração da chave primária
                entity.HasKey(e => e.Id);
                
                // Configuração do relacionamento com o usuário criador
                entity.HasOne(e => e.UsuarioCriador)
                    .WithMany()
                    .HasForeignKey(e => e.UsuarioCriadorId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Configuração do relacionamento com o técnico responsável
                entity.HasOne(e => e.TecnicoResponsavel)
                    .WithMany()
                    .HasForeignKey(e => e.TecnicoResponsavelId)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // Configuração de índices para melhor performance
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.DataCriacao);
                entity.HasIndex(e => e.UsuarioCriadorId);
                
                // Índices compostos para otimização de consultas frequentes
                entity.HasIndex(e => new { e.DataCriacao, e.Status })
                    .HasDatabaseName("IX_OrdensServico_DataCriacao_Status");
                entity.HasIndex(e => new { e.TecnicoResponsavelId, e.Status })
                    .HasDatabaseName("IX_OrdensServico_TecnicoResponsavelId_Status");
                entity.HasIndex(e => new { e.UsuarioCriadorId, e.DataCriacao })
                    .HasDatabaseName("IX_OrdensServico_UsuarioCriadorId_DataCriacao");
                entity.HasIndex(e => new { e.Prioridade, e.Status })
                    .HasDatabaseName("IX_OrdensServico_Prioridade_Status");
                entity.HasIndex(e => new { e.DataConclusao, e.Status })
                    .HasDatabaseName("IX_OrdensServico_DataConclusao_Status");
            });
            
            // Configuração da entidade Mensagem
            builder.Entity<Mensagem>(entity =>
            {
                // Configuração da chave primária
                entity.HasKey(e => e.Id);
                
                // Configuração do relacionamento com a ordem de serviço
                entity.HasOne(e => e.OrdemServico)
                    .WithMany(o => o.Mensagens)
                    .HasForeignKey(e => e.OrdemServicoId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Configuração do relacionamento com o usuário
                entity.HasOne(e => e.Usuario)
                    .WithMany()
                    .HasForeignKey(e => e.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Configuração de índices para melhor performance
                entity.HasIndex(e => e.OrdemServicoId);
                entity.HasIndex(e => e.DataEnvio);
                
                // Índice composto para otimização de consultas por ordem e data
                entity.HasIndex(e => new { e.OrdemServicoId, e.DataEnvio })
                    .HasDatabaseName("IX_Mensagens_OrdemServicoId_DataEnvio");
            });
            
            // Configuração da entidade Anexo
            builder.Entity<Anexo>(entity =>
            {
                // Configuração da chave primária
                entity.HasKey(e => e.Id);
                
                // Configuração do relacionamento com a ordem de serviço
                // SQL Server não permite múltiplos caminhos de cascade delete
                // (Anexo→OrdemServico e Anexo→Mensagem→OrdemServico criam ciclo)
                entity.HasOne(e => e.OrdemServico)
                    .WithMany(o => o.Anexos)
                    .HasForeignKey(e => e.OrdemServicoId)
                    .OnDelete(isSqlServer ? DeleteBehavior.NoAction : DeleteBehavior.Cascade);
                
                // Configuração do relacionamento com a mensagem
                entity.HasOne(e => e.Mensagem)
                    .WithMany(m => m.Anexos)
                    .HasForeignKey(e => e.MensagemId)
                    .OnDelete(isSqlServer ? DeleteBehavior.NoAction : DeleteBehavior.Cascade);
                
                // Configuração do relacionamento com o usuário
                entity.HasOne(e => e.Usuario)
                    .WithMany()
                    .HasForeignKey(e => e.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Configuração de índices para melhor performance
                entity.HasIndex(e => e.OrdemServicoId);
                entity.HasIndex(e => e.MensagemId);
                entity.HasIndex(e => e.DataUpload);
                
                // Índice composto para otimização de consultas por ordem e data
                entity.HasIndex(e => new { e.OrdemServicoId, e.DataUpload })
                    .HasDatabaseName("IX_Anexos_OrdemServicoId_DataUpload");
            });
            
            // Configuração da entidade ApplicationUser
            builder.Entity<ApplicationUser>(entity =>
            {
                // Configuração de propriedades específicas
                entity.Property(e => e.NomeLoja)
                    .HasMaxLength(100);
            });
        }
    }
}