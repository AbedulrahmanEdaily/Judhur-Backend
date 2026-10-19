using System.Collections.Concurrent;

using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common;
using Judhur.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Infrastructure.Data.Seed;

public sealed class DemoDataSeeder(
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    IFileStorage fileStorage,
    TimeProvider timeProvider,
    ILogger<DemoDataSeeder> logger)
{
    private const string DocumentResourceName = "DemoData.ownership-document-sample.pdf";
    private const string DocumentFileName = "ownership-document-sample.pdf";
    private const int UploadParallelism = 4;

    private readonly AppDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IFileStorage _fileStorage = fileStorage;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<DemoDataSeeder> _logger = logger;

    public async Task SeedAsync(Guid adminId, CancellationToken cancellationToken = default)
    {
        var alreadySeeded = await _context.Properties
            .IgnoreQueryFilters()
            .AnyAsync(p => p.PropertyImages.Any(i => i.PublicId.StartsWith(DemoWorldBuilder.SeedPublicIdPrefix)), cancellationToken);
        if (alreadySeeded)
        {
            _logger.LogInformation("Demo listings already exist, skipping demo data");
            return;
        }

        var now = _timeProvider.GetUtcNow();
        var userIds = await EnsureUsersAsync(now);
        if (userIds is null)
        {
            return;
        }

        var propertyIds = DemoListings.All.ToDictionary(l => l.Key, _ => Guid.CreateVersion7());
        var documentIds = await UploadDocumentsAsync(propertyIds, cancellationToken);
        var world = DemoWorldBuilder.Build(now, adminId, userIds, propertyIds, documentIds);

        _context.Properties.AddRange(world.Properties);
        _context.Conversations.AddRange(world.Conversations);
        _context.Reviews.AddRange(world.Reviews);
        _context.Reports.AddRange(world.Reports);
        _context.Favorites.AddRange(world.Favorites);
        _context.Notifications.AddRange(world.Notifications);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var (entity, createdAt) in world.CreatedAt)
        {
            entity.CreatedAtUtc = createdAt;
        }
        await _context.SaveChangesAsync(cancellationToken);

        if (world.Ban is { } ban)
        {
            await BanAsync(ban);
        }

        _logger.LogInformation(
            "Demo data seeded: {UserCount} users, {PropertyCount} listings, {ConversationCount} conversations, {ReviewCount} reviews, {ReportCount} reports, {NotificationCount} notifications",
            userIds.Count,
            world.Properties.Count,
            world.Conversations.Count,
            world.Reviews.Count,
            world.Reports.Count,
            world.Notifications.Count);
    }

    private async Task<IReadOnlyDictionary<string, Guid>?> EnsureUsersAsync(DateTimeOffset now)
    {
        var userIds = new Dictionary<string, Guid>();
        foreach (var demo in DemoUsers.All)
        {
            var user = await _userManager.FindByEmailAsync(demo.Email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    Id = Guid.CreateVersion7(),
                    Email = demo.Email,
                    UserName = demo.Email,
                    FullName = demo.FullName,
                    City = demo.City,
                    PhoneNumber = demo.PhoneNumber,
                    Bio = demo.Bio,
                    EmailConfirmed = true,
                    CreatedAtUtc = now.AddDays(-demo.JoinedDaysAgo)
                };
                var createResult = await _userManager.CreateAsync(user, DemoUsers.Password);
                if (!createResult.Succeeded)
                {
                    _logger.LogError(
                        "Demo user {Email} could not be created, skipping demo data: {Errors}",
                        demo.Email,
                        string.Join(" | ", createResult.Errors.Select(e => e.Code)));
                    return null;
                }
            }

            if (!await _userManager.IsInRoleAsync(user, Roles.User))
            {
                var roleResult = await _userManager.AddToRoleAsync(user, Roles.User);
                if (!roleResult.Succeeded)
                {
                    _logger.LogError(
                        "Role {Role} could not be assigned to demo user {Email}, skipping demo data: {Errors}",
                        Roles.User,
                        demo.Email,
                        string.Join(" | ", roleResult.Errors.Select(e => e.Code)));
                    return null;
                }
            }

            userIds[demo.Key] = user.Id;
        }

        return userIds;
    }

    private async Task<IReadOnlyDictionary<string, string>> UploadDocumentsAsync(
        IReadOnlyDictionary<string, Guid> propertyIds,
        CancellationToken cancellationToken)
    {
        var listings = DemoListings.All.Where(l => l.HasOwnershipDocument).ToArray();
        var document = ReadSampleDocument();
        var uploaded = new ConcurrentDictionary<string, string>();
        var failed = 0;

        await Parallel.ForEachAsync(
            listings,
            new ParallelOptions { MaxDegreeOfParallelism = UploadParallelism, CancellationToken = cancellationToken },
            async (listing, token) =>
            {
                if (Volatile.Read(ref failed) == 1)
                {
                    return;
                }
                await using var content = new MemoryStream(document, writable: false);
                var result = await _fileStorage.UploadPrivateDocumentAsync(
                    content,
                    DocumentFileName,
                    $"judhur/ownership-documents/{propertyIds[listing.Key]}",
                    token);
                if (result.IsError)
                {
                    Interlocked.Exchange(ref failed, 1);
                    return;
                }
                uploaded[listing.Key] = result.Value;
            });

        if (failed == 1)
        {
            _logger.LogWarning(
                "Uploading the demo ownership document failed; {Count} of {Total} demo listings get a placeholder document that cannot be opened",
                listings.Length - uploaded.Count,
                listings.Length);
        }
        else
        {
            _logger.LogInformation("Uploaded the demo ownership document for {Count} listings", uploaded.Count);
        }

        return listings.ToDictionary(
            l => l.Key,
            l => uploaded.TryGetValue(l.Key, out var publicId)
                ? publicId
                : $"{DemoWorldBuilder.SeedPublicIdPrefix}{l.Key}-ownership-document");
    }

    private async Task BanAsync(DemoBanState ban)
    {
        var user = await _userManager.FindByIdAsync(ban.UserId.ToString());
        if (user is null || user.BannedAtUtc is not null)
        {
            return;
        }
        user.BannedAtUtc = ban.BannedAtUtc;
        user.BanReason = ban.Reason;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            _logger.LogError(
                "Demo user {UserId} could not be banned: {Errors}",
                ban.UserId,
                string.Join(" | ", result.Errors.Select(e => e.Code)));
        }
    }

    private static byte[] ReadSampleDocument()
    {
        using var stream = typeof(DemoDataSeeder).Assembly.GetManifestResourceStream(DocumentResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{DocumentResourceName}' is missing.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
