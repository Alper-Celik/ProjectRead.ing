// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Mostly Ai Generated - Start
using Api.Auth.Handlers;
using Api.Auth.Models;
using Api.Auth.Utils;
using Api.Files.FileProviders;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Api.Files.Endpoints;

public static class GetFileEndpoint
{
    static readonly string[] s_inlineContentTypes =
    [
        "image/avif",
        "image/jpeg",
        "application/epub+zip",
    ];

    [PermissionCheckAuthorize(UserPermissionBits.FileRead)]
    public static async Task<Results<FileStreamHttpResult, NotFound>> GetFileAsync(
        Guid id,
        [FromServices] ICurrentUserId userId,
        [FromServices] UserFileRouter router,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        if (await router.GetFileAsync(userId.Id!.Value, id, ct) is not { } content)
        {
            return TypedResults.NotFound();
        }

        httpContext.Response.Headers["Cache-Control"] =
            "private, max-age=31536000, immutable";
        httpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";

        var inline = s_inlineContentTypes.Contains(content.ContentType);

        return TypedResults.File(
            content.Stream,
            inline ? content.ContentType : "application/octet-stream",
            inline ? null : content.OriginalFileName ?? id.ToString(),
            content.MetadataAddedAt.ToDateTimeOffset(),
            new EntityTagHeaderValue($"\"{Convert.ToHexStringLower(content.SHA256)}\""),
            enableRangeProcessing: true
        );
    }
}
// Mostly Ai Generated - End
