// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Api;

public static partial class ErrorCodes
{
    public const string INVALID_CREDS = "INVALID_CREDS";
    public const string INVALID_ID = "INVALID_ID";
    public const string IDS_DOES_NOT_EXIST = "IDS_DOES_NOT_EXIST";
    public const string ID_DOES_NOT_EXIST = "ID_DOES_NOT_EXIST";
    public const string IS_NOT_DISTINCT = "IS_NOT_DISTINCT";
    public const string ROW_VERSION_MISMATCH = "ROW_VERSION_MISMATCH";
}