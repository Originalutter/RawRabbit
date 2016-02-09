using System.Threading.Tasks;

namespace RawRabbit.Extensions.Transaction.Model
{
	public class Transaction<TCompleteType>
	{
		public Task<TCompleteType> Task { get; set; }
		public TransactionState State { get; set; }
		public bool Aborted { get; set; }
	}
}
