using System;
using System.Threading.Tasks;
using RawRabbit.Extensions.Saga.Model;
using RawRabbit.Extensions.Saga.Repository.Abstractions;

namespace RawRabbit.Extensions.Saga.Repository
{
	public class  SingleSagaRepository : ISagaRepository
	{
		private ISaga _saga;

		public Task<ISaga> GetAsync(Guid globalRequestId)
		{
			return _saga?.GlobalMessageId == globalRequestId
				? Task.FromResult(_saga)
				: Task.FromResult<ISaga>(null);
		}

		public Task UpdateAsync(ISaga saga)
		{
			if (_saga == null)
			{
				throw new InvalidOperationException("Can not update saga before created.");
			}
			if (_saga.GlobalMessageId != saga.GlobalMessageId)
			{
				throw new ArgumentException($"Expected saga with id '{_saga.GlobalMessageId}', got '{saga.GlobalMessageId}'");
			}
			return Task.FromResult(_saga);
		}

		public Task<ISaga> CreateAsync(Guid globalMessageId)
		{
			_saga = new Model.Saga
			{
				GlobalMessageId = globalMessageId
			};
			return Task.FromResult(_saga);
		}
	}
}
