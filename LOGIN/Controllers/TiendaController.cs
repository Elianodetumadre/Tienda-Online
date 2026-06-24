using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LOGIN.Data;
using LOGIN.Models;

namespace LOGIN.Controllers
{
    public class TiendaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TiendaController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool UsuarioLogueado()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioId"));
        }

        private int GetUsuarioId()
        {
            return int.Parse(HttpContext.Session.GetString("UsuarioId") ?? "0");
        }

        // GET: Tienda (vitrina de productos)
        public async Task<IActionResult> Index()
        {
            if (!UsuarioLogueado())
                return RedirectToAction("Login", "Account");

            var productos = await _context.Productos.ToListAsync();
            var carritoCount = await _context.CarritoItems
                .Where(c => c.UsuarioId == GetUsuarioId())
                .SumAsync(c => c.Cantidad);

            ViewBag.CarritoCount = carritoCount;
            return View(productos);
        }

        // POST: Agregar al carrito
        [HttpPost]
        public async Task<IActionResult> AgregarAlCarrito(int productoId, int cantidad = 1)
        {
            if (!UsuarioLogueado())
                return Json(new { success = false, message = "Debes iniciar sesión" });

            var usuarioId = GetUsuarioId();
            var producto = await _context.Productos.FindAsync(productoId);

            if (producto == null)
                return Json(new { success = false, message = "Producto no encontrado" });

            if (producto.Cantidad < cantidad)
                return Json(new { success = false, message = "Stock insuficiente" });

            var itemExistente = await _context.CarritoItems
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId && c.ProductoId == productoId);

            if (itemExistente != null)
            {
                itemExistente.Cantidad += cantidad;
            }
            else
            {
                var carritoItem = new CarritoItem
                {
                    UsuarioId = usuarioId,
                    ProductoId = productoId,
                    Cantidad = cantidad
                };
                _context.CarritoItems.Add(carritoItem);
            }

            await _context.SaveChangesAsync();

            var totalItems = await _context.CarritoItems
                .Where(c => c.UsuarioId == usuarioId)
                .SumAsync(c => c.Cantidad);

            return Json(new { success = true, message = "Producto agregado", count = totalItems });
        }

        // GET: Carrito
        public async Task<IActionResult> Carrito()
        {
            if (!UsuarioLogueado())
                return RedirectToAction("Login", "Account");

            var carritoItems = await _context.CarritoItems
                .Include(c => c.Producto)
                .Where(c => c.UsuarioId == GetUsuarioId())
                .ToListAsync();

            var total = carritoItems.Sum(c => c.Cantidad * c.Producto!.Precio);
            ViewBag.Total = total;

            return View(carritoItems);
        }

        // POST: Actualizar cantidad
        [HttpPost]
        public async Task<IActionResult> ActualizarCantidad(int itemId, int cantidad)
        {
            if (!UsuarioLogueado())
                return Json(new { success = false });

            var item = await _context.CarritoItems
                .Include(c => c.Producto)
                .FirstOrDefaultAsync(c => c.Id == itemId && c.UsuarioId == GetUsuarioId());

            if (item == null)
                return Json(new { success = false });

            if (cantidad <= 0)
            {
                _context.CarritoItems.Remove(item);
            }
            else
            {
                if (item.Producto != null && item.Producto.Cantidad < cantidad)
                    return Json(new { success = false, message = "Stock insuficiente" });

                item.Cantidad = cantidad;
            }

            await _context.SaveChangesAsync();

            var carritoItems = await _context.CarritoItems
                .Include(c => c.Producto)
                .Where(c => c.UsuarioId == GetUsuarioId())
                .ToListAsync();

            var total = carritoItems.Sum(c => c.Cantidad * c.Producto!.Precio);
            var totalItems = carritoItems.Sum(c => c.Cantidad);

            return Json(new
            {
                success = true,
                subtotal = item.Cantidad * (item.Producto?.Precio ?? 0),
                total = total,
                count = totalItems,
                itemRemoved = cantidad <= 0
            });
        }

        // GET: Checkout
        public async Task<IActionResult> Checkout()
        {
            if (!UsuarioLogueado())
                return RedirectToAction("Login", "Account");

            var carritoItems = await _context.CarritoItems
                .Include(c => c.Producto)
                .Where(c => c.UsuarioId == GetUsuarioId())
                .ToListAsync();

            if (!carritoItems.Any())
                return RedirectToAction("Carrito");

            ViewBag.Total = carritoItems.Sum(c => c.Cantidad * c.Producto!.Precio);
            return View();
        }

        // POST: Confirmar pedido
        [HttpPost]
        public async Task<IActionResult> ConfirmarPedido(string direccion, string metodoPago)
        {
            if (!UsuarioLogueado())
                return RedirectToAction("Login", "Account");

            var usuarioId = GetUsuarioId();
            var carritoItems = await _context.CarritoItems
                .Include(c => c.Producto)
                .Where(c => c.UsuarioId == usuarioId)
                .ToListAsync();

            if (!carritoItems.Any())
                return RedirectToAction("Carrito");

            // Verificar stock
            foreach (var item in carritoItems)
            {
                if (item.Producto!.Cantidad < item.Cantidad)
                {
                    TempData["Error"] = $"Stock insuficiente para {item.Producto.Nombre}";
                    return RedirectToAction("Carrito");
                }
            }

            // Crear pedido
            var total = carritoItems.Sum(c => c.Cantidad * c.Producto!.Precio);
            var pedido = new Pedido
            {
                UsuarioId = usuarioId,
                Total = total,
                DireccionEnvio = direccion,
                MetodoPago = metodoPago,
                NumeroReferencia = "REF-" + DateTime.Now.Ticks.ToString().Substring(0, 8),
                Estado = EstadoPedido.Confirmado
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Crear detalles y actualizar stock
            foreach (var item in carritoItems)
            {
                var detalle = new PedidoDetalle
                {
                    PedidoId = pedido.Id,
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.Producto!.Precio
                };
                _context.PedidoDetalles.Add(detalle);

                // Actualizar stock
                item.Producto.Cantidad -= item.Cantidad;
                _context.Entry(item.Producto).State = EntityState.Modified;
            }

            // Limpiar carrito
            _context.CarritoItems.RemoveRange(carritoItems);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "¡Pedido realizado con éxito!";
            return RedirectToAction("MisCompras");
        }

        // GET: Mis Compras
        public async Task<IActionResult> MisCompras()
        {
            if (!UsuarioLogueado())
                return RedirectToAction("Login", "Account");

            var pedidos = await _context.Pedidos
                .Include(p => p.Detalles!)
                .ThenInclude(d => d.Producto)
                .Where(p => p.UsuarioId == GetUsuarioId())
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync();

            return View(pedidos);
        }
    }
}