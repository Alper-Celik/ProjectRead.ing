// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Api.Database;
using HotChocolate.Authorization;
using static Api.Auth.Utils.AuthUtils;

namespace Api.Auth.Queries;

public record RegisterInfo(bool CanRegisterAsAdmin);

[QueryType]
public partial class RegisterInfoQueries
{
    [AllowAnonymous]
    public static async Task<RegisterInfo> GetRegisterInfoAsync([Service] PGContext db) =>
        new(CanRegisterAsAdmin: await CanAdminRegister(db));
}
