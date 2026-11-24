using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MovieCatalog.Models;

namespace MovieCatalog.Data;

/// <summary>Creates the SQLite database on first run and fills it with demo data.</summary>
public static class DbSeeder
{
    public const string DemoPassword = "Demo123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        await db.Database.EnsureCreatedAsync();
        if (await db.Movies.AnyAsync()) return;

        // ---------- users ----------
        var alice = await CreateUser(users, "alice@demo.com", "Alice");
        var bob = await CreateUser(users, "bob@demo.com", "Bob");

        // ---------- genres ----------
        var genres = new[] { "Sci-Fi", "Action", "Crime", "Drama", "Animation", "Thriller", "Comedy", "Adventure", "Romance", "Horror" }
            .ToDictionary(n => n, n => new Genre { Name = n });
        db.Genres.AddRange(genres.Values);

        // ---------- studios ----------
        var studios = new Dictionary<string, Studio>
        {
            ["Warner"] = new() { Name = "Warner Bros. Pictures", Country = "United States", FoundedYear = 1923 },
            ["Paramount"] = new() { Name = "Paramount Pictures", Country = "United States", FoundedYear = 1912 },
            ["Miramax"] = new() { Name = "Miramax", Country = "United States", FoundedYear = 1979 },
            ["CastleRock"] = new() { Name = "Castle Rock Entertainment", Country = "United States", FoundedYear = 1987 },
            ["Ghibli"] = new() { Name = "Studio Ghibli", Country = "Japan", FoundedYear = 1985 },
            ["CJ"] = new() { Name = "CJ Entertainment", Country = "South Korea", FoundedYear = 1995 },
            ["Legendary"] = new() { Name = "Legendary Pictures", Country = "United States", FoundedYear = 2000 },
            ["SonyClassics"] = new() { Name = "Sony Pictures Classics", Country = "United States", FoundedYear = 1992 },
            ["Searchlight"] = new() { Name = "Searchlight Pictures", Country = "United States", FoundedYear = 1994 },
        };
        db.Studios.AddRange(studios.Values);

        // ---------- people ----------
        var people = new Dictionary<string, Person>();
        Person P(string first, string last, string? born = null, string? bio = null)
        {
            var key = $"{first} {last}";
            if (!people.TryGetValue(key, out var p))
            {
                p = new Person { FirstName = first, LastName = last, BirthDate = born is null ? null : DateTime.Parse(born), Bio = bio };
                people[key] = p;
            }
            return p;
        }

        var nolan = P("Christopher", "Nolan", "1970-07-30", "British-American director, writer and producer known for ambitious, time-bending blockbusters.");
        var hardy = P("Tom", "Hardy", "1977-09-15", "English actor known for intense physical performances.");

        // ---------- movies ----------
        var movies = new List<Movie>();
        void M(string title, int year, int minutes, string genre, string studio, string poster, string description,
               Person director, params (Person person, string character)[] cast)
        {
            var m = new Movie
            {
                Title = title, ReleaseYear = year, DurationMinutes = minutes, Description = description,
                Genre = genres[genre], Studio = studios[studio], PosterPath = $"/images/posters/{poster}",
                CreatedAt = DateTime.UtcNow.AddDays(-movies.Count - 1)
            };
            m.Cast.Add(new MovieCast { Person = director, Role = CastRole.Director, BillingOrder = 0 });
            var order = 1;
            foreach (var (person, character) in cast)
                m.Cast.Add(new MovieCast { Person = person, Role = CastRole.Actor, CharacterName = character, BillingOrder = order++ });
            movies.Add(m);
        }

        M("Inception", 2010, 148, "Sci-Fi", "Warner", "inception.jpg",
            "A thief who steals secrets from inside people's dreams is offered one last job: planting an idea instead of taking one.",
            nolan,
            (P("Leonardo", "DiCaprio", "1974-11-11", "American actor and producer."), "Dom Cobb"),
            (P("Joseph", "Gordon-Levitt", "1981-02-17", "American actor and filmmaker."), "Arthur"),
            (P("Elliot", "Page", "1987-02-21", "Canadian actor and producer."), "Ariadne"),
            (hardy, "Eames"));

        M("The Dark Knight", 2008, 152, "Action", "Warner", "the-dark-knight.jpg",
            "Batman faces his greatest test when a chaos-loving criminal mastermind plunges Gotham City into anarchy.",
            nolan,
            (P("Christian", "Bale", "1974-01-30", "English actor known for dramatic physical transformations."), "Bruce Wayne / Batman"),
            (P("Heath", "Ledger", "1979-04-04", "Australian actor (1979-2008)."), "The Joker"),
            (P("Gary", "Oldman", "1958-03-21", "English actor and filmmaker."), "James Gordon"));

        M("Interstellar", 2014, 169, "Sci-Fi", "Paramount", "interstellar.jpg",
            "With Earth dying, a former pilot leads a team through a wormhole in search of a new home for humanity.",
            nolan,
            (P("Matthew", "McConaughey", "1969-11-04", "American actor and producer."), "Cooper"),
            (P("Anne", "Hathaway", "1982-11-12", "American actress."), "Brand"),
            (P("Jessica", "Chastain", "1977-03-24", "American actress and producer."), "Murph"));

        M("The Matrix", 1999, 136, "Sci-Fi", "Warner", "the-matrix.png",
            "A hacker discovers that the world he lives in is a simulation and joins a rebellion against its machine creators.",
            P("Lana", "Wachowski", "1965-06-21", "American filmmaker and co-creator of The Matrix."),
            (P("Keanu", "Reeves", "1964-09-02", "Canadian actor."), "Neo"),
            (P("Laurence", "Fishburne", "1961-07-30", "American actor."), "Morpheus"),
            (P("Carrie-Anne", "Moss", "1967-08-21", "Canadian actress."), "Trinity"));

        M("Pulp Fiction", 1994, 154, "Crime", "Miramax", "pulp-fiction.jpg",
            "Interlocking stories of hitmen, a boxer and a gangster's wife collide in the Los Angeles criminal underworld.",
            P("Quentin", "Tarantino", "1963-03-27", "American director and screenwriter."),
            (P("John", "Travolta", "1954-02-18", "American actor."), "Vincent Vega"),
            (P("Samuel L.", "Jackson", "1948-12-21", "American actor and producer."), "Jules Winnfield"),
            (P("Uma", "Thurman", "1970-04-29", "American actress."), "Mia Wallace"));

        M("The Shawshank Redemption", 1994, 142, "Drama", "CastleRock", "the-shawshank-redemption.jpg",
            "A banker sentenced to life in prison forms a lasting friendship and never gives up hope of freedom.",
            P("Frank", "Darabont", "1959-01-28", "American director and screenwriter."),
            (P("Tim", "Robbins", "1958-10-16", "American actor and director."), "Andy Dufresne"),
            (P("Morgan", "Freeman", "1937-06-01", "American actor and narrator."), "Ellis \"Red\" Redding"));

        M("Spirited Away", 2001, 125, "Animation", "Ghibli", "spirited-away.png",
            "A young girl wanders into a world of spirits and must work in a bathhouse to free herself and her parents.",
            P("Hayao", "Miyazaki", "1941-01-05", "Japanese animator, director and co-founder of Studio Ghibli."),
            (P("Rumi", "Hiiragi", null, "Japanese actress."), "Chihiro (voice)"));

        M("Parasite", 2019, 132, "Thriller", "CJ", "parasite.png",
            "A poor family schemes its way into the household of a wealthy family, with unexpected consequences.",
            P("Bong", "Joon-ho", "1969-09-14", "South Korean director and screenwriter."),
            (P("Song", "Kang-ho", "1967-01-17", "South Korean actor."), "Kim Ki-taek"),
            (P("Choi", "Woo-shik", null, "South Korean actor."), "Kim Ki-woo"),
            (P("Park", "So-dam", null, "South Korean actress."), "Kim Ki-jung"));

        M("Dune", 2021, 155, "Sci-Fi", "Legendary", "dune.jpg",
            "The heir of a noble house is drawn into a deadly struggle over the most valuable planet in the universe.",
            P("Denis", "Villeneuve", "1967-10-03", "Canadian director."),
            (P("Timothée", "Chalamet", "1995-12-27", "American actor."), "Paul Atreides"),
            (P("Rebecca", "Ferguson", "1983-10-19", "Swedish actress."), "Lady Jessica"),
            (P("Zendaya", "Coleman", "1996-09-01", "American actress and singer, credited as Zendaya."), "Chani"));

        M("Mad Max: Fury Road", 2015, 120, "Action", "Warner", "mad-max-fury-road.jpg",
            "In a desert wasteland, a drifter and a rebel warrior flee a tyrant across the sands in a relentless chase.",
            P("George", "Miller", "1945-03-03", "Australian director and producer."),
            (hardy, "Max Rockatansky"),
            (P("Charlize", "Theron", "1975-08-07", "South African-American actress and producer."), "Imperator Furiosa"));

        M("Whiplash", 2014, 107, "Drama", "SonyClassics", "whiplash.jpg",
            "An ambitious young jazz drummer is pushed to his limits by a ruthless music instructor.",
            P("Damien", "Chazelle", "1985-01-19", "American director and screenwriter."),
            (P("Miles", "Teller", "1987-02-20", "American actor."), "Andrew Neiman"),
            (P("J.K.", "Simmons", "1955-01-09", "American actor."), "Terence Fletcher"));

        M("The Grand Budapest Hotel", 2014, 99, "Comedy", "Searchlight", "the-grand-budapest-hotel.png",
            "A legendary concierge and his young lobby boy are caught up in the theft of a priceless painting.",
            P("Wes", "Anderson", "1969-05-01", "American director known for his symmetrical, colourful style."),
            (P("Ralph", "Fiennes", "1962-12-22", "English actor."), "M. Gustave"),
            (P("Tony", "Revolori", null, "American actor."), "Zero Moustafa"));

        db.People.AddRange(people.Values);
        db.Movies.AddRange(movies);

        // ---------- reviews ----------
        void R(string title, AppUser user, int rating, string comment, int daysAgo) =>
            db.Reviews.Add(new Review
            {
                Movie = movies.Single(m => m.Title == title), UserId = user.Id, Rating = rating,
                Comment = comment, CreatedAt = DateTime.UtcNow.AddDays(-daysAgo).AddHours(-rating)
            });

        R("Inception", alice, 9, "Still blows my mind every time. That ending!", 6);
        R("Inception", bob, 8, "Great concept, and the score is incredible.", 5);
        R("The Dark Knight", alice, 10, "The best superhero movie ever made.", 4);
        R("The Dark Knight", bob, 9, "An unforgettable villain performance.", 3);
        R("Interstellar", bob, 9, "Emotional and huge in scale. The docking scene!", 7);
        R("The Matrix", alice, 9, "Changed action movies forever.", 8);
        R("Pulp Fiction", bob, 8, "Brilliant dialogue, very rewatchable.", 2);
        R("The Shawshank Redemption", alice, 10, "A perfect film about hope.", 9);
        R("Spirited Away", alice, 10, "Pure magic, beautiful animation.", 1);
        R("Parasite", bob, 9, "Didn't see that twist coming at all.", 2);
        R("Dune", alice, 8, "Visually stunning, can't wait for the next part.", 3);
        R("Mad Max: Fury Road", bob, 9, "Two hours of pure adrenaline.", 4);
        R("Whiplash", alice, 9, "Intense from the first beat to the last.", 5);

        await db.SaveChangesAsync();
    }

    private static async Task<AppUser> CreateUser(UserManager<AppUser> users, string email, string displayName)
    {
        var user = new AppUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = displayName };
        var result = await users.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        return user;
    }
}
