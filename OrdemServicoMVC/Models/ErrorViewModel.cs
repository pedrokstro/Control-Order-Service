namespace OrdemServicoMVC.Models;

public class ErrorViewModel
{
    // ID da requisição para rastreamento
    public string? RequestId { get; set; }
    
    // Mensagem de erro personalizada
    public string? ErrorMessage { get; set; }
    
    // Código de status HTTP
    public int StatusCode { get; set; }
    
    // Timestamp do erro
    public DateTime ErrorTime { get; set; } = DateTime.Now;
    
    // Path da requisição que causou o erro
    public string? RequestPath { get; set; }

    // Propriedade para mostrar o RequestId
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    
    // Propriedade para mostrar detalhes em desenvolvimento
    public bool ShowDetails { get; set; }
}
