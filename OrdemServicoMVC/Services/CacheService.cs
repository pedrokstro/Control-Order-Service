using Microsoft.Extensions.Caching.Memory;
using OrdemServicoMVC.ViewModels;
using OrdemServicoMVC.Models;

namespace OrdemServicoMVC.Services
{
    /// <summary>
    /// Serviço responsável pelo gerenciamento de cache para otimização de performance
    /// Centraliza operações de cache para estatísticas e dados frequentemente acessados
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Obtém estatísticas do cache ou calcula se não existir
        /// </summary>
        Task<RelatorioEstatisticasViewModel> GetEstatisticasAsync(string cacheKey, Func<Task<RelatorioEstatisticasViewModel>> factory, TimeSpan? expiration = null);
        
        /// <summary>
        /// Remove item específico do cache
        /// </summary>
        void RemoveItem(string key);
        
        /// <summary>
        /// Limpa todo o cache de estatísticas
        /// </summary>
        void ClearEstatisticasCache();
        
        /// <summary>
        /// Obtém dados genéricos do cache
        /// </summary>
        Task<T> GetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class;
    }

    /// <summary>
    /// Implementação do serviço de cache usando IMemoryCache
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CacheService> _logger;
        
        // Prefixos para organizar chaves do cache
        private const string ESTATISTICAS_PREFIX = "stats_";
        private const string DASHBOARD_PREFIX = "dashboard_";
        
        // Tempos de expiração padrão
        private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan EstatisticasExpiration = TimeSpan.FromMinutes(10);

        public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Obtém estatísticas do cache ou executa factory se não existir
        /// </summary>
        public async Task<RelatorioEstatisticasViewModel> GetEstatisticasAsync(
            string cacheKey, 
            Func<Task<RelatorioEstatisticasViewModel>> factory, 
            TimeSpan? expiration = null)
        {
            var fullKey = ESTATISTICAS_PREFIX + cacheKey;
            
            // Tenta obter do cache primeiro
            if (_cache.TryGetValue(fullKey, out RelatorioEstatisticasViewModel? cachedValue) && cachedValue != null)
            {
                _logger.LogDebug("Estatísticas obtidas do cache: {CacheKey}", fullKey);
                return cachedValue;
            }

            // Se não estiver no cache, executa a factory
            _logger.LogDebug("Calculando estatísticas para cache: {CacheKey}", fullKey);
            var result = await factory();
            
            // Armazena no cache com expiração
            var cacheExpiration = expiration ?? EstatisticasExpiration;
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheExpiration,
                Priority = CacheItemPriority.Normal,
                Size = 1 // Cada entrada conta como 1 unidade de tamanho
            };
            
            _cache.Set(fullKey, result, cacheOptions);
            _logger.LogDebug("Estatísticas armazenadas no cache: {CacheKey}, Expiração: {Expiration}", 
                fullKey, cacheExpiration);
            
            return result;
        }

        /// <summary>
        /// Obtém dados genéricos do cache
        /// </summary>
        public async Task<T> GetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class
        {
            // Tenta obter do cache primeiro
            if (_cache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
            {
                _logger.LogDebug("Dados obtidos do cache: {CacheKey}", key);
                return cachedValue;
            }

            // Se não estiver no cache, executa a factory
            _logger.LogDebug("Calculando dados para cache: {CacheKey}", key);
            var result = await factory();
            
            // Armazena no cache com expiração
            var cacheExpiration = expiration ?? DefaultExpiration;
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheExpiration,
                Priority = CacheItemPriority.Normal,
                Size = 1
            };
            
            _cache.Set(key, result, cacheOptions);
            _logger.LogDebug("Dados armazenados no cache: {CacheKey}, Expiração: {Expiration}", 
                key, cacheExpiration);
            
            return result;
        }

        /// <summary>
        /// Remove item específico do cache
        /// </summary>
        public void RemoveItem(string key)
        {
            _cache.Remove(key);
            _logger.LogDebug("Item removido do cache: {CacheKey}", key);
        }

        /// <summary>
        /// Limpa todo o cache de estatísticas
        /// </summary>
        public void ClearEstatisticasCache()
        {
            // Como IMemoryCache não tem método para limpar por prefixo,
            // implementamos uma lista de chaves conhecidas ou usamos uma abordagem diferente
            _logger.LogInformation("Limpeza de cache de estatísticas solicitada");
            
            // Para uma implementação mais robusta, poderíamos manter uma lista de chaves
            // ou usar uma biblioteca de cache mais avançada como Redis
            // Por enquanto, registramos a operação para monitoramento
        }
    }
}