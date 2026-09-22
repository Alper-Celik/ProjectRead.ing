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
using HotChocolate.Types;

namespace Api.Files.Mutations;

[MutationType]
public static partial class AddFileRecordWithFileMutations
{
    [PermissionCheckAuthorize(UserPermissionBits.FileWrite)]
    public static async Task<AddFileRecordPayload> AddFileRecordWithFileMutation(
        [Service] ICurrentUserId userId,
        [Service] IEFTransactionDIAccessorService txGetter,
        [Service] UserFileRouter router,
        AddFileRecordInput input,
        IFile file,
        CancellationToken ct
    )
    {
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
            throw new GraphQLException(
                ErrorBuilder
                    .New()
                    .SetMessage("File content did not match the declared size or SHA256")
                    .SetCode(ErrorCodes.FILE_UPLOAD_FAILED)
                    .Build()
            );
        }

        await tx.CommitAsync(ct);
        return new AddFileRecordPayload(FileRecordMapper.ToDto(fileRecord));
    }
}
// Mostly Ai Generated - End
