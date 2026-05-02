import re

with open('OrdemServicoMVC/Views/OrdemServico/Index.cshtml', 'r', encoding='utf-8') as f:
    content = f.read()

# Only strip icons inside <option> tags
content = re.sub(r'(<option[^>]*>)<i class="bi[^"]*"(?: style="[^"]*")?></i>\s*', r'\1', content)

# Remove the Html.Raw() wrappers inside the <option> tag generation loops for prioridade and status
# From:
# <option value="@((int)prioridade)" selected="@((int)prioridade == (int)ordem.Prioridade)">@Html.Raw(prioridadeIcon) @prioridade.ToString()</option>
# To:
# <option value="@((int)prioridade)" selected="@((int)prioridade == (int)ordem.Prioridade)">@prioridade.ToString()</option>

content = content.replace(
    '<option value="@((int)prioridade)" selected="@((int)prioridade == (int)ordem.Prioridade)">@Html.Raw(prioridadeIcon) @prioridade.ToString()</option>',
    '<option value="@((int)prioridade)" selected="@((int)prioridade == (int)ordem.Prioridade)">@prioridade.ToString()</option>'
)

content = content.replace(
    '<option value="@status.Value" selected="@(status.Value == ((int)ordem.Status).ToString())">@Html.Raw(statusIcon) @status.Text</option>',
    '<option value="@status.Value" selected="@(status.Value == ((int)ordem.Status).ToString())">@status.Text</option>'
)

# Also for the table dropdowns (which don't use @Html.Raw but rather the same foreach)
content = content.replace(
    '<option value="@status.Value">@Html.Raw(statusIcon) @status.Text</option>',
    '<option value="@status.Value">@status.Text</option>'
)
content = content.replace(
    '<option value="@((int)prioridade)">@Html.Raw(prioridadeIcon) @prioridade.ToString()</option>',
    '<option value="@((int)prioridade)">@prioridade.ToString()</option>'
)

with open('OrdemServicoMVC/Views/OrdemServico/Index.cshtml', 'w', encoding='utf-8') as f:
    f.write(content)
