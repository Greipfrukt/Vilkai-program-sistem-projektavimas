namespace Synq.Common.Contracts;

public enum WorkspaceRole { Owner, Administrator, Member }

public enum ChannelVisibility { Public, Private }

public enum ConversationType { Channel, Direct, Group }

public enum MessageKind { User, DocumentCard, System }

public enum DeletedBy { Author, Moderator }

public enum PresenceStatus { Online, Offline }

public enum NotificationType { DirectMessage, Mention }

public static class WorkspaceRoleExtensions
{
    public static bool IsManager(this WorkspaceRole role) => role is WorkspaceRole.Owner or WorkspaceRole.Administrator;
}
