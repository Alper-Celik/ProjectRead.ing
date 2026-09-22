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
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Files.Endpoints;

public static class UploadFileEndpoint
{
    [PermissionCheckAuthorize(UserPermissionBits.FileWrite)]
    [RequestFormLimits(MultipartBodyLengthLimit = 5L * 1024 * 1024 * 1024)]
    public static async Task<
        Results<NoContent, NotFound, Conflict, UnprocessableEntity<string>>
    > UploadFileAsync(
        Guid id,
        IFormFile file,
        [FromServices] PGContext db,
        [FromServices] ICurrentUserId userId,
        [FromServices] IEFTransactionDIAccessorService txGetter,
        [FromServices] UserFileRouter router,
        CancellationToken ct
    )
    {
        var ownerId = userId.Id!.Value;

        var uploaded = await db
            .FileRecords.Where(f => f.OwnerId == ownerId && f.Id == id)
            .Select(f => (bool?)f.Uploaded)
            .FirstOrDefaultAsync(ct);

        if (uploaded is null)
        {
            return TypedResults.NotFound();
        }

        if (uploaded.Value)
        {
            return TypedResults.Conflict();
        }

        var tx = await txGetter.BeginOrGetTransactionAsync(ct);

        if (await router.SetFileAsync(ownerId, id, file.OpenReadStream(), ct) is null)
        {
            return TypedResults.UnprocessableEntity(
                "File content did not match the declared size or SHA256"
            );
        }

        await tx.CommitAsync(ct);
        return TypedResults.NoContent();
    }
}
// Mostly Ai Generated - End
