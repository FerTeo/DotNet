using OSSocial.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace OSSocial.Models
{
    public static class SeedData
    {
        
        private static void Seed_Roles(ApplicationDbContext context)
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
            
            context.SaveChanges();
        }
        private static void Seed_Users(ApplicationDbContext context)
        {
            var hasher = new PasswordHasher<ApplicationUser>();

            var admin = new ApplicationUser
            {
                Id = "8e445865-a24d-4543-a6c6-9443d048cdb0",
                UserName = "admin",
                EmailConfirmed = true,
                NormalizedEmail = "ADMIN@TEST.COM",
                Email = "admin@test.com",
                NormalizedUserName = "ADMIN",
                DisplayName = "Admin",
                Bio = "Admin of the web application",
                PhoneNumber = "0888888888",
                PhoneNumberConfirmed = true,
            };
            admin.PasswordHash = hasher.HashPassword(admin, "Admin123!");

            var editor = new ApplicationUser
            {
                Id = "8e445865-a24d-4543-a6c6-9443d048cdb1",
                UserName = "editor",
                EmailConfirmed = true,
                NormalizedEmail = "EDITOR@TEST.COM",
                Email = "editor@test.com",
                NormalizedUserName = "editor",
                DisplayName = "Editor",
                Bio = "Your favourite editor",
                PhoneNumber = "0888888888",
                PhoneNumberConfirmed = true,
            };
            editor.PasswordHash = hasher.HashPassword(editor, "Editor123!");

            var user = new ApplicationUser
            {
                Id = "8e445865-a24d-4543-a6c6-9443d048cdb2",
                UserName = "user",
                EmailConfirmed = true,
                NormalizedEmail = "USER@TEST.COM",
                Email = "user@test.com",
                NormalizedUserName = "USER",
                DisplayName = "User",
                Bio = "Your favourite user",
                PhoneNumber = "0888888888",
                PhoneNumberConfirmed = true,
            };
            user.PasswordHash = hasher.HashPassword(user, "User123!");

            var fernando = new ApplicationUser
            {
                Id = "8e445865-a24d-4543-a6c6-9443d048cdb3",
                UserName = "fernando",
                EmailConfirmed = true,
                NormalizedEmail = "FERNANDO@TEST.COM",
                Email = "fernando@test.com",
                PhoneNumber = "0666666666",
                NormalizedUserName = "FERNANDO",
                DisplayName = "Fernando-Emanuel",
                Bio = "Your one and only fernando",
                ProfileImageUrl = "/images/b19f355f-9ec5-4047-bd3d-d129db850b79_emo.JPG",
                IsPrivate = true
            };
            fernando.PasswordHash = hasher.HashPassword(fernando, "Fernando1!");

            var r0scat = new ApplicationUser
            {
                Id = "8e445865-a24d-4543-a6c6-9443d048cdb4",
                UserName = "r0scat",
                EmailConfirmed = true,
                NormalizedEmail = "teo@test.COM",
                Email = "teo@test.com",
                PhoneNumber = "0676767677",
                NormalizedUserName = "R0SCAT",
                DisplayName = "Teo",
                ProfileImageUrl = "/images/04def228-790e-403d-ae29-f5bebf347492_cute.JPG",
                IsPrivate = false
            };
            r0scat.PasswordHash = hasher.HashPassword(r0scat, "Teo123!");

            var musiclover = new ApplicationUser
            {
                Id = "8e445865-a24d-4543-a6c6-9443d048cdb5",
                UserName = "musiclover",
                EmailConfirmed = true,
                NormalizedEmail = "MUSICLOVER@TEST.COM",
                Email = "musiclover@test.com",
                PhoneNumber = "0666666666",
                NormalizedUserName = "MUSICLOVER",
                DisplayName = "Music Lover",
                Bio = "Hi! I really love music",
                ProfileImageUrl = "/images/2424195d-2666-4529-bed6-046bdf17f19b_ipod.jpg",
                IsPrivate = true
            };
            musiclover.PasswordHash = hasher.HashPassword(musiclover, "Musiclover123!");

            context.Users.AddRange(admin, editor, user, fernando, r0scat, musiclover);
            context.SaveChanges();
        }
        
        private static void Seed_UserRoles(ApplicationDbContext context)
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
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb2" //user
                },
                new IdentityUserRole<string>
                {
                    RoleId = "2c5e174e-3b0e-446f-86af-483d56fd7212", //rol user
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb3" //fernando
                },
                new IdentityUserRole<string>
                {
                    RoleId = "2c5e174e-3b0e-446f-86af-483d56fd7212", //rol user
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb4" //teo
                },
                new IdentityUserRole<string>
                {
                    RoleId = "2c5e174e-3b0e-446f-86af-483d56fd7212", //rol user
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb5" //musiclover
                }
            );
            
            context.SaveChanges();
        }

        private static void Seed_Groups(ApplicationDbContext context)
        {
            context.Groups.AddRange
                (
                    new Group
                    {
                        Id = 1,
                        Name = "Muziker",
                        Description = "Group for music enthusiasts where u can post about your passion!",
                        UserId = "8e445865-a24d-4543-a6c6-9443d048cdb5",//musiclover
                        IsPublic = true
                    },
                    new Group
                    {
                        Id = 2,
                        Name = "Travelling",
                        Description = "Group for people how like to travel and eat good food",
                        UserId = "8e445865-a24d-4543-a6c6-9443d048cdb3",//fernando
                        IsPublic = true
                    }
                
                );
            
            context.SaveChanges();
        }

        private static void Seed_GroupMembers(ApplicationDbContext context)
        {
            context.GroupMembers.AddRange
            (
                new GroupMember
                {
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb5", //musiclover
                    GroupId = 1, //muziker
                    JoinDate = DateTime.Now,
                    IsModerator = true
                },
                new GroupMember
                {
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb4", //teo
                    GroupId = 1, //muziker
                    JoinDate = DateTime.Now,
                    IsModerator = false
                },
                new GroupMember
                {
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb3", //fernando
                    GroupId = 1, //muziker
                    JoinDate = DateTime.Now,
                    IsModerator = false
                },
                new GroupMember
                {
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb3", //fernando
                    GroupId = 2, //travell
                    JoinDate = DateTime.Now,
                    IsModerator = true
                },
                
                new GroupMember
                {
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb4", //teo
                    GroupId = 2, //travell
                    JoinDate = DateTime.Now,
                    IsModerator = true
                }
                
            );
            
            context.SaveChanges();
        }
        
        private static void Seed_Posts(ApplicationDbContext context)
        {
            context.Posts.AddRange
            (
                new Post
                {
                    Title = "Concert Radiohead",
                    Content = "Am fost la un concert Radiohead in Bologna. So cool!",
                    Time = DateTime.Now,
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb4", // teo
                    Media = "/images/a6cb4296-94ec-4cee-aced-9e7e14b53be7_radiohead.jpg",
                    GroupId = 1
                },
                new Post
                {
                    Title = "Paris trip",
                    Content = "Am fost dupa sesiune in Paris. Nu trec la ASC. YOLO ",
                    Time = DateTime.Now,
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb3", // fernando
                    Media = "/images/5638f79b-18ad-4215-87c2-353fdd544c42_paris.JPG",
                    GroupId = 2 //travell
                },
                new Post
                {
                    Title = "Granada Trip",
                    Content = "Ce frumoasa e Spania! Am mancat si o paella foarte buna!",
                    Time = DateTime.Now,
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb3", // fernando
                    Media = "/images/c3b7eb59-8fbc-49f3-87f1-0a4726692535_granada.JPG",
                    GroupId = null
                },
                new Post
                {
                    Title = "Makeup Inspo",
                    Content = "Found this makeup inspo. So pretty!!",
                    Time = DateTime.Now,
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb4", // teo
                    Media = "/images/a068374d-b18c-4795-98c6-77c6bf98cbe8_makeup.jpg",
                    GroupId = null
                },
                new Post
                {
                    Title = "New CD",
                    Content = "I bought a new CD. Do you like this album?",
                    Time = DateTime.Now,
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb5", // musiclover
                    Media = "/images/47224870-cda4-4c45-8b62-20412038e052_cd.jpg",
                    GroupId = 1
                },
                new Post
                {
                    Title = "London Trip",
                    Content = "Big ben!!",
                    Time = DateTime.Now,
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb3", // fernando
                    Media = "/images/7acca6fc-7d54-419d-ad30-cce79925e4c4_london.JPG",
                    GroupId = 2 //travel
                }
                
            );
            
            context.SaveChanges();
        }

        
        private static void Seed_Reactions(ApplicationDbContext context)
        {
            context.Reactions.AddRange
            (
                new Reaction
                {
                    PostId = 1,//postare radiohead teo
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb3",//fernando
                    DateCreated = DateTime.Now
                }
            );
            
            context.SaveChanges();
        }

        private static void Seed_Comments(ApplicationDbContext context)
        {
            context.Comments.AddRange
            (
                new Comment
                {
                    Content = "Pacat ca nu au cantat Jigsaw :( In rest a fost absolut superb",
                    PostId = 1, //postarea radiohead teo
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb3",//fernando
                    DateCreated = DateTime.Now
                }
            );
            
            context.SaveChanges();
        }

        private static void seed_Follow(ApplicationDbContext context)
        {
            context.Follows.AddRange
            (
                new Follow
                {
                    FollowerId = "8e445865-a24d-4543-a6c6-9443d048cdb3",//fernando
                    FolloweeId = "8e445865-a24d-4543-a6c6-9443d048cdb4",//teo
                    Status = FollowStatus.Accepted,
                    RequestedAt = DateTime.Now, // FIX: Data
                    RespondedAt = DateTime.Now
                },
                new Follow
                {
                    FollowerId = "8e445865-a24d-4543-a6c6-9443d048cdb4",//teo
                    FolloweeId = "8e445865-a24d-4543-a6c6-9443d048cdb3",//fernando
                    Status = FollowStatus.Accepted,
                    RequestedAt = DateTime.Now,
                    RespondedAt = DateTime.Now
                }
                
            );
            
            context.SaveChanges();
            
        }
        

        
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                       serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                
                // Verificam daca in baza de date exista cel putin un rol
                // insemnand ca a fost rulat codul
                // De aceea facem return pentru a nu insera rolurile inca o data
                // Acesta metoda trebuie sa se execute o singura data
                
                
                if (!context.Roles.Any()) Seed_Roles(context);
                if (!context.Users.Any()) Seed_Users(context);
                if (!context.UserRoles.Any()) Seed_UserRoles(context);
                
                if (!context.Groups.Any()) Seed_Groups(context);
                if (!context.GroupMembers.Any()) Seed_GroupMembers(context);

                if (!context.Posts.Any()) Seed_Posts(context);
                if (!context.Reactions.Any()) Seed_Reactions(context);
                if (!context.Comments.Any()) Seed_Comments(context);
                
                if (!context.Follows.Any()) seed_Follow(context);
                
            }
        }
        
        
    }
}