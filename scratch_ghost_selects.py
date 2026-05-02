import re

with open('OrdemServicoMVC/Views/OrdemServico/Index.cshtml', 'r', encoding='utf-8') as f:
    content = f.read()

css_code = '''
        /* Ghost Selects */
        .ghost-select {
            appearance: none;
            -webkit-appearance: none;
            -moz-appearance: none;
            border: 1px solid transparent;
            border-radius: 50rem;
            padding: 0.25rem 1.5rem 0.25rem 0.75rem;
            font-size: 0.85rem;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s ease;
            box-shadow: 0 1px 2px rgba(0,0,0,0.05);
            background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 16 16'%3E%3Cpath fill='none' stroke='currentColor' stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M2 5l6 6 6-6'/%3E%3C/svg%3E");
            background-repeat: no-repeat;
            background-position: right 0.5rem center;
            background-size: 10px 10px;
            white-space: nowrap;
            text-overflow: ellipsis;
            overflow: hidden;
            display: inline-block;
            max-width: 100%;
        }
        .ghost-select:hover {
            filter: brightness(0.95);
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        .ghost-select:focus {
            outline: none;
            border-color: rgba(0,0,0,0.1);
            box-shadow: 0 0 0 0.2rem rgba(13, 110, 253, 0.25);
        }
        
        .ghost-prioridade-Alta { background-color: #ffe5e8; color: #dc3545; }
        .ghost-prioridade-Media { background-color: #fff3cd; color: #856404; }
        .ghost-prioridade-Baixa { background-color: #e0f7fa; color: #0c5460; }
        
        .ghost-status-Aberta { background-color: #fff8e1; color: #f57f17; }
        .ghost-status-EmAndamento { background-color: #e3f2fd; color: #1976d2; }
        .ghost-status-Concluida { background-color: #e8f5e9; color: #2e7d32; }
        
        .ghost-tecnico { background-color: #f3f4f6; color: #4b5563; }
'''

if '/* Ghost Selects */' not in content:
    content = content.replace('<style>', '<style>' + css_code)

# Replace Prioridade Selects
content = re.sub(
    r'<select class="form-select form-select-sm prioridade-dropdown" data-ordem-id="@ordem\.Id" data-current-value="@\(\(int\)ordem\.Prioridade\)" style="[^"]+">',
    r'<select class="ghost-select prioridade-dropdown @(ordem.Prioridade == OrdemServicoMVC.Models.PrioridadeEnum.Alta ? "ghost-prioridade-Alta" : ordem.Prioridade == OrdemServicoMVC.Models.PrioridadeEnum.Media ? "ghost-prioridade-Media" : "ghost-prioridade-Baixa")" data-ordem-id="@ordem.Id" data-current-value="@((int)ordem.Prioridade)">',
    content
)

# Replace Status Selects
content = re.sub(
    r'<select class="form-select form-select-sm status-dropdown" data-ordem-id="@ordem\.Id" data-current-value="@\(\(int\)ordem\.Status\)" style="[^"]+">',
    r'<select class="ghost-select status-dropdown @(ordem.Status == OrdemServicoMVC.Models.StatusEnum.Aberta ? "ghost-status-Aberta" : ordem.Status == OrdemServicoMVC.Models.StatusEnum.EmAndamento ? "ghost-status-EmAndamento" : "ghost-status-Concluida")" data-ordem-id="@ordem.Id" data-current-value="@((int)ordem.Status)">',
    content
)

# Replace Tecnico Selects
content = re.sub(
    r'<select class="form-select form-select-sm tecnico-dropdown" data-ordem-id="@ordem\.Id" data-current-value="@\(ordem\.TecnicoResponsavelId \?\? ""\)">',
    r'<select class="ghost-select ghost-tecnico tecnico-dropdown" data-ordem-id="@ordem.Id" data-current-value="@(ordem.TecnicoResponsavelId ?? "")">',
    content
)

with open('OrdemServicoMVC/Views/OrdemServico/Index.cshtml', 'w', encoding='utf-8') as f:
    f.write(content)
