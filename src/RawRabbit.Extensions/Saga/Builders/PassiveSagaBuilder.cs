using System;
using System.Threading.Tasks;
using RawRabbit.Context;
using RawRabbit.Extensions.Saga.Builders.Abstractions;
using RawRabbit.Extensions.Saga.Repository.Abstractions;

namespace RawRabbit.Extensions.Saga.Builders
{
	public class PassiveSagaBuilder<TMessageContext> : SagaBuilderBase<TMessageContext>, IPassiveSagaBuilder<TMessageContext> where TMessageContext: IMessageContext
	{
		public PassiveSagaBuilder(ISagaRepository sagaRepo, IMessageHandlerRepository handlerRepo, IBusClient<TMessageContext> busClient)
			: base(sagaRepo, handlerRepo, busClient)
		{}

		public IPassiveSagaBuilder<TMessageContext> When<TMessage>(Action<TMessage, TMessageContext> whenFunc, Action<IStepConfigurationBuilder<TMessage, TMessageContext>> config = null)
		{
			return WhenBase(whenFunc, config) as IPassiveSagaBuilder<TMessageContext>;
		}

		public IPassiveSagaBuilder<TMessageContext> WhenAsync<TMessage>(Func<TMessage, TMessageContext, Task> whenFunc, Action<IStepConfigurationBuilder<TMessage, TMessageContext>> config = null)
		{
			return WhenAsyncBase(whenFunc, config) as IPassiveSagaBuilder<TMessageContext>;
		}

		public IPassiveSagaBuilder<TMessageContext> WhenAsync<TMessage>(Func<TMessage, TMessageContext, Task> whenFunc, StepConfiguration<TMessage, TMessageContext> config)
		{
			return WhenAsyncBase(whenFunc, config) as IPassiveSagaBuilder<TMessageContext>;
		}

		public IPassiveSagaBuilder<TMessageContext> When<TMessage>(Action<TMessage, TMessageContext> whenFunc, StepConfiguration<TMessage, TMessageContext> config)
		{
			return WhenBase(whenFunc, config) as IPassiveSagaBuilder<TMessageContext>;
		}

	}
}