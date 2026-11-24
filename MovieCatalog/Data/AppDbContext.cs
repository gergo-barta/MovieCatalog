using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MovieCatalog.Models;

namespace MovieCatalog.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Studio> Studios => Set<Studio>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<MovieCast> MovieCast => Set<MovieCast>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Genre>().HasIndex(g => g.Name).IsUnique();

        b.Entity<Movie>()
            .HasOne(m => m.Genre).WithMany(g => g.Movies)
            .HasForeignKey(m => m.GenreId)
            .OnDelete(DeleteBehavior.Restrict);      // can't delete a genre that still has movies

        b.Entity<Movie>()
            .HasOne(m => m.Studio).WithMany(s => s.Movies)
            .HasForeignKey(m => m.StudioId)
            .OnDelete(DeleteBehavior.SetNull);       // deleting a studio keeps its movies

        b.Entity<MovieCast>()
            .HasOne(c => c.Movie).WithMany(m => m.Cast)
            .HasForeignKey(c => c.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<MovieCast>()
            .HasOne(c => c.Person).WithMany(p => p.Credits)
            .HasForeignKey(c => c.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<MovieCast>().Property(c => c.Role).HasConversion<string>().HasMaxLength(20);

        b.Entity<Review>()
            .HasOne(r => r.Movie).WithMany(m => m.Reviews)
            .HasForeignKey(r => r.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Review>()
            .HasOne(r => r.User).WithMany(u => u.Reviews)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // one review per user per movie
        b.Entity<Review>().HasIndex(r => new { r.MovieId, r.UserId }).IsUnique();
    }
}
