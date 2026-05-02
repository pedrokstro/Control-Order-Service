import re

with open('OrdemServicoMVC/Views/OrdemServico/Index.cshtml', 'r', encoding='utf-8') as f:
    content = f.read()

# Remove icons from static options
content = re.sub(r'<i class="bi[^"]*"(?: style="[^"]*")?></i> ', '', content)
content = re.sub(r'<i class=\'bi[^\']*\'(?: style=\'[^\']*\')?></i> ', '', content)
content = re.sub(r'<i class=\\"bi[^"]*\\"(?: style=\\"[^"]*\\")?></i> ', '', content)

# Fix dynamically generated options
content = content.replace(
    'var prioridadeIcon = (int)prioridade == 3 ? "<i class=\\"bi bi-arrow-up-circle-fill\\" style=\\"color: #dc3545;\\"></i>" : (int)prioridade == 2 ? "<i class=\\"bi bi-dash-circle-fill\\" style=\\"color: #ffc107;\\"></i>" : "<i class=\\"bi bi-arrow-down-circle-fill\\" style=\\"color: #0dcaf0;\\"></i>";\n                                                <option value="@((int)prioridade)" selected="@((int)prioridade == (int)ordem.Prioridade)">@Html.Raw(prioridadeIcon) @prioridade.ToString()</option>',
    '<option value="@((int)prioridade)" selected="@((int)prioridade == (int)ordem.Prioridade)">@prioridade.ToString()</option>'
)

content = content.replace(
    'var statusIcon = status.Value == "1" ? "<i class=\\"bi bi-folder2-open\\"></i>" : status.Value == "2" ? "<i class=\\"bi bi-gear-fill\\"></i>" : "<i class=\\"bi bi-check-circle-fill\\"></i>";\n                                                <option value="@status.Value" selected="@(status.Value == ((int)ordem.Status).ToString())">@Html.Raw(statusIcon) @status.Text</option>',
    '<option value="@status.Value" selected="@(status.Value == ((int)ordem.Status).ToString())">@status.Text</option>'
)

with open('OrdemServicoMVC/Views/OrdemServico/Index.cshtml', 'w', encoding='utf-8') as f:
    f.write(content)
