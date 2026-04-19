using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdemServicoMVC.Models;
using OrdemServicoMVC.ViewModels;

namespace OrdemServicoMVC.Controllers
{
    // Controller para gerenciamento de técnicos (apenas para administradores)
    [Authorize(Roles = "Admin")]
    public class TecnicoController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<TecnicoController> _logger;

        public TecnicoController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<TecnicoController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        /// <summary>
        /// Exibe a lista de técnicos cadastrados no sistema
        /// Carrega informações dos usuários marcados como técnicos
        /// </summary>
        /// <returns>View com lista de técnicos ordenada por nome da loja</returns>
        // GET: Tecnico
        public async Task<IActionResult> Index()
        {
            // Filtra apenas usuários que são técnicos
            var tecnicos = await _userManager.Users
                .Where(u => u.IsTecnico)
                .ToListAsync();
            
            var tecnicoViewModels = new List<UserViewModel>();

            // Converte cada técnico para ViewModel incluindo suas roles
            foreach (var tecnico in tecnicos)
            {
                var roles = await _userManager.GetRolesAsync(tecnico);
                tecnicoViewModels.Add(new UserViewModel
                {
                    Id = tecnico.Id,
                    UserName = tecnico.UserName ?? "",
                    Email = tecnico.Email ?? "",
                    NomeLoja = tecnico.NomeLoja ?? "",
                    IsAdmin = tecnico.IsAdmin,
                    DataCriacao = tecnico.DataCriacao,
                    EmailConfirmed = tecnico.EmailConfirmed,
                    Roles = string.Join(", ", roles) // Concatena todas as roles do técnico
                });
            }

            // Retorna a view ordenada por nome da loja
            return View(tecnicoViewModels.OrderBy(t => t.NomeLoja));
        }

        // GET: Tecnico/Create
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Processa a criação de um novo técnico no sistema
        /// Cria um usuário marcado como técnico com permissões específicas
        /// </summary>
        /// <param name="model">Dados do técnico a ser criado</param>
        /// <returns>Redirecionamento para Index em caso de sucesso, ou view com erros</returns>
        // POST: Tecnico/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Cria um novo usuário com perfil de técnico
                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    NomeLoja = model.NomeLoja,
                    IsAdmin = false, // Técnicos não são administradores por padrão
                    IsTecnico = true, // Marca como técnico para diferenciação
                    EmailConfirmed = true, // Email confirmado automaticamente
                    // Define data de criação com horário brasileiro (UTC-3)
                DataCriacao = DateTime.UtcNow.AddHours(-3)
                };

                // Tenta criar o técnico no Identity
                var result = await _userManager.CreateAsync(user, model.Senha);

                if (result.Succeeded)
                {
                    // Log da criação do técnico para auditoria
                    _logger.LogInformation($"Técnico {user.UserName} criado com sucesso.");
                    TempData["SuccessMessage"] = "Técnico criado com sucesso!";
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

        // GET: Tecnico/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null || !user.IsTecnico)
            {
                return NotFound();
            }

            var model = new EditUserViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email ?? "",
                NomeLoja = user.NomeLoja ?? "",
                EmailConfirmed = user.EmailConfirmed
            };

            return View(model);
        }

        // POST: Tecnico/Edit/5
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
                if (user == null || !user.IsTecnico)
                {
                    return NotFound();
                }

                user.UserName = model.UserName;
                user.Email = model.Email;
                user.NomeLoja = model.NomeLoja;
                user.EmailConfirmed = model.EmailConfirmed;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"Técnico {user.UserName} atualizado com sucesso.");
                    TempData["SuccessMessage"] = "Técnico atualizado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // GET: Tecnico/ChangePassword/5
        public async Task<IActionResult> ChangePassword(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null || !user.IsTecnico)
            {
                return NotFound();
            }

            var model = new ChangePasswordViewModel
            {
                UserId = user.Id,
                UserName = user.UserName ?? ""
            };

            return View(model);
        }

        // POST: Tecnico/ChangePassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null || !user.IsTecnico)
                {
                    return NotFound();
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, model.NovaSenha);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"Senha do técnico {user.UserName} alterada com sucesso.");
                    TempData["SuccessMessage"] = "Senha alterada com sucesso!";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // GET: Tecnico/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null || !user.IsTecnico)
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
                DataCriacao = user.DataCriacao,
                EmailConfirmed = user.EmailConfirmed
            };

            return View(model);
        }

        // POST: Tecnico/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null && user.IsTecnico)
            {
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation($"Técnico {user.UserName} excluído com sucesso.");
                    TempData["SuccessMessage"] = "Técnico excluído com sucesso!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao excluir técnico.";
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}