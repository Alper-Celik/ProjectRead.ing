// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Api.Utils;

/// <summary>
/// An expected failure carrying the error code reported to the client. Throw it wherever the
/// failure is detected — input rules, business conflicts, rejected uploads — and
/// HotChocolate's mutation error middleware (see <c>[Error]</c>) reports it in the payload's
/// <c>errors</c> list. Unexpected failures stay exceptions and remain GraphQL errors.
/// </summary>
public sealed class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>
/// The error shape reported in mutation payloads, paired with <see cref="DomainException"/>
/// through its <c>CreateErrorFrom</c> factory. Named <c>DomainError</c> because <c>Error</c>
/// is the interface HotChocolate adds to every error type.
/// </summary>
public sealed class DomainError
{
    private DomainError(string message, string code) => (Message, Code) = (message, code);

    public string Message { get; }

    public string Code { get; }

    public static DomainError CreateErrorFrom(DomainException ex) =>
        new(ex.Message, ex.Code);
}
