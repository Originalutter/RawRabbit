using System;
using System.Threading.Tasks;

namespace RawRabbit.Extensions.Saga.Builders.Abstractions
{
	public interface IPassiveSagaBuilder<out TMessageContext>
	{
		IPassiveSagaBuilder<TMessageContext> When<TMessage>(Action<TMessage, TMessageContext> whenFunc, Action<IStepConfigurationBuilder<TMessage, TMessageContext>> config = null);
		IPassiveSagaBuilder<TMessageContext> WhenAsync<TMessage>(Func<TMessage, TMessageContext, Task> whenFunc, Action<IStepConfigurationBuilder<TMessage, TMessageContext>> config = null);
	}
}