using System;

namespace RawRabbit.Extensions.Saga.Model
{
	public class SagaStep
	{
		public Type MessageType { get; set; }
		public DateTime Completed { get; set; }
	}
}