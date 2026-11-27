namespace Synq.Common.Errors;

/// <summary>
/// Visas klaidų kodų katalogas iš Synq.yaml („Error code catalogue“) su HTTP statusais (P-02).
/// Kodas yra stabilus raktas klientų kalbų failams – nekeisti be susitarimo.
/// </summary>
public static class ErrorCodes
{
    public const string BadRequest = "BAD_REQUEST";
    public const string AuthRequired = "AUTH_REQUIRED";
    public const string SessionExpired = "SESSION_EXPIRED";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string Forbidden = "FORBIDDEN";
    public const string ChannelArchived = "CHANNEL_ARCHIVED";
    public const string InvitationEmailMismatch = "INVITATION_EMAIL_MISMATCH";
    public const string NotFound = "NOT_FOUND";
    public const string MethodNotAllowed = "METHOD_NOT_ALLOWED";
    public const string EmailTaken = "EMAIL_TAKEN";
    public const string ChannelNameTaken = "CHANNEL_NAME_TAKEN";
    public const string AlreadyMember = "ALREADY_MEMBER";
    public const string InvitationUsed = "INVITATION_USED";
    public const string InvitationExpired = "INVITATION_EXPIRED";
    public const string InvitationRevoked = "INVITATION_REVOKED";
    public const string OwnerMustTransfer = "OWNER_MUST_TRANSFER";
    public const string EditConflict = "EDIT_CONFLICT";
    public const string FileNotReady = "FILE_NOT_READY";
    public const string FileAttached = "FILE_ATTACHED";
    public const string PayloadTooLarge = "PAYLOAD_TOO_LARGE";
    public const string FileTooLarge = "FILE_TOO_LARGE";
    public const string UnsupportedMediaType = "UNSUPPORTED_MEDIA_TYPE";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string NotAMember = "NOT_A_MEMBER";
    public const string AttachmentNotReady = "ATTACHMENT_NOT_READY";
    public const string UnknownEmoji = "UNKNOWN_EMOJI";
    public const string UseOwnershipTransfer = "USE_OWNERSHIP_TRANSFER";
    public const string UnarchiveNotSupported = "UNARCHIVE_NOT_SUPPORTED";
    public const string RateLimited = "RATE_LIMITED";
    public const string InternalError = "INTERNAL_ERROR";
    public const string DependencyUnavailable = "DEPENDENCY_UNAVAILABLE";
    public const string GatewayTimeout = "GATEWAY_TIMEOUT";

    /// <summary>Kodas → (HTTP statusas, numatytas angliškas tekstas).</summary>
    public static readonly IReadOnlyDictionary<string, (int Status, string Message)> Catalogue =
        new Dictionary<string, (int, string)>
        {
            [BadRequest] = (400, "The request is malformed."),
            [AuthRequired] = (401, "Please sign in."),
            [SessionExpired] = (401, "Your session has expired. Please sign in again."),
            [InvalidCredentials] = (401, "Email or password is incorrect."),
            [Forbidden] = (403, "You do not have permission to do this."),
            [ChannelArchived] = (403, "This channel is archived and is read-only."),
            [InvitationEmailMismatch] = (403, "This invitation was sent to a different email address."),
            [NotFound] = (404, "The requested resource was not found."),
            [MethodNotAllowed] = (405, "This method is not supported."),
            [EmailTaken] = (409, "This email is already registered."),
            [ChannelNameTaken] = (409, "A channel with this name already exists."),
            [AlreadyMember] = (409, "The user is already a member of this workspace."),
            [InvitationUsed] = (409, "This invitation has already been used."),
            [InvitationExpired] = (409, "This invitation has expired."),
            [InvitationRevoked] = (409, "This invitation has been revoked."),
            [OwnerMustTransfer] = (409, "The owner must transfer ownership first."),
            [EditConflict] = (409, "The message was changed by someone else."),
            [FileNotReady] = (409, "The file is not ready."),
            [FileAttached] = (409, "An attached file cannot be cancelled."),
            [PayloadTooLarge] = (413, "The request body is too large."),
            [FileTooLarge] = (413, "The file is too large."),
            [UnsupportedMediaType] = (415, "This file type is not allowed."),
            [ValidationFailed] = (422, "Some fields are invalid."),
            [NotAMember] = (422, "The user is not a member of this workspace."),
            [AttachmentNotReady] = (422, "The attachment is not ready."),
            [UnknownEmoji] = (422, "This emoji is not supported."),
            [UseOwnershipTransfer] = (422, "Use ownership transfer to change the owner."),
            [UnarchiveNotSupported] = (422, "Archived channels cannot be unarchived."),
            [RateLimited] = (429, "Too many requests. Please try again later."),
            [InternalError] = (500, "Something went wrong. Please try again."),
            [DependencyUnavailable] = (503, "The service is temporarily unavailable. Please try again."),
            [GatewayTimeout] = (504, "The service did not respond in time."),
        };

    public static int StatusOf(string code) => Catalogue.TryGetValue(code, out var e) ? e.Status : 500;

    public static string MessageOf(string code) =>
        Catalogue.TryGetValue(code, out var e) ? e.Message : Catalogue[InternalError].Message;

    /// <summary>HTTP statusas be kūno (pvz. routing 404) → tinkamas kodas.</summary>
    public static string ForStatus(int status) => status switch
    {
        400 => BadRequest,
        401 => AuthRequired,
        403 => Forbidden,
        404 => NotFound,
        405 => MethodNotAllowed,
        413 => PayloadTooLarge,
        415 => UnsupportedMediaType,
        422 => ValidationFailed,
        429 => RateLimited,
        503 => DependencyUnavailable,
        504 => GatewayTimeout,
        _ => InternalError,
    };
}
