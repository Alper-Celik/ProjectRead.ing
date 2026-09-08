// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore.Storage;

namespace Api.Database.Utils;

public interface IEFTransactionDIAccessorService : IAsyncDisposable, IDisposable
{
    public IDbContextTransaction BeginOrGetTransaction();
    public Task<IDbContextTransaction> BeginOrGetTransactionAsync(
        CancellationToken? ct = null
    );
    public ValueTask FinishTransaction();
}

public class EFTransactionDIAccessorService(PGContext db)
    : IEFTransactionDIAccessorService
{
    private IDbContextTransaction? _tx;

    public IDbContextTransaction BeginOrGetTransaction()
    {
        _tx ??= db.Database.BeginTransaction();
        return _tx;
    }

    public async Task<IDbContextTransaction> BeginOrGetTransactionAsync(
        CancellationToken? ct = null
    )
    {
        _tx ??= await db.Database.BeginTransactionAsync(
            cancellationToken: ct ?? CancellationToken.None
        );
        return _tx;
    }

    public async ValueTask FinishTransaction()
    {
        if (_tx is not null)
            await _tx.DisposeAsync();

        _tx = null;
    }

    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
        _tx?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        if (_tx is not null)
            await _tx.DisposeAsync();
    }
}
