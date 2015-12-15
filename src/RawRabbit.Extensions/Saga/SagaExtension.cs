using System;
using System.Threading.Tasks;
using RawRabbit.Context;
using RawRabbit.Extensions.Client;
using RawRabbit.Extensions.Saga.Builders.Abstractions;

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
				throw new InvalidOperationException("SagaExtension is only availbable for ExtendableBusClient.");
			}
		}
	}

	public class Foo
	{
		private void DoStuff()
		{
			var client = RawRabbitFactory.GetExtendableClient() as ExtendableBusClient<MessageContext>;
			client.CreateSaga(saga => saga
				.Recieve<FirstMessage>((message, context) =>
					{
						//sone matching stuff
					}, cfg => cfg.Matching((message, context) => true))
				.WhenAsync<SecondMessage>((message, context) =>
					{
						// some stuff
						return Task.FromResult(true);
					}, cfg => cfg
						.Matching((message, context) =>
							{
								return true;
							})
						.Until((message, context) =>
							{
								return true;
							})
						.IsOptional()
						)
			);
			client.CreateSaga(cfg => cfg
				.SendAsync(new FirstMessage
					{
						Prop = "Value"
					})
				.WhenAsync<SecondMessage>((message, context) =>
					{
						// do stuff..
						return Task.FromResult(true);
					})
				.Complete<ThirdMessage>()
			);
		}
	}

	public class FirstMessage {
		public string Prop { get; set; }
	}
	public class SecondMessage { }
	public class ThirdMessage { }
}
