import re

with open('OrdemServicoMVC/Views/OrdemServico/Index.cshtml', 'r', encoding='utf-8') as f:
    content = f.read()

replacements = {
    '📂 Aberta': '<i class=\"bi bi-folder2-open\"></i> Aberta',
    '⚙️ Em Andamento': '<i class=\"bi bi-gear-fill\"></i> Em Andamento',
    '⚙️ Em andamento': '<i class=\"bi bi-gear-fill\"></i> Em andamento',
    '✅ Concluída': '<i class=\"bi bi-check-circle-fill\"></i> Concluída',
    '🔵 Baixa': '<i class=\"bi bi-arrow-down-circle-fill\" style=\"color: #0dcaf0;\"></i> Baixa',
    '🟡 Média': '<i class=\"bi bi-dash-circle-fill\" style=\"color: #ffc107;\"></i> Média',
    '🔴 Alta': '<i class=\"bi bi-arrow-up-circle-fill\" style=\"color: #dc3545;\"></i> Alta',
    '⚠️ Sem técnico': '<i class=\"bi bi-exclamation-triangle-fill text-warning\"></i> Sem técnico',
    '👤 ': '',
    '🏪 ': '',
    '🛒 Frente de Caixa': 'Frente de Caixa',
    '💰 Financeiro': 'Financeiro',
    '📄 Faturamento': 'Faturamento',
    '🏢 Administrativo': 'Administrativo',
    '👥 RH': 'RH',
    '🛡️ Prevenção': 'Prevenção',
    '👔 Gerência': 'Gerência',
    '📊 Contabilidade': 'Contabilidade',
    '🏛️ Patrimônio': 'Patrimônio',
    '🛍️ Compras': 'Compras',
}

for emoji_str, html_str in replacements.items():
    content = content.replace(emoji_str, html_str)

content = content.replace(
    'var prioridadeIcon = (int)prioridade == 3 ? "🔴" : (int)prioridade == 2 ? "🟡" : "🔵";',
    'var prioridadeIcon = (int)prioridade == 3 ? "<i class=\'bi bi-arrow-up-circle-fill\' style=\'color: #dc3545;\'></i>" : (int)prioridade == 2 ? "<i class=\'bi bi-dash-circle-fill\' style=\'color: #ffc107;\'></i>" : "<i class=\'bi bi-arrow-down-circle-fill\' style=\'color: #0dcaf0;\'></i>";'
)

content = content.replace(
    '<option value="@((int)prioridade)" selected>@prioridadeIcon @prioridade.ToString()</option>',
    '<option value="@((int)prioridade)" selected>@Html.Raw(prioridadeIcon) @prioridade.ToString()</option>'
)
content = content.replace(
    '<option value="@((int)prioridade)">@prioridadeIcon @prioridade.ToString()</option>',
    '<option value="@((int)prioridade)">@Html.Raw(prioridadeIcon) @prioridade.ToString()</option>'
)
content = content.replace(
    '<option value="@((int)prioridade)" selected="@((int)prioridade == (int)ordem.Prioridade)">@prioridadeIcon @prioridade.ToString()</option>',
    '<option value="@((int)prioridade)" selected="@((int)prioridade == (int)ordem.Prioridade)">@Html.Raw(prioridadeIcon) @prioridade.ToString()</option>'
)

content = content.replace(
    'var statusIcon = status.Value == "1" ? "📂" : status.Value == "2" ? "⚙️" : "✅";',
    'var statusIcon = status.Value == "1" ? "<i class=\'bi bi-folder2-open\'></i>" : status.Value == "2" ? "<i class=\'bi bi-gear-fill\'></i>" : "<i class=\'bi bi-check-circle-fill\'></i>";'
)

content = content.replace(
    '<option value="@status.Value" selected>@statusIcon @status.Text</option>',
    '<option value="@status.Value" selected>@Html.Raw(statusIcon) @status.Text</option>'
)
content = content.replace(
    '<option value="@status.Value">@statusIcon @status.Text</option>',
    '<option value="@status.Value">@Html.Raw(statusIcon) @status.Text</option>'
)
content = content.replace(
    '<option value="@status.Value" selected="@(status.Value == ((int)ordem.Status).ToString())">@statusIcon @status.Text</option>',
    '<option value="@status.Value" selected="@(status.Value == ((int)ordem.Status).ToString())">@Html.Raw(statusIcon) @status.Text</option>'
)

with open('OrdemServicoMVC/Views/OrdemServico/Index.cshtml', 'w', encoding='utf-8') as f:
    f.write(content)
