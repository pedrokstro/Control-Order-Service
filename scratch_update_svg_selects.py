import urllib.parse
import re

def create_svg(path, fill='%23000'):
    svg = f"<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16' fill='{fill}' viewBox='0 0 16 16'><path d='{path}'/></svg>"
    return 'data:image/svg+xml,' + urllib.parse.quote(svg)

arrow_svg = "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 16 16'%3E%3Cpath fill='none' stroke='%234b5563' stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M2 5l6 6 6-6'/%3E%3C/svg%3E"

alta_path = 'M16 8A8 8 0 1 0 0 8a8 8 0 0 0 16 0zm-7.5 3.5a.5.5 0 0 1-1 0V5.707L5.354 7.854a.5.5 0 1 1-.708-.708l3-3a.5.5 0 0 1 .708 0l3 3a.5.5 0 0 1-.708.708L8.5 5.707V11.5z'
media_path = 'M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0zM4.5 7.5a.5.5 0 0 0 0 1h7a.5.5 0 0 0 0-1h-7z'
baixa_path = 'M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0zM8.5 4.5a.5.5 0 0 0-1 0v5.793L5.354 8.146a.5.5 0 1 0-.708.708l3 3a.5.5 0 0 0 .708 0l3-3a.5.5 0 0 0-.708-.708L8.5 10.293V4.5z'

aberta_path = 'M1 3.5A1.5 1.5 0 0 1 2.5 2h2.764c.958 0 1.76.56 2.311 1.184C7.985 3.648 8.48 4 9 4h4.5A1.5 1.5 0 0 1 15 5.5v.64c.57.265.94.876.856 1.546l-.64 5.124A2.5 2.5 0 0 1 12.733 15H3.266a2.5 2.5 0 0 1-2.481-2.19l-.64-5.124A1.5 1.5 0 0 1 1 6.14V3.5zM2 6h12v-.5a.5.5 0 0 0-.5-.5H9c-.964 0-1.71-.629-2.174-1.154C6.374 3.334 5.82 3 5.264 3H2.5a.5.5 0 0 0-.5.5V6zm-.367 1a.5.5 0 0 0-.496.562l.64 5.124A1.5 1.5 0 0 0 3.266 14h9.468a1.5 1.5 0 0 0 1.489-1.314l.64-5.124A.5.5 0 0 0 14.367 7H1.633z'
andamento_path = 'M9.405 1.05c-.413-1.4-2.397-1.4-2.81 0l-.1.34a1.464 1.464 0 0 1-2.105.872l-.31-.17c-1.283-.698-2.686.705-1.987 1.987l.169.311c.446.82.023 1.841-.872 2.105l-.34.1c-1.4.413-1.4 2.397 0 2.81l.34.1a1.464 1.464 0 0 1 .872 2.105l-.17.31c-.698 1.283.705 2.686 1.987 1.987l.311-.169a1.464 1.464 0 0 1 2.105.872l.1.34c.413 1.4 2.397 1.4 2.81 0l.1-.34a1.464 1.464 0 0 1 2.105-.872l.31.17c1.283.698 2.686-.705 1.987-1.987l-.169-.311a1.464 1.464 0 0 1 .872-2.105l.34-.1c1.4-.413 1.4-2.397 0-2.81l-.34-.1a1.464 1.464 0 0 1-.872-2.105l.17-.31c.698-1.283-.705-2.686-1.987-1.987l-.311.169a1.464 1.464 0 0 1-2.105-.872l-.1-.34zM8 10.93a2.929 2.929 0 1 1 0-5.86 2.929 2.929 0 0 1 0 5.858z'
concluida_path = 'M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0zm-3.97-3.03a.75.75 0 0 0-1.08.022L7.477 9.417 5.384 7.323a.75.75 0 0 0-1.06 1.06L6.97 11.03a.75.75 0 0 0 1.079-.02l3.992-4.99a.75.75 0 0 0-.01-1.05z'

with open('OrdemServicoMVC/Views/OrdemServico/Index.cshtml', 'r', encoding='utf-8') as f:
    content = f.read()

# Replace .ghost-select css specifically
css_replace = '''
        .ghost-select {
            appearance: none;
            -webkit-appearance: none;
            -moz-appearance: none;
            border: 1px solid transparent;
            border-radius: 50rem;
            padding: 0.25rem 1.5rem 0.25rem 2.0rem; /* space for left icon */
            font-size: 0.85rem;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s ease;
            box-shadow: 0 1px 2px rgba(0,0,0,0.05);
            background-repeat: no-repeat;
            background-position: left 0.5rem center, right 0.5rem center;
            background-size: 14px 14px, 10px 10px;
            white-space: nowrap;
            text-overflow: ellipsis;
            overflow: hidden;
            display: inline-block;
            max-width: 100%;
        }
'''
content = re.sub(r'\.ghost-select\s*\{[^}]+\}', css_replace.strip(), content)

classes_css = f'''
        .ghost-prioridade-Alta {{ background-color: #ffe5e8; color: #dc3545; background-image: url("{create_svg(alta_path, '%23dc3545')}"), url("{arrow_svg}"); }}
        .ghost-prioridade-Media {{ background-color: #fff3cd; color: #856404; background-image: url("{create_svg(media_path, '%23856404')}"), url("{arrow_svg}"); }}
        .ghost-prioridade-Baixa {{ background-color: #e0f7fa; color: #0c5460; background-image: url("{create_svg(baixa_path, '%230c5460')}"), url("{arrow_svg}"); }}
        
        .ghost-status-Aberta {{ background-color: #fff8e1; color: #f57f17; background-image: url("{create_svg(aberta_path, '%23f57f17')}"), url("{arrow_svg}"); }}
        .ghost-status-EmAndamento {{ background-color: #e3f2fd; color: #1976d2; background-image: url("{create_svg(andamento_path, '%231976d2')}"), url("{arrow_svg}"); }}
        .ghost-status-Concluida {{ background-color: #e8f5e9; color: #2e7d32; background-image: url("{create_svg(concluida_path, '%232e7d32')}"), url("{arrow_svg}"); }}
        
        .ghost-tecnico {{ background-color: #f3f4f6; color: #4b5563; padding-left: 0.75rem; background-image: url("{arrow_svg}"); background-position: right 0.5rem center; background-size: 10px 10px; }}
'''

content = re.sub(r'\.ghost-prioridade-Alta[\s\S]*?\.ghost-tecnico[^}]+\}', classes_css.strip(), content)

with open('OrdemServicoMVC/Views/OrdemServico/Index.cshtml', 'w', encoding='utf-8') as f:
    f.write(content)
