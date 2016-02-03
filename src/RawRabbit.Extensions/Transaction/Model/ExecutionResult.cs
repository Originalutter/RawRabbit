using System;

namespace RawRabbit.Extensions.Transaction.Model
{
	public class ExecutionResult
	{
		public DateTime Time { get; set; }
		public Type MessageType { get; set; }
	}
}