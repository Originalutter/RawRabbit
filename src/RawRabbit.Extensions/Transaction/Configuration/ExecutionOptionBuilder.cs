using RawRabbit.Extensions.Transaction.Model;

namespace RawRabbit.Extensions.Transaction.Configuration
{
	public class ExecutionOptionBuilder : IExecutionOptionBuilder
	{
		public ExecutionOption Option { get; set; }

		public ExecutionOptionBuilder()
		{
			Option = new ExecutionOption();
		}

		public IExecutionOptionBuilder AbortsExecution(bool aborts = true)
		{
			Option.AbortsExecution = aborts;
			return this;
		}

		public IExecutionOptionBuilder IsOptional(bool optional = true)
		{
			Option.Optional = optional;
			return this;
		}
	}
}