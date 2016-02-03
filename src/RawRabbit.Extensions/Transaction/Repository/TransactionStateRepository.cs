using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RawRabbit.Extensions.Transaction.Model;

namespace RawRabbit.Extensions.Transaction.Repository
{
	public class TransactionStateRepository : ITransactionStateRepository 
	{
		private readonly List<TransactionState> _states;
		private readonly Task _completed = Task.FromResult(true);

		public TransactionStateRepository()
		{
			_states = new List<TransactionState>();
		}

		public Task<TransactionState> GetOrCreateAsync(Guid globalMessageId)
		{
			var state = _states.FirstOrDefault(s => s.GlobalMessageId == globalMessageId);
			if (state == null)
			{
				state = new TransactionState
				{
					GlobalMessageId = globalMessageId
				};
				_states.Add(state);
			}
			return Task.FromResult(state);
		}

		public Task UpdateAsync(TransactionState state)
		{
			return _completed;
		}
	}
}