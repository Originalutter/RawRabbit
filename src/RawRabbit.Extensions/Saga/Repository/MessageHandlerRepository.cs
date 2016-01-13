using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
			var handler = new MessageHandler<TMessage, TMessageContext>
			{
				Configuration = config,
				SyncHandler = whenAction
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
			var handler = new MessageHandler<TMessage, TMessageContext>
			{
				Configuration = config,
				AsyncHandler = whenAction
			};
			_handlerDictionary.GetOrAdd(metadata, handler);
		}

		public bool TryExecute<TMessage, TMessageContext>(TMessage message, TMessageContext context, ISaga saga)
		{
			var nextStep = new List<HandlerMetadata> { _handlerDictionary.Keys.Skip(saga.Steps.Count).FirstOrDefault() };
			var optionals = _handlerDictionary.Keys.Skip(saga.Steps.Count);
			optionals = optionals.Where(s => s.Optional);
			var potentialSteps = nextStep.Concat(optionals);
			potentialSteps = potentialSteps.Distinct();
			
			var metadata = potentialSteps.FirstOrDefault(m => m.MessageType == typeof(TMessage));
			if (metadata == null)
			{
				return false;
			}

			var handler = _handlerDictionary[metadata] as MessageHandler<TMessage, TMessageContext>;
			if (handler != null)
			{
				var handlerTask = TryExecuteAsync(handler, message, context);
				Task.WaitAll(handlerTask);
				return handlerTask.Result;
			}

			throw new Exception(); //TODO: figure out good exception.
		}

		private async Task<bool> TryExecuteAsync<TMessage, TMessageContext>(MessageHandler<TMessage, TMessageContext> handler, TMessage message, TMessageContext context)
		{
			var matchesAsync = await handler.Configuration.MatchesPredicateAsync(message, context);
			var matchesSync = handler.Configuration.MatchesPredicate(message, context);
			if (!(matchesSync && matchesAsync))
			{
				return false;
			}

			await handler.AsyncHandler.Invoke(message, context);
			handler.SyncHandler(message, context);

			var completedAsync = await handler.Configuration.UntilAsyncFunc(message, context);
			var completedSync = handler.Configuration.UntilFunc(message, context);
			if (completedSync || completedAsync)
			{
				return true;
			}

			return false;
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
		
		private class MessageHandler<TMessage, TMessageContext>
		{
			public StepConfiguration<TMessage, TMessageContext> Configuration { get; set; }
			public Action<TMessage, TMessageContext> SyncHandler { get; set; }
			public Func<TMessage, TMessageContext, Task> AsyncHandler { get; set; }

			public MessageHandler()
			{
				SyncHandler = (message, context) => { };
				AsyncHandler = (message, context) => Task.FromResult(true);
			}
		}
	}
}