using ECommercialApi.Application.Repositories;
using ECommercialApi.Domain.Entities;
using ECommercialApi.Persistence.Contexts;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommercialApi.Persistence.Repositories
{
    internal class OrderWriteRepository : WriteRepository<Order>, IOrderWriteRepository
    {
        public OrderWriteRepository(ECommercialApiDbContext eCommercialApiDbContext) : base(eCommercialApiDbContext)
        {
        }
    }
}
