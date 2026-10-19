namespace Judhur.Infrastructure.Data.Seed;

internal static class DemoPhotos
{
    private const string UrlFormat = "https://images.unsplash.com/photo-{0}?auto=format&fit=crop&w=1600&q=80";

    private static readonly string[] LivingRooms =
    [
        "1522708323590-d24dbb6b0267",
        "1502672260266-1c1ef2d93688",
        "1583847268964-b28dc8f51f92",
        "1560448204-e02f11c3d0e2",
        "1564078516393-cf04bd966897",
        "1628592102751-ba83b0314276",
        "1757524503555-b62633f47670",
        "1759691555393-60cd5355d840",
        "1773578978637-c9771e7a9913"
    ];

    private static readonly string[] Kitchens =
    [
        "1484154218962-a197022b5858",
        "1630699144641-72fa7a6b8aa1",
        "1610527003928-47afd5f470c6",
        "1755624222023-621f7718950b",
        "1560448075-cbc16bb4af8e",
        "1630699376167-3870469e7598"
    ];

    private static readonly string[] Bedrooms =
    [
        "1616594039964-ae9021a400a0",
        "1560185893-a55cbc8c57e8",
        "1562438668-bcf0ca6578f0",
        "1595526051245-4506e0005bd0",
        "1615529162924-f8605388461d",
        "1512918728675-ed5a9ecdebfd",
        "1750420556288-d0e32a6f517b"
    ];

    private static readonly string[] ApartmentBuildings =
    [
        "1512845296467-183ccf124347",
        "1521208059781-bcf3fd4d1245",
        "1525953776754-6c4b7ee655ab",
        "1514895413746-feb3d266273d",
        "1775733924031-521cd86c69cc",
        "1764722870653-e59e737d1d30",
        "1784853879843-f3c5aacf2569",
        "1764722870631-877f5c694b76"
    ];

    private static readonly string[] HouseExteriors =
    [
        "1600596542815-ffad4c1539a9",
        "1602769921397-e870d926e1e8",
        "1621620844255-4b0d09b12c3c",
        "1505843513577-22bb7d21e455",
        "1621620844577-e43740553901",
        "1492889971304-ac16ab4a4a5a",
        "1613667650324-ed09383eb8e5",
        "1722020287826-130aac89df7b"
    ];

    private static readonly string[] StoneHouses =
    [
        "1653750934543-aebe598499f3",
        "1691345215365-b8d9742b6826",
        "1613777548195-07f098da30fe"
    ];

    private static readonly string[] Pools =
    [
        "1593714604578-d9e41b00c6c6",
        "1663293761246-56a9b0693052",
        "1663293761188-eb1dde142bd8"
    ];

    private static readonly string[] OpenLand =
    [
        "1598007829291-7f533c554316",
        "1590319494483-27c2247e81f7",
        "1593266314085-c349a6e877b6",
        "1518965377251-de929fb61f2b",
        "1601694090572-98694c8b3b85",
        "1506260408121-e353d10b87c7"
    ];

    private static readonly string[] OliveTrees =
    [
        "1764011954039-9c38c8912c56",
        "1758818710855-b60cf3b74de6",
        "1698036867785-a8aeb0f59c48",
        "1601238884151-88a7092027bb",
        "1565459374854-6fa73a99abf5"
    ];

    private static readonly string[] Offices =
    [
        "1497366811353-6870744d04b2",
        "1497366754035-f200968a6e72",
        "1768836180167-6d4a25c421b2",
        "1774953037913-af0cf688491a",
        "1571624436279-b272aff752b5",
        "1525896544042-354764aa27e6",
        "1531972111231-7482a960e109"
    ];

    private static readonly string[] Warehouses =
    [
        "1553413077-190dd305871c",
        "1772305336606-989a457ffbae",
        "1772300704502-410f0fbd43bb",
        "1587293852726-70cdb56c2866",
        "1586528116311-ad8dd3c8310d",
        "1644079446600-219068676743"
    ];

    private static readonly Dictionary<DemoGallery, string[][]> Layouts = new()
    {
        [DemoGallery.Apartment] = [LivingRooms, Kitchens, Bedrooms, ApartmentBuildings, LivingRooms, Bedrooms, Kitchens],
        [DemoGallery.House] = [HouseExteriors, LivingRooms, Kitchens, Bedrooms, HouseExteriors, LivingRooms],
        [DemoGallery.StoneHouse] = [StoneHouses, LivingRooms, StoneHouses, Bedrooms, Kitchens, StoneHouses],
        [DemoGallery.Villa] = [Pools, HouseExteriors, LivingRooms, Kitchens, Bedrooms, Pools, Bedrooms],
        [DemoGallery.Land] = [OpenLand, OpenLand, OliveTrees, OpenLand],
        [DemoGallery.OliveGrove] = [OliveTrees, OliveTrees, OpenLand, OliveTrees],
        [DemoGallery.Office] = [Offices, Offices, Offices, Offices],
        [DemoGallery.Storage] = [Warehouses, Warehouses, Warehouses, Warehouses],
        [DemoGallery.Building] = [ApartmentBuildings, ApartmentBuildings, LivingRooms, Kitchens, Bedrooms, ApartmentBuildings]
    };

    public static IReadOnlyList<string> For(DemoGallery gallery, int ordinal, int count)
    {
        var layout = Layouts[gallery];
        var rotation = ordinal / layout[0].Length * 3;
        var urls = new List<string>(count);
        for (var slot = 0; urls.Count < count && slot < count * 4; slot++)
        {
            var pool = layout[(slot + rotation) % layout.Length];
            var index = slot == 0 ? ordinal : ordinal * 3 + slot;
            var url = string.Format(UrlFormat, pool[index % pool.Length]);
            if (!urls.Contains(url))
            {
                urls.Add(url);
            }
        }
        return urls;
    }
}
