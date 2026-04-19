using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdemServicoMVC.Data;
using OrdemServicoMVC.Models;
using OrdemServicoMVC.ViewModels;

namespace OrdemServicoMVC.Controllers
{
    // Controller para gerenciamento de usuários (apenas para administradores)
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<UserController> _logger;
        private readonly ApplicationDbContext _context;

        public UserController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<UserController> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Exibe a lista de usuários do sistema (excluindo técnicos)
        /// Carrega informações dos usuários e suas roles para exibição
        /// </summary>
        /// <returns>View com lista de usuários ordenada por nome da loja</returns>
        // GET: User
        public async Task<IActionResult> Index()
        {
            // Filtra apenas usuários que não são técnicos (usuários comuns e admins)
            var users = await _userManager.Users
                .Where(u => !u.IsTecnico)
                .ToListAsync();
            var userViewModels = new List<UserViewModel>();

            // Converte cada usuário para ViewModel incluindo suas roles
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName ?? "",
                    Email = user.Email ?? "",
                    NomeLoja = user.NomeLoja ?? "",
                    IsAdmin = user.IsAdmin,
                    DataCriacao = user.DataCriacao,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = string.Join(", ", roles) // Concatena todas as roles do usuário
                });
            }

            // Retorna a view ordenada por nome da loja
            return View(userViewModels.OrderBy(u => u.NomeLoja));
        }

        // GET: User/Create
        public IActionResult Create()
        {
            return View(new CreateUserViewModel());
        }

        /// <summary>
        /// Processa a criação de um novo usuário no sistema
        /// Valida dados, verifica duplicatas, cria o usuário e atribui roles
        /// </summary>
        /// <param name="model">Dados do usuário a ser criado</param>
        /// <returns>Redirecionamento para Index em caso de sucesso, ou view com erros</returns>
        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Verifica se o nome de usuário já existe no sistema
                var existingUser = await _userManager.FindByNameAsync(model.UserName);
                if (existingUser != null)
                {
                    ModelState.AddModelError("UserName", "Este nome de usuário já existe.");
                    return View(model);
                }

                // Cria o novo usuário com os dados fornecidos
                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    NomeLoja = model.NomeLoja,
                    IsAdmin = model.IsAdmin,
                    EmailConfirmed = true // Email já confirmado por padrão para usuários criados por admin
                };

                // Tenta criar o usuário no Identity
                var result = await _userManager.CreateAsync(user, model.Senha);

                if (result.Succeeded)
                {
                    // Atribui a role apropriada baseada no tipo de usuário
                    if (model.IsAdmin)
                    {
                        await _userManager.AddToRoleAsync(user, "Admin");
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, "User");
                    }

                    // Log da criação do usuário para auditoria
                    _logger.LogInformation($"Novo usuário criado: {user.UserName} por {User.Identity?.Name}");
                    return RedirectToAction(nameof(Index));
                }

                // Adiciona erros do Identity ao ModelState para exibição
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Retorna a view com os erros de validação
            return View(model);
        }

        // GET: User/Edit/5
        public async Task<IActionResult> Edit(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditUserViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email ?? "",
                NomeLoja = user.NomeLoja ?? "",
                IsAdmin = user.IsAdmin
            };

            return View(model);
        }

        // POST: User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUserViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                // Atualiza as propriedades do usuário
                user.Email = model.Email;
                user.NomeLoja = model.NomeLoja;
                
                // Verifica se o status de admin mudou
                if (user.IsAdmin != model.IsAdmin)
                {
                    user.IsAdmin = model.IsAdmin;
                    
                    // Remove todas as roles atuais
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    
                    // Adiciona a nova role
                    if (model.IsAdmin)
                    {
                        await _userManager.AddToRoleAsync(user, "Admin");
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, "User");
                    }
                }

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"Usuário {user.UserName} atualizado por {User.Identity?.Name}");
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // GET: User/ChangePassword/5
        public async Task<IActionResult> ChangePassword(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ChangePasswordViewModel
            {
                UserId = user.Id,
                UserName = user.UserName ?? "",
                NomeLoja = user.NomeLoja ?? ""
            };

            return View(model);
        }

        // POST: User/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    return NotFound();
                }

                // Remove a senha atual e define a nova
                var removePasswordResult = await _userManager.RemovePasswordAsync(user);
                if (removePasswordResult.Succeeded)
                {
                    var addPasswordResult = await _userManager.AddPasswordAsync(user, model.NovaSenha);
                    if (addPasswordResult.Succeeded)
                    {
                        _logger.LogInformation($"Senha do usuário {user.UserName} alterada por {User.Identity?.Name}");
                        return RedirectToAction(nameof(Index));
                    }

                    foreach (var error in addPasswordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                else
                {
                    foreach (var error in removePasswordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            return View(model);
        }

        // GET: User/Delete/5
        public async Task<IActionResult> Delete(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email ?? "",
                NomeLoja = user.NomeLoja ?? "",
                IsAdmin = user.IsAdmin,
                DataCriacao = user.DataCriacao
            };

            return View(model);
        }

        // POST: User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Usuário não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Verifica se o usuário possui ordens de serviço como criador
                var ordensComoCriador = await _context.OrdensServico
                    .Where(o => o.UsuarioCriadorId == user.Id)
                    .CountAsync();

                if (ordensComoCriador > 0)
                {
                    TempData["ErrorMessage"] = $"Não é possível excluir o usuário {user.UserName} pois ele possui {ordensComoCriador} ordem(ns) de serviço vinculada(s). Exclua ou transfira as ordens primeiro.";
                    return RedirectToAction(nameof(Index));
                }

                // Verifica se o usuário possui mensagens vinculadas
                var mensagensVinculadas = await _context.Mensagens
                    .Where(m => m.UsuarioId == user.Id)
                    .CountAsync();

                if (mensagensVinculadas > 0)
                {
                    TempData["ErrorMessage"] = $"Não é possível excluir o usuário {user.UserName} pois ele possui {mensagensVinculadas} mensagem(ns) vinculada(s). Exclua as mensagens primeiro.";
                    return RedirectToAction(nameof(Index));
                }

                // Verifica se o usuário possui anexos vinculados
                var anexosVinculados = await _context.Anexos
                    .Where(a => a.UsuarioId == user.Id)
                    .CountAsync();

                if (anexosVinculados > 0)
                {
                    TempData["ErrorMessage"] = $"Não é possível excluir o usuário {user.UserName} pois ele possui {anexosVinculados} anexo(s) vinculado(s). Exclua os anexos primeiro.";
                    return RedirectToAction(nameof(Index));
                }

                // Tenta excluir o usuário
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation($"Usuário {user.UserName} excluído com sucesso por {User.Identity?.Name}");
                    TempData["SuccessMessage"] = $"Usuário {user.UserName} excluído com sucesso!";
                }
                else
                {
                    // Se a exclusão falhou, exibe os erros
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError($"Falha ao excluir usuário {user.UserName}: {errors}");
                    TempData["ErrorMessage"] = $"Erro ao excluir usuário: {errors}";
                }
            }
            catch (Exception ex)
            {
                // Captura qualquer exceção durante o processo de exclusão
                _logger.LogError(ex, $"Exceção ao tentar excluir usuário {user.UserName}");
                TempData["ErrorMessage"] = $"Erro inesperado ao excluir usuário: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}