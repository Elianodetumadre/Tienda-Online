using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LOGIN.Models;
using LOGIN.Data;

namespace LOGIN.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioId")))
            {
                string rolActual = HttpContext.Session.GetString("UsuarioRol") ?? "";

                if (EsAdmin(rolActual))
                {
                    return RedirectToAction("Index", "ProductosView");
                }

                return RedirectToAction("Index", "Tienda");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.Password == model.Password);

                if (usuario != null)
                {
                    string rolUsuario = string.IsNullOrWhiteSpace(usuario.Rol)
                        ? "Cliente"
                        : usuario.Rol.Trim();

                    HttpContext.Session.SetString("UsuarioId", usuario.Id.ToString());
                    HttpContext.Session.SetString("UsuarioNombre", usuario.Nombre ?? "");
                    HttpContext.Session.SetString("UsuarioEmail", usuario.Email ?? "");
                    HttpContext.Session.SetString("UsuarioRol", rolUsuario);

                    if (EsAdmin(rolUsuario))
                    {
                        return RedirectToAction("Index", "ProductosView");
                    }

                    return RedirectToAction("Index", "Tienda");
                }

                ModelState.AddModelError("", "Email o contraseña incorrectos");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioId")))
            {
                return RedirectToAction("Index", "Tienda");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Usuario usuario)
        {
            if (ModelState.IsValid)
            {
                var existe = await _context.Usuarios.AnyAsync(u => u.Email == usuario.Email);

                if (existe)
                {
                    ModelState.AddModelError("Email", "Este email ya está registrado");
                    return View(usuario);
                }

                usuario.FechaRegistro = DateTime.UtcNow;

                // Todo usuario registrado desde la web será Cliente.
                // Nadie podrá registrarse como Admin desde el formulario.
                usuario.Rol = "Cliente";

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                TempData["Mensaje"] = "Registro exitoso. ¡Inicia sesión!";
                return RedirectToAction("Login");
            }

            return View(usuario);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private bool EsAdmin(string rol)
        {
            if (string.IsNullOrWhiteSpace(rol))
            {
                return false;
            }

            rol = rol.Trim().ToLower();

            return rol == "admin" || rol == "administrador";
        }
    }
}