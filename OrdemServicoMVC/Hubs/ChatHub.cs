using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace OrdemServicoMVC.Hubs
{
    /// <summary>
    /// Hub do SignalR para gerenciar comunicação em tempo real do chat
    /// </summary>
    // [Authorize] // Temporariamente removido para resolver erro 404 na negociação SignalR
    public class ChatHub : Hub
    {
        /// <summary>
        /// Método chamado quando um cliente se conecta ao hub
        /// </summary>
        /// <returns></returns>
        public override async Task OnConnectedAsync()
        {
            // Adiciona o usuário ao grupo baseado no seu ID
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            }
            
            // Notifica outros usuários que alguém se conectou
            await Clients.Others.SendAsync("UserConnected", Context.User?.Identity?.Name ?? "Usuário");
            
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Método chamado quando um cliente se desconecta do hub
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Remove o usuário do grupo
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
            }
            
            // Notifica outros usuários que alguém se desconectou
            await Clients.Others.SendAsync("UserDisconnected", Context.User?.Identity?.Name ?? "Usuário");
            
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Envia uma mensagem para todos os usuários conectados
        /// </summary>
        /// <param name="message">Conteúdo da mensagem</param>
        /// <returns></returns>
        public async Task SendMessage(string message)
        {
            var userName = Context.User?.Identity?.Name ?? "Usuário Anônimo";
            // Gera timestamp com horário brasileiro (UTC-3)
            var timestamp = DateTime.UtcNow.AddHours(-3).ToString("dd/MM/yyyy às HH:mm");
            
            // Envia a mensagem para todos os clientes conectados
            await Clients.All.SendAsync("ReceiveMessage", userName, message, timestamp);
        }

        /// <summary>
        /// Envia uma mensagem para um usuário específico
        /// </summary>
        /// <param name="userId">ID do usuário destinatário</param>
        /// <param name="message">Conteúdo da mensagem</param>
        /// <returns></returns>
        public async Task SendMessageToUser(string userId, string message)
        {
            var userName = Context.User?.Identity?.Name ?? "Usuário Anônimo";
            // Gera timestamp com horário brasileiro (UTC-3)
            var timestamp = DateTime.UtcNow.AddHours(-3).ToString("dd/MM/yyyy às HH:mm");
            
            // Envia a mensagem para o usuário específico
            await Clients.Group($"User_{userId}").SendAsync("ReceivePrivateMessage", userName, message, timestamp);
        }

        /// <summary>
        /// Notifica sobre uma nova ordem de serviço criada
        /// </summary>
        /// <param name="ordemId">ID da ordem de serviço</param>
        /// <param name="titulo">Título da ordem</param>
        /// <returns></returns>
        public async Task NotifyNewOrder(int ordemId, string titulo)
        {
            // Notifica todos os usuários sobre a nova ordem
            // Notifica com horário brasileiro (UTC-3)
            await Clients.All.SendAsync("NewOrderCreated", ordemId, titulo, DateTime.UtcNow.AddHours(-3).ToString("dd/MM/yyyy às HH:mm"));
        }

        /// <summary>
        /// Notifica sobre atualização no status de uma ordem de serviço
        /// </summary>
        /// <param name="ordemId">ID da ordem de serviço</param>
        /// <param name="novoStatus">Novo status da ordem</param>
        /// <returns></returns>
        public async Task NotifyOrderStatusUpdate(int ordemId, string novoStatus)
        {
            // Notifica todos os usuários sobre a mudança de status
            // Notifica com horário brasileiro (UTC-3)
            await Clients.All.SendAsync("OrderStatusUpdated", ordemId, novoStatus, DateTime.UtcNow.AddHours(-3).ToString("dd/MM/yyyy às HH:mm"));
        }

        /// <summary>
        /// Notifica sobre uma nova mensagem não lida
        /// </summary>
        /// <param name="userId">ID do usuário que deve receber a notificação</param>
        /// <param name="count">Número de mensagens não lidas</param>
        /// <returns></returns>
        public async Task NotifyUnreadMessages(string userId, int count)
        {
            // Envia notificação de mensagens não lidas para o usuário específico
            await Clients.Group($"User_{userId}").SendAsync("UnreadMessagesCount", count);
        }

        /// <summary>
        /// Adiciona o usuário ao grupo de uma ordem de serviço específica
        /// </summary>
        /// <param name="ordemServicoId">ID da ordem de serviço</param>
        /// <returns></returns>
        public async Task EntrarGrupoOrdemServico(int ordemServicoId)
        {
            // Adiciona a conexão atual ao grupo da ordem de serviço
            await Groups.AddToGroupAsync(Context.ConnectionId, $"OrdemServico_{ordemServicoId}");
            
            // Log para debug
            Console.WriteLine($"Usuário {Context.User?.Identity?.Name} entrou no grupo da ordem {ordemServicoId}");
        }

        /// <summary>
        /// Remove o usuário do grupo de uma ordem de serviço específica
        /// </summary>
        /// <param name="ordemServicoId">ID da ordem de serviço</param>
        /// <returns></returns>
        public async Task SairGrupoOrdemServico(int ordemServicoId)
        {
            // Remove a conexão atual do grupo da ordem de serviço
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"OrdemServico_{ordemServicoId}");
            
            // Log para debug
            Console.WriteLine($"Usuário {Context.User?.Identity?.Name} saiu do grupo da ordem {ordemServicoId}");
        }
    }
}