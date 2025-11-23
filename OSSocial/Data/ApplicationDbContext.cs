using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace OSSocial.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
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