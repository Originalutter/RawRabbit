using System;
using System.Threading.Tasks;
using RawRabbit.Context;
using RawRabbit.Extensions.Saga.Builders.Abstractions;
using RawRabbit.Extensions.Saga.Model;
using RawRabbit.Extensions.Saga.Repository.Abstractions;

namespace RawRabbit.Extensions.Saga.Builders
{
	public class ActiveSagaBuilder<TFirstMessage, TMessageContext> : SagaBuilderBase<TMessageContext>, IActiveSagaBuilder<TMessageContext>
		where TMessageContext : IMessageContext
	{
		private readonly TFirstMessage _firstMessage;
		private readonly Guid _globalMessageId;

		public ActiveSagaBuilder(IBusClient<TMessageContext> busClient, ISagaRepository sagaRepo, IMessageHandlerRepository handlerRepo, TFirstMessage firstMessage)
			: base(sagaRepo, handlerRepo,busClient)
		{
			_firstMessage = firstMessage;
			_globalMessageId = Guid.NewGuid();
		}

		public IActiveSagaBuilder<TMessageContext> WhenAsync<TMessage>(Func<TMessage, TMessageContext, Task> whenFunc, Action<IStepConfigurationBuilder<TMessage, TMessageContext>> config = null)
		{
			return WhenAsyncBase(whenFunc, config) as IActiveSagaBuilder<TMessageContext>;
		}

		public IActiveSagaBuilder<TMessageContext> When<TMessage>(Action<TMessage, TMessageContext> whenAction, Action<IStepConfigurationBuilder<TMessage, TMessageContext>> config = null)
		{
			return WhenBase(whenAction, config) as IActiveSagaBuilder<TMessageContext>;
		}

		public IActiveSaga<TMessage> Complete<TMessage>(Action<IStepConfigurationBuilder<TMessage, TMessageContext>> config = null)
		{
			var cfg = StepConfigurationBuilder<TMessage, TMessageContext>.GetConfiguration(config);
			if (cfg.Optional)
			{
				throw new ArgumentException("The 'Complete' Message can not be optional.");
			}
			cfg.IsCompleteMessage = true;
			var sagaTcs = new TaskCompletionSource<TMessage>();
		
			WhenAsyncBase(async (message, context) =>
			{
				if (context.GlobalRequestId != _globalMessageId)
				{
					return;
				}
				var saga = await SagaRepo.GetAsync(_globalMessageId);
				if (HandlerRepo.IsCompleted(saga))
				{
					sagaTcs.TrySetResult(message);
				}
			}, cfg);

			var sagaTask =  SagaRepo
				.CreateAsync(_globalMessageId)
				.ContinueWith(s => BusClient.PublishAsync(_firstMessage, _globalMessageId))
				.ContinueWith(t => new ActiveSaga<TMessage>
				{
					GlobalMessageId = _globalMessageId,
					SagaTask = sagaTcs.Task
				} as IActiveSaga<TMessage>);
			Task.WaitAll(sagaTask);
			return sagaTask.Result;
		}
	}
}