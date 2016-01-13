using System;
using System.Threading.Tasks;

namespace RawRabbit.Extensions.Saga.Builders.Abstractions
{
	public interface ISagaInitializer<TMessageContext>
	{
		IPassiveSagaBuilder<TMessageContext> Recieve<TMessage>(Action<TMessage, TMessageContext> whenFunc, Action<IStepConfigurationBuilder<TMessage, TMessageContext>> config = null);
		IPassiveSagaBuilder<TMessageContext> RecieveAsync<TMessage>(Func<TMessage, TMessageContext, Task> whenFunc, Action<IStepConfigurationBuilder<TMessage, TMessageContext>> config = null);
		IActiveSagaBuilder<TMessageContext> PublishAsync<TMessage>(TMessage message);
	}
}