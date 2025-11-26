using Microsoft.AspNetCore.Mvc;
using Library2.Models;
using Library2.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Library2.Controllers
{
    public class ReadersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReadersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Readers
        public async Task<IActionResult> Index()
        {
            return View(await _context.Readers.ToListAsync());
        }

        // GET: Readers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reader = await _context.Readers
                .Include(r => r.BookLoans)
                    .ThenInclude(bl => bl.Book)
                .FirstOrDefaultAsync(m => m.IdReader == id);

            if (reader == null)
            {
                return NotFound();
            }

            return View(reader);
        }

        // GET: Readers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Readers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reader reader)
        {
            if (ModelState.IsValid)
            {
                _context.Add(reader);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(reader);
        }

        // GET: Readers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reader = await _context.Readers.FindAsync(id);
            if (reader == null)
            {
                return NotFound();
            }
            return View(reader);
        }

        // POST: Readers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Reader reader)
        {
            if (id != reader.IdReader)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reader);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReaderExists(reader.IdReader))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(reader);
        }

        // GET: Readers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reader = await _context.Readers
                .FirstOrDefaultAsync(m => m.IdReader == id);
            if (reader == null)
            {
                return NotFound();
            }

            return View(reader);
        }

        // POST: Readers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reader = await _context.Readers.FindAsync(id);
            if (reader != null)
            {
                _context.Readers.Remove(reader);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReaderExists(int id)
        {
            return _context.Readers.Any(e => e.IdReader == id);
        }
    }
}
