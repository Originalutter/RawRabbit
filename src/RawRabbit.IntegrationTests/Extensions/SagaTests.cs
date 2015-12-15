using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using RawRabbit.Extensions.Client;
using RawRabbit.Extensions.Saga;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.Extensions
{
	public class SagaTests
	{
		[Fact]
		public async Task Should_Handle_Simple_Active_Saga()
		{
			/* Setup */
			var simpleMessage = new SimpleMessage
			{
				IsSimple = true
			};
			var subscribe = BusClientFactory.CreateDefault();
			var client = RawRabbitFactory.GetExtendableClient();
			subscribe.SubscribeAsync<BasicMessage>((message, context) =>
			{
				return subscribe.PublishAsync(simpleMessage, context.GlobalRequestId);
			});


			/* Test */
			var saga = client.CreateSaga(cfg => cfg
				.PublishAsync(new BasicMessage())
				.Complete<SimpleMessage>()
			);
			await saga.SagaTask;

			/* Assert */
			Assert.Equal(saga.SagaTask.Result.IsSimple, simpleMessage.IsSimple);
		}

		[Fact]
		public async Task Should_Handle_Simple_Passive_Saga()
		{
			var globalId = Guid.NewGuid();
			var message = new BasicMessage { Prop = "Value" };
			var publisher = BusClientFactory.CreateDefault();
			var client = RawRabbitFactory.GetExtendableClient();
			var recieveTcs = new TaskCompletionSource<BasicMessage>();
			client.CreateSaga(cfg => cfg
				.RecieveAsync<BasicMessage>((msg, ctx) =>
					{
						if (ctx.GlobalRequestId != globalId)
						{
							return Task.FromResult(true);
						}
						recieveTcs.TrySetResult(msg);
						return Task.FromResult(true);
					})
				);

			/* Test */
			await publisher.PublishAsync(message, globalId);
			await recieveTcs.Task;

			/* Assert */
			Assert.Equal(recieveTcs.Task.Result.Prop, message.Prop);
		}
	}
}
