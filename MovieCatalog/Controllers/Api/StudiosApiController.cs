using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieCatalog.Data;
using MovieCatalog.Dtos;
using MovieCatalog.Models;

namespace MovieCatalog.Controllers.Api;

[ApiController, Route("api/studios"), Tags("Studios")]
public class StudiosApiController(AppDbContext db) : ControllerBase
{
    private static readonly System.Linq.Expressions.Expression<Func<Studio, StudioDto>> ToDto =
        s => new StudioDto(s.Id, s.Name, s.Country, s.FoundedYear, s.Movies.Count);

    [HttpGet]
    public async Task<List<StudioDto>> GetAll() => await db.Studios.OrderBy(s => s.Name).Select(ToDto).ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StudioDto>> Get(int id)
    {
        var dto = await db.Studios.Where(s => s.Id == id).Select(ToDto).FirstOrDefaultAsync();
        return dto is null ? NotFound() : dto;
    }

    [HttpPost, Authorize]
    public async Task<ActionResult<StudioDto>> Create(StudioInput input)
    {
        var studio = new Studio { Name = input.Name, Country = input.Country, FoundedYear = input.FoundedYear };
        db.Studios.Add(studio);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = studio.Id }, ToDto.Compile()(studio));
    }

    [HttpPut("{id:int}"), Authorize]
    public async Task<IActionResult> Update(int id, StudioInput input)
    {
        var studio = await db.Studios.FindAsync(id);
        if (studio is null) return NotFound();
        studio.Name = input.Name;
        studio.Country = input.Country;
        studio.FoundedYear = input.FoundedYear;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}"), Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var studio = await db.Studios.FindAsync(id);
        if (studio is null) return NotFound();
        db.Studios.Remove(studio);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
