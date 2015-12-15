using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RawRabbit.Extensions.Saga.Model;
using RawRabbit.Extensions.Saga.Repository.Abstractions;

namespace RawRabbit.Extensions.Saga.Repository
{
	public class OpertunisticSagaRepository : ISagaRepository
	{
		private readonly List<ISaga> _sagas;
		private readonly Task _completed = Task.FromResult(true);

		public OpertunisticSagaRepository()
		{
			_sagas = new List<ISaga>();
		}

		public async Task<ISaga> GetAsync(Guid globalRequestId)
		{
			return _sagas.FirstOrDefault(s => s.GlobalMessageId == globalRequestId)
				?? await CreateAsync(globalRequestId);
		}

		public Task UpdateAsync(ISaga saga)
		{
			return _completed;
		}

		public Task<ISaga> CreateAsync(Guid globalMessageId)
		{
			var saga = new Model.Saga
			{
				GlobalMessageId = globalMessageId
			};
			_sagas.Add(saga);
			return Task.FromResult<ISaga>(saga);
		}
	}
}
