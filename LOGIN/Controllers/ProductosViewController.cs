using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LOGIN.Data;
using LOGIN.Models;

namespace LOGIN.Controllers
{
    public class ProductosViewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductosViewController(
            ApplicationDbContext context,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioId")))
            {
                return RedirectToAction("Login", "Account");
            }

            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioId")))
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var producto = await _context.Productos.FirstOrDefaultAsync(m => m.Id == id);

            if (producto == null) return NotFound();

            return View(producto);
        }

        public IActionResult Create()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioId")))
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Producto producto)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioId")))
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (producto.ImagenArchivo != null)
                    {
                        producto.ImagenUrl = await SubirImagenASupabase(producto.ImagenArchivo);
                    }

                    producto.FechaRegistro = DateTime.UtcNow;

                    _context.Add(producto);
                    await _context.SaveChangesAsync();

                    TempData["Mensaje"] = "Producto creado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("ImagenArchivo", ex.Message);
                }
            }

            return View(producto);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioId")))
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var producto = await _context.Productos.FindAsync(id);

            if (producto == null) return NotFound();

            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Producto producto)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioId")))
            {
                return RedirectToAction("Login", "Account");
            }

            if (id != producto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existente = await _context.Productos.FindAsync(id);

                    if (existente == null) return NotFound();

                    existente.Nombre = producto.Nombre;
                    existente.Descripcion = producto.Descripcion;
                    existente.Cantidad = producto.Cantidad;
                    existente.Precio = producto.Precio;

                    if (producto.ImagenArchivo != null)
                    {
                        existente.ImagenUrl = await SubirImagenASupabase(producto.ImagenArchivo);
                    }

                    _context.Update(existente);
                    await _context.SaveChangesAsync();

                    TempData["Mensaje"] = "Producto actualizado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductoExists(producto.Id)) return NotFound();
                    throw;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("ImagenArchivo", ex.Message);
                }
            }

            return View(producto);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioId")))
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var producto = await _context.Productos.FirstOrDefaultAsync(m => m.Id == id);

            if (producto == null) return NotFound();

            return View(producto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await _context.Productos.FindAsync(id);

            if (producto != null)
            {
                var enCarritos = _context.CarritoItems.Where(c => c.ProductoId == id);
                _context.CarritoItems.RemoveRange(enCarritos);

                _context.Productos.Remove(producto);

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["Mensaje"] = "Producto eliminado exitosamente";
                }
                catch (DbUpdateException)
                {
                    TempData["Mensaje"] = "Error: No puedes borrar este producto porque ya está registrado en el historial de compras de un cliente.";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SubirImagenASupabase(IFormFile imagenArchivo)
        {
            if (imagenArchivo == null || imagenArchivo.Length == 0)
            {
                throw new InvalidOperationException("No se seleccionó ninguna imagen.");
            }

            if (!imagenArchivo.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("El archivo seleccionado no es una imagen válida.");
            }

            string extension = Path.GetExtension(imagenArchivo.FileName).ToLowerInvariant();

            string[] extensionesPermitidas =
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp",
                ".gif"
            };

            if (!extensionesPermitidas.Contains(extension))
            {
                throw new InvalidOperationException("Solo se permiten imágenes JPG, JPEG, PNG, WEBP o GIF.");
            }

            string? supabaseUrl = _configuration["Supabase:Url"];
            string? supabaseKey = _configuration["Supabase:ServiceRoleKey"];
            string? bucket = _configuration["Supabase:Bucket"];

            if (string.IsNullOrWhiteSpace(supabaseUrl))
            {
                throw new InvalidOperationException("Falta configurar Supabase:Url en appsettings.json.");
            }

            if (string.IsNullOrWhiteSpace(supabaseKey))
            {
                throw new InvalidOperationException("Falta configurar Supabase:ServiceRoleKey en appsettings.json.");
            }

            if (string.IsNullOrWhiteSpace(bucket))
            {
                throw new InvalidOperationException("Falta configurar Supabase:Bucket en appsettings.json.");
            }

            supabaseUrl = supabaseUrl.TrimEnd('/');
            bucket = bucket.Trim();

            string nombreArchivo = $"{Guid.NewGuid():N}{extension}";

            string urlSubida = $"{supabaseUrl}/storage/v1/object/{bucket}/{nombreArchivo}";

            using var stream = imagenArchivo.OpenReadStream();
            using var contenido = new StreamContent(stream);

            string contentType = string.IsNullOrWhiteSpace(imagenArchivo.ContentType)
                ? "application/octet-stream"
                : imagenArchivo.ContentType;

            contenido.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            using var request = new HttpRequestMessage(HttpMethod.Post, urlSubida);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", supabaseKey);
            request.Headers.Add("apikey", supabaseKey);
            request.Headers.Add("x-upsert", "true");

            request.Content = contenido;

            var client = _httpClientFactory.CreateClient();

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();

                throw new InvalidOperationException(
                    $"Error al subir la imagen a Supabase: {response.StatusCode} - {error}"
                );
            }

            string urlPublica = $"{supabaseUrl}/storage/v1/object/public/{bucket}/{nombreArchivo}";

            return urlPublica;
        }

        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.Id == id);
        }
    }
}