using System;
using System.Threading.Tasks;
using RawRabbit.Context;
using RawRabbit.Extensions.Saga.Builders.Abstractions;
using RawRabbit.Extensions.Saga.Model;
using RawRabbit.Extensions.Saga.Repository;
using RawRabbit.Extensions.Saga.Repository.Abstractions;

namespace RawRabbit.Extensions.Saga.Builders
{
	public abstract class SagaBuilderBase<TMessageContext> where TMessageContext : IMessageContext
	{
		protected readonly ISagaRepository SagaRepo;
		protected readonly IMessageHandlerRepository HandlerRepo;
		protected readonly IBusClient<TMessageContext> BusClient;
		private readonly Task _completedTask = Task.FromResult(true);

		protected SagaBuilderBase(ISagaRepository sagaRepo, IMessageHandlerRepository handlerRepo, IBusClient<TMessageContext> busClient)
		{
			SagaRepo = sagaRepo;
			HandlerRepo = handlerRepo;
			BusClient = busClient;
		}

		protected SagaBuilderBase<TMessageContext> WhenBase<TMessage>(Action<TMessage, TMessageContext> whenAction, StepConfiguration<TMessage, TMessageContext> config)
		{
			WireUpSubscriber<TMessage>();
			HandlerRepo.Add(whenAction, config);
			return this;
		}

		public SagaBuilderBase<TMessageContext> WhenBase<TMessage>(Action<TMessage, TMessageContext> whenAction, Action<IStepConfigurationBuilder<TMessage, TMessageContext>> config = null)
		{
			var cfg = StepConfigurationBuilder<TMessage, TMessageContext>.GetConfiguration(config);
			return WhenBase(whenAction, cfg);
		}

		public SagaBuilderBase<TMessageContext> WhenAsyncBase<TMessage>(Func<TMessage, TMessageContext, Task> whenFunc, Action<IStepConfigurationBuilder<TMessage, TMessageContext>> config = null)
		{
			var cfg = StepConfigurationBuilder<TMessage, TMessageContext>.GetConfiguration(config);
			return WhenAsyncBase(whenFunc, cfg);
		}

		protected SagaBuilderBase<TMessageContext> WhenAsyncBase<TMessage>(Func<TMessage, TMessageContext, Task> whenFunc, StepConfiguration<TMessage, TMessageContext> config)
		{
			WireUpSubscriber<TMessage>();
			HandlerRepo.Add(whenFunc, config);
			return this;
		}

		protected void WireUpSubscriber<TMessage>()
		{
			if (HandlerRepo.IsRegistered(typeof(TMessage)))
			{
				return;
			}
			BusClient.SubscribeAsync<TMessage>((message, context) =>
			{
				return SagaRepo
					.GetAsync(context.GlobalRequestId)
					.ContinueWith(sagaTask =>
					{
						if (sagaTask.Result == null)
						{
							return _completedTask;
						}
						if (HandlerRepo.TryExecute(message, context, sagaTask.Result))
						{
							var completed = new SagaStep
							{
								MessageType = typeof(TMessage),
								Completed = DateTime.Now
							};
							sagaTask.Result.Steps.Add(completed);
							return SagaRepo.UpdateAsync(sagaTask.Result);
						}
						return _completedTask;
					});
			}, builder => builder.WithSubscriberId("saga"));
		}
	}
}
