using System;
using System.Threading.Tasks;
using RawRabbit.Context;
using RawRabbit.Extensions.Transaction.Model;

namespace RawRabbit.Extensions.Transaction.Configuration
{
	public interface ITransactionPublisher<TMessageContext> where TMessageContext : IMessageContext
	{
		ITransactionBuilder<TMessageContext> PublishAsync<TMessage>(TMessage message = default(TMessage), Guid globalMessageId = default(Guid))
			where TMessage : new();
	}

	public interface ITransactionBuilder<TMessageContext> where TMessageContext : IMessageContext
	{
		ITransactionBuilder<TMessageContext> When<TMessage>(Func<TMessage, TMessageContext, Task> action, Action<IExecutionOptionBuilder> options = null);
		Transaction<TMessage> Complete<TMessage>(Func<TMessage, TMessageContext, Task> func = null);
	}
}
