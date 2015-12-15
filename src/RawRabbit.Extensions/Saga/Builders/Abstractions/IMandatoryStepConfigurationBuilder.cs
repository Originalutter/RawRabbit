using System;
using System.Threading.Tasks;

namespace RawRabbit.Extensions.Saga.Builders.Abstractions
{
	public interface IMandatoryStepConfigurationBuilder<TMessage, TMessageContext>
	{
		IMandatoryStepConfigurationBuilder<TMessage, TMessageContext> Matching(Func<TMessage, TMessageContext, bool> predicate);
		IMandatoryStepConfigurationBuilder<TMessage, TMessageContext> MatchingAsync(Func<TMessage, TMessageContext, Task<bool>> predicate);
	}
}