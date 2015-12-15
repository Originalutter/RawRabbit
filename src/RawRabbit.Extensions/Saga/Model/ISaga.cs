using System;
using System.Collections.Generic;

namespace RawRabbit.Extensions.Saga.Model
{
	public interface ISaga
	{
		Guid GlobalMessageId { get; }
		IList<SagaStep> Steps { get; } 
	}
}