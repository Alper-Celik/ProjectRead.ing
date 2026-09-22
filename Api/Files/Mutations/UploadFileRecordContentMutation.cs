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
using FluentValidation;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using static Api.Utils.ValidatorUtils;

namespace Api.Files.Mutations;

[MutationType]
public static partial class UploadFileRecordContentMutations
{
    [PermissionCheckAuthorize(UserPermissionBits.FileWrite)]
    public static async Task<AddFileRecordPayload> UploadFileRecordContentMutation(
        [Service] ICurrentUserId userId,
        [Service] IEFTransactionDIAccessorService txGetter,
        [Service] UserFileRouter router,
        UploadFileRecordContentInput input,
        IFile file,
        CancellationToken ct
    )
    {
        var tx = await txGetter.BeginOrGetTransactionAsync(ct);

        var fileRecord =
            await router.SetFileAsync(
                userId.Id!.Value,
                input.Id,
                file.OpenReadStream(),
                ct
            )
            ?? throw new GraphQLException(
                ErrorBuilder
                    .New()
                    .SetMessage("File content did not match the declared size or SHA256")
                    .SetCode(ErrorCodes.FILE_UPLOAD_FAILED)
                    .Build()
            );

        await tx.CommitAsync(ct);
        return new AddFileRecordPayload(FileRecordMapper.ToDto(fileRecord));
    }
}

public record UploadFileRecordContentInput
{
    public required Guid Id { get; init; }

    public class UploadFileRecordContentInputValidator
        : AbstractValidator<UploadFileRecordContentInput>
    {
        public UploadFileRecordContentInputValidator(
            PGContext db,
            ICurrentUserId userId,
            IEFTransactionDIAccessorService tx
        )
        {
            RuleFor(w => w).BeginTransaction(tx);

            RuleFor(w => w.Id).IdMustExist(db.FileRecords, userId.Id);

            RuleFor(w => w.Id)
                .MustAsync(
                    async (id, ct) =>
                        !await db.FileRecords.AnyAsync(
                            f => f.OwnerId == userId.Id && f.Id == id && f.Uploaded,
                            ct
                        )
                )
                .WithMessage("File is already uploaded")
                .WithErrorCode(ErrorCodes.FILE_ALREADY_UPLOADED);
        }
    }
}
// Mostly Ai Generated - End
