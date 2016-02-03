using System;
using System.Threading.Tasks;
using RawRabbit.Extensions.Transaction.Model;

namespace RawRabbit.Extensions.Transaction.Repository
{
	public interface ITransactionStateRepository
	{
		Task<TransactionState> GetOrCreateAsync(Guid globalMessageId);
		Task UpdateAsync(TransactionState state);
	}
}