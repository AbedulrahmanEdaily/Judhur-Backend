using Judhur.Domain.Reports.Enums;

namespace Judhur.Infrastructure.Data.Seed;

internal static class DemoActivity
{
    public static readonly DemoConversation[] Conversations =
    [
        new("mais", "masyoun-apartment", 30, SellerHasRead: true, BuyerHasRead: true,
        [
            new(true, "مرحباً، هل الشقة ما زالت متاحة؟ وهل السعر قابل للتفاوض؟", 0),
            new(false, "أهلاً وسهلاً، نعم متاحة. السعر قابل لنقاش بسيط عند الجدية، وفي إمكانية تقسيط عن طريق البنك.", 25),
            new(true, "ممتاز. هل يمكن المعاينة يوم الخميس بعد العصر؟", 40),
            new(false, "أكيد، الساعة الخامسة مناسبة؟ سأرسل لك الموقع الدقيق.", 55),
            new(true, "مناسب جداً، شكراً لك.", 60)
        ]),
        new("tareq", "tireh-rent", 120, SellerHasRead: true, BuyerHasRead: true,
        [
            new(true, "السلام عليكم، هل الإيجار يشمل رسوم العمارة والمصعد؟", 0),
            new(false, "وعليكم السلام، الإيجار يشمل رسوم العمارة، والكهرباء والمياه على المستأجر.", 18),
            new(true, "تمام، وهل تقبلون عقداً لمدة سنة بدفعات شهرية؟", 30),
            new(false, "نعم، العقد سنوي والدفع شهري مع شيك ضمان.", 42)
        ]),
        new("huda", "rafidia-penthouse", 60, SellerHasRead: true, BuyerHasRead: true,
        [
            new(true, "مرحباً، كم عمر البناء؟ وهل يوجد مولد كهرباء للعمارة؟", 0),
            new(false, "أهلاً، البناء عمره ثلاث سنوات، ويوجد مولد احتياطي للمصعد والإنارة العامة.", 95),
            new(true, "رائع، هل يمكن رؤية الشقة مساءً؟", 110),
            new(false, "نعم، أي يوم بعد الساعة السادسة. تواصلي معي قبلها بساعة.", 130)
        ]),
        new("tareq", "beit-sahour-stone", 80, SellerHasRead: true, BuyerHasRead: true,
        [
            new(true, "مرحباً، المنزل رائع. هل الترميم شمل التمديدات الكهربائية والصحية؟", 0),
            new(false, "أهلاً بك، نعم الترميم شامل للتمديدات والعزل، مع الحفاظ على الحجر والأقواس الأصلية.", 35),
            new(true, "هل يوجد مخطط للمنزل يمكن الاطلاع عليه؟", 50),
            new(false, "أكيد، أحضره معي وقت المعاينة.", 65)
        ]),
        new("mais", "ein-sara-apartment", 20, SellerHasRead: false, BuyerHasRead: true,
        [
            new(true, "مرحباً، هل الشقة في الطابق الأرضي أم في طابق علوي؟", 0),
            new(false, "أهلاً، في الطابق الثالث مع مصعد.", 45),
            new(true, "هل يمكن تخفيض السعر إذا كان الدفع نقداً؟", 70)
        ]),
        new("faheem", "bireh-building", 300, SellerHasRead: true, BuyerHasRead: true,
        [
            new(true, "مرحباً، هل الشقق كلها مؤجرة بعقود سارية؟ وكم الدخل السنوي التقريبي؟", 0),
            new(false, "أهلاً، نعم جميعها مؤجرة بعقود سنوية، والدخل السنوي موضح في كشف أستطيع إرساله لك.", 40),
            new(true, "أرسليه لو سمحتِ، وأريد أن أعرف إن كانت هناك ديون على العقار.", 55),
            new(false, "لا توجد أي ديون، والعقار مسجل طابو باسم المالك مباشرة.", 80)
        ]),
        new("huda", "beit-hanina-apartment", 100, SellerHasRead: true, BuyerHasRead: true,
        [
            new(true, "مرحباً، هل الشقة مناسبة لعائلة لديها أطفال؟ وما المدارس القريبة؟", 0),
            new(false, "أهلاً، الحي هادئ وفيه أكثر من مدرسة على بعد دقائق مشياً.", 20),
            new(true, "شكراً، سأحضر مع زوجي للمعاينة.", 35),
            new(false, "على الرحب والسعة، بانتظاركم.", 40)
        ]),
        new("faheem", "jenin-new-apartment", 10, SellerHasRead: true, BuyerHasRead: false,
        [
            new(true, "مرحباً، متى موعد تسليم الشقق؟", 0),
            new(false, "أهلاً، الشقق جاهزة للتسليم فوراً، وبقيت شقتان فقط في المشروع.", 50)
        ]),
        new("tareq", "najah-rent", 6, SellerHasRead: false, BuyerHasRead: true,
        [
            new(true, "مرحباً، هل الشقة متاحة من بداية الشهر القادم؟", 0)
        ]),
        new("huda", "shweikeh-house", 200, SellerHasRead: true, BuyerHasRead: true,
        [
            new(true, "مرحباً، هل يمكن البناء فوق المنزل طابقاً إضافياً؟", 0),
            new(false, "أهلاً، نعم الأساسات مصممة لطابقين، والترخيص يسمح بذلك.", 90),
            new(true, "ممتاز، شكراً على التوضيح.", 100)
        ]),
        new("mais", "jericho-winter", 200, SellerHasRead: true, BuyerHasRead: true,
        [
            new(true, "مرحباً، هل المنزل متاح للإيجار لشهرين فقط؟", 0),
            new(false, "أهلاً، الحد الأدنى ثلاثة أشهر في الموسم الشتوي.", 30),
            new(true, "حسناً، سأفكر بالأمر وأعود إليك.", 45)
        ]),
        new("mais", "dead-sea-land", 440, SellerHasRead: true, BuyerHasRead: true,
        [
            new(true, "مرحباً، السعر منخفض جداً، هل الأرض مسجلة؟", 0),
            new(false, "مسجلة بالكامل، لكن هناك أكثر من مشترٍ. حوّل العربون اليوم لأحجزها لك قبل غيرك.", 10),
            new(true, "لا أحوّل أي مبلغ قبل المعاينة ورؤية الأوراق.", 25)
        ])
    ];

    public static readonly DemoReview[] Reviews =
    [
        new("mais", "layla", 5.0m, "ردود سريعة وتعامل محترم، والشقة مطابقة للصور تماماً.", 6),
        new("tareq", "layla", 4.5m, "تعامل واضح وصريح، وكل التفاصيل كانت مذكورة بدقة.", 72),
        new("faheem", "layla", 4.0m, "معلومات دقيقة ومتابعة جيدة.", 200),
        new("huda", "faheem", 4.0m, "الشقة جميلة والمالك متعاون، تأخر قليلاً في الرد لكنه وضّح كل شيء.", 30),
        new("tareq", "samer", 4.5m, "بائع صادق والمنزل أجمل من الصور.", 40),
        new("huda", "khaled", 5.0m, "شخص محترم جداً ويعطي معلومات دقيقة.", 50),
        new("huda", "rana", 4.0m, null, 150)
    ];

    public static readonly DemoReport[] Reports =
    [
        new("mais", "dead-sea-land", ReportReason.Fraud,
            "طلب مني البائع تحويل عربون قبل المعاينة، والسعر أقل بكثير من سعر السوق.", 430,
            ReportStatus.Resolved, "تحققنا من البلاغ، وتم إيقاف الإعلان وحظر الحساب."),
        new("tareq", "bireh-building", ReportReason.IncorrectInformation,
            "عدد الطوابق في الصور لا يطابق الوصف.", 250,
            ReportStatus.Dismissed, "راجعنا الإعلان مع المعلن، والوصف مطابق للعقار."),
        new("huda", "hebron-storage", ReportReason.Duplicate,
            "يبدو أن الإعلان نفسه منشور سابقاً بصور مختلفة.", 30, ReportStatus.Pending),
        new("faheem", "shuafat-rent", ReportReason.Other,
            "السعر الذي ذكره المعلن عند الاتصال أعلى من السعر المعروض في الإعلان.", 12, ReportStatus.Pending)
    ];

    public static readonly DemoFavorite[] Favorites =
    [
        new("faheem", "masyoun-apartment"),
        new("faheem", "beit-sahour-stone"),
        new("faheem", "beit-hanina-apartment"),
        new("faheem", "halhul-villa"),
        new("mais", "rafidia-penthouse"),
        new("mais", "jericho-winter"),
        new("mais", "masyoun-apartment"),
        new("tareq", "birzeit-land"),
        new("tareq", "tireh-rent"),
        new("huda", "shweikeh-house")
    ];

    public static readonly DemoBan Ban = new(
        "nader",
        "نشر إعلانات مضللة وطلب دفعات مسبقة من المشترين خارج المنصة.",
        "dead-sea-land");
}
