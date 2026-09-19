using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Conversations;
using Judhur.Domain.Conversations.Messages;
using Judhur.Domain.Favorites;
using Judhur.Domain.Identity;
using Judhur.Domain.Notifications;
using Judhur.Domain.Properties;
using Judhur.Domain.Properties.PropertyImages;
using Judhur.Domain.Reports;
using Judhur.Domain.Reviews;
using Judhur.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Judhur.Infrastructure.Data;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    { }
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<Message> Messages => Set<Message>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Identity brings seven tables. Only three are used here -- the user, the roles
        // and the link between them -- so the other four are dropped from the model.
        // The APIs behind them (claims, external logins, persisted tokens, role claims)
        // now throw, because their types are no longer mapped.
        builder.Ignore<IdentityUserClaim<Guid>>();
        builder.Ignore<IdentityUserLogin<Guid>>();
        builder.Ignore<IdentityUserToken<Guid>>();
        builder.Ignore<IdentityRoleClaim<Guid>>();

        // Drop the AspNet prefix on the three that remain.
        builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
