// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Api.Files.FileProviders;

namespace Api.Files.Models;

public class FileProviderBackendConfig : IEntityMetadata
{
    public static byte IdPostfix => (byte)IdPostfixes.FileProviderBackend;

    [Key]
    public Guid Id { get; set; }
    public int RowVersion { get; set; }
    public NodaTime.Instant MetadataAddedAt { get; set; }
    public NodaTime.Instant MetadataUpdatedAt { get; set; }

    public Guid? OwnerId { get; set; }
    public bool InstanceWide { get; set; }
    public bool FallbackDefault { get; set; }
    public FileKind[] DefaultFor { get; set; } = null!;

    public required FileProviderId ProviderId { get; set; }
    public JsonDocument? ProviderConfig { get; set; }
    public List<byte[]>? EncryptedSecrets { get; set; }
}
