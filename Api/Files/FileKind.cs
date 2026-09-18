// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Api.Files;

public enum FileKind
{
    /// <summary>
    /// <para>regular user file like uploaded epub or explicitly uploaded cover</para>
    /// <em>doesn't include fetched covers etc.</em></summary>
    UserFile = 1,

    /// <summary>
    /// optimized files like difrent sizes of cover with browser optimized format.
    /// </summary>
    OptimizedFile = 2,

    /// <summary>
    /// non processed and extracted files from already existing files like covers from epubs.
    /// </summary>
    ExtractedFile = 3,

    /// <summary>
    /// Generated files that can be easily recreated like a epub generated from html files or .kepub generated from epub
    /// </summary>
    GeneratedFile = 4,

    /// <summary>
    /// files uploaded for further processing like epub uploaded for extracting metadata
    /// and cover or cover uploaded for optimization to web friendly versions and blur
    /// hash extraction
    /// </summary>
    IngestFile = 5,

    /// <summary>
    ///     <para>
    ///     files fetched for behalf of user.
    ///     </para>
    /// Like :
    /// <list type="bullet">
    ///     <item> webpage saved from a rss feed or a work</item>
    ///     <item> a cover fetched from a book metadata website</item>
    ///     <item> a fiction fetched with FanFicFare like tool by the server</item>
    /// </list>
    /// </summary>
    FetchedFile = 6,
}
