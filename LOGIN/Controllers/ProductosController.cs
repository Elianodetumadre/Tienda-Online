using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LOGIN.Data;
using LOGIN.Models;

namespace LOGIN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductosController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Producto>>> GetProductos()
        {
            return await _context.Productos.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Producto>> GetProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound(new { mensaje = "Producto no encontrado" });
            return producto;
        }

        [HttpPost]
        public async Task<ActionResult<Producto>> PostProducto(Producto producto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            producto.FechaRegistro = DateTime.UtcNow;
            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProducto), new { id = producto.Id }, producto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutProducto(int id, Producto producto)
        {
            if (id != producto.Id) return BadRequest(new { mensaje = "El ID no coincide" });
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existente = await _context.Productos.FindAsync(id);
            if (existente == null) return NotFound(new { mensaje = "Producto no encontrado" });

            existente.Nombre = producto.Nombre;
            existente.Descripcion = producto.Descripcion;
            existente.Cantidad = producto.Cantidad;
            existente.Precio = producto.Precio;

            await _context.SaveChangesAsync();
            return Ok(existente);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound(new { mensaje = "Producto no encontrado" });

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Producto eliminado correctamente" });
        }
    }
}