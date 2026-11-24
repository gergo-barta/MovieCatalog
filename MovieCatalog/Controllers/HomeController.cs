using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieCatalog.Data;
using MovieCatalog.Models;
using MovieCatalog.ViewModels;

namespace MovieCatalog.Controllers;

public class HomeController(AppDbContext db) : Controller
{
    // GET / : search bar + the 5 best rated movies
    public async Task<IActionResult> Index()
    {
        var cards = await MoviesController.ToCards(db.Movies).ToListAsync();
        var vm = new HomeViewModel
        {
            TopRated = cards.Where(c => c.Average is not null)
                .OrderByDescending(c => c.Average).ThenByDescending(c => c.ReviewCount).ThenBy(c => c.Title)
                .Take(5).ToList()
        };
        return View(vm);
    }

    // GET /Home/Search?q=... : matching movies (by title) and people (by name)
    public async Task<IActionResult> Search(string? q)
    {
        var vm = new SearchViewModel { Query = q?.Trim() ?? "" };
        if (vm.Query.Length > 0)
        {
            var pattern = $"%{vm.Query}%";
            vm.Movies = await MoviesController.ToCards(db.Movies
                .Where(m => EF.Functions.Like(m.Title, pattern))
                .OrderBy(m => m.Title)).ToListAsync();
            vm.People = await db.People
                .Where(p => EF.Functions.Like(p.FirstName + " " + p.LastName, pattern))
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .Include(p => p.Credits).ThenInclude(c => c.Movie)
                .ToListAsync();
        }
        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
