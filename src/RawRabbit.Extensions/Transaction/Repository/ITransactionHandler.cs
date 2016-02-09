using System;
using System.Threading.Tasks;
using RawRabbit.Context;
using RawRabbit.Extensions.Transaction.Model;

namespace RawRabbit.Extensions.Transaction.Repository
{
	public interface ITransactionHandler
	{
		bool IsRegistered<TMessage>();
		void Register<TMessage, TMessageContext>(Func<TMessage, TMessageContext, Task> func, ExecutionOption option) where TMessageContext : IMessageContext;
		Task<ExecutionFlow> QueueForExecutionAsync<TMessage, TMessageContext>(TMessage message, TMessageContext context) where TMessageContext : IMessageContext;
		TransactionState GetState(Guid globalMessageId);
	}
}