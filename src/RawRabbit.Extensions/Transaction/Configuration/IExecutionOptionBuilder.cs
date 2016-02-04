namespace RawRabbit.Extensions.Transaction.Configuration
{
	public interface IExecutionOptionBuilder
	{
		IExecutionOptionBuilder AbortsExecution(bool aborts = true);
		IExecutionOptionBuilder IsOptional(bool optional = true);
	}
}
