// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Api.Utils;

public static class GeneralUtils
{
    public static string NormalizeEmail(this string email) =>
        email.Trim().Normalize().ToLowerInvariant();

    public static NodaTime.Instant Now() =>
        NodaTime.SystemClock.Instance.GetCurrentInstant();

    public static byte[] NewRowVersion()
    {
        var result = new byte[9];
        Random.Shared.NextBytes(result);
        return result;
    }
}
