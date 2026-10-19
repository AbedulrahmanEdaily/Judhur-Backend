using Judhur.Domain.Properties.Enums;

namespace Judhur.Infrastructure.Data.Seed;

internal static class DemoListings
{
    public static readonly DemoListing[] All =
    [
        new("rafidia-penthouse", "faheem", "شقة فاخرة في رفيديا بإطلالة مفتوحة",
            "شقة في الطابق الرابع بتشطيب سوبر ديلوكس، ثلاث غرف نوم منها ماستر، صالون واسع ومطبخ أمريكي مجهز. العمارة فيها مصعد ومولد احتياطي وموقف سيارة خاص، وقريبة من المدارس والمستشفى والجامعة.",
            520_000m, PaymentType.Installments, PropertyType.Apartment, PropertyStatus.ForSale, 165m,
            "نابلس", "رفيديا", "رفيديا، شارع تونس، بالقرب من مسجد الحاج نمر", 32.2276, 35.2381,
            LandClassification.A, LegalStatus.Tabo, DemoGallery.Apartment, 7, DemoOutcome.Approved, 3),

        new("junaid-land", "faheem", "أرض سكنية في الجنيد قريبة من الجامعة",
            "قطعة أرض مستوية على شارعين، مناسبة لبناء عمارة سكنية أو سكن طلاب. الكهرباء والمياه والصرف الصحي على حدود القطعة، والمنطقة في توسع عمراني سريع.",
            280_000m, PaymentType.Cash, PropertyType.Land, PropertyStatus.ForSale, 750m,
            "نابلس", "الجنيد", "الجنيد، خلف الحرم الجديد لجامعة النجاح", 32.2352, 35.2146,
            LandClassification.B, LegalStatus.Taswiye, DemoGallery.Land, 4, DemoOutcome.Approved, 12),

        new("najah-rent", "faheem", "شقة مفروشة للإيجار قرب جامعة النجاح",
            "شقة مفروشة بالكامل، غرفتا نوم وصالون ومطبخ، مناسبة للطلاب أو لعائلة صغيرة. الإيجار شهري ويشمل الإنترنت ورسوم العمارة.",
            2_200m, PaymentType.Cash, PropertyType.Apartment, PropertyStatus.ForRent, 110m,
            "نابلس", "شارع الجامعة", "شارع الجامعة، عمارة الريان، الطابق الثاني", 32.2238, 35.2477,
            LandClassification.A, LegalStatus.Tabo, DemoGallery.Apartment, 5, DemoOutcome.Approved, 5),

        new("shuhada-office", "faheem", "مكتب تجاري على دوار الشهداء",
            "مكتب في موقع حيوي وسط البلد، مناسب لمكتب محاماة أو عيادة، مع غرفة انتظار ومطبخ صغير وحمام مستقل.",
            3_500m, PaymentType.Negotiable, PropertyType.Office, PropertyStatus.ForRent, 90m,
            "نابلس", "وسط البلد", "دوار الشهداء، مجمع البلدية التجاري، الطابق الثالث", 32.2213, 35.2610,
            LandClassification.A, LegalStatus.Tabo, DemoGallery.Office, 4, DemoOutcome.Deactivated, 30),

        new("makhfiya-house", "faheem", "منزل مستقل في المخفية مع حديقة",
            "منزل من طابقين مع حديقة أمامية ومخزن، أربع غرف نوم، تدفئة مركزية، وإطلالة على المدينة من الطابق العلوي.",
            890_000m, PaymentType.DownPaymentAndInstallments, PropertyType.House, PropertyStatus.ForSale, 240m,
            "نابلس", "المخفية", "المخفية، شارع المدارس", 32.2305, 35.2550,
            LandClassification.A, LegalStatus.Tabo, DemoGallery.House, 5, DemoOutcome.Pending, 1),

        new("east-storage", "faheem", "مخزن في المنطقة الصناعية الشرقية",
            "مخزن بسقف مرتفع وباب كبير لدخول الشاحنات، مناسب للتخزين أو ورشة.",
            1_500m, PaymentType.Cash, PropertyType.Storage, PropertyStatus.ForRent, 200m,
            "نابلس", "المنطقة الصناعية", "المنطقة الصناعية الشرقية، قرب مفرق بلاطة", 32.2175, 35.2835,
            LandClassification.B, LegalStatus.Maliye, DemoGallery.Storage, 2, DemoOutcome.Pending, 2,
            HasOwnershipDocument: false),

        new("dahiya-apartment", "faheem", "شقة في حي الضاحية بسعر مناسب",
            "شقة ثلاث غرف نوم بحاجة لبعض الترميم، في عمارة هادئة وقريبة من المواصلات.",
            410_000m, PaymentType.Installments, PropertyType.Apartment, PropertyStatus.ForSale, 130m,
            "نابلس", "الضاحية", "الضاحية، شارع الأمل", 32.2150, 35.2480,
            LandClassification.A, LegalStatus.Tabo, DemoGallery.Apartment, 3, DemoOutcome.Rejected, 8,
            "الصور لا تظهر الغرف والمطبخ بوضوح، يرجى رفع صور أوضح ثم إعادة الإرسال للمراجعة."),

        new("asira-olives", "faheem", "أرض مزروعة زيتون في عصيرة الشمالية",
            "أرض مزروعة بأشجار زيتون معمرة، على طريق زراعي معبّد، وفيها بئر لجمع مياه الأمطار.",
            120_000m, PaymentType.Negotiable, PropertyType.Land, PropertyStatus.ForSale, 1_500m,
            "نابلس", "عصيرة الشمالية", "عصيرة الشمالية، الحوض الشرقي", 32.2560, 35.2420,
            LandClassification.C, LegalStatus.Maliye, DemoGallery.OliveGrove, 3, DemoOutcome.Rejected, 15,
            "وثيقة الملكية غير مرفقة، يرجى رفعها ثم إعادة الإرسال للمراجعة.",
            HasOwnershipDocument: false),

        new("rafidia-sold", "faheem", "شقة في رفيديا بالقرب من المستشفى",
            "شقة بتشطيب جيد، ثلاث غرف نوم وشرفتان، وقريبة من مستشفى رفيديا.",
            470_000m, PaymentType.Cash, PropertyType.Apartment, PropertyStatus.ForSale, 150m,
            "نابلس", "رفيديا", "رفيديا، قرب مستشفى رفيديا الحكومي", 32.2262, 35.2332,
            LandClassification.A, LegalStatus.Tabo, DemoGallery.Apartment, 4, DemoOutcome.Sold, 45),

        new("masyoun-apartment", "layla", "شقة حديثة في الماصيون بتشطيب فاخر",
            "شقة جديدة لم تُسكن، أربع غرف نوم منها ماستر، مطبخ مجهز بالكامل، وشرفة واسعة. العمارة فيها مصعدان وحارس وموقفان لكل شقة.",
            950_000m, PaymentType.Installments, PropertyType.Apartment, PropertyStatus.ForSale, 180m,
            "رام الله", "الماصيون", "الماصيون، شارع الإرسال، خلف مجمع فلسطين الطبي", 31.9002, 35.1985,
            LandClassification.A, LegalStatus.Tabo, DemoGallery.Apartment, 7, DemoOutcome.Approved, 2),

        new("tireh-rent", "layla", "شقة للإيجار في الطيرة",
            "شقة غير مفروشة في حي هادئ، غرفتا نوم وصالون كبير وشرفة. الإيجار سنوي بدفعات شهرية ويشمل رسوم العمارة.",
            3_800m, PaymentType.Cash, PropertyType.Apartment, PropertyStatus.ForRent, 140m,
            "رام الله", "الطيرة", "الطيرة، شارع السفارات", 31.9125, 35.1890,
            LandClassification.A, LegalStatus.Tabo, DemoGallery.Apartment, 5, DemoOutcome.Approved, 6),

        new("bireh-building", "layla", "عمارة سكنية مؤجرة بالكامل في البيرة",
            "عمارة من خمسة طوابق فيها عشر شقق مؤجرة بعقود سنوية، دخل شهري ثابت، مع مصعد ومواقف. فرصة استثمارية لمن يبحث عن عائد مضمون.",
            3_200_000m, PaymentType.Negotiable, PropertyType.Building, PropertyStatus.ForSale, 900m,
            "رام الله", "البيرة", "البيرة، شارع القدس، قرب البلدية", 31.9105, 35.2160,
            LandClassification.A, LegalStatus.Tabo, DemoGallery.Building, 6, DemoOutcome.Approved, 20),

        new("manara-office", "layla", "مكتب في المنارة وسط رام الله",
            "مكتب بمساحة مفتوحة قابلة للتقسيم، مناسب لشركة ناشئة، في مبنى تجاري حديث مع قاعة اجتماعات مشتركة.",
            380_000m, PaymentType.DownPaymentAndInstallments, PropertyType.Office, PropertyStatus.ForSale, 120m,
            "رام الله", "المنارة", "دوار المنارة، شارع ركب، مجمع الريادة التجاري", 31.9039, 35.2055,
            LandClassification.A, LegalStatus.Tabo, DemoGallery.Office, 4, DemoOutcome.Approved, 10),

        new("birzeit-land", "layla", "أرض في بيرزيت بإطلالة جبلية",
            "قطعة أرض مرتفعة بإطلالة مفتوحة، مناسبة لفيلا أو بيت ريفي، وقريبة من جامعة بيرزيت.",
            450_000m, PaymentType.Cash, PropertyType.Land, PropertyStatus.ForSale, 1_000m,
            "رام الله", "بيرزيت", "بيرزيت، الحي الغربي", 31.9588, 35.1935,
            LandClassification.B, LegalStatus.Tabo, DemoGallery.Land, 4, DemoOutcome.Approved, 25),

        new("ein-misbah-rented", "layla", "شقة في عين مصباح",
            "شقة بغرفتي نوم، مفروشة جزئياً، قرب المدارس والأسواق.",
            2_800m, PaymentType.Cash, PropertyType.Apartment, PropertyStatus.ForRent, 120m,
            "رام الله", "عين مصباح", "عين مصباح، شارع المدارس", 31.9068, 35.1978,
            LandClassification.A, LegalStatus.Tabo, DemoGallery.Apartment, 4, DemoOutcome.Rented, 40),

        new("beitunia-house", "layla", "منزل واسع في بيتونيا",
            "منزل مستقل على أرض مساحتها خمسمئة متر، خمس غرف نوم، حديقة كبيرة وملحق للضيوف.",
            1_100_000m, PaymentType.Installments, PropertyType.House, PropertyStatus.ForSale, 280m,
            "رام الله", "بيتونيا", "بيتونيا، الحي الشمالي", 31.8942, 35.1680,
            LandClassification.B, LegalStatus.Maliye, DemoGallery.House, 5, DemoOutcome.Pending, 1),

        new("irsal-studio", "layla", "استوديو مفروش للإيجار في حي الإرسال",
            "استوديو حديث مفروش بالكامل مع مطبخ صغير وحمام، مناسب لموظف أو طالب دراسات عليا. الإيجار يشمل الكهرباء والإنترنت.",
            1_900m, PaymentType.Cash, PropertyType.Apartment, PropertyStatus.ForRent, 55m,
            "رام الله", "الإرسال", "حي الإرسال، قرب دوار الساعة", 31.9046, 35.2010,
            LandClassification.A, LegalStatus.Tabo, DemoGallery.Apartment, 4, DemoOutcome.Approved, 4),

        new("beit-sahour-stone", "samer", "منزل حجري قديم مرمم في بيت ساحور",
            "منزل حجري تراثي مرمم بالكامل مع الحفاظ على الأقواس والعقود القديمة، ثلاث غرف نوم وساحة داخلية. التمديدات الكهربائية والصحية جديدة بالكامل.",
            1_350_000m, PaymentType.Negotiable, PropertyType.House, PropertyStatus.ForSale, 320m,
            "بيت لحم", "بيت ساحور", "بيت ساحور، البلدة القديمة، قرب حقل الرعاة", 31.7010, 35.2250,
            LandClassification.A, LegalStatus.Tabo, DemoGallery.StoneHouse, 6, DemoOutcome.Approved, 4),

        new("ein-sara-apartment", "samer", "شقة في عين سارة بالخليل",
            "شقة بإطلالة مفتوحة في الطابق الثالث، ثلاث غرف نوم ومطبخ واسع، في عمارة جديدة مع مصعد.",
            360_000m, PaymentType.Installments, PropertyType.Apartment, PropertyStatus.ForSale, 145m,
            "الخليل", "عين سارة", "عين سارة، شارع السلام", 31.5410, 35.0980,
            LandClassification.A, LegalStatus.Tabo, DemoGallery.Apartment, 5, DemoOutcome.Approved, 9),

        new("hebron-storage", "samer", "مخزن تجاري في المنطقة الصناعية بالخليل",
            "مخزن بأرضية إسمنتية وسقف معدني مرتفع، مع مكتب صغير ومدخل للشاحنات.",
            250_000m, PaymentType.Cash, PropertyType.Storage, PropertyStatus.ForSale, 300m,
            "الخليل", "المنطقة الصناعية", "المنطقة الصناعية، شارع عين سارة الجنوبي", 31.5205, 35.1080,
            LandClassification.B, LegalStatus.Maliye, DemoGallery.Storage, 4, DemoOutcome.Approved, 18),

        new("halhul-villa", "samer", "فيلا حديثة مع مسبح في حلحول",
            "فيلا من طابقين على أرض دونم، خمس غرف نوم، مسبح خاص وحديقة، وتدفئة أرضية. إطلالة على جبال الخليل.",
            1_650_000m, PaymentType.DownPaymentAndInstallments, PropertyType.House, PropertyStatus.ForSale, 420m,
            "الخليل", "حلحول", "حلحول، الحي الشمالي", 31.5850, 35.1000,
            LandClassification.A, LegalStatus.Tabo, DemoGallery.Villa, 7, DemoOutcome.Approved, 7),

        new("mahd-office", "samer", "مكتب للإيجار في شارع المهد",
            "مكتب صغير قريب من ساحة المهد، مناسب لمكتب سياحي أو شركة خدمات.",
            2_500m, PaymentType.Cash, PropertyType.Office, PropertyStatus.ForRent, 80m,
            "بيت لحم", "شارع المهد", "شارع المهد، قرب ساحة المهد", 31.7045, 35.2070,
            LandClassification.A, LegalStatus.Tabo, DemoGallery.Office, 3, DemoOutcome.Approved, 16),

        new("jabriyat-land", "yousef", "أرض سكنية في الجابريات",
            "أرض على سفح الجبل بإطلالة على مرج ابن عامر، مخدومة بشارع معبّد وشبكة كهرباء.",
            95_000m, PaymentType.Installments, PropertyType.Land, PropertyStatus.ForSale, 600m,
            "جنين", "الجابريات", "الجابريات، الشارع الرئيسي", 32.4655, 35.2950,
            LandClassification.B, LegalStatus.Taswiye, DemoGallery.Land, 4, DemoOutcome.Approved, 14),

        new("jenin-new-apartment", "yousef", "شقة جديدة في حي البساتين بجنين",
            "شقة من مشروع سكني جديد، ثلاث غرف نوم وصالة ومطبخ، تشطيب حديث وعزل حراري، ومخزن خاص في الطابق الأرضي.",
            285_000m, PaymentType.Installments, PropertyType.Apartment, PropertyStatus.ForSale, 140m,
            "جنين", "البساتين", "حي البساتين، شارع حيفا", 32.4590, 35.2920,
            LandClassification.A, LegalStatus.Tabo, DemoGallery.Apartment, 5, DemoOutcome.Approved, 3),

        new("zababdeh-rent", "yousef", "شقة للإيجار قرب الجامعة العربية الأمريكية",
            "شقة مفروشة بغرفتي نوم، مناسبة لطلاب الجامعة، مع إنترنت وسخان شمسي. الإيجار بالفصل الدراسي أو شهرياً.",
            1_600m, PaymentType.Cash, PropertyType.Apartment, PropertyStatus.ForRent, 100m,
            "جنين", "الزبابدة", "الزبابدة، الطريق المؤدي إلى الجامعة العربية الأمريكية", 32.4045, 35.3115,
            LandClassification.A, LegalStatus.Tabo, DemoGallery.Apartment, 4, DemoOutcome.Approved, 6),

        new("tubas-land", "yousef", "أرض زراعية في الفارعة",
            "أرض واسعة مستوية مناسبة للزراعة أو لمشروع دواجن، مع إمكانية توصيل المياه من البئر المجاور.",
            70_000m, PaymentType.Cash, PropertyType.Land, PropertyStatus.ForSale, 900m,
            "طوباس", "الفارعة", "الفارعة، طريق طوباس نابلس", 32.2960, 35.3500,
            LandClassification.C, LegalStatus.Taswiye, DemoGallery.Land, 4, DemoOutcome.Pending, 3),

        new("dhinnaba-rent", "rana", "شقة للإيجار في ضاحية ذنابة",
            "شقة نظيفة بغرفتي نوم، قريبة من جامعة فلسطين التقنية خضوري، مناسبة للموظفين والطلاب.",
            1_800m, PaymentType.Cash, PropertyType.Apartment, PropertyStatus.ForRent, 115m,
            "طولكرم", "ذنابة", "ذنابة، قرب جامعة فلسطين التقنية خضوري", 32.3145, 35.0360,
            LandClassification.A, LegalStatus.Tabo, DemoGallery.Apartment, 4, DemoOutcome.Approved, 7),

        new("shweikeh-house", "rana", "منزل عائلي في شويكة مع حديقة حمضيات",
            "منزل من طابق واحد على أرض نصف دونم، ثلاث غرف نوم وصالونان، وحديقة مزروعة بالحمضيات، والأساسات مصممة لطابق إضافي.",
            560_000m, PaymentType.Installments, PropertyType.House, PropertyStatus.ForSale, 230m,
            "طولكرم", "شويكة", "شويكة، الحي الغربي", 32.3345, 35.0315,
            LandClassification.B, LegalStatus.Tabo, DemoGallery.House, 5, DemoOutcome.Approved, 13),

        new("tulkarm-office", "rana", "مكتب في وسط طولكرم قرب البلدية",
            "مكتب بغرفتين وصالة استقبال، مناسب لمكتب محاسبة أو هندسة، قريب من البنوك والدوائر الحكومية.",
            1_200m, PaymentType.Cash, PropertyType.Office, PropertyStatus.ForRent, 75m,
            "طولكرم", "وسط البلد", "وسط البلد، شارع نابلس", 32.3110, 35.0280,
            LandClassification.A, LegalStatus.Tabo, DemoGallery.Office, 3, DemoOutcome.Approved, 21),

        new("habla-land", "rana", "أرض زراعية في حبلة",
            "أرض خصبة مزروعة بالحمضيات مع بئر ماء، على طريق زراعي.",
            180_000m, PaymentType.Cash, PropertyType.Land, PropertyStatus.ForSale, 2_000m,
            "قلقيلية", "حبلة", "حبلة، طريق قلقيلية الجنوبي", 32.1645, 34.9760,
            LandClassification.C, LegalStatus.Taswiye, DemoGallery.OliveGrove, 4, DemoOutcome.Approved, 22),

        new("salfit-house", "rana", "منزل ريفي في سلفيت",
            "منزل هادئ محاط بأشجار الزيتون، ثلاث غرف نوم ومضافة كبيرة.",
            420_000m, PaymentType.DownPaymentAndInstallments, PropertyType.House, PropertyStatus.ForSale, 200m,
            "سلفيت", "الحي الشرقي", "سلفيت، الحي الشرقي، طريق البلدة القديمة", 32.0840, 35.1825,
            LandClassification.B, LegalStatus.Maliye, DemoGallery.StoneHouse, 4, DemoOutcome.Approved, 28),

        new("beit-hanina-apartment", "khaled", "شقة واسعة في بيت حنينا",
            "شقة بثلاث غرف نوم وصالون كبير، تدفئة مركزية وموقف خاص، في عمارة حديثة بحي هادئ وقريبة من المدارس.",
            1_900_000m, PaymentType.Negotiable, PropertyType.Apartment, PropertyStatus.ForSale, 165m,
            "القدس", "بيت حنينا", "بيت حنينا، الشارع الرئيسي", 31.8295, 35.2235,
            LandClassification.A, LegalStatus.Tabo, DemoGallery.Apartment, 6, DemoOutcome.Approved, 5),

        new("shuafat-rent", "khaled", "شقة للإيجار في شعفاط",
            "شقة بغرفتي نوم وصالون، شرفة واسعة ومطبخ مجهز. الإيجار سنوي بدفعات شهرية.",
            5_500m, PaymentType.Cash, PropertyType.Apartment, PropertyStatus.ForRent, 120m,
            "القدس", "شعفاط", "شعفاط، الشارع الرئيسي", 31.8150, 35.2300,
            LandClassification.A, LegalStatus.Tabo, DemoGallery.Apartment, 4, DemoOutcome.Approved, 9),

        new("jericho-winter", "khaled", "منزل شتوي للإيجار في أريحا",
            "منزل مع مسبح وحديقة نخيل، مناسب للإقامة الشتوية، والإيجار شهري بحد أدنى ثلاثة أشهر.",
            4_500m, PaymentType.Negotiable, PropertyType.House, PropertyStatus.ForRent, 250m,
            "أريحا", "عين السلطان", "عين السلطان، قرب تل السلطان", 31.8710, 35.4440,
            LandClassification.B, LegalStatus.Tabo, DemoGallery.Villa, 5, DemoOutcome.Approved, 11),

        new("dead-sea-land", "nader", "أرض استثمارية قرب البحر الميت بسعر لا يتكرر",
            "فرصة لن تتكرر! أرض واسعة بسعر رمزي، ادفع العربون اليوم لحجز القطعة قبل المعاينة.",
            35_000m, PaymentType.Cash, PropertyType.Land, PropertyStatus.ForSale, 5_000m,
            "أريحا", "طريق البحر الميت", "طريق أريحا البحر الميت", 31.8000, 35.4700,
            LandClassification.C, LegalStatus.Maliye, DemoGallery.Land, 3, DemoOutcome.Suspended, 19,
            "معلومات مضللة وطلب دفعات مسبقة من المشترين قبل المعاينة.")
    ];
}
