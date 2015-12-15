using System;
using System.Threading.Tasks;

namespace RawRabbit.Extensions.Saga.Builders.Abstractions
{
	public interface IStepConfigurationBuilder<TMessage, TMessageContext>
	{
		IStepConfigurationBuilder<TMessage, TMessageContext> Matching(Func<TMessage, TMessageContext, bool> predicate);
		IStepConfigurationBuilder<TMessage, TMessageContext> MatchingAsync(Func<TMessage, TMessageContext, Task<bool>> predicate);
		IStepConfigurationBuilder<TMessage, TMessageContext> Until(Func<TMessage, TMessageContext, bool> predicate);
		IStepConfigurationBuilder<TMessage, TMessageContext> UntilAsync(Func<TMessage, TMessageContext, Task<bool>> predicate);
		IStepConfigurationBuilder<TMessage, TMessageContext> IsOptional(bool optional = true);
	}
}