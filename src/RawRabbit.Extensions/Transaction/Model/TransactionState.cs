using System;
using System.Collections.Generic;

namespace RawRabbit.Extensions.Transaction.Model
{
	public class TransactionState
	{
		public List<ExecutionResult> Skipped { get; set; }
		public List<ExecutionResult> Completed { get; set; }
		public Guid GlobalMessageId { get; set; }

		public TransactionState()
		{
			Skipped = new List<ExecutionResult>();
			Completed = new List<ExecutionResult>();
		}
	}
}
