# OSSocial

A social networking web application built with ASP.NET Core 9.0 MVC. Users can create posts, follow each other, react and comment, join groups, and receive notifications. Post content is moderated automatically using the Google Gemini AI API.

## Features

- **Authentication** — Registration, login, and role-based access control via ASP.NET Identity
- **Posts** — Create, edit, delete, and explore posts; image uploads via Cloudinary
- **Comments** — Add and edit comments on posts
- **Reactions** — React to posts
- **Follow system** — Follow/unfollow users; support for private accounts
- **Groups** — Create and join groups; group profile pages
- **Notifications** — In-app notification feed
- **User profiles** — Display name, bio, profile picture, follower/following counts
- **Content moderation** — AI-powered content analysis using Google Gemini to screen posts and comments
- **Seeded data** — Roles, demo users, groups, and posts are automatically inserted on first run

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 9.0 MVC |
| ORM | Entity Framework Core 9.0 |
| Database | MySQL 8.4 |
| Identity | ASP.NET Core Identity |
| AI | Google Generative AI (Gemini) |
| Frontend | Bootstrap, jQuery |
| Containerisation | Docker / Docker Compose |

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) and Docker Compose, **or** [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- A [Google AI Studio](https://aistudio.google.com/app/apikey) API key
- (Optional) A [Cloudinary](https://cloudinary.com/) account for image uploads

## How to Run

### Option 1 — Docker Compose (recommended)

1. Clone the repository:
   ```bash
   git clone <repo-url>
   cd DotNet
   ```

2. Create a `.env` file in the project root (next to `docker-compose.yml`) with your secrets:
   ```env
   GOOGLE_API_KEY=your_google_ai_api_key

   # Optional — defaults shown below
   MYSQL_DATABASE=OS_DB
   MYSQL_USER=appuser
   MYSQL_PASSWORD=apppass
   MYSQL_ROOT_PASSWORD=rootpass
   APP_PORT=8080
   ```

3. Add the following tools/ packages to your app container (docker exec commands): 
   
   ```bash
    dotnet tool install --global dotnet-ef

    dotnet add package Pomelo.EntityFrameworkCore.MySql --version 9.0.0
    ```

4. Start the application:
   ```bash
   docker compose up -d
   ```

5. Open your browser at [http://localhost:8080](http://localhost:8080).

> Migrations are applied and the database is seeded automatically on startup.

---

### Option 2 — Local (dotnet run)

1. Make sure you have the .NET 9 SDK and a running MySQL 8.4 instance.

2. Update `OSSocial/appsettings.json` with your real values:
   ```json
   {
     "GoogleAi": {
       "ApiKey": "your_google_ai_api_key"
     },
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Port=3306;Database=OS_DB;User=appuser;Password=apppass;"
     }
   }
   ```

3. Restore dependencies and run:
   ```bash
   cd OSSocial
   dotnet run
   ```

4. Open your browser at the URL printed in the terminal

## Default Seeded Accounts

After the first run, demo accounts are available. Check `OSSocial/Models/SeedData.cs` for the exact usernames and passwords.

| Role | Purpose |
|---|---|
| Admin | Full administrative access |
| User | Standard account |

## Project Structure

```
OSSocial/
├── Controllers/      # MVC controllers (Posts, Comments, Groups, Profiles, …)
├── Models/           # Entity models and ApplicationUser
├── Views/            # Razor views
├── Data/             # EF Core DbContext
├── Migrations/       # EF Core migrations
├── Services/         # ContentAnalysisService (Google Gemini integration)
├── Areas/Identity/   # ASP.NET Identity Razor pages (login, register, …)
└── wwwroot/          # Static assets (CSS, JS, images)
```
