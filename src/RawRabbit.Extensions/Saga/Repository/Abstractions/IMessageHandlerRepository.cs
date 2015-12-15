using System;
using System.Threading.Tasks;
using RawRabbit.Extensions.Saga.Builders.Abstractions;
using RawRabbit.Extensions.Saga.Model;

namespace RawRabbit.Extensions.Saga.Repository.Abstractions
{
	public interface IMessageHandlerRepository
	{
		void Add<TMessage, TMessageContext>(Func<TMessage, TMessageContext, Task> whenAction, StepConfiguration<TMessage, TMessageContext> config);
		void Add<TMessage, TMessageContext>(Action<TMessage, TMessageContext> whenAction, StepConfiguration<TMessage, TMessageContext> config);
		bool TryExecute<TMessage, TMessageContext>(TMessage message, TMessageContext context, ISaga saga);
		bool IsRegistered(Type type);
		bool IsCompleted(ISaga globalMessageId);
	}
}