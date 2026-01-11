using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OSSocial.Models;

namespace OSSocial.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext>
        options)
        : base(options)
    {
    }
    
    
    public DbSet<ApplicationUser> ApplicationUsers { get; set; }
    
    public DbSet<Follow> Follows { get; set; }

    public DbSet<Post> Posts { get; set; }
    
    public DbSet<Comment> Comments { get; set; }
    
    public DbSet<Reaction> Reactions { get; set; }
    
    public DbSet<Group> Groups { get; set; }
    
    public DbSet<GroupMember> GroupMembers { get; set; }
    
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // definirea relatiei one-to-many dintre ApplicationUser si Group
        builder.Entity<Group>()
            .HasOne(g => g.User)          // Un grup are un singur User (Owner)
            .WithMany(u => u.OwnedGroups) // Un User are multe grupuri create (OwnedGroups)
            .HasForeignKey(g => g.UserId) // Cheia straina in Group este UserId
            .OnDelete(DeleteBehavior.Restrict); // sa nu stearga userul daca stergi grupul.........
        
        // primary key compus pentru GroupMembers
        builder.Entity<GroupMember>()
            .HasKey(gm => new { gm.Id, gm.GroupId, gm.UserId });
        
        // definesc relatia propriu-zisa 
        builder.Entity<GroupMember>()
            .HasOne(gm => gm.User)
            .WithMany(u => u.GroupMembership)
            .HasForeignKey(gm => gm.UserId);
        
        builder.Entity<GroupMember>()
            .HasOne(gm => gm.Group)
            .WithMany(u => u.Members)
            .HasForeignKey(gm => gm.GroupId);
        
        builder.Entity<Follow>()
            .HasOne(f=>f.Follower)
            .WithMany(u=>u.Following)
            .HasForeignKey(f=>f.FollowerId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Entity<Follow>()
            .HasOne(f=>f.Followee)
            .WithMany(u=>u.Followers)
            .HasForeignKey(f=>f.FolloweeId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Entity<Follow>()
            .HasIndex(f=>new{f.FollowerId,f.FolloweeId})
            .IsUnique();
        
        // Configure string properties for MySQL compatibility
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(string))
                {
                    var maxLength = property.GetMaxLength();
                    if (maxLength == null)
                    {
                        // Set default max length for string primary keys and foreign keys
                        if (property.IsKey() || property.IsForeignKey())
                        {
                            property.SetMaxLength(255);
                        }
                    }
                }
            }
        }
    }
}