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
using Api.Utils;
using FluentValidation;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using static Api.Utils.ValidatorUtils;

namespace Api.Files.Mutations;

[MutationType]
public static partial class UploadFileRecordContentMutations
{
    [PermissionCheckAuthorize(UserPermissionBits.FileWrite)]
    [Error(typeof(DomainError))]
    public static async Task<UploadFileRecordContentPayload> UploadFileRecordContentAsync(
        [Service] ICurrentUserId userId,
        [Service] IEFTransactionDIAccessorService txGetter,
        [Service] UserFileRouter router,
        [Service] IValidator<UploadFileRecordContentInput> validator,
        UploadFileRecordContentInput input,
        IFile file,
        CancellationToken ct
    )
    {
        await validator.ValidateOrThrowInputAsync(input, ct);

        var tx = await txGetter.BeginOrGetTransactionAsync(ct);

        var fileRecord = await router.SetFileAsync(
            userId.Id!.Value,
            input.Id,
            file.OpenReadStream(),
            ct
        );

        if (fileRecord is null)
        {
            throw new DomainException(
                ErrorCodes.FILE_UPLOAD_FAILED,
                "File content did not match the declared size or SHA256"
            );
        }

        await tx.CommitAsync(ct);
        return new UploadFileRecordContentPayload(FileRecordMapper.ToDto(fileRecord));
    }
}

public record UploadFileRecordContentPayload(Queries.FileRecord FileRecord);

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
