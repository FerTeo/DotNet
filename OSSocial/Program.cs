using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OSSocial.Data;
using OSSocial.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4))));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// servicii pentru user custom
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
        options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>(
    );

builder.Services.AddControllersWithViews();

builder.Services.AddCookiePolicy(
    options =>
    {
        options.CheckConsentNeeded = context => false; // DOAR PT TESTING MOMENTAN --> TB SCHIMBAT LA TRUE DUPA
        options.MinimumSameSitePolicy = SameSiteMode.None;
    }
);

var app = builder.Build();

//Pasul 5
// popularea bazei de date
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();

    SeedData.Initialize(services);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles(); // ca sa functioneze wwwroot (frontend yay!)

app.UseRouting();

app.UseAuthentication(); 

app.UseAuthorization();

app.MapStaticAssets();

// mappare postare


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
