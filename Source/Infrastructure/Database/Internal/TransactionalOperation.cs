using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Database.Internal;

internal class TransactionalOperation : IDisposable
{
    private readonly IDbContextTransaction Transaction;
    public TransactionalOperation(IDbContextTransaction transaction) => this.Transaction = transaction;
    
    public void Dispose()
    {
        this.Transaction.Commit();
        this.Transaction.Dispose();
    }
}
