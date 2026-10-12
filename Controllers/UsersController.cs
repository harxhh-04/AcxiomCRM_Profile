using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAuditService _auditService;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IAuditService auditService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var modelList = new List<UserListViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var isLockedOut = await _userManager.IsLockedOutAsync(user);

                modelList.Add(new UserListViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "None",
                    IsActive = user.IsActive,
                    IsLockedOut = isLockedOut,
                    FailedLoginCount = user.AccessFailedCount,
                    CreatedDate = user.CreatedDate
                });
            }

            return View(modelList);
        }

        public IActionResult Create()
        {
            ViewBag.Roles = new SelectList(new[] { "Admin", "Manager", "SalesExecutive" });
            return View(new CreateUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "A user with this email already exists.");
                    ViewBag.Roles = new SelectList(new[] { "Admin", "Manager", "SalesExecutive" });
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                    await _auditService.LogAsync("Create", "User", user.Id, null, $"Admin created user {user.Email} with role {model.Role}");

                    TempData["SuccessMessage"] = $"User '{user.FullName}' created successfully.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
            }

            ViewBag.Roles = new SelectList(new[] { "Admin", "Manager", "SalesExecutive" });
            return View(model);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var model = new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "SalesExecutive",
                IsActive = user.IsActive
            };

            ViewBag.Roles = new SelectList(new[] { "Admin", "Manager", "SalesExecutive" }, model.Role);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUserViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                var oldRole = currentRoles.FirstOrDefault() ?? "None";

                user.FullName = model.FullName;
                user.IsActive = model.IsActive;

                var updateResult = await _userManager.UpdateAsync(user);
                if (updateResult.Succeeded)
                {
                    if (oldRole != model.Role)
                    {
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                        await _userManager.AddToRoleAsync(user, model.Role);
                        await _auditService.LogAsync("Role Change", "User", user.Id, oldRole, model.Role);
                    }

                    await _auditService.LogAsync("Update", "User", user.Id, null, $"Updated user {user.Email}");

                    TempData["SuccessMessage"] = $"User '{user.FullName}' updated successfully.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var err in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
            }

            ViewBag.Roles = new SelectList(new[] { "Admin", "Manager", "SalesExecutive" }, model.Role);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            await _auditService.LogAsync("Security", "User", user.Id, null,
                $"User {user.Email} status changed to {(user.IsActive ? "Active" : "Inactive")}");

            TempData["SuccessMessage"] = $"User status updated to {(user.IsActive ? "Active" : "Inactive")}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            await _auditService.LogAsync("Security", "User", user.Id, null, $"Unlocked user {user.Email}");

            TempData["SuccessMessage"] = $"User '{user.Email}' has been unlocked.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                TempData["ErrorMessage"] = "Password must be at least 8 characters long.";
                return RedirectToAction(nameof(Index));
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                await _auditService.LogAsync("Security", "User", user.Id, null, $"Admin reset password for {user.Email}");
                TempData["SuccessMessage"] = $"Password for '{user.Email}' was reset successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
