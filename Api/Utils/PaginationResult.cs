// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Api.Utils;

public record PaginationResult<T>(
    IReadOnlyCollection<T> Items,
    int TotalCount,
    int PageSize,
    int CurrentPage,
    int TotalPages
);
