using Judhur.Domain.Common.Results;

namespace Judhur.Domain.Conversations;

public static class ConversationErrors
{
    public static readonly Error BuyerIdRequired = Error.Validation("ConversationErrors.BuyerIdRequired", "معرّف المشتري مطلوب");
    public static readonly Error SellerIdRequired = Error.Validation("ConversationErrors.SellerIdRequired", "معرّف البائع مطلوب");
    public static readonly Error PropertyIdRequired = Error.Validation("ConversationErrors.PropertyIdRequired", "معرّف العقار مطلوب");
    public static readonly Error CannotStartConversationWithSelf = Error.Conflict("ConversationErrors.CannotStartConversationWithSelf", "لا تستطيع بدء محادثة مع نفسك");
    public static readonly Error NotParticipant = Error.Forbidden("ConversationErrors.NotParticipant", "لست طرفًا في هذه المحادثة");
    public static readonly Error NotFound = Error.NotFound("ConversationErrors.NotFound", "المحادثة غير موجودة");
    public static readonly Error InvalidCursor = Error.Validation("ConversationErrors.InvalidCursor", "الرسالة المرجعية غير موجودة في هذه المحادثة");
    public static readonly Error ConversationAlreadyExists = Error.Conflict("ConversationErrors.ConversationAlreadyExists", "توجد محادثة قائمة حول هذا العقار");
    public static readonly Error SellerIsNotPropertyOwner = Error.Conflict("ConversationErrors.SellerIsNotPropertyOwner", "البائع ليس صاحب هذا العقار");
}
