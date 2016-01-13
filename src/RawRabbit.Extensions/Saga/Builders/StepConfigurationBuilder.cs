using System;
using System.Threading.Tasks;
using RawRabbit.Extensions.Saga.Builders.Abstractions;

namespace RawRabbit.Extensions.Saga.Builders
{
	public class StepConfigurationBuilder<TMessage, TMessageContext> : IStepConfigurationBuilder<TMessage, TMessageContext>, IMandatoryStepConfigurationBuilder<TMessage, TMessageContext>
	{
		public StepConfiguration<TMessage, TMessageContext> Configuration { get; set; }

		public StepConfigurationBuilder()
		{
			Configuration =	StepConfiguration<TMessage, TMessageContext>.Default;
		}

		public static StepConfiguration<TMessage, TMessageContext> GetConfiguration(
			Action<IStepConfigurationBuilder<TMessage, TMessageContext>> action)
		{
			if (action == null)
			{
				return StepConfiguration<TMessage, TMessageContext>.Default;
			}
			var builder = new StepConfigurationBuilder<TMessage, TMessageContext>();
			action(builder);
			return builder.Configuration;
		}

		public static StepConfiguration<TMessage, TMessageContext> GetConfiguration(
			Action<IMandatoryStepConfigurationBuilder<TMessage, TMessageContext>> action)
		{
			if (action == null)
			{
				return StepConfiguration<TMessage, TMessageContext>.Default;
			}
			var builder = new StepConfigurationBuilder<TMessage, TMessageContext>();
			action(builder);
			return builder.Configuration;
		}

		public IStepConfigurationBuilder<TMessage, TMessageContext> Matching(Func<TMessage, TMessageContext, bool> predicate)
		{
			Configuration.MatchesPredicate = predicate;
			return this;
		}

		IMandatoryStepConfigurationBuilder<TMessage, TMessageContext> IMandatoryStepConfigurationBuilder<TMessage, TMessageContext>.MatchingAsync(Func<TMessage, TMessageContext, Task<bool>> predicate)
		{
			return MatchingAsync(predicate) as IMandatoryStepConfigurationBuilder<TMessage, TMessageContext>;
		}

		IMandatoryStepConfigurationBuilder<TMessage, TMessageContext> IMandatoryStepConfigurationBuilder<TMessage, TMessageContext>.Matching(Func<TMessage, TMessageContext, bool> predicate)
		{
			return Matching(predicate) as IMandatoryStepConfigurationBuilder<TMessage, TMessageContext>;
		}

		public IStepConfigurationBuilder<TMessage, TMessageContext> MatchingAsync(Func<TMessage, TMessageContext, Task<bool>> predicate)
		{
			Configuration.MatchesPredicateAsync = predicate;
			return this;
		}

		public IStepConfigurationBuilder<TMessage, TMessageContext> Until(Func<TMessage, TMessageContext, bool> predicate)
		{
			Configuration.UntilFunc = predicate;
			return this;
		}

		public IStepConfigurationBuilder<TMessage, TMessageContext> UntilAsync(Func<TMessage, TMessageContext, Task<bool>> predicate)
		{
			Configuration.UntilAsyncFunc = predicate;
			return this;
		}

		public IStepConfigurationBuilder<TMessage, TMessageContext> IsOptional(bool optional = true)
		{
			Configuration.Optional = optional;
			return this;
		}
	}
}