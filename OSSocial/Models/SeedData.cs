using OSSocial.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace OSSocial.Models
{
    public static class SeedData
    {
        
        private static readonly bool resetDb=false;

        // CREAREA ROLURILOR IN BD
        private static void SeedRoles(ApplicationDbContext context)
        {
            context.Roles.AddRange
            (
                new IdentityRole
                {
                    Id = "2c5e174e-3b0e-446f-86af-483d56fd7210",
                    Name = "Admin",
                    NormalizedName = "Admin".ToUpper()
                },
                new IdentityRole
                {
                    Id = "2c5e174e-3b0e-446f-86af-483d56fd7211",
                    Name = "Editor",
                    NormalizedName = "Editor".ToUpper()
                },
                new IdentityRole
                {
                    Id = "2c5e174e-3b0e-446f-86af-483d56fd7212",
                    Name = "User",
                    NormalizedName = "User".ToUpper()
                }
            );
        }

        // CREAREA USERILOR IN BD
        private static void SeedUsers(ApplicationDbContext context)
        {
            // o noua instanta pe care o vom utiliza pentru crearea parolelor utilizatorilor
            // parolele sunt de tip hash
            var hasher = new PasswordHasher<ApplicationUser>();
            
            context.Users.AddRange
            (
                new ApplicationUser
                {
                    Id = "8e445865-a24d-4543-a6c6-9443d048cdb0", // primary key
                    UserName = "admin",
                    EmailConfirmed = true,
                    NormalizedEmail = "ADMIN@TEST.COM",
                    Email = "admin@test.com",
                    NormalizedUserName = "ADMIN",
                    PasswordHash = hasher.HashPassword(null, "Admin1!"),
                    DisplayName = "Admin",
                    Bio = "Adminul aplicatiei web",
                    PhoneNumber = "0888888888",
                    PhoneNumberConfirmed = true,
                },
                new ApplicationUser
                {
                    Id = "8e445865-a24d-4543-a6c6-9443d048cdb1", // primary key
                    UserName = "edtitor123",
                    EmailConfirmed = true,
                    NormalizedEmail = "EDITOR@TEST.COM",
                    Email = "editor@test.com",
                    NormalizedUserName = "EDITOR123@TEST.COM",
                    PasswordHash = hasher.HashPassword(null, "Editor1!"),
                    DisplayName = "Editor", 
                    Bio = "Your favourite editor",
                    PhoneNumber = "0888888888",
                    PhoneNumberConfirmed = true,
                },
                new ApplicationUser
                {
                    Id = "8e445865-a24d-4543-a6c6-9443d048cdb2", // primary key
                    UserName = "user01",
                    EmailConfirmed = true,
                    NormalizedEmail = "USER@TEST.COM",
                    Email = "user@test.com",
                    NormalizedUserName = "USER01",
                    PasswordHash = hasher.HashPassword(null, "User1!"),
                    DisplayName = "User",
                    Bio = "Your favourite user",
                    PhoneNumber = "0888888888",
                    PhoneNumberConfirmed = true,
                },
                new ApplicationUser
                {
                    Id = "8e445865-a24d-4543-a6c6-9443d048cdb3", // primary key
                    UserName = "fernando",
                    EmailConfirmed = true,
                    NormalizedEmail = "FERNANDO@TEST.COM",
                    Email = "FERNANDO@test.com",
                    NormalizedUserName = "FERNANDO",
                    PasswordHash = hasher.HashPassword(null, "Fernando1!"),
                    DisplayName = "Fernando-Emanuel",
                    Bio = "Your one and only fernando",
                }
            );
        }


        // ASOCIEREA USER-ROLE
        private static void SeedUserRoles(ApplicationDbContext context)
        {
            context.UserRoles.AddRange
            (
                new IdentityUserRole<string>
                {
                    RoleId = "2c5e174e-3b0e-446f-86af-483d56fd7210", //rol admin
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb0"
                },
                new IdentityUserRole<string>
                {
                    RoleId = "2c5e174e-3b0e-446f-86af-483d56fd7211", //rol editor
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb1"
                },
                new IdentityUserRole<string>
                {
                    RoleId = "2c5e174e-3b0e-446f-86af-483d56fd7212", //rol user
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb2"
                },
                new IdentityUserRole<string>
                {
                    RoleId = "2c5e174e-3b0e-446f-86af-483d56fd7212", //rol user
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb3"
                }
            );
        }
        
        // RESETARE BD
        private static void ResetDatabase(ApplicationDbContext context)
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }
        
        
        
        
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                       serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                
                if (resetDb == true)
                {
                    ResetDatabase(context);
                }
                
                
                // Verificam daca in baza de date exista cel putin un rol
                // insemnand ca a fost rulat codul
                // De aceea facem return pentru a nu insera rolurile inca o data
                // Acesta metoda trebuie sa se execute o singura data
                if (context.Roles.Any())
                {
                    return; // baza de date contine deja roluri
                }


                
                SeedRoles(context);
                SeedUsers(context);
                SeedUserRoles(context);
        
                context.SaveChanges();
            }
        }
        
        
    }
}