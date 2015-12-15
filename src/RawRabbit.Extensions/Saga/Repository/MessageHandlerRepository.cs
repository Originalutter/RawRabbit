using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using RawRabbit.Extensions.Saga.Builders;
using RawRabbit.Extensions.Saga.Builders.Abstractions;
using RawRabbit.Extensions.Saga.Model;
using RawRabbit.Extensions.Saga.Repository.Abstractions;

namespace RawRabbit.Extensions.Saga.Repository
{
	public class MessageHandlerRepository : IMessageHandlerRepository
	{
		private readonly ConcurrentDictionary<HandlerMetadata, object> _handlerDictionary; 

		public MessageHandlerRepository()
		{
			_handlerDictionary = new ConcurrentDictionary<HandlerMetadata, object>();
		} 

		public void Add<TMessage, TMessageContext>(Action<TMessage, TMessageContext> whenAction, StepConfiguration<TMessage, TMessageContext> config)
		{
			var metadata = new HandlerMetadata
			{
				MessageType = typeof(TMessage),
				Optional = config.Optional,
			};
			var handler = new SyncMessageHandler<TMessage, TMessageContext>
			{
				Configuration = config,
				Action = whenAction
			};
			_handlerDictionary.GetOrAdd(metadata, handler);
		}

		public void Add<TMessage, TMessageContext>(Func<TMessage, TMessageContext, Task> whenAction, StepConfiguration<TMessage, TMessageContext> config)
		{
			var metadata = new HandlerMetadata
			{
				MessageType = typeof(TMessage),
				Optional = config.Optional
			};
			var handler = new AsyncMessageHandler<TMessage, TMessageContext>
			{
				Configuration = config,
				Func = whenAction
			};
			_handlerDictionary.GetOrAdd(metadata, handler);
		}

		public bool TryExecute<TMessage, TMessageContext>(TMessage message, TMessageContext context, ISaga saga)
		{
			var potentialSteps = Enumerable
				.Empty<HandlerMetadata>()
				.Concat(_handlerDictionary.Keys.Skip(saga.Steps.Count).Where(s => s.Optional))
				.Concat(new[] {_handlerDictionary.Keys.Skip(saga.Steps.Count).FirstOrDefault()})
				.Where(m => m != null)
				.Distinct();

			var metadata = potentialSteps.FirstOrDefault(m => m.MessageType == typeof (TMessage));
			if (metadata == null)
			{
				return false;
			}

			var syncHandler = _handlerDictionary[metadata] as SyncMessageHandler<TMessage, TMessageContext>;
			if (syncHandler != null)
			{
				return TryExecuteSync(syncHandler, message, context);
			}

			var asyncHandler = _handlerDictionary[metadata] as AsyncMessageHandler<TMessage, TMessageContext>;
			if (asyncHandler != null)
			{
				var handlerTask = TryExecuteAsync(asyncHandler, message, context);
				Task.WaitAll(handlerTask);
				return handlerTask.Result;
			}

			throw new Exception(); //TODO: figure out good exception.
		}

		private async Task<bool> TryExecuteAsync<TMessage, TMessageContext>(AsyncMessageHandler<TMessage, TMessageContext> asyncHandler, TMessage message, TMessageContext context)
		{
			if (! (await asyncHandler.Configuration.MatchesPredicateAsync(message, context)))
			{
				return false;
			}

			await asyncHandler.Func(message, context);

			if (!(await asyncHandler.Configuration.UntilAsyncFunc(message, context)))
			{
				return false;
			}

			return true;
		}

		private static bool TryExecuteSync<TMessage, TMessageContext>(SyncMessageHandler<TMessage, TMessageContext> syncHandler, TMessage message, TMessageContext context)
		{
			if (!syncHandler.Configuration.MatchesPredicate(message, context))
			{
				return false;
			}

			syncHandler.Action(message, context);

			if (!syncHandler.Configuration.UntilFunc(message, context))
			{
				return false;
			}

			return true;
		}

		public bool IsRegistered(Type type)
		{
			return _handlerDictionary.Keys.Select(k => k.MessageType).Contains(type);
		}

		public bool IsCompleted(ISaga saga)
		{
			if (saga == null)
			{
				return false;
			}
			var mandatorySteps = _handlerDictionary.Keys
				.Where(k => !k.Optional)
				.ToList();

			//TODO: trim this algo, plz
			return mandatorySteps.Count <= saga.Steps.Count;
		}

		private class HandlerMetadata
		{
			public Type MessageType { get; set; }
			public bool Optional { get; set; }
		}

		private class SyncMessageHandler<TMessage, TMessageContext>
		{
			public StepConfiguration<TMessage, TMessageContext> Configuration { get; set; }
			public Action<TMessage, TMessageContext> Action { get; set; }
		}

		private class AsyncMessageHandler<TMessage, TMessageContext>
		{
			public StepConfiguration<TMessage, TMessageContext> Configuration { get; set; }
			public Func<TMessage, TMessageContext, Task> Func { get; set; }
		}
	}
}