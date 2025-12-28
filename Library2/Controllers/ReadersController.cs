using Microsoft.AspNetCore.Mvc;
using Library2.Models;
using Library2.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var readers = await _context.Readers
                .Include(r => r.BookLoans)
                    .ThenInclude(bl => bl.Book)
                .ToListAsync();
            return View(readers);
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
            return View(new Reader());
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
                .Include(r => r.BookLoans)
                .FirstOrDefaultAsync(m => m.IdReader == id);

            if (reader == null)
            {
                return NotFound();
            }

            int activeLoansCount = reader.BookLoans.Count(bl => bl.ReturnDate == null);

            if (activeLoansCount > 0)
            {
                ViewData["ActiveLoansError"] = $"Невозможно удалить читателя. У него на руках {activeLoansCount} невозвращенная(ые) книга(и).";
            }

            return View(reader);
        }

        // POST: Readers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reader = await _context.Readers.FindAsync(id);
            if (reader == null)
            {
                return NotFound();
            }

            if (reader.BookLoans.Any(bl => bl.ReturnDate == null))
            {

                int activeLoansCount = reader.BookLoans.Count(bl => bl.ReturnDate == null);
                TempData["ActiveLoansError"] = $"Не удалось удалить. У читателя {activeLoansCount} активная(ые) выдача(и).";
                return RedirectToAction(nameof(Delete), new { id = id });
            }

            var bookLoans = _context.BookLoans.Where(bl => bl.ReaderId == id);
            _context.BookLoans.RemoveRange(bookLoans);

            
            _context.Readers.Remove(reader);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool ReaderExists(int id)
        {
            return _context.Readers.Any(e => e.IdReader == id);
        }
    }
}