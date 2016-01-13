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

			[Fact]
			public async Task Should_Throw_Exception_If_The_Complete_Message_Is_Marked_As_Optional()
			{
				var client = RawRabbitFactory.GetExtendableClient();
				Assert.Throws<ArgumentException>(() => client.CreateSaga(saga => saga
					.PublishAsync(new BasicMessage())
					.Complete<SimpleMessage>(cfg => cfg.IsOptional())));
			}

			[Fact]
			public async Task Should_Call_Next_Mandatory_Handler()
			{
				/* Setup */
				var subscribe = BusClientFactory.CreateDefault();
				var client = RawRabbitFactory.GetExtendableClient();
				var secondTcs = new TaskCompletionSource<bool>();
				subscribe.SubscribeAsync<FirstMessage>(async (message, context) =>
				{
					await subscribe.PublishAsync(new SecondMessage(), context.GlobalRequestId);
				});

				client.CreateSaga(saga => saga
					.PublishAsync(new FirstMessage())
					.WhenAsync<SecondMessage>((message, context) =>
					{
						secondTcs.SetResult(true);
						return Task.FromResult(true);
					})
					.WhenAsync<ThirdMessage>((message, context) => Task.FromResult(true))
					.Complete<ForthMessage>()
				);

				/* Test */
				await secondTcs.Task;

				/* Assert */
				Assert.True(secondTcs.Task.Result);
			}

			[Fact]
			public async Task Should_Not_Call_Handler_Until_All_Mandatory_Handlers_Have_Been_Called()
			{
				/* Setup */
				var subscribe = BusClientFactory.CreateDefault();
				var client = RawRabbitFactory.GetExtendableClient();
				var thirdTcs = new TaskCompletionSource<bool>();
				subscribe.SubscribeAsync<FirstMessage>(async (message, context) =>
				{
					await subscribe.PublishAsync(new ThirdMessage(), context.GlobalRequestId);
				});

				client.CreateSaga(saga => saga
					.PublishAsync(new FirstMessage())
					.WhenAsync<SecondMessage>((message, context) => Task.FromResult(true))
					.WhenAsync<ThirdMessage>((message, context) =>
					{
						thirdTcs.SetResult(true);
						return Task.FromResult(true);
					})
					.Complete<ForthMessage>()
				);

				/* Test */
				thirdTcs.Task.Wait(TimeSpan.FromMilliseconds(100));

				/* Assert */
				Assert.False(thirdTcs.Task.IsCompleted);
			}

			[Fact]
			public async Task Should_Call_Handler_If_Previous_Uncalled_Handler_Is_Optional()
			{
				/* Setup */
				var subscribe = BusClientFactory.CreateDefault();
				var client = RawRabbitFactory.GetExtendableClient();
				var thirdTcs = new TaskCompletionSource<bool>();
				subscribe.SubscribeAsync<FirstMessage>(async (message, context) =>
				{
					await subscribe.PublishAsync(new ThirdMessage(), context.GlobalRequestId);
				});

				client.CreateSaga(saga => saga
					.PublishAsync(new FirstMessage())
					.WhenAsync<SecondMessage>((message, context) => Task.FromResult(true), cfg => cfg.IsOptional())
					.WhenAsync<ThirdMessage>((message, context) =>
					{
						thirdTcs.SetResult(true);
						return Task.FromResult(true);
					})
					.Complete<ForthMessage>()
				);

				/* Test */
				await thirdTcs.Task;

				/* Assert */
				Assert.True(thirdTcs.Task.IsCompleted);
			}

			[Fact]
			public async Task Should_Not_Call_Handler_If_Previous_Until_Predicate_Is_Not_Met()
			{
				/* Setup */
				var subscribe = BusClientFactory.CreateDefault();
				var client = RawRabbitFactory.GetExtendableClient();
				var thirdTcs = new TaskCompletionSource<bool>();
				subscribe.SubscribeAsync<FirstMessage>(async (message, context) =>
				{
					await subscribe.PublishAsync(new SecondMessage(), context.GlobalRequestId);
				});
				subscribe.SubscribeAsync<SecondMessage>(async (message, context) =>
				{
					await subscribe.PublishAsync(new ThirdMessage(), context.GlobalRequestId);
				});

				client.CreateSaga(saga => saga
					.PublishAsync(new FirstMessage())
					.WhenAsync<SecondMessage>((message, context) => Task.FromResult(true), cfg => cfg.Until((message, context) => false))
					.WhenAsync<ThirdMessage>((message, context) =>
					{
						thirdTcs.SetResult(true);
						return Task.FromResult(true);
					})
					.Complete<ForthMessage>()
				);

				/* Test */
				thirdTcs.Task.Wait(TimeSpan.FromMilliseconds(100));

				/* Assert */
				Assert.False(thirdTcs.Task.IsCompleted);
			}
		}
	}
}
