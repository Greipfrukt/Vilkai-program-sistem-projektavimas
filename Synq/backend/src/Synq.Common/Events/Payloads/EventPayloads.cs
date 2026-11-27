using System.Text.Json.Serialization;
using Synq.Common.Contracts;

namespace Synq.Common.Events.Payloads;

// Įvykių payload'ai pagal yaml Event* schemas (P-03). Laukų pavadinimai = kontraktas, nekeisti vienašališkai.
// Kas publikuoja → kas vartoja nurodyta prie kiekvieno.

/// <summary><c>user.profile-updated</c> | Auth → Workspace, Messaging, Search. profileVersion 1 registruojantis.</summary>
public sealed record UserProfileUpdated(Guid UserId, string DisplayName, int AvatarVersion, string Email, int ProfileVersion);

/// <summary>
/// <c>workspace.member-added</c> / <c>workspace.member-removed</c> / <c>workspace.role-changed</c> | Workspace → Messaging, Presence, Search, Notification, Auth.
/// Role nėra (null) member-removed įvykyje. Taikyti tik jei membershipVersion didesnis.
/// </summary>
public sealed record MembershipChanged(
    Guid UserId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] WorkspaceRole? Role,
    long MembershipVersion);

/// <summary><c>channel.created</c> | Workspace → Messaging (sukuria CHANNEL pokalbį, id = channelId), Search.</summary>
public sealed record ChannelCreated(Guid ChannelId, string Name, ChannelVisibility Visibility, IReadOnlyList<Guid> ParticipantUserIds);

/// <summary><c>channel.updated</c> | Workspace → Messaging, Search. Taikyti tik jei channelVersion didesnis.</summary>
public sealed record ChannelUpdated(Guid ChannelId, string Name, bool Archived, long ChannelVersion);

/// <summary><c>channel.participant-added</c> / <c>channel.participant-removed</c> | Workspace → Messaging, Search.</summary>
public sealed record ChannelParticipantChanged(Guid ChannelId, Guid UserId, long MembershipVersion);

/// <summary><c>conversation.created</c> | Messaging → Search, Messaging live.</summary>
public sealed record ConversationCreated(Guid ConversationId, ConversationType Type, IReadOnlyList<Guid> ParticipantUserIds);

/// <summary><c>message.created</c> | Messaging → Messaging live, Search, Notification. Message + participantUserIds.</summary>
public sealed record MessageCreated : MessageModel
{
    public IReadOnlyList<Guid> ParticipantUserIds { get; init; } = [];
}

/// <summary><c>message.updated</c> | Messaging → Messaging live, Search.</summary>
public sealed record MessageUpdated(Guid MessageId, Guid ConversationId, string Text, int EditVersion, IReadOnlyList<Guid> MentionedUserIds);

/// <summary><c>message.deleted</c> | Messaging → Messaging live, Search, Notification.</summary>
public sealed record MessageDeleted(Guid MessageId, Guid ConversationId, long Seq, IReadOnlyList<Guid> AttachmentFileIds, DeletedBy DeletedBy);

/// <summary><c>reaction.changed</c> | Messaging → Messaging live.</summary>
public sealed record ReactionChanged(Guid MessageId, Guid ConversationId, string Emoji, Guid UserId, bool Added, IReadOnlyDictionary<string, int> Counts);

/// <summary><c>read-cursor.updated</c> | Messaging → Messaging live.</summary>
public sealed record ReadCursorUpdated(Guid UserId, Guid ConversationId, long LastReadSeq);

/// <summary><c>presence.changed</c> (synq.transient) | Presence → Messaging live.</summary>
public sealed record PresenceChanged(Guid UserId, PresenceStatus Status, DateTime? LastSeenAtUtc, IReadOnlyList<Guid> WorkspaceIds);
