using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieCatalog.Data;
using MovieCatalog.Models;

namespace MovieCatalog.Controllers;

public class GenresController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await db.Genres.Include(g => g.Movies).OrderBy(g => g.Name).ToListAsync());

    [Authorize]
    public IActionResult Create() => View("Form", new Genre());

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name")] Genre genre)
    {
        await ValidateUnique(genre);
        if (!ModelState.IsValid) return View("Form", genre);
        db.Genres.Add(genre);
        await db.SaveChangesAsync();
        TempData["Toast"] = $"Genre \"{genre.Name}\" created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    public async Task<IActionResult> Edit(int id)
    {
        var genre = await db.Genres.FindAsync(id);
        return genre is null ? NotFound() : View("Form", genre);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Genre input)
    {
        var genre = await db.Genres.FindAsync(id);
        if (genre is null) return NotFound();
        await ValidateUnique(input, id);
        if (!ModelState.IsValid) return View("Form", input);
        genre.Name = input.Name;
        await db.SaveChangesAsync();
        TempData["Toast"] = "Genre updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var genre = await db.Genres.Include(g => g.Movies).FirstOrDefaultAsync(g => g.Id == id);
        if (genre is null) return NotFound();
        if (genre.Movies.Count > 0)
        {
            TempData["ToastError"] = $"\"{genre.Name}\" still has {genre.Movies.Count} movie(s), so it can't be deleted.";
            return RedirectToAction(nameof(Index));
        }
        db.Genres.Remove(genre);
        await db.SaveChangesAsync();
        TempData["Toast"] = $"Genre \"{genre.Name}\" deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateUnique(Genre genre, int? id = null)
    {
        if (await db.Genres.AnyAsync(g => g.Name == genre.Name && g.Id != id))
            ModelState.AddModelError(nameof(Genre.Name), "This genre already exists.");
    }
}
