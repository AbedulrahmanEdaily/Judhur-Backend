namespace Judhur.Infrastructure.Data.Seed;

internal static class DemoUsers
{
    public const string Password = "User@12345";

    public static readonly DemoUser[] All =
    [
        new("faheem", "user@judhur.com", "فهيم حسّون", "نابلس", "0599123456",
            "مالك عقارات في نابلس، أعرض شققاً وأراضي من أملاك العائلة وأتعامل مباشرة دون وسطاء.", 240),
        new("layla", "layla@judhur.com", "ليلى منصور", "رام الله", "0598765432",
            "وسيطة عقارية في رام الله والبيرة منذ عشر سنوات، متخصصة في الشقق السكنية والمكاتب.", 300),
        new("samer", "samer@judhur.com", "سامر عودة", "الخليل", "0569876543",
            "أبيع وأؤجر عقارات في الخليل وبيت لحم، وأهتم بالبيوت الحجرية المرممة.", 270),
        new("yousef", "yousef@judhur.com", "يوسف عبد الهادي", "جنين", "0597112233",
            "مقاول بناء، أعرض شققاً جديدة من مشاريعي في جنين والزبابدة وطوباس.", 210),
        new("rana", "rana@judhur.com", "رنا خليل", "طولكرم", "0592334455",
            "أعرض عقارات العائلة في طولكرم وقلقيلية وسلفيت.", 160),
        new("khaled", "khaled@judhur.com", "خالد حمدان", "القدس", "0595667788",
            "وكيل عقاري في القدس وضواحيها وأريحا.", 190),
        new("mais", "mais@judhur.com", "ميس داود", "بيت لحم", "0594556677", null, 70),
        new("tareq", "tareq@judhur.com", "طارق سليم", "رام الله", "0568123456", null, 55),
        new("huda", "huda@judhur.com", "هدى الشامي", "نابلس", "0593445566", null, 40),
        new("nader", "nader@judhur.com", "نادر عيسى", "أريحا", "0591239876", null, 35)
    ];
}
