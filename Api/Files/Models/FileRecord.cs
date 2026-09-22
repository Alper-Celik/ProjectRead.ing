// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
// Mostly Ai Generated - Start
using Riok.Mapperly.Abstractions;

// Mostly Ai Generated - End

namespace Api.Files.Models;

[Table("file_records")]
[Index(nameof(OwnerId), nameof(FileKind))]
// Mostly Ai Generated - Start
public class FileRecord : IDbEntityMetadata
// Mostly Ai Generated - End
{
    public static byte IdPostfix => (byte)IdPostfixes.FileRecord;

    [Key]
    public Guid Id { get; set; }
    public int RowVersion { get; set; }
    public NodaTime.Instant MetadataAddedAt { get; set; }
    public NodaTime.Instant MetadataUpdatedAt { get; set; }

    // Mostly Ai Generated - Start
    [MapperIgnore]
    // Mostly Ai Generated - End
    public Guid OwnerId { get; set; }

    public FileKind FileKind { get; set; }

    [ForeignKey(nameof(FileProviderBackendConfig))]
    public required Guid FileProviderBackendConfigId { get; set; }

    public bool Uploaded { get; set; } = false;

    [MaxLength(1000)]
    public string? OriginalFileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public required byte[] SHA256 { get; set; }

    //Navigation properties
    // Mostly Ai Generated - Start
    [MapperIgnore]
    // Mostly Ai Generated - End
    public FileProviderBackendConfig FileProviderBackendConfig { get; set; } = null!;
}
