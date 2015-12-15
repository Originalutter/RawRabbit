using System;
using System.Threading.Tasks;
using RawRabbit.Context;
using RawRabbit.Extensions.Client;
using RawRabbit.Extensions.Saga.Builders.Abstractions;
using RawRabbit.Extensions.Saga.Repository;
using RawRabbit.Extensions.Saga.Repository.Abstractions;

namespace RawRabbit.Extensions.Saga.Builders
{
	public class SagaInitializer<TMessageContext> : ISagaInitializer<TMessageContext> where TMessageContext : IMessageContext
	{
		private readonly ExtendableBusClient<TMessageContext> _busClient;

		public SagaInitializer(ExtendableBusClient<TMessageContext> busClient)
		{
			_busClient = busClient;
		}

		public IPassiveSagaBuilder<TMessageContext> Recieve<TMessage>(Action<TMessage, TMessageContext> whenFunc, Action<IMandatoryStepConfigurationBuilder<TMessage, TMessageContext>> config = null)
		{
			var handler = new MessageHandlerRepository();
			var sagaRepo = _busClient.GetService<ISagaRepository>();
			var builder = new PassiveSagaBuilder<TMessageContext>(sagaRepo, handler, _busClient);
			var cfg = StepConfigurationBuilder<TMessage, TMessageContext>.GetConfiguration(config);
			builder.When(whenFunc, cfg);
			return builder;
		}

		public IPassiveSagaBuilder<TMessageContext> RecieveAsync<TMessage>(Func<TMessage, TMessageContext, Task> whenFunc, Action<IMandatoryStepConfigurationBuilder<TMessage, TMessageContext>> config = null)
		{
			var handler = new MessageHandlerRepository();
			var sagaRepo = _busClient.GetService<ISagaRepository>();
			var builder = new PassiveSagaBuilder<TMessageContext>(sagaRepo, handler, _busClient);
			var cfg = StepConfigurationBuilder<TMessage, TMessageContext>.GetConfiguration(config);
			builder.WhenAsync(whenFunc, cfg);
			return builder;
		}

		public IActiveSagaBuilder<TMessageContext> PublishAsync<TMessage>(TMessage message)
		{
			return new ActiveSagaBuilder<TMessage, TMessageContext>(_busClient, new SingleSagaRepository(), new MessageHandlerRepository(), message);
		}
	}
}