using System;
using System.Threading.Tasks;
using RawRabbit.Context;
using RawRabbit.Extensions.Client;
using RawRabbit.Extensions.Saga.Builders;
using RawRabbit.Extensions.Saga.Builders.Abstractions;
using RawRabbit.Extensions.Saga.Model;

namespace RawRabbit.Extensions.Saga
{
	public static class SagaExtension
	{
		public static void CreateSaga<TMessageContext>(this IBusClient<TMessageContext> busClient, Action<ISagaInitializer<TMessageContext>> initializer)
			where TMessageContext : IMessageContext
		{
			var extended = (busClient as ExtendableBusClient<TMessageContext>);
			if (extended == null)
			{
				throw new InvalidOperationException("Sagas are only availbable for ExtendableBusClient.");
			}

			var sagaInitializer = new SagaInitializer<TMessageContext>(extended);
			initializer(sagaInitializer);
		}

		public static IActiveSaga<TLastMessage> CreateSaga<TMessageContext, TLastMessage>(this IBusClient<TMessageContext> busClient, Func<ISagaInitializer<TMessageContext>, IActiveSaga<TLastMessage>> initializer)
			where TMessageContext : IMessageContext
		{
			var extended = (busClient as ExtendableBusClient<TMessageContext>);
			if (extended == null)
			{
				throw new InvalidOperationException("Sagas are only availbable for ExtendableBusClient.");
			}

			var sagaInitializer = new SagaInitializer<TMessageContext>(extended);
			var saga = initializer(sagaInitializer);
			return saga;
		}
	}
}
