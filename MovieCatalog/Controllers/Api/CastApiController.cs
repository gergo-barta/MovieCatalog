using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieCatalog.Data;
using MovieCatalog.Dtos;
using MovieCatalog.Models;

namespace MovieCatalog.Controllers.Api;

[ApiController, Route("api/cast"), Tags("Cast")]
public class CastApiController(AppDbContext db) : ControllerBase
{
    internal static readonly System.Linq.Expressions.Expression<Func<MovieCast, CastDto>> ToDto =
        c => new CastDto(c.Id, c.MovieId, c.Movie!.Title, c.PersonId, c.Person!.FirstName + " " + c.Person.LastName,
            c.Role, c.CharacterName, c.BillingOrder);

    /// <summary>All cast entries, optionally for one movie or one person.</summary>
    [HttpGet]
    public async Task<List<CastDto>> GetAll(int? movieId, int? personId)
    {
        var query = db.MovieCast.AsQueryable();
        if (movieId is > 0) query = query.Where(c => c.MovieId == movieId);
        if (personId is > 0) query = query.Where(c => c.PersonId == personId);
        return await query.OrderBy(c => c.MovieId).ThenBy(c => c.BillingOrder).Select(ToDto).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CastDto>> Get(int id)
    {
        var dto = await db.MovieCast.Where(c => c.Id == id).Select(ToDto).FirstOrDefaultAsync();
        return dto is null ? NotFound() : dto;
    }

    [HttpPost, Authorize]
    public async Task<ActionResult<CastDto>> Create(CastInput input)
    {
        if (await Validate(input) is { } problem) return problem;
        var cast = new MovieCast
        {
            MovieId = input.MovieId, PersonId = input.PersonId, Role = input.Role,
            CharacterName = input.CharacterName, BillingOrder = input.BillingOrder
        };
        db.MovieCast.Add(cast);
        await db.SaveChangesAsync();
        var dto = await db.MovieCast.Where(c => c.Id == cast.Id).Select(ToDto).FirstAsync();
        return CreatedAtAction(nameof(Get), new { id = cast.Id }, dto);
    }

    [HttpPut("{id:int}"), Authorize]
    public async Task<IActionResult> Update(int id, CastInput input)
    {
        var cast = await db.MovieCast.FindAsync(id);
        if (cast is null) return NotFound();
        if (await Validate(input) is { } problem) return problem;
        cast.MovieId = input.MovieId;
        cast.PersonId = input.PersonId;
        cast.Role = input.Role;
        cast.CharacterName = input.CharacterName;
        cast.BillingOrder = input.BillingOrder;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}"), Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var cast = await db.MovieCast.FindAsync(id);
        if (cast is null) return NotFound();
        db.MovieCast.Remove(cast);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<ActionResult?> Validate(CastInput input)
    {
        if (!await db.Movies.AnyAsync(m => m.Id == input.MovieId)) return BadRequest(new { error = $"Movie {input.MovieId} does not exist." });
        if (!await db.People.AnyAsync(p => p.Id == input.PersonId)) return BadRequest(new { error = $"Person {input.PersonId} does not exist." });
        return null;
    }
}
