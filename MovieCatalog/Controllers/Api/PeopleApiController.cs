using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieCatalog.Data;
using MovieCatalog.Dtos;
using MovieCatalog.Models;

namespace MovieCatalog.Controllers.Api;

[ApiController, Route("api/people"), Tags("People")]
public class PeopleApiController(AppDbContext db) : ControllerBase
{
    private static readonly System.Linq.Expressions.Expression<Func<Person, PersonDto>> ToDto =
        p => new PersonDto(p.Id, p.FirstName, p.LastName, p.FirstName + " " + p.LastName, p.BirthDate, p.Bio, p.Credits.Count);

    [HttpGet]
    public async Task<List<PersonDto>> GetAll(string? search)
    {
        var query = db.People.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => EF.Functions.Like(p.FirstName + " " + p.LastName, $"%{search}%"));
        return await query.OrderBy(p => p.LastName).Select(ToDto).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PersonDto>> Get(int id)
    {
        var dto = await db.People.Where(p => p.Id == id).Select(ToDto).FirstOrDefaultAsync();
        return dto is null ? NotFound() : dto;
    }

    [HttpPost, Authorize]
    public async Task<ActionResult<PersonDto>> Create(PersonInput input)
    {
        var person = new Person { FirstName = input.FirstName, LastName = input.LastName, BirthDate = input.BirthDate, Bio = input.Bio };
        db.People.Add(person);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = person.Id }, ToDto.Compile()(person));
    }

    [HttpPut("{id:int}"), Authorize]
    public async Task<IActionResult> Update(int id, PersonInput input)
    {
        var person = await db.People.FindAsync(id);
        if (person is null) return NotFound();
        person.FirstName = input.FirstName;
        person.LastName = input.LastName;
        person.BirthDate = input.BirthDate;
        person.Bio = input.Bio;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}"), Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var person = await db.People.FindAsync(id);
        if (person is null) return NotFound();
        db.People.Remove(person);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
