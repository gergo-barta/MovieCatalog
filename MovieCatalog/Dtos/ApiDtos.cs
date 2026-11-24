using System.ComponentModel.DataAnnotations;
using MovieCatalog.Models;

namespace MovieCatalog.Dtos;

// ---------- read models (what the API returns) ----------

public record GenreDto(int Id, string Name, int MovieCount);

public record StudioDto(int Id, string Name, string? Country, int? FoundedYear, int MovieCount);

public record PersonDto(int Id, string FirstName, string LastName, string FullName, DateTime? BirthDate, string? Bio, int CreditCount);

public record MovieDto(int Id, string Title, int ReleaseYear, int DurationMinutes, string? Description, string? PosterUrl,
    int GenreId, string Genre, int? StudioId, string? Studio, double? AverageRating, int ReviewCount);

public record MovieDetailDto(MovieDto Movie, List<CastDto> Cast, List<ReviewDto> Reviews);

public record CastDto(int Id, int MovieId, string MovieTitle, int PersonId, string PersonName, CastRole Role, string? CharacterName, int BillingOrder);

public record ReviewDto(int Id, int MovieId, string MovieTitle, string UserId, string Author, int Rating, string? Comment, DateTime CreatedAt, DateTime? UpdatedAt);

// ---------- write models (request bodies) ----------

public record GenreInput([Required, StringLength(50)] string Name);

public record StudioInput([Required, StringLength(100)] string Name, [StringLength(60)] string? Country, [Range(1850, 2100)] int? FoundedYear);

public record PersonInput([Required, StringLength(60)] string FirstName, [Required, StringLength(60)] string LastName,
    DateTime? BirthDate, [StringLength(1000)] string? Bio);

public record MovieInput([Required, StringLength(150)] string Title, [Range(1888, 2100)] int ReleaseYear, [Range(1, 600)] int DurationMinutes,
    [StringLength(2000)] string? Description, [Range(1, int.MaxValue)] int GenreId, int? StudioId);

public record CastInput([Range(1, int.MaxValue)] int MovieId, [Range(1, int.MaxValue)] int PersonId, CastRole Role,
    [StringLength(100)] string? CharacterName, [Range(0, 999)] int BillingOrder);

public record ReviewInput([Range(1, int.MaxValue)] int MovieId, [Range(1, 10)] int Rating, [StringLength(1000)] string? Comment);

public record ReviewUpdateInput([Range(1, 10)] int Rating, [StringLength(1000)] string? Comment);
