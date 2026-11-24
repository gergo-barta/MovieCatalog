# Movies – movie catalogue demo

A small IMDb-style web app: browse movies, people, cast and studios, log in, and post reviews that show up **live** in every open browser.

**Tech:** ASP.NET Core 8 MVC · Entity Framework Core + SQLite · ASP.NET Core Identity · SignalR · REST API + Swagger · Bootstrap 5

📖 **Full explanation with diagrams (architecture, database, HTTP, authentication, SignalR):** [DOCUMENTATION.md](DOCUMENTATION.md)

---

## Run it (Rider / Visual Studio)

1. Open `MovieCatalog.sln`.
2. Press **Run** (profile `MovieCatalog`).
3. The browser opens at **http://localhost:5000**.

That's it. On the first run the SQLite database (`MovieCatalog/moviecatalog.db`) is created automatically and filled with demo data. To start fresh, stop the app and delete that file.

Needs the .NET SDK 8 or newer (Visual Studio and Rider install one). The app runs on .NET 8, 9 or 10 (`RollForward=Major`).

From a terminal instead: `dotnet run --project MovieCatalog`

---

## What's inside

### The 6 tables (full CRUD in the website **and** the API)

| Table       | What it stores                                  | Relations                          |
|-------------|-------------------------------------------------|------------------------------------|
| `Movies`    | title, year, duration, description, poster      | → Genre, → Studio                  |
| `Genres`    | name (unique)                                   | 1 genre – many movies              |
| `Studios`   | name, country, founded year                     | 1 studio – many movies             |
| `People`    | actors / directors / writers                    |                                    |
| `MovieCast` | person + movie + role + character name          | **many-to-many** join Movie ↔ Person |
| `Reviews`   | rating 1–10, comment, author                    | → Movie, → User (one per user per movie) |

Plus the ASP.NET Core Identity tables (`AspNetUsers`, …) for login.

Delete rules: deleting a movie removes its cast and reviews, deleting a studio keeps its movies, and a genre that still has movies can't be deleted.

### Authentication
- Not logged in: everything is readable.
- Logged in: create / edit / delete in all tables.
- Reviews belong to their author, so only you can edit or delete your own review.
- Website uses **cookie** login, the API uses **bearer tokens** (same users).

### Real-time (SignalR) – hub at `/hubs/catalog`
- Online counter in the navbar (members + guests).
- New review → the movie page's review list and rating update instantly, everyone gets a toast.
- New movie → toast for everyone with a link to it.
- Works for changes made from the API too (Postman → browser updates live).

---

## Demo script (≈5 minutes)

1. **Home page** – tour the design, the top-rated grid, and the live online counter.
2. **Two windows:** open a normal and an **incognito** window. Log in as **Alice** in one and **Bob** in the other. The counter shows 2 members.
3. Both open the same movie. Bob posts a review, and Alice's page updates without a refresh, with a toast.
4. **CRUD:** add a movie with a poster upload, add cast members to it, edit it, then delete it (confirmation dialog).
5. **Rules:** try editing someone else's review (not possible), or deleting a genre that has movies (blocked with a message).
6. **API:** open **http://localhost:5000/swagger**, or use Postman:
   - Import `postman/Movies.postman_collection.json`.
   - Run **Auth → Login (Alice)**. The token is saved automatically.
   - Run **Movies → Create movie** while the browser is open, and watch the toast appear.
   - Run **Reviews → Edit someone else's review** and get **403**.

No Postman? Open `MovieCatalog/MovieCatalog.http` in Rider and click the green arrows (Rider's built-in HTTP client).

---

## API overview

| Method | URL | Auth |
|---|---|---|
| POST | `/api/auth/login` · `/api/auth/register` | – |
| GET | `/api/movies?search=&genreId=` · `/api/movies/{id}` | – |
| POST / PUT / DELETE | `/api/movies` · `/api/movies/{id}` | Bearer |
| GET / POST / PUT / DELETE | `/api/genres` · `/api/studios` · `/api/people` · `/api/cast` | GET public, rest Bearer |
| GET | `/api/reviews?movieId=` · `/api/reviews/mine` | – / Bearer |
| POST / PUT / DELETE | `/api/reviews` · `/api/reviews/{id}` | Bearer, own reviews only |

Full interactive docs: **/swagger** (click *Authorize* and paste the `accessToken` from login).

---

## Project structure

```
MovieCatalog/
├─ Controllers/          MVC controllers (web pages)
│  └─ Api/               REST API controllers (JSON)
├─ Data/                 AppDbContext (EF Core) + DbSeeder (demo data)
├─ Dtos/                 API request/response records
├─ Hubs/                 SignalR hub
├─ Models/               Entities (Movie, Genre, Studio, Person, MovieCast, Review, AppUser)
├─ Services/             CatalogNotifier (SignalR broadcasts), PosterStorage (uploads)
├─ ViewModels/           Models for the views
├─ Views/                Razor views
└─ wwwroot/              CSS, JS, posters, libraries (all local, works offline)
postman/                 Postman collection
publish-portable.ps1     builds a stand-alone .exe (backup plan)
```

## Backup plan: run without any .NET install

```powershell
./publish-portable.ps1
```

This creates a `portable/` folder. Copy it to a USB stick, run `MovieCatalog.exe` on the demo PC, and open http://localhost:5000.

## Notes
- The poster images in `wwwroot/images/posters` are the official film posters (low-resolution copies from Wikipedia). They are copyrighted by their studios and included only for this private, non-commercial demo, so keep the repository **private**. Movies without a poster get a generated gradient placeholder.
- Everything (Bootstrap, SignalR client, fonts, images) is local, so the demo works without internet once the NuGet packages are restored.
