using System;
using RawRabbit.Context;
using RawRabbit.Extensions.Client;
using RawRabbit.Extensions.Transaction.Configuration;
using RawRabbit.Extensions.Transaction.Model;
using RawRabbit.Extensions.Transaction.Repository;

namespace RawRabbit.Extensions.Transaction
{
	public static class TransactionExtension
	{
		public static Transaction<TResult> PerformTransaction<TMessageContext, TResult>(
				this IBusClient<TMessageContext> client,
				Func<ITransactionPublisher<TMessageContext>, Transaction<TResult>> builderFunc) where TMessageContext : IMessageContext
		{
			var extended = (client as ExtendableBusClient<TMessageContext>);
			if (extended == null)
			{
				throw new InvalidOperationException("Extention methods only available for ExtendableBusClients.");
			}

			var builder = new TransactionBuilder<TMessageContext>(extended, new SingelTransactionHandler());
			return builderFunc(builder);
		}
	}
}
