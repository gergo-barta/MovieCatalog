using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieCatalog.Data;
using MovieCatalog.Models;

namespace MovieCatalog.Controllers;

public class PeopleController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? search)
    {
        var query = db.People.Include(p => p.Credits).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => EF.Functions.Like(p.FirstName + " " + p.LastName, $"%{search.Trim()}%"));
        ViewBag.Search = search;
        return View(await query.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var person = await db.People
            .Include(p => p.Credits).ThenInclude(c => c.Movie)
            .FirstOrDefaultAsync(p => p.Id == id);
        return person is null ? NotFound() : View(person);
    }

    [Authorize]
    public IActionResult Create() => View("Form", new Person());

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FirstName,LastName,BirthDate,Bio")] Person person)
    {
        if (!ModelState.IsValid) return View("Form", person);
        db.People.Add(person);
        await db.SaveChangesAsync();
        TempData["Toast"] = $"{person.FullName} added.";
        return RedirectToAction(nameof(Details), new { id = person.Id });
    }

    [Authorize]
    public async Task<IActionResult> Edit(int id)
    {
        var person = await db.People.FindAsync(id);
        return person is null ? NotFound() : View("Form", person);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,BirthDate,Bio")] Person input)
    {
        var person = await db.People.FindAsync(id);
        if (person is null) return NotFound();
        if (!ModelState.IsValid) return View("Form", input);
        person.FirstName = input.FirstName;
        person.LastName = input.LastName;
        person.BirthDate = input.BirthDate;
        person.Bio = input.Bio;
        await db.SaveChangesAsync();
        TempData["Toast"] = "Person updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var person = await db.People.FindAsync(id);
        if (person is null) return NotFound();
        db.People.Remove(person); // their cast credits are deleted too (cascade)
        await db.SaveChangesAsync();
        TempData["Toast"] = $"{person.FullName} deleted.";
        return RedirectToAction(nameof(Index));
    }
}
