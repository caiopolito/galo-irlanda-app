using GaloIrlanda.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<LoginToken> LoginTokens => Set<LoginToken>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<PostCategory> PostCategories => Set<PostCategory>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<SiteRebuildRequest> SiteRebuildRequests => Set<SiteRebuildRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
