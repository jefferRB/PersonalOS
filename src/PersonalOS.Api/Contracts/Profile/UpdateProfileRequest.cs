namespace PersonalOS.Api.Contracts.Profile;

/// <summary>
/// Values a client may change through <c>PUT /api/profile</c>.
/// </summary>
/// <remarks>
/// <para>
/// The contract exposes only the two editable fields. It carries no account identifier, so a
/// client cannot select another account, and it carries no email, so an email change cannot be
/// smuggled in through over-posting. Unknown JSON properties are ignored by the serializer.
/// </para>
/// <para>
/// The properties are nullable and carry no data annotations on purpose: all validation runs in
/// <see cref="Application.Profile.UserProfileService"/>, which keeps one set of rules and returns
/// field names that match this contract.
/// </para>
/// </remarks>
public sealed class UpdateProfileRequest
{
    /// <summary>
    /// Requested display name. The server trims it before validating.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Requested IANA time-zone identifier, for example <c>America/Costa_Rica</c>.
    /// </summary>
    public string? TimeZoneId { get; init; }
}
