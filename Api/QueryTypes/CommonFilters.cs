// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Api.Utils;

namespace Api.QueryTypes;

public record EntityMetadataFilter<T> : OneOfFilter<T>
    where T : IEntityMetadata
{
    public GuidFilter? Id
    {
        get { return (GuidFilter?)GetOneOf(); }
        set { SetOneOf(value, e => e.Id); }
    }

    public RowVersionFilter? RowVersion
    {
        get { return (RowVersionFilter?)GetOneOf(); }
        set { SetOneOf(value, e => e.RowVersion); }
    }

    public TimeFilter? MetadataAddedAt
    {
        get { return (TimeFilter?)GetOneOf(); }
        set { SetOneOf(value, e => e.MetadataAddedAt); }
    }

    public TimeFilter? MetadataUpdatedAt
    {
        get { return (TimeFilter?)GetOneOf(); }
        set { SetOneOf(value, e => e.MetadataUpdatedAt); }
    }
}
