using System.Reflection;
using Bislerium.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Bislerium.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        Database.EnsureCreated();
    }

    public DbSet<Blog> Blogs { get; set; }
    
    public DbSet<BlogImage> BlogImages { get; set; }

    public DbSet<BlogLog> BlogLogs { get; set; }
    
    public DbSet<Comment> Comments { get; set; }

    public DbSet<CommentLog> CommentLogs { get; set; }

    public DbSet<Notification> Notifications { get; set; }

    public DbSet<Reaction> Reactions { get; set; }
    
    public DbSet<Role> Roles { get; set; }
    
    public DbSet<User> Users { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        
        base.OnModelCreating(builder);
	}
}