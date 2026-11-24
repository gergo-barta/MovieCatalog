using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieCatalog.Data;
using MovieCatalog.Models;
using MovieCatalog.Services;

namespace MovieCatalog.Controllers;

/// <summary>Reviews belong to the user who wrote them: only the author can edit or delete a review.</summary>
public class ReviewsController(AppDbContext db, UserManager<AppUser> users, CatalogNotifier notifier) : Controller
{
    public async Task<IActionResult> Index(bool mine = false)
    {
        var query = db.Reviews.Include(r => r.Movie).Include(r => r.User).AsQueryable();
        if (mine) query = query.Where(r => r.UserId == users.GetUserId(User));
        ViewBag.Mine = mine;
        return View(await query.OrderByDescending(r => r.CreatedAt).ToListAsync());
    }

    // Posted from the movie details page
    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MovieId,Rating,Comment")] Review review)
    {
        var user = (await users.GetUserAsync(User))!;
        if (!await db.Movies.AnyAsync(m => m.Id == review.MovieId)) return NotFound();

        if (await db.Reviews.AnyAsync(r => r.MovieId == review.MovieId && r.UserId == user.Id))
        {
            TempData["ToastError"] = "You have already reviewed this movie - edit your review instead.";
            return RedirectToAction("Details", "Movies", new { id = review.MovieId });
        }
        if (!ModelState.IsValid)
        {
            TempData["ToastError"] = "The rating must be between 1 and 10.";
            return RedirectToAction("Details", "Movies", new { id = review.MovieId });
        }

        review.UserId = user.Id;
        review.CreatedAt = DateTime.UtcNow;
        db.Reviews.Add(review);
        await db.SaveChangesAsync();
        await notifier.ReviewsChangedAsync(review.MovieId, "posted", review.Rating, user);

        TempData["Toast"] = "Thanks for your review!";
        return Redirect(Url.Action("Details", "Movies", new { id = review.MovieId }) + "#reviews");
    }

    [Authorize]
    public async Task<IActionResult> Edit(int id)
    {
        var review = await db.Reviews.Include(r => r.Movie).FirstOrDefaultAsync(r => r.Id == id);
        if (review is null) return NotFound();
        if (review.UserId != users.GetUserId(User)) return Forbid();
        return View(review);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Rating,Comment")] Review input)
    {
        var review = await db.Reviews.Include(r => r.Movie).FirstOrDefaultAsync(r => r.Id == id);
        if (review is null) return NotFound();
        var user = (await users.GetUserAsync(User))!;
        if (review.UserId != user.Id) return Forbid();

        ModelState.Remove(nameof(Review.UserId));
        if (!ModelState.IsValid)
        {
            input.Movie = review.Movie;
            input.Id = id;
            return View(input);
        }

        review.Rating = input.Rating;
        review.Comment = input.Comment;
        review.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await notifier.ReviewsChangedAsync(review.MovieId, "updated", review.Rating, user);

        TempData["Toast"] = "Review updated.";
        return Redirect(Url.Action("Details", "Movies", new { id = review.MovieId }) + "#reviews");
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnTo)
    {
        var review = await db.Reviews.FindAsync(id);
        if (review is null) return NotFound();
        var user = (await users.GetUserAsync(User))!;
        if (review.UserId != user.Id) return Forbid();

        db.Reviews.Remove(review);
        await db.SaveChangesAsync();
        await notifier.ReviewsChangedAsync(review.MovieId, "deleted", null, user);

        TempData["Toast"] = "Review deleted.";
        return returnTo == "list"
            ? RedirectToAction(nameof(Index), new { mine = true })
            : Redirect(Url.Action("Details", "Movies", new { id = review.MovieId }) + "#reviews");
    }
}
