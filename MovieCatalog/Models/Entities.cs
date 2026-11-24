using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace MovieCatalog.Models;

/// <summary>Application user (ASP.NET Core Identity). The e-mail address is also the user name.</summary>
public class AppUser : IdentityUser
{
    [StringLength(50)]
    public string? DisplayName { get; set; }

    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    [NotMapped]
    public string Name => string.IsNullOrWhiteSpace(DisplayName) ? (UserName ?? "").Split('@')[0] : DisplayName;
}

public class Genre
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string Name { get; set; } = "";

    public ICollection<Movie> Movies { get; set; } = new List<Movie>();
}

public class Studio
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    [StringLength(60)]
    public string? Country { get; set; }

    [Display(Name = "Founded")]
    [Range(1850, 2100)]
    public int? FoundedYear { get; set; }

    public ICollection<Movie> Movies { get; set; } = new List<Movie>();
}

public class Person
{
    public int Id { get; set; }

    [Required, StringLength(60), Display(Name = "First name")]
    public string FirstName { get; set; } = "";

    [Required, StringLength(60), Display(Name = "Last name")]
    public string LastName { get; set; } = "";

    [DataType(DataType.Date), Display(Name = "Birth date")]
    public DateTime? BirthDate { get; set; }

    [StringLength(1000)]
    public string? Bio { get; set; }

    public ICollection<MovieCast> Credits { get; set; } = new List<MovieCast>();

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();

    [NotMapped]
    public string Initials => $"{FirstName.FirstOrDefault()}{LastName.FirstOrDefault()}".ToUpperInvariant();
}

public class Movie
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Title { get; set; } = "";

    [Display(Name = "Year"), Range(1888, 2100)]
    public int ReleaseYear { get; set; } = DateTime.Today.Year;

    [Display(Name = "Duration (min)"), Range(1, 600)]
    public int DurationMinutes { get; set; } = 120;

    [StringLength(2000)]
    public string? Description { get; set; }

    /// <summary>Relative URL of the poster image inside wwwroot, e.g. /images/posters/inception.jpg</summary>
    public string? PosterPath { get; set; }

    [Display(Name = "Genre"), Range(1, int.MaxValue, ErrorMessage = "Please choose a genre.")]
    public int GenreId { get; set; }
    public Genre? Genre { get; set; }

    [Display(Name = "Studio")]
    public int? StudioId { get; set; }
    public Studio? Studio { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MovieCast> Cast { get; set; } = new List<MovieCast>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}

public enum CastRole
{
    Actor,
    Director,
    Writer
}

/// <summary>Join table between Movie and Person (many-to-many with extra data).</summary>
public class MovieCast
{
    public int Id { get; set; }

    [Display(Name = "Movie"), Range(1, int.MaxValue, ErrorMessage = "Please choose a movie.")]
    public int MovieId { get; set; }
    public Movie? Movie { get; set; }

    [Display(Name = "Person"), Range(1, int.MaxValue, ErrorMessage = "Please choose a person.")]
    public int PersonId { get; set; }
    public Person? Person { get; set; }

    public CastRole Role { get; set; } = CastRole.Actor;

    [StringLength(100), Display(Name = "Character")]
    public string? CharacterName { get; set; }

    [Display(Name = "Billing order"), Range(0, 999)]
    public int BillingOrder { get; set; }
}

public class Review
{
    public int Id { get; set; }

    public int MovieId { get; set; }
    public Movie? Movie { get; set; }

    public string UserId { get; set; } = "";
    public AppUser? User { get; set; }

    [Range(1, 10)]
    public int Rating { get; set; } = 8;

    [StringLength(1000)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
