using ECommercialApi.Domain.Entities.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommercialApi.Application.Repositories
{
    public interface IRepository <T> where T : BaseEntity
    {
        DbSet<T> Table { get; } 
    }
}