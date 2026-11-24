using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieCatalog.Data;
using MovieCatalog.Dtos;
using MovieCatalog.Models;
using MovieCatalog.Services;

namespace MovieCatalog.Controllers.Api;

/// <summary>Reviews are always written as the logged-in user; only the author can change or delete them.</summary>
[ApiController, Route("api/reviews"), Tags("Reviews")]
public class ReviewsApiController(AppDbContext db, UserManager<AppUser> users, CatalogNotifier notifier) : ControllerBase
{
    internal static readonly System.Linq.Expressions.Expression<Func<Review, ReviewDto>> ToDto =
        r => new ReviewDto(r.Id, r.MovieId, r.Movie!.Title, r.UserId,
            r.User!.DisplayName ?? r.User.UserName ?? "", r.Rating, r.Comment, r.CreatedAt, r.UpdatedAt);

    [HttpGet]
    public async Task<List<ReviewDto>> GetAll(int? movieId)
    {
        var query = db.Reviews.AsQueryable();
        if (movieId is > 0) query = query.Where(r => r.MovieId == movieId);
        return await query.OrderByDescending(r => r.CreatedAt).Select(ToDto).ToListAsync();
    }

    /// <summary>Reviews written by the logged-in user.</summary>
    [HttpGet("mine"), Authorize]
    public async Task<List<ReviewDto>> Mine()
    {
        var userId = users.GetUserId(User);
        return await db.Reviews.Where(r => r.UserId == userId).OrderByDescending(r => r.CreatedAt).Select(ToDto).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReviewDto>> Get(int id)
    {
        var dto = await db.Reviews.Where(r => r.Id == id).Select(ToDto).FirstOrDefaultAsync();
        return dto is null ? NotFound() : dto;
    }

    [HttpPost, Authorize]
    public async Task<ActionResult<ReviewDto>> Create(ReviewInput input)
    {
        var user = (await users.GetUserAsync(User))!;
        if (!await db.Movies.AnyAsync(m => m.Id == input.MovieId)) return BadRequest(new { error = $"Movie {input.MovieId} does not exist." });
        if (await db.Reviews.AnyAsync(r => r.MovieId == input.MovieId && r.UserId == user.Id))
            return Conflict(new { error = "You have already reviewed this movie." });

        var review = new Review { MovieId = input.MovieId, UserId = user.Id, Rating = input.Rating, Comment = input.Comment };
        db.Reviews.Add(review);
        await db.SaveChangesAsync();
        await notifier.ReviewsChangedAsync(review.MovieId, "posted", review.Rating, user);

        var dto = await db.Reviews.Where(r => r.Id == review.Id).Select(ToDto).FirstAsync();
        return CreatedAtAction(nameof(Get), new { id = review.Id }, dto);
    }

    [HttpPut("{id:int}"), Authorize]
    public async Task<IActionResult> Update(int id, ReviewUpdateInput input)
    {
        var review = await db.Reviews.FindAsync(id);
        if (review is null) return NotFound();
        var user = (await users.GetUserAsync(User))!;
        if (review.UserId != user.Id) return StatusCode(StatusCodes.Status403Forbidden, new { error = "You can only edit your own reviews." });

        review.Rating = input.Rating;
        review.Comment = input.Comment;
        review.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await notifier.ReviewsChangedAsync(review.MovieId, "updated", review.Rating, user);
        return NoContent();
    }

    [HttpDelete("{id:int}"), Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await db.Reviews.FindAsync(id);
        if (review is null) return NotFound();
        var user = (await users.GetUserAsync(User))!;
        if (review.UserId != user.Id) return StatusCode(StatusCodes.Status403Forbidden, new { error = "You can only delete your own reviews." });

        db.Reviews.Remove(review);
        await db.SaveChangesAsync();
        await notifier.ReviewsChangedAsync(review.MovieId, "deleted", null, user);
        return NoContent();
    }
}
