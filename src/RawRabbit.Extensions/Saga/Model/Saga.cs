using System;
using System.Collections.Generic;

namespace RawRabbit.Extensions.Saga.Model
{
	public class Saga : ISaga
	{
		public Guid GlobalMessageId { get; set; }
		public IList<SagaStep> Steps { get; set; }

		public Saga()
		{
			Steps = new List<SagaStep>();
		}
	}
}