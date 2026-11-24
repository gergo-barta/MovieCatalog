using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MovieCatalog.Data;
using MovieCatalog.Models;

namespace MovieCatalog.Controllers;

/// <summary>CRUD for the MovieCast join table (who played / directed what).</summary>
public class CastController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(int? movieId)
    {
        var query = db.MovieCast.Include(c => c.Movie).Include(c => c.Person).AsQueryable();
        if (movieId is > 0) query = query.Where(c => c.MovieId == movieId);
        ViewBag.Movies = new SelectList(await db.Movies.OrderBy(m => m.Title).ToListAsync(), "Id", "Title", movieId);
        ViewBag.MovieId = movieId;
        return View(await query.OrderBy(c => c.Movie!.Title).ThenBy(c => c.BillingOrder).ToListAsync());
    }

    [Authorize]
    public async Task<IActionResult> Create(int? movieId)
    {
        var next = movieId is > 0 ? (await db.MovieCast.Where(c => c.MovieId == movieId).MaxAsync(c => (int?)c.BillingOrder) ?? 0) + 1 : 1;
        await FillLists();
        return View("Form", new MovieCast { MovieId = movieId ?? 0, BillingOrder = next });
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MovieId,PersonId,Role,CharacterName,BillingOrder")] MovieCast cast, string? returnTo)
    {
        if (!ModelState.IsValid)
        {
            await FillLists();
            return View("Form", cast);
        }
        db.MovieCast.Add(cast);
        await db.SaveChangesAsync();
        TempData["Toast"] = "Cast member added.";
        return returnTo == "movie"
            ? RedirectToAction("Details", "Movies", new { id = cast.MovieId })
            : RedirectToAction(nameof(Index), new { movieId = cast.MovieId });
    }

    [Authorize]
    public async Task<IActionResult> Edit(int id)
    {
        var cast = await db.MovieCast.FindAsync(id);
        if (cast is null) return NotFound();
        await FillLists();
        return View("Form", cast);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,MovieId,PersonId,Role,CharacterName,BillingOrder")] MovieCast input, string? returnTo)
    {
        var cast = await db.MovieCast.FindAsync(id);
        if (cast is null) return NotFound();
        if (!ModelState.IsValid)
        {
            await FillLists();
            return View("Form", input);
        }
        cast.MovieId = input.MovieId;
        cast.PersonId = input.PersonId;
        cast.Role = input.Role;
        cast.CharacterName = input.CharacterName;
        cast.BillingOrder = input.BillingOrder;
        await db.SaveChangesAsync();
        TempData["Toast"] = "Cast member updated.";
        return returnTo == "movie"
            ? RedirectToAction("Details", "Movies", new { id = cast.MovieId })
            : RedirectToAction(nameof(Index), new { movieId = cast.MovieId });
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnTo)
    {
        var cast = await db.MovieCast.FindAsync(id);
        if (cast is null) return NotFound();
        db.MovieCast.Remove(cast);
        await db.SaveChangesAsync();
        TempData["Toast"] = "Cast member removed.";
        return returnTo == "movie"
            ? RedirectToAction("Details", "Movies", new { id = cast.MovieId })
            : RedirectToAction(nameof(Index));
    }

    private async Task FillLists()
    {
        ViewBag.Movies = new SelectList(await db.Movies.OrderBy(m => m.Title).ToListAsync(), "Id", "Title");
        ViewBag.People = new SelectList(
            (await db.People.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToListAsync())
                .Select(p => new { p.Id, Name = p.FullName }), "Id", "Name");
        ViewBag.Roles = new SelectList(Enum.GetValues<CastRole>());
    }
}
