// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Api.Auth.Handlers;
using Api.Auth.Models;
using Api.Auth.Utils;
using Api.Database;
using Riok.Mapperly.Abstractions;

namespace Api.Auth.Queries;

[QueryType]
public partial class CurrentUserQueries
{
    [PermissionCheckAuthorize(UserPermissionBits.UserRead)]
    public static async Task<User> GetCurrentUser(
        [Service] PGContext db,
        [Service] ICurrentUserId id,
        CancellationToken ct
    )
    {
        return UserMapper.ToDto(
            await db.Users.FindAsync([id.Id], cancellationToken: ct)
        )!;
    }
}

[Node]
public record User(
    Guid Id,
    int RowVersion,
    NodaTime.Instant MetadataAddedAt,
    NodaTime.Instant MetadataUpdatedAt,
    string Email,
    bool EmailVerified,
    bool Admin
) : IEntityMetadata, INode
{
    public static byte IdPostfix => UserEF.IdPostfix;

    [PermissionCheckAuthorize(UserPermissionBits.UserRead)]
    public static async Task<User?> GetUserAsync(
        [Service] PGContext db,
        Guid id,
        CancellationToken ct
    )
    {
        var user = await db.Users.FindAsync([id], cancellationToken: ct);
        return user?.Id == id ? UserMapper.ToDto(user) : null;
    }
}

[Mapper]
public static partial class UserMapper
{
    public static partial IQueryable<User> ProjectToDto(IQueryable<UserEF> q);

    [MapperIgnoreSource(nameof(UserEF.PasswordHash))]
    public static partial User? ToDto(UserEF? o);
}
