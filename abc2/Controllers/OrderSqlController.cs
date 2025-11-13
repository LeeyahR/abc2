using abc2.Data;
using abc2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace abc2.Controllers
{
    public class OrderSqlController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderSqlController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /OrderSql
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.CustomerID)
                .Include(o => o.ProductId)
                .ToListAsync();
            return View(orders);
        }

        // GET: /OrderSql/Create
        public IActionResult Create()
        {
            ViewData["Customers"] = _context.Customers.ToList();
            ViewData["Products"] = _context.Products.ToList();
            return View();
        }

        // POST: /OrderSql/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order)
        {
            if (!ModelState.IsValid)
            {
                ViewData["Customers"] = _context.Customers.ToList();
                ViewData["Products"] = _context.Products.ToList();
                return View(order);
            }

            _context.Add(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /OrderSql/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            ViewData["Customers"] = _context.Customers.ToList();
            ViewData["Products"] = _context.Products.ToList();
            return View(order);
        }

        // POST: /OrderSql/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Order order)
        {
            if (id != order.OrderId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.OrderId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["Customers"] = _context.Customers.ToList();
            ViewData["Products"] = _context.Products.ToList();
            return View(order);
        }

        // GET: /OrderSql/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.CustomerID)
                .Include(o => o.ProductId)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // GET: /OrderSql/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.CustomerID)
                .Include(o => o.ProductId)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // POST: /OrderSql/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }
    }
}
