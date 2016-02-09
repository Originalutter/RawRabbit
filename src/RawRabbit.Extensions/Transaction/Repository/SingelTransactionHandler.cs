using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RawRabbit.Context;
using RawRabbit.Extensions.Transaction.Model;

namespace RawRabbit.Extensions.Transaction.Repository
{
	public class SingelTransactionHandler : ITransactionHandler
	{
		private readonly List<ExecutionHandlerContainer> _handlers;
		private readonly Task _completed = Task.FromResult(true);
		private readonly TransactionState _state;
		private readonly Queue<Func<Task<ExecutionFlow>>> _executionQueue;
		private TaskCompletionSource<ExecutionFlow> _executingTcs;
		private bool _isConsuming;
		private static readonly object Padlock = new object();
		 
		public SingelTransactionHandler()
		{
			_state = new TransactionState();
			_handlers = new List<ExecutionHandlerContainer>();
			_executionQueue = new Queue<Func<Task<ExecutionFlow>>>();
		}
		public bool IsRegistered<TMessage>()
		{
			return _handlers.Any(h => h.MessageType == typeof(TMessage));
		}

		public TransactionState GetState(Guid globalMessageId)
		{
			return _state;
		}

		public void Register<TMessage, TMessageContext>(Func<TMessage, TMessageContext, Task> func, ExecutionOption option) where TMessageContext : IMessageContext
		{
			_handlers.Add(new ExecutionHandlerContainer 
			{
				Optional = option.Optional,
				AbortsExecution = option.AbortsExecution,
				MessageHandler = func,
				MessageType = typeof(TMessage)
			});
		}

		public Task<ExecutionFlow> QueueForExecutionAsync<TMessage, TMessageContext>(TMessage message, TMessageContext context) where TMessageContext : IMessageContext
		{
			Func<Task<ExecutionFlow>> exeuctionFunc = () =>
			{
				var potentialHandlers = GetPotentalHandlers();
				var handler = potentialHandlers.FirstOrDefault(h => h.MessageType == typeof(TMessage));

				if (handler == null)
				{
					throw new ArgumentException($"No handler for message {typeof(TMessage).Name}.");
				}
				_state.Completed.Add(new ExecutionResult
				{
					Time = DateTime.Now,
					MessageType = handler.MessageType
				});
				foreach (var skipped in potentialHandlers.TakeWhile(h => h != handler).Except(new[] { handler }))
				{
					_state.Skipped.Add(new ExecutionResult
					{
						Time = DateTime.Now,
						MessageType = skipped.MessageType
					});
				}
				var handlerFunc = handler.MessageHandler as Func<TMessage, TMessageContext, Task>;
				return handlerFunc?
					.Invoke(message, context)
					.ContinueWith(t => handler.AbortsExecution ? ExecutionFlow.Abort : ExecutionFlow.Continue)
					?? Task.FromResult(ExecutionFlow.Unknown);
			};
			_executionQueue.Enqueue(exeuctionFunc);
			
			return EnsureQueueExecution();
		}

		private Task<ExecutionFlow> EnsureQueueExecution()
		{
			if (_isConsuming)
			{
				return _executingTcs.Task;
			}
			lock (Padlock)
			{
				_isConsuming = true;
				_executingTcs = new TaskCompletionSource<ExecutionFlow>();
			}
			while (_executionQueue.Any())
			{
				var executionTask = _executionQueue.Dequeue().Invoke();
				executionTask.Wait();
				if (executionTask.Result == ExecutionFlow.Abort)
				{
					_executionQueue.Clear();
					return Task.FromResult(ExecutionFlow.Abort);
				}
			}
			lock (Padlock)
			{
				_isConsuming = false;
				_executingTcs.TrySetResult(ExecutionFlow.Continue);
			}
			return _executingTcs.Task;
		}

		private List<ExecutionHandlerContainer> GetPotentalHandlers()
		{
			List<ExecutionHandlerContainer> potentialHandlers;
			var next = _handlers
				.Skip(_state.Skipped.Count)
				.Skip(_state.Completed.Count)
				.FirstOrDefault();

			if (next == null)
			{
				throw new InvalidOperationException($"No message handler for saga.");
			}
			if (!next.Optional)
			{
				potentialHandlers = new List<ExecutionHandlerContainer> { next };
			}
			else
			{
				var numberOfOptionals = _handlers
					.Skip(_state.Skipped.Count)
					.Skip(_state.Completed.Count)
					.TakeWhile(s => s.Optional)
					.Count();
				potentialHandlers = _handlers
					.Skip(_state.Skipped.Count)
					.Skip(_state.Completed.Count)
					.Take(numberOfOptionals + 1)
					.ToList();
			}
			return potentialHandlers;
		}
	}
	public  enum ExecutionFlow
	{
		Unknown, Continue, Abort
	}
}