using System;
using System.Threading.Tasks;

namespace RawRabbit.Extensions.Saga.Builders.Abstractions
{
	public interface IStepConfigurationBuilder<out TMessage, out TMessageContext>
	{
		IStepConfigurationBuilder<TMessage, TMessageContext> Matching(Func<TMessage, TMessageContext, bool> predicate);
		IStepConfigurationBuilder<TMessage, TMessageContext> MatchingAsync(Func<TMessage, TMessageContext, Task<bool>> predicate);
		IStepConfigurationBuilder<TMessage, TMessageContext> Until(Func<TMessage, TMessageContext, bool> predicate);
		IStepConfigurationBuilder<TMessage, TMessageContext> UntilAsync(Func<TMessage, TMessageContext, Task<bool>> predicate);
		IStepConfigurationBuilder<TMessage, TMessageContext> IsOptional(bool optional = true);
	}

	public class StepConfiguration<TMessage, TMessageContext>
	{
		public Func<TMessage, TMessageContext, bool> MatchesPredicate { get; set; }
		public Func<TMessage, TMessageContext, Task<bool>> MatchesPredicateAsync { get; set; }
		public Func<TMessage, TMessageContext, bool> UntilFunc { get; set; }
		public Func<TMessage, TMessageContext, Task<bool>> UntilAsyncFunc { get; set; }
		public bool Optional { get; set; }
		public bool IsCompleteMessage { get; set; }

		public static StepConfiguration<TMessage, TMessageContext> Default => new StepConfiguration<TMessage, TMessageContext>
		{
			Optional = false,
			IsCompleteMessage = false,
			MatchesPredicateAsync = (message, context) => Task.FromResult(true),
			MatchesPredicate = (message, context) => true,
			UntilAsyncFunc = (message, context) => Task.FromResult(false),
			UntilFunc = (message, context) => false
		};
	}
}