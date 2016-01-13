using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Extensions.Client;
using RawRabbit.Extensions.Saga;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.Extensions
{
	public class SagaTests : IntegrationTestBase
	{
		public class When_Declaring_Passive_Saga : SagaTests
		{
			[Fact]
			public async Task Should_Handle_Simple_Passive_Saga()
			{
				var globalId = Guid.NewGuid();
				var message = new BasicMessage {Prop = "Value"};
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

			[Fact]
			public void Should_Not_Act_On_Messages_That_Does_Not_Match_Predicate()
			{
				var globalId = Guid.NewGuid();
				var message = new BasicMessage {Prop = "Value"};
				var publisher = BusClientFactory.CreateDefault();
				var client = RawRabbitFactory.GetExtendableClient();
				var recieveTcs = new TaskCompletionSource<BasicMessage>();
				client.CreateSaga(saga => saga
					.RecieveAsync<BasicMessage>((msg, ctx) =>
					{
						if (ctx.GlobalRequestId != globalId)
						{
							return Task.FromResult(true);
						}
						recieveTcs.TrySetResult(msg);
						return Task.FromResult(true);
					}, cfg => cfg.Matching((basicMessage, context) => basicMessage.Prop != message.Prop))
					);

				/* Test */
				var publishTask = publisher.PublishAsync(message, globalId);
				Task.WaitAll(new Task[] {publishTask, recieveTcs.Task}, TimeSpan.FromMilliseconds(200));

				/* Assert */
				Assert.False(recieveTcs.Task.IsCompleted);
			}


			[Fact]
			public async Task Should_Act_On_Message_Until_The_Until_Statement_Is_Matched()
			{
				/* Setup */
				var globalMessageId = Guid.NewGuid();
				var publisher = BusClientFactory.CreateDefault();
				var client = RawRabbitFactory.GetExtendableClient();
				var message = new BasicMessage { Prop = "Value" };
				var untilTcs = new TaskCompletionSource<bool>();
				var currentCallIndex = 0;
				const int maxCallCount = 5;
				client.CreateSaga(saga => saga
					.RecieveAsync<BasicMessage>((msg, context) =>
					{
						Interlocked.Increment(ref currentCallIndex);
						if (currentCallIndex == maxCallCount)
						{
							untilTcs.SetResult(true);
						}
						return Task.FromResult(true);
					}, cfg => cfg.Until((msg, ctx) => untilTcs.Task.IsCompleted)));

				/* Test */
				for (var i = 0; i < maxCallCount; i++)
				{
					await publisher.PublishAsync(message, globalMessageId);
				}
				await untilTcs.Task;

				/* Assert */
				Assert.Equal(currentCallIndex, maxCallCount);
			}
		}

		public class When_Declaring_Active_Saga : SagaTests
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
		}
	}
}
