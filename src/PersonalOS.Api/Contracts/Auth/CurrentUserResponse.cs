using PersonalOS.Infrastructure.Identity;

namespace PersonalOS.Api.Contracts.Auth;

public sealed record CurrentUserResponse(Guid Id, string DisplayName, string Email)
{
    public static CurrentUserResponse FromUser(AppUser user) =>
        new(user.Id, user.DisplayName, user.Email ?? string.Empty);
}
