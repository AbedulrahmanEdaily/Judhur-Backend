using Judhur.Domain.Common.Results;
using Judhur.Domain.Favorites;
using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Infrastructure.Data.Seed;

internal static class PropertySeeder
{
    private const string SeedPublicIdPrefix = "seed/";

    private enum SeedOutcome
    {
        Pending,
        Approved,
        Deactivated,
        Sold,
        Rented,
        Rejected
    }

    private sealed record SeedListing(
        int Seller,
        string Title,
        string? Description,
        decimal Price,
        PaymentType PaymentType,
        PropertyType PropertyType,
        PropertyStatus PropertyStatus,
        decimal Area,
        string City,
        string? Region,
        string FullAddress,
        double Latitude,
        double Longitude,
        LandClassification LandClassification,
        LegalStatus LegalStatus,
        int ImageCount,
        bool HasOwnershipDocument,
        SeedOutcome Outcome,
        int DaysAgo,
        string? RejectionReason = null);

    private static readonly SeedListing[] Listings =
    [
        new(0, "شقة فاخرة في رفيديا بإطلالة مفتوحة",
            "شقة في الطابق الرابع بتشطيب سوبر ديلوكس، ثلاث غرف نوم وصالون واسع ومطبخ أمريكي، مع مصعد وموقف سيارة خاص وقريبة من الخدمات والمدارس.",
            520_000m, PaymentType.Installments, PropertyType.Apartment, PropertyStatus.ForSale, 165m,
            "نابلس", "رفيديا", "رفيديا، شارع تونس، بالقرب من مسجد الحاج نمر", 32.2276, 35.2381,
            LandClassification.A, LegalStatus.Tabo, 5, true, SeedOutcome.Approved, 3),

        new(0, "أرض سكنية في الجنيد قريبة من الجامعة",
            "قطعة أرض مستوية على شارعين، مناسبة لبناء عمارة سكنية، والكهرباء والمياه متوفرة على حدود القطعة.",
            280_000m, PaymentType.Cash, PropertyType.Land, PropertyStatus.ForSale, 750m,
            "نابلس", "الجنيد", "الجنيد، خلف الحرم الجديد لجامعة النجاح", 32.2352, 35.2146,
            LandClassification.B, LegalStatus.Taswiye, 3, true, SeedOutcome.Approved, 12),

        new(0, "شقة للإيجار قرب جامعة النجاح",
            "شقة مفروشة بالكامل، غرفتا نوم وصالون، مناسبة للطلاب أو لعائلة صغيرة، والإيجار شهري ويشمل الإنترنت.",
            2_200m, PaymentType.Cash, PropertyType.Apartment, PropertyStatus.ForRent, 110m,
            "نابلس", "شارع الجامعة", "شارع الجامعة، عمارة الريان، الطابق الثاني", 32.2238, 35.2477,
            LandClassification.A, LegalStatus.Tabo, 4, true, SeedOutcome.Approved, 5),

        new(0, "مكتب تجاري على دوار الشهداء",
            "مكتب في موقع حيوي وسط البلد، مناسب لمكتب محاماة أو عيادة، مع غرفة انتظار ومطبخ صغير.",
            3_500m, PaymentType.Negotiable, PropertyType.Office, PropertyStatus.ForRent, 90m,
            "نابلس", "وسط البلد", "دوار الشهداء، مجمع البلدية التجاري، الطابق الثالث", 32.2213, 35.2610,
            LandClassification.A, LegalStatus.Tabo, 3, true, SeedOutcome.Deactivated, 30),

        new(0, "منزل مستقل في المخفية مع حديقة",
            "منزل من طابقين مع حديقة أمامية ومخزن، أربع غرف نوم، تدفئة مركزية، وإطلالة على المدينة.",
            890_000m, PaymentType.DownPaymentAndInstallments, PropertyType.House, PropertyStatus.ForSale, 240m,
            "نابلس", "المخفية", "المخفية، شارع المدارس", 32.2305, 35.2550,
            LandClassification.A, LegalStatus.Tabo, 4, true, SeedOutcome.Pending, 1),

        new(0, "مخزن في المنطقة الصناعية الشرقية",
            "مخزن بسقف مرتفع وباب كبير لدخول الشاحنات، مناسب للتخزين أو ورشة.",
            1_500m, PaymentType.Cash, PropertyType.Storage, PropertyStatus.ForRent, 200m,
            "نابلس", "المنطقة الصناعية", "المنطقة الصناعية الشرقية، قرب مفرق بلاطة", 32.2175, 35.2835,
            LandClassification.B, LegalStatus.Maliye, 2, false, SeedOutcome.Pending, 2),

        new(0, "شقة في حي الضاحية بسعر مناسب",
            "شقة ثلاث غرف نوم بحاجة لبعض الترميم، في عمارة هادئة وقريبة من المواصلات.",
            410_000m, PaymentType.Installments, PropertyType.Apartment, PropertyStatus.ForSale, 130m,
            "نابلس", "الضاحية", "الضاحية، شارع الأمل", 32.2150, 35.2480,
            LandClassification.A, LegalStatus.Tabo, 3, true, SeedOutcome.Rejected, 8,
            "الصور غير واضحة، يرجى رفع صور أوضح للغرف والمطبخ ثم إعادة الإرسال للمراجعة."),

        new(0, "أرض زراعية مزروعة زيتون في عصيرة الشمالية",
            "أرض مزروعة بأشجار زيتون معمرة، على طريق زراعي معبّد.",
            120_000m, PaymentType.Negotiable, PropertyType.Land, PropertyStatus.ForSale, 1_500m,
            "نابلس", "عصيرة الشمالية", "عصيرة الشمالية، الحوض الشرقي", 32.2560, 35.2420,
            LandClassification.C, LegalStatus.Maliye, 3, false, SeedOutcome.Rejected, 15,
            "وثيقة الملكية غير مرفقة، يرجى رفعها ثم إعادة الإرسال للمراجعة."),

        new(0, "شقة في رفيديا بالقرب من المستشفى",
            "شقة بتشطيب جيد، ثلاث غرف نوم وبلكونتان، وقريبة من مستشفى رفيديا.",
            470_000m, PaymentType.Cash, PropertyType.Apartment, PropertyStatus.ForSale, 150m,
            "نابلس", "رفيديا", "رفيديا، قرب مستشفى رفيديا الحكومي", 32.2262, 35.2332,
            LandClassification.A, LegalStatus.Tabo, 3, true, SeedOutcome.Sold, 45),

        new(1, "شقة حديثة في الماصيون بتشطيب فاخر",
            "شقة جديدة لم تسكن، أربع غرف نوم منها ماستر، مطبخ مجهز بالكامل، مصعدان وحارس للعمارة وموقفان للسيارات.",
            950_000m, PaymentType.Installments, PropertyType.Apartment, PropertyStatus.ForSale, 180m,
            "رام الله", "الماصيون", "الماصيون، شارع الإرسال، خلف مجمع فلسطين الطبي", 31.9002, 35.1985,
            LandClassification.A, LegalStatus.Tabo, 6, true, SeedOutcome.Approved, 2),

        new(1, "شقة للإيجار في الطيرة",
            "شقة غير مفروشة في حي هادئ، غرفتا نوم وصالون كبير وبلكونة، والإيجار سنوي بدفعات شهرية.",
            3_800m, PaymentType.Cash, PropertyType.Apartment, PropertyStatus.ForRent, 140m,
            "رام الله", "الطيرة", "الطيرة، شارع السفارات", 31.9125, 35.1890,
            LandClassification.A, LegalStatus.Tabo, 4, true, SeedOutcome.Approved, 6),

        new(1, "عمارة سكنية كاملة في البيرة",
            "عمارة من خمسة طوابق، عشر شقق مؤجرة بالكامل، دخل شهري ثابت، مع مصعد ومواقف.",
            3_200_000m, PaymentType.Negotiable, PropertyType.Building, PropertyStatus.ForSale, 900m,
            "رام الله", "البيرة", "البيرة، شارع القدس، قرب البلدية", 31.9105, 35.2160,
            LandClassification.A, LegalStatus.Tabo, 5, true, SeedOutcome.Approved, 20),

        new(1, "مكتب في المنارة وسط رام الله",
            "مكتب بمساحة مفتوحة قابلة للتقسيم، مناسب لشركة ناشئة، في مبنى تجاري حديث.",
            380_000m, PaymentType.DownPaymentAndInstallments, PropertyType.Office, PropertyStatus.ForSale, 120m,
            "رام الله", "المنارة", "دوار المنارة، شارع ركب، مجمع الريادة التجاري", 31.9039, 35.2055,
            LandClassification.A, LegalStatus.Tabo, 3, true, SeedOutcome.Approved, 10),

        new(1, "أرض في بيرزيت بإطلالة جبلية",
            "قطعة أرض مرتفعة بإطلالة رائعة، مناسبة لفيلا، وقريبة من جامعة بيرزيت.",
            450_000m, PaymentType.Cash, PropertyType.Land, PropertyStatus.ForSale, 1_000m,
            "رام الله", "بيرزيت", "بيرزيت، الحي الغربي", 31.9588, 35.1935,
            LandClassification.B, LegalStatus.Tabo, 3, true, SeedOutcome.Approved, 25),

        new(1, "شقة في عين مصباح",
            "شقة بغرفتي نوم، مفروشة جزئياً، قرب المدارس والأسواق.",
            2_800m, PaymentType.Cash, PropertyType.Apartment, PropertyStatus.ForRent, 120m,
            "رام الله", "عين مصباح", "عين مصباح، شارع المدارس", 31.9068, 35.1978,
            LandClassification.A, LegalStatus.Tabo, 3, true, SeedOutcome.Rented, 40),

        new(1, "منزل واسع في بيتونيا",
            "منزل مستقل على أرض 500 متر، خمس غرف نوم وحديقة كبيرة وملحق للضيوف.",
            1_100_000m, PaymentType.Installments, PropertyType.House, PropertyStatus.ForSale, 280m,
            "رام الله", "بيتونيا", "بيتونيا، الحي الشمالي", 31.8942, 35.1680,
            LandClassification.B, LegalStatus.Maliye, 4, true, SeedOutcome.Pending, 1),

        new(2, "منزل حجري قديم مرمم في بيت ساحور",
            "منزل حجري تراثي مرمم بالكامل مع الحفاظ على الأقواس القديمة، ثلاث غرف نوم وساحة داخلية.",
            1_350_000m, PaymentType.Negotiable, PropertyType.House, PropertyStatus.ForSale, 320m,
            "بيت لحم", "بيت ساحور", "بيت ساحور، البلدة القديمة، قرب حقل الرعاة", 31.7010, 35.2250,
            LandClassification.A, LegalStatus.Tabo, 5, true, SeedOutcome.Approved, 4),

        new(2, "شقة في عين سارة بالخليل",
            "شقة بإطلالة مفتوحة، ثلاث غرف نوم، ومطبخ واسع، في عمارة جديدة.",
            360_000m, PaymentType.Installments, PropertyType.Apartment, PropertyStatus.ForSale, 145m,
            "الخليل", "عين سارة", "عين سارة، شارع السلام", 31.5410, 35.0980,
            LandClassification.A, LegalStatus.Tabo, 4, true, SeedOutcome.Approved, 9),

        new(2, "مخزن تجاري في المنطقة الصناعية بالخليل",
            null,
            250_000m, PaymentType.Cash, PropertyType.Storage, PropertyStatus.ForSale, 300m,
            "الخليل", "المنطقة الصناعية", "المنطقة الصناعية، شارع عين سارة الجنوبي", 31.5205, 35.1080,
            LandClassification.B, LegalStatus.Maliye, 3, true, SeedOutcome.Approved, 18),

        new(2, "أرض سكنية في الجابريات",
            "أرض على سفح الجبل بإطلالة على مرج ابن عامر، مخدومة بشارع معبّد.",
            95_000m, PaymentType.Installments, PropertyType.Land, PropertyStatus.ForSale, 600m,
            "جنين", "الجابريات", "الجابريات، الشارع الرئيسي", 32.4655, 35.2950,
            LandClassification.B, LegalStatus.Taswiye, 3, true, SeedOutcome.Approved, 14),

        new(2, "شقة للإيجار في ضاحية ذنابة",
            "شقة نظيفة بغرفتي نوم، قريبة من جامعة خضوري، مناسبة للموظفين والطلاب.",
            1_800m, PaymentType.Cash, PropertyType.Apartment, PropertyStatus.ForRent, 115m,
            "طولكرم", "ذنابة", "ذنابة، قرب جامعة فلسطين التقنية خضوري", 32.3145, 35.0360,
            LandClassification.A, LegalStatus.Tabo, 3, true, SeedOutcome.Approved, 7),

        new(2, "أرض زراعية في حبلة",
            "أرض خصبة مزروعة بالحمضيات مع بئر ماء، على طريق زراعي.",
            180_000m, PaymentType.Cash, PropertyType.Land, PropertyStatus.ForSale, 2_000m,
            "قلقيلية", "حبلة", "حبلة، طريق قلقيلية الجنوبي", 32.1645, 34.9760,
            LandClassification.C, LegalStatus.Taswiye, 3, true, SeedOutcome.Approved, 22),

        new(2, "منزل ريفي في سلفيت",
            "منزل هادئ محاط بأشجار الزيتون، ثلاث غرف نوم ومضافة كبيرة.",
            420_000m, PaymentType.DownPaymentAndInstallments, PropertyType.House, PropertyStatus.ForSale, 200m,
            "سلفيت", "الحي الشرقي", "سلفيت، الحي الشرقي، طريق البلدة القديمة", 32.0840, 35.1825,
            LandClassification.B, LegalStatus.Maliye, 3, true, SeedOutcome.Approved, 28),

        new(2, "منزل شتوي للإيجار في أريحا",
            "منزل مع مسبح وحديقة نخيل، مناسب للإقامة الشتوية، والإيجار شهري.",
            4_500m, PaymentType.Negotiable, PropertyType.House, PropertyStatus.ForRent, 250m,
            "أريحا", "عين السلطان", "عين السلطان، قرب تل السلطان", 31.8710, 35.4440,
            LandClassification.B, LegalStatus.Tabo, 4, true, SeedOutcome.Approved, 11),

        new(2, "أرض في طوباس بسعر مغري",
            "أرض واسعة مناسبة للزراعة أو لمشروع دواجن.",
            70_000m, PaymentType.Cash, PropertyType.Land, PropertyStatus.ForSale, 900m,
            "طوباس", "الفارعة", "الفارعة، طريق طوباس نابلس", 32.2960, 35.3500,
            LandClassification.C, LegalStatus.Taswiye, 3, true, SeedOutcome.Pending, 3),

        new(2, "مكتب للإيجار في شارع المهد",
            "مكتب صغير قريب من كنيسة المهد، مناسب لمكتب سياحي أو شركة خدمات.",
            2_500m, PaymentType.Cash, PropertyType.Office, PropertyStatus.ForRent, 80m,
            "بيت لحم", "شارع المهد", "شارع المهد، قرب ساحة المهد", 31.7045, 35.2070,
            LandClassification.A, LegalStatus.Tabo, 3, true, SeedOutcome.Approved, 16)
    ];

    private static readonly int[] FavoriteListingIndexes = [9, 10, 16];

    public static async Task SeedAsync(
        AppDbContext context,
        Guid adminId,
        IReadOnlyList<Guid> sellerIds,
        Guid favoritesOwnerId,
        DateTimeOffset now,
        ILogger logger)
    {
        var alreadySeeded = await context.Properties
            .IgnoreQueryFilters()
            .AnyAsync(p => p.PropertyImages.Any(i => i.PublicId.StartsWith(SeedPublicIdPrefix)));
        if (alreadySeeded)
        {
            logger.LogInformation("Seed properties already exist, skipping property seeding");
            return;
        }

        var properties = new List<Property>();
        for (var index = 0; index < Listings.Length; index++)
        {
            var listing = Listings[index];
            var property = Build(listing, index + 1, sellerIds[listing.Seller], adminId, now.AddDays(-listing.DaysAgo).AddHours(3));
            properties.Add(property);
            context.Properties.Add(property);
        }

        await context.SaveChangesAsync();

        for (var index = 0; index < properties.Count; index++)
        {
            properties[index].CreatedAtUtc = now.AddDays(-Listings[index].DaysAgo);
        }

        foreach (var index in FavoriteListingIndexes)
        {
            var favorite = Ensure(Favorite.Create(Guid.CreateVersion7(), favoritesOwnerId, properties[index].Id), "favorite");
            context.Favorites.Add(favorite);
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {PropertyCount} properties and {FavoriteCount} favorites", properties.Count, FavoriteListingIndexes.Length);
    }

    private static Property Build(SeedListing listing, int number, Guid sellerId, Guid adminId, DateTimeOffset reviewedAtUtc)
    {
        var property = Ensure(Property.Create(
            Guid.CreateVersion7(),
            listing.Title,
            listing.Description,
            listing.Price,
            listing.PaymentType,
            listing.PropertyType,
            listing.PropertyStatus,
            listing.Area,
            listing.City,
            listing.Region,
            listing.FullAddress,
            listing.Latitude,
            listing.Longitude,
            listing.LandClassification,
            listing.LegalStatus,
            sellerId), listing.Title);

        for (var image = 1; image <= listing.ImageCount; image++)
        {
            Ensure(property.AddImage(
                Guid.CreateVersion7(),
                $"https://picsum.photos/seed/judhur-{number}-{image}/1200/800",
                $"{SeedPublicIdPrefix}property-{number}-{image}",
                isMainImage: false), listing.Title);
        }

        if (listing.HasOwnershipDocument)
        {
            Ensure(property.SetOwnershipDocument($"{SeedPublicIdPrefix}ownership-document-{number}"), listing.Title);
        }

        switch (listing.Outcome)
        {
            case SeedOutcome.Pending:
                break;
            case SeedOutcome.Rejected:
                Ensure(property.Reject(adminId, reviewedAtUtc, listing.RejectionReason!), listing.Title);
                break;
            default:
                Ensure(property.Approve(adminId, reviewedAtUtc), listing.Title);
                break;
        }

        switch (listing.Outcome)
        {
            case SeedOutcome.Deactivated:
                Ensure(property.Deactivate(), listing.Title);
                break;
            case SeedOutcome.Sold:
                Ensure(property.MarkAsSold(), listing.Title);
                break;
            case SeedOutcome.Rented:
                Ensure(property.MarkAsRented(), listing.Title);
                break;
        }

        return property;
    }

    private static T Ensure<T>(Result<T> result, string title) =>
        result.IsError
            ? throw new InvalidOperationException($"Seeding '{title}' failed: {result.TopError.Code}")
            : result.Value;
}
