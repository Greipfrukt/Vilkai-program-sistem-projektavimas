namespace Synq.Common.Contracts;


public sealed record UserRef(Guid UserId, string DisplayName);

public sealed record Attachment(Guid FileId, string Name, string MimeType, long SizeBytes);

public sealed record ReactionSummary(string Emoji, int Count, bool ReactedByMe);

public record MessageModel
{
    public required Guid Id { get; init; }
    public required Guid ConversationId { get; init; }
    public required long Seq { get; init; }
    public Guid? ClientRequestId { get; init; }
    public required MessageKind Kind { get; init; }
    public required UserRef Sender { get; init; }
    public string? Text { get; init; }
    public IReadOnlyList<Guid> MentionedUserIds { get; init; } = [];
    public IReadOnlyList<Attachment> Attachments { get; init; } = [];
    public Guid? ParentMessageId { get; init; }
    public int ReplyCount { get; init; }
    public DateTime? LastReplyAtUtc { get; init; }
    public IReadOnlyList<ReactionSummary> Reactions { get; init; } = [];
    public Guid? DocumentId { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public int EditVersion { get; init; }
    public DateTime? EditedAtUtc { get; init; }
    public bool Deleted { get; init; }
    public DeletedBy? DeletedBy { get; init; }
}

public sealed record NotificationModel(
    Guid Id,
    NotificationType Type,
    Guid WorkspaceId,
    Guid ConversationId,
    Guid MessageId,
    UserRef Actor,
    string Preview,
    bool Read,
    DateTime CreatedAtUtc);
