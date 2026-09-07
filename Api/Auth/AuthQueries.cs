// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Api.Auth.Handlers;
using Api.Auth.Models;
using Api.Auth.Utils;
using Api.Database;

using HotChocolate.Authorization;

using Riok.Mapperly.Abstractions;

using static Api.Auth.Utils.LoginUtils;

namespace Api.Auth;

[QueryType]
public partial class AuthQueries
{
    [AllowAnonymous]
    public static async Task<RegisterInfo> GetRegisterInfoAsync([Service] PGContext db)
       => new(
         CanRegisterAsAdmin: await CanAdminRegister(db)
        );

    public record RegisterInfo(bool CanRegisterAsAdmin);

    [PermissionCheckAuthorize(UserPermissionBits.UserRead)]
    public static async Task<AuthQueriesUtils.User> GetCurrentUser([Service] PGContext db, [Service] ICurrentUserId id, CancellationToken ct)
    {
        return AuthQueriesUtils.UserMapper.ToDto(await db.Users.FindAsync([id.Id], cancellationToken: ct))!;
    }
}

public static partial class AuthQueriesUtils
{

    [Node]
    public record User(
            Guid Id,
            int RowVersion,
            NodaTime.Instant MetadataAddedAt,
            NodaTime.Instant MetadataUpdatedAt,

            string Email,
            bool EmailVerified,
            bool Admin) : IEntityMetadata
    {
        public static byte IdPostfix => UserEF.IdPostfix;
        public static async Task<User?> GetAsync([Service] PGContext db, Guid id, CancellationToken ct) => UserMapper.ToDto(await db.Users.FindAsync([id], cancellationToken: ct));
    }


    [Mapper]
    public static partial class UserMapper
    {
        public static partial IQueryable<User> ProjectToDto(IQueryable<UserEF> q);

        [MapperIgnoreSource(nameof(UserEF.PasswordHash))]
        public static partial User? ToDto(UserEF? o);
    }
}