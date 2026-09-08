// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel.DataAnnotations.Schema;

public interface IDbEntityMetadata : IEntityMetadata
{
    public Guid OwnerId { get; }
}
public interface IEntityMetadata : IBasicEntityMetadata
{
    public NodaTime.Instant MetadataAddedAt { get; }
    public NodaTime.Instant MetadataUpdatedAt { get; }
}
public interface IBasicEntityMetadata
{
    [NotMapped]
    public static abstract byte IdPostfix { get; }
    public Guid Id { get; }
    public int RowVersion { get; }
}