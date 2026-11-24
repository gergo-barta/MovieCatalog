using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using MovieCatalog.Models;

namespace MovieCatalog.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; } = true;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required, StringLength(50), Display(Name = "Display name")]
    public string DisplayName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required, DataType(DataType.Password), Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";
}

public record PosterModel(string? Path, string Title, int Year);

public record MovieCard(int Id, string Title, int Year, int Duration, string? PosterPath, string Genre, double? Average, int ReviewCount);

public class HomeViewModel
{
    public List<MovieCard> TopRated { get; set; } = [];
}

public class SearchViewModel
{
    public string Query { get; set; } = "";
    public List<MovieCard> Movies { get; set; } = [];
    public List<Person> People { get; set; } = [];
}

public class MovieIndexViewModel
{
    public List<MovieCard> Movies { get; set; } = [];
    public string? Search { get; set; }
    public int? GenreId { get; set; }
    public string Sort { get; set; } = "rating";
    public SelectList Genres { get; set; } = default!;
}

public class MovieDetailsViewModel
{
    public Movie Movie { get; set; } = default!;
    public double? Average { get; set; }
    public int ReviewCount { get; set; }
    public List<MovieCast> Directors { get; set; } = [];
    public List<MovieCast> Actors { get; set; } = [];
    public List<Review> Reviews { get; set; } = [];
    public Review? MyReview { get; set; }
}

public class MovieFormViewModel
{
    public Movie Movie { get; set; } = new();
    public IFormFile? Poster { get; set; }
    public bool RemovePoster { get; set; }
}
