using System;
using System.Threading.Tasks;
using RawRabbit.Extensions.Client;
using RawRabbit.Extensions.Transaction;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.Extensions
{
	public class TransactionTests
	{
		[Fact]
		public async Task Should_Perform_Simple_Transaction()
		{
			/* Setup */
			var normal = BusClientFactory.CreateDefault();
			var extended = RawRabbitFactory.GetExtendableClient() as ExtendableBusClient;
			var secondCalled = false;
			var thirdCalled = false;

			normal.SubscribeAsync<FirstMessage>((msg, ctx) => normal.PublishAsync(new SecondMessage(), ctx.GlobalRequestId));
			normal.SubscribeAsync<SecondMessage>((msg, ctx) => normal.PublishAsync(new ThirdMessage(), ctx.GlobalRequestId));

			/* Test */
			var transaction = extended.PerformTransaction(cfg => cfg
				.PublishAsync<FirstMessage>()
				.When<SecondMessage>((msg, ctx) =>
				{
					secondCalled = true;
					return Task.FromResult(true);
				})
				.Complete<ThirdMessage>((msg, ctx) =>
				{
					thirdCalled = true;
					return Task.FromResult(true);
				})
			);
			await transaction.Task;

			/* Assert */
			Assert.True(secondCalled);
			Assert.True(thirdCalled);
		}

		[Fact]
		public async Task Should_Skip_Optional_Step_If_Following_Step_Is_Invoked()
		{
			/* Setup */
			var normal = BusClientFactory.CreateDefault();
			var extended = RawRabbitFactory.GetExtendableClient() as ExtendableBusClient;
			var optional = false;
			var thirdCalled = false;

			normal.SubscribeAsync<FirstMessage>((msg, ctx) => normal.PublishAsync(new ForthMessage(), ctx.GlobalRequestId));

			/* Test */
			var transaction = extended.PerformTransaction(cfg => cfg
				.PublishAsync<FirstMessage>()
				.When<SecondMessage>((msg, ctx) =>
				{
					optional = true;
					return Task.FromResult(true);
				}, it => it.IsOptional())
				.When<ThirdMessage>((msg, ctx) =>
				{
					optional = true;
					return Task.FromResult(true);
				}, it => it.IsOptional())
				.Complete<ForthMessage>((msg, ctx) =>
				{
					thirdCalled = true;
					return Task.FromResult(true);
				})
			);
			await transaction.Task;

			/* Assert */
			Assert.False(optional);
			Assert.True(thirdCalled);
		}

		[Fact]
		public void Should_Not_Invoke_Handler_If_Earlier_Mandatory_Handler_Not_Called()
		{
			/* Setup */
			var normal = BusClientFactory.CreateDefault();
			var extended = RawRabbitFactory.GetExtendableClient() as ExtendableBusClient;
			var thirdCalled = false;

			normal.SubscribeAsync<FirstMessage>((msg, ctx) => normal.PublishAsync(new ThirdMessage(), ctx.GlobalRequestId));

			/* Test */
			var transaction = extended.PerformTransaction(cfg => cfg
				.PublishAsync<FirstMessage>()
				.When<SecondMessage>((msg, ctx) =>
				{
					return Task.FromResult(true);
				})
				.Complete<ThirdMessage>((msg, ctx) =>
				{
					thirdCalled = true;
					return Task.FromResult(true);
				})
			);
			transaction.Task.Wait(TimeSpan.FromMilliseconds(300));
			
			/* Assert */
			Assert.False(thirdCalled);
		}
	}
}
