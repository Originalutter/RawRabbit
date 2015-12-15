using System;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Context;
using RawRabbit.Extensions.Saga.Repository;
using RawRabbit.Extensions.Saga.Repository.Abstractions;
using RawRabbit.vNext;

namespace RawRabbit.Extensions.Client
{
	public class RawRabbitFactory
	{
		public static IBusClient<MessageContext> GetExtendableClient(Action<IServiceCollection> custom = null)
		{
			var provider = new ServiceCollection()
				.AddRawRabbit(null, custom)
				.AddSingleton<ISagaRepository, OpertunisticSagaRepository>()
				.BuildServiceProvider();	
			return new ExtendableBusClient(provider);
		}
	}
}
