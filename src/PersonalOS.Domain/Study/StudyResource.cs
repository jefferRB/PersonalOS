using PersonalOS.Domain.Common;

namespace PersonalOS.Domain.Study;

/// <summary>
/// A reference to material that belongs to a study project.
/// </summary>
/// <remarks>
/// Only metadata is stored: a title, a type, an optional link, and an optional note. No file is
/// uploaded and nothing is stored on the server's behalf, because binary storage would need
/// virus scanning, size limits, signed access, quota handling, and a deletion policy that this
/// milestone does not have.
/// </remarks>
public sealed class StudyResource
{
    /// <summary>Maximum stored length of the resource title.</summary>
    public const int TitleMaxLength = 200;

    /// <summary>Maximum stored length of the resource notes.</summary>
    public const int NotesMaxLength = 1000;

    private StudyResource()
    {
    }

    /// <summary>Identifier of this resource.</summary>
    public Guid Id { get; private set; }

    /// <summary>Project this resource belongs to.</summary>
    public Guid StudyProjectId { get; private set; }

    /// <summary>How the user named the material.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>What kind of material it is.</summary>
    public StudyResourceType ResourceType { get; private set; }

    /// <summary>Optional <c>http</c> or <c>https</c> address. Never fetched by the server.</summary>
    public string? ExternalUrl { get; private set; }

    /// <summary>Optional note, for example where the material stops being useful.</summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Creates a resource reference.
    /// </summary>
    /// <param name="title">How the user named the material.</param>
    /// <param name="resourceType">What kind of material it is.</param>
    /// <param name="externalUrl">Optional web address.</param>
    /// <param name="notes">Optional note.</param>
    public static StudyResource Create(
        string? title,
        StudyResourceType resourceType,
        string? externalUrl,
        string? notes) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = TextRules.NormalizeRequiredOrThrow(title, 1, TitleMaxLength, nameof(title)),
            ResourceType = resourceType,
            ExternalUrl = ExternalUrlRules.NormalizeOrThrow(externalUrl, nameof(externalUrl)),
            Notes = TextRules.NormalizeOptionalOrThrow(notes, NotesMaxLength, nameof(notes)),
        };

    internal void AttachTo(Guid studyProjectId) => StudyProjectId = studyProjectId;
}
