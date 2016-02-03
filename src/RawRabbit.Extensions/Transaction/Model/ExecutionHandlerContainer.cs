using System;

namespace RawRabbit.Extensions.Transaction.Model
{
	public class ExecutionHandlerContainer
	{
		public Type MessageType { get; set; }
		public bool Optional { get; set; }
		public object MessageHandler { get; set; }
	}
}
