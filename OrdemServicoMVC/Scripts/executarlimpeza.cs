using Microsoft.EntityFrameworkCore;
using OrdemServicoMVC.Data;
using OrdemServicoMVC.Models;

namespace OrdemServicoMVC.Scripts
{
    /// <summary>
    /// Script para limpeza completa de todas as ordens de serviço do banco de dados
    /// Remove ordens, mensagens e anexos relacionados
    /// </summary>
    public class ExecutarLimpeza
    {
        private readonly ApplicationDbContext _context;

        public ExecutarLimpeza(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Executa a limpeza completa de todas as ordens de serviço
        /// Remove em ordem: Mensagens -> Anexos -> Ordens de Serviço
        /// </summary>
        /// <returns>Número total de registros removidos</returns>
        public async Task<int> LimparTodasOrdensServicoAsync()
        {
            int totalRemovidos = 0;

            try
            {
                // Inicia uma transação para garantir consistência
                using var transaction = await _context.Database.BeginTransactionAsync();

                // 1. Remove todas as mensagens relacionadas às ordens de serviço
                var mensagens = await _context.Mensagens.ToListAsync();
                if (mensagens.Any())
                {
                    _context.Mensagens.RemoveRange(mensagens);
                    totalRemovidos += mensagens.Count;
                    Console.WriteLine($"Removidas {mensagens.Count} mensagens");
                }

                // 2. Remove todos os anexos relacionados às ordens de serviço
                var anexos = await _context.Anexos.ToListAsync();
                if (anexos.Any())
                {
                    _context.Anexos.RemoveRange(anexos);
                    totalRemovidos += anexos.Count;
                    Console.WriteLine($"Removidos {anexos.Count} anexos");
                }

                // 3. Remove todas as ordens de serviço
                var ordensServico = await _context.OrdensServico.ToListAsync();
                if (ordensServico.Any())
                {
                    _context.OrdensServico.RemoveRange(ordensServico);
                    totalRemovidos += ordensServico.Count;
                    Console.WriteLine($"Removidas {ordensServico.Count} ordens de serviço");
                }

                // Salva todas as alterações no banco de dados
                await _context.SaveChangesAsync();

                // Confirma a transação
                await transaction.CommitAsync();

                Console.WriteLine($"\nLimpeza concluída com sucesso! Total de registros removidos: {totalRemovidos}");
                return totalRemovidos;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro durante a limpeza: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Executa a limpeza e reseta os contadores de ID (IDENTITY)
        /// Apenas para SQLite - reseta a sequência de IDs
        /// </summary>
        /// <returns>Número total de registros removidos</returns>
        public async Task<int> LimparEResetarContadoresAsync()
        {
            int totalRemovidos = await LimparTodasOrdensServicoAsync();

            try
            {
                // Reset dos contadores de ID para SQLite
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence WHERE name IN ('OrdensServico', 'Mensagens', 'Anexos')");
                Console.WriteLine("Contadores de ID resetados com sucesso!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Aviso: Não foi possível resetar os contadores de ID: {ex.Message}");
                // Não relança a exceção pois a limpeza principal foi bem-sucedida
            }

            return totalRemovidos;
        }

        /// <summary>
        /// Verifica quantos registros existem antes da limpeza
        /// </summary>
        /// <returns>Relatório com contagem de registros</returns>
        public async Task<string> VerificarRegistrosAsync()
        {
            var countOrdens = await _context.OrdensServico.CountAsync();
            var countMensagens = await _context.Mensagens.CountAsync();
            var countAnexos = await _context.Anexos.CountAsync();

            return $"Registros encontrados:\n" +
                   $"- Ordens de Serviço: {countOrdens}\n" +
                   $"- Mensagens: {countMensagens}\n" +
                   $"- Anexos: {countAnexos}\n" +
                   $"Total: {countOrdens + countMensagens + countAnexos}";
        }
    }
}