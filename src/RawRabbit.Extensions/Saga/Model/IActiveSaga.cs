using System.Threading.Tasks;

namespace RawRabbit.Extensions.Saga.Model
{
	public interface IActiveSaga<TMessage> : ISaga
	{
		Task<TMessage> SagaTask { get; }
	}
}