using System;
using System.Threading.Tasks;
using RawRabbit.Extensions.Saga.Model;

namespace RawRabbit.Extensions.Saga.Repository.Abstractions
{
	public interface ISagaRepository
	{
		Task<ISaga> GetAsync(Guid globalRequestId);
		Task UpdateAsync(ISaga saga);
		Task<ISaga>  CreateAsync(Guid globalMessageId);
	}
}