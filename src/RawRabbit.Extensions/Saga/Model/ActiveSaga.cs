using System.Threading.Tasks;

namespace RawRabbit.Extensions.Saga.Model
{
	public class ActiveSaga <TMessage>: Saga, IActiveSaga<TMessage>
	{
		public Task<TMessage> SagaTask { get; set; }
	}
}
