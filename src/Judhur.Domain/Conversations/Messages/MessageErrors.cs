using Judhur.Domain.Common.Results;

namespace Judhur.Domain.Conversations.Messages;

public static class MessageErrors
{
    public static readonly Error ConversationIdRequired = Error.Validation("MessageErrors.ConversationIdRequired", "معرف المحادثة مطلوب");
    public static readonly Error SenderIdRequired = Error.Validation("MessageErrors.SenderIdRequired", "معرف المرسل مطلوب");
    public static readonly Error ContentRequired = Error.Validation("MessageErrors.ContentRequired", "نص الرسالة مطلوب");
    public static readonly Error ContentTooLong = Error.Validation("MessageErrors.ContentTooLong", $"لا يمكن أن تتجاوز الرسالة {Message.MaxContentLength} حرف");
}
