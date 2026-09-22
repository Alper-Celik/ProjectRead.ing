// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Mostly Ai Generated - Start
using Api.Auth.Handlers;
using Api.Auth.Models;
using Api.Auth.Utils;
using Api.Database;
using Api.Database.Utils;
using Api.Files.FileProviders;
using Api.Files.Queries;
using FairyBread;
using FluentValidation;
using static Api.Utils.ValidatorUtils;

namespace Api.Files.Mutations;

[MutationType]
public static partial class AddFileRecordMutations
{
    [PermissionCheckAuthorize(UserPermissionBits.FileWrite)]
    public static async Task<AddFileRecordPayload> AddFileRecordMutation(
        [Service] ICurrentUserId userId,
        [Service] IEFTransactionDIAccessorService txGetter,
        [Service] UserFileRouter router,
        AddFileRecordInput input,
        CancellationToken ct
    )
    {
        var tx = await txGetter.BeginOrGetTransactionAsync(ct);

        var fileRecord = await router.CreateFileAsync(
            userId.Id!.Value,
            input.FileKind,
            input.ContentType,
            input.OriginalFileName,
            input.SizeBytes,
            Convert.FromHexString(input.Sha256),
            ct
        );

        await tx.CommitAsync(ct);
        return new AddFileRecordPayload(FileRecordMapper.ToDto(fileRecord));
    }
}

public record AddFileRecordPayload(Queries.FileRecord FileRecord);

public record AddFileRecordInput
{
    public required FileKind FileKind { get; init; }

    public required string ContentType { get; init; }

    public string? OriginalFileName { get; init; }

    public required long SizeBytes { get; init; }

    public required string Sha256 { get; init; }

    public class AddFileRecordInputValidator : AbstractValidator<AddFileRecordInput>
    {
        public AddFileRecordInputValidator(
            PGContext db,
            ICurrentUserId userId,
            IEFTransactionDIAccessorService tx
        )
        {
            RuleFor(w => w).BeginTransaction(tx);

            RuleFor(w => w.ContentType).NotEmpty();
            RuleFor(w => w.OriginalFileName).MaximumLength(1000);
            RuleFor(w => w.SizeBytes).GreaterThan(0);
            RuleFor(w => w.Sha256)
                .Must(BeValidSha256)
                .WithMessage("SHA256 must be 64 hexadecimal characters")
                .WithErrorCode(ErrorCodes.INVALID_SHA256);
        }

        static bool BeValidSha256(string sha256) =>
            sha256.Length == 64 && sha256.All(Uri.IsHexDigit);
    }
}
// Mostly Ai Generated - End
