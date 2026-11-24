namespace MovieCatalog.Services;

/// <summary>Saves uploaded poster images into wwwroot/uploads.</summary>
public class PosterStorage(IWebHostEnvironment env)
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    public const long MaxBytes = 5 * 1024 * 1024;

    public string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0) return null;
        if (file.Length > MaxBytes) return "The poster must be smaller than 5 MB.";
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return AllowedExtensions.Contains(ext) ? null : "Allowed formats: JPG, PNG, WEBP, GIF.";
    }

    public async Task<string> SaveAsync(IFormFile file)
    {
        var folder = Path.Combine(env.WebRootPath, "uploads");
        Directory.CreateDirectory(folder);
        var name = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        await using var stream = File.Create(Path.Combine(folder, name));
        await file.CopyToAsync(stream);
        return $"/uploads/{name}";
    }
}
