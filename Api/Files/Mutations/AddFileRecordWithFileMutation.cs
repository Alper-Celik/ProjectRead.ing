// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Mostly Ai Generated - Start
using Api.Auth.Handlers;
using Api.Auth.Models;
using Api.Auth.Utils;
using Api.Database.Utils;
using Api.Files.FileProviders;
using Api.Files.Queries;
using Api.Utils;
using FluentValidation;
using HotChocolate.Types;

namespace Api.Files.Mutations;

[MutationType]
public static partial class AddFileRecordWithFileMutations
{
    [PermissionCheckAuthorize(UserPermissionBits.FileWrite)]
    [Error(typeof(DomainError))]
    // The input type is shared with `addFileRecord`, so it cannot follow the derived name.
    [UseMutationConvention(InputTypeName = "AddFileRecordInput")]
    public static async Task<AddFileRecordWithFilePayload> AddFileRecordWithFileAsync(
        [Service] ICurrentUserId userId,
        [Service] IEFTransactionDIAccessorService txGetter,
        [Service] UserFileRouter router,
        [Service] IValidator<AddFileRecordInput> validator,
        AddFileRecordInput input,
        IFile file,
        CancellationToken ct
    )
    {
        await validator.ValidateOrThrowInputAsync(input, ct);

        var tx = await txGetter.BeginOrGetTransactionAsync(ct);
        var ownerId = userId.Id!.Value;

        var fileRecord = await router.CreateFileAsync(
            ownerId,
            input.FileKind,
            input.ContentType,
            input.OriginalFileName,
            input.SizeBytes,
            Convert.FromHexString(input.Sha256),
            ct
        );

        if (
            await router.SetFileAsync(ownerId, fileRecord.Id, file.OpenReadStream(), ct)
            is null
        )
        {
            throw new DomainException(
                ErrorCodes.FILE_UPLOAD_FAILED,
                "File content did not match the declared size or SHA256"
            );
        }

        await tx.CommitAsync(ct);
        return new AddFileRecordWithFilePayload(FileRecordMapper.ToDto(fileRecord));
    }
}

public record AddFileRecordWithFilePayload(Queries.FileRecord FileRecord);
// Mostly Ai Generated - End
