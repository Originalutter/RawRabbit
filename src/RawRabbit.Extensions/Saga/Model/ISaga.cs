using System.Threading.Tasks;

namespace RawRabbit.Extensions.Saga.Model
{
	public interface ISaga<TMessage>
	{
		Task<TMessage> SagaTask { get; }
	}
}