using ECommercialApi.Application.Repositories;
using ECommercialApi.Domain.Entities;
using ECommercialApi.Persistence.Contexts;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommercialApi.Persistence.Repositories
{
    internal class CustomerReadRepository : ReadRepository<Customer>, ICustomerReadRepository
    {
        public CustomerReadRepository(ECommercialApiDbContext eCommercialApiDbContext) : base(eCommercialApiDbContext)
        {
        }
    }
}
