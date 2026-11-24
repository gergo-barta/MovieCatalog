using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieCatalog.Data;
using MovieCatalog.Models;

namespace MovieCatalog.Controllers;

public class StudiosController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await db.Studios.Include(s => s.Movies).OrderBy(s => s.Name).ToListAsync());

    [Authorize]
    public IActionResult Create() => View("Form", new Studio());

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Country,FoundedYear")] Studio studio)
    {
        if (!ModelState.IsValid) return View("Form", studio);
        db.Studios.Add(studio);
        await db.SaveChangesAsync();
        TempData["Toast"] = $"Studio \"{studio.Name}\" created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    public async Task<IActionResult> Edit(int id)
    {
        var studio = await db.Studios.FindAsync(id);
        return studio is null ? NotFound() : View("Form", studio);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Country,FoundedYear")] Studio input)
    {
        var studio = await db.Studios.FindAsync(id);
        if (studio is null) return NotFound();
        if (!ModelState.IsValid) return View("Form", input);
        studio.Name = input.Name;
        studio.Country = input.Country;
        studio.FoundedYear = input.FoundedYear;
        await db.SaveChangesAsync();
        TempData["Toast"] = "Studio updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var studio = await db.Studios.FindAsync(id);
        if (studio is null) return NotFound();
        db.Studios.Remove(studio); // movies keep existing, their studio becomes empty
        await db.SaveChangesAsync();
        TempData["Toast"] = $"Studio \"{studio.Name}\" deleted.";
        return RedirectToAction(nameof(Index));
    }
}
