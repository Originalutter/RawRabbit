using System;
using System.Threading.Tasks;
using RawRabbit.Extensions.Saga.Model;

namespace RawRabbit.Extensions.Saga.Builders.Abstractions
{
	public interface IActiveSagaBuilder<TMessageContext>
	{
		IActiveSagaBuilder<TMessageContext> WhenAsync<TMessage>(Func<TMessage, TMessageContext, Task> whenFunc, Action<IStepConfigurationBuilder<TMessage, TMessageContext>> config = null);
		IActiveSagaBuilder<TMessageContext> When<TMessage>(Action<TMessage, TMessageContext> whenFunc, Action<IStepConfigurationBuilder<TMessage, TMessageContext>> config = null);
		ISaga<TMessage> Complete<TMessage>(Action<IMandatoryStepConfigurationBuilder<TMessage, TMessageContext>> matching = null);
	}
}