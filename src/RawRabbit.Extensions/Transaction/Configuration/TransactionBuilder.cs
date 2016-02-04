using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Context;
using RawRabbit.Extensions.Client;
using RawRabbit.Extensions.Transaction.Model;
using RawRabbit.Extensions.Transaction.Repository;

namespace RawRabbit.Extensions.Transaction.Configuration
{
	public class TransactionBuilder<TMessageContext> 
		: ITransactionBuilder<TMessageContext> 
		, ITransactionPublisher<TMessageContext> where TMessageContext : IMessageContext
	{
		private readonly ExtendableBusClient<TMessageContext> _client;
		private readonly ITransactionHandler _transactionHandler;
		private readonly IConfigurationEvaluator _configEvaluator;
		private readonly Task _completed = Task.FromResult(true);
		private readonly List<string> _transactionQueues;
		private Guid _globalMessageId;
		private Action _publishAction;
		private Action<ISubscriptionConfigurationBuilder> _transactionConfig;

		public TransactionBuilder(ExtendableBusClient<TMessageContext> client, ITransactionHandler transactionHandler)
		{
			_client = client;
			_configEvaluator = client.GetService<IConfigurationEvaluator>();
			_transactionQueues = new List<string>();
			_transactionHandler = transactionHandler;
		}

		public ITransactionBuilder<TMessageContext> PublishAsync<TMessage>(TMessage message = default(TMessage), Guid globalMessageId = new Guid()) where TMessage : new()
		{
			_globalMessageId = globalMessageId == Guid.Empty
				? Guid.NewGuid()
				: globalMessageId;
			_publishAction = () => _client.PublishAsync(message, _globalMessageId);
			_transactionConfig = builder => builder
				.WithSubscriberId(_globalMessageId.ToString())
				.WithQueue(q => q
					.WithAutoDelete()
					.WithExclusivity()
				);
			return this;
		}

		public ITransactionBuilder<TMessageContext> When<TMessage>(Func<TMessage, TMessageContext, Task> func, Action<IExecutionOptionBuilder> options = null)
		{
			var builder = new ExecutionOptionBuilder();
			options?.Invoke(builder);

			WireUpMessageHandler(func, builder.Option);
			return this;
		}

		public Transaction<TMessage> Complete<TMessage>(Func<TMessage, TMessageContext, Task> func = null)
		{
			if (_transactionHandler.IsRegistered<TMessage>())
			{
				throw new ArgumentException("Complete message type is allready registered.");
			}

			var msgTsc = new TaskCompletionSource<TMessage>();
			WireUpMessageHandler(func, new ExecutionOption(), (message, context) =>
			{
				msgTsc.TrySetResult(message);
				using (var channel = _client.GetService<IChannelFactory>().CreateChannel())
				{
					foreach (var queue in _transactionQueues)
					{
						channel.QueueDelete(queue);
					}
				}
				return _completed;
			});
			_publishAction();
			return new Transaction<TMessage>
			{
				Task = msgTsc.Task,
				State = _transactionHandler.GetState(_globalMessageId)
			};
		}

		protected void WireUpMessageHandler<TMessage>(Func<TMessage, TMessageContext, Task> userFunc, ExecutionOption option, Func<TMessage, TMessageContext, Task> extraFunc = null)
		{
			if (!_transactionHandler.IsRegistered<TMessage>())
			{
				_client.SubscribeAsync<TMessage>((msg, ctx) =>
				{
					if (ctx.GlobalRequestId != _globalMessageId)
					{
						return _completed;
					}

					return _transactionHandler
						.QueueForExecutionAsync(msg, ctx)
						.ContinueWith(t =>
						{
							if (t.IsFaulted)
							{
								return _completed;
							}
							return extraFunc?.Invoke(msg, ctx) ?? _completed;
						});

				}, _transactionConfig);
				_transactionQueues.Add(_configEvaluator.GetConfiguration<TMessage>(_transactionConfig).Queue.FullQueueName);
			}
			
			if (userFunc != null)
			{
				_transactionHandler.Register(userFunc, option);
			}
		}
	}
}