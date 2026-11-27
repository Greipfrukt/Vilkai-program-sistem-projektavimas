namespace Synq.Common.Events;

/// <summary>
/// Įvykių tipai (= routing key) iš yaml <c>EventEnvelope.eventType</c> (P-03).
/// Be dokumentų ir failų įvykių – jie ne mūsų apimtyje.
/// </summary>
public static class EventTypes
{
    public const string UserProfileUpdated = "user.profile-updated";

    public const string WorkspaceMemberAdded = "workspace.member-added";
    public const string WorkspaceMemberRemoved = "workspace.member-removed";
    public const string WorkspaceRoleChanged = "workspace.role-changed";

    public const string ChannelCreated = "channel.created";
    public const string ChannelUpdated = "channel.updated";
    public const string ChannelParticipantAdded = "channel.participant-added";
    public const string ChannelParticipantRemoved = "channel.participant-removed";

    public const string ConversationCreated = "conversation.created";
    public const string MessageCreated = "message.created";
    public const string MessageUpdated = "message.updated";
    public const string MessageDeleted = "message.deleted";
    public const string ReactionChanged = "reaction.changed";
    public const string ReadCursorUpdated = "read-cursor.updated";

    public const string NotificationCreated = "notification.created";

    /// <summary>Tik per <see cref="RabbitMqTopology.TransientExchange"/>.</summary>
    public const string PresenceChanged = "presence.changed";
}
