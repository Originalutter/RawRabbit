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
	}
}
