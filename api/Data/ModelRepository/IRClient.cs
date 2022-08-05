using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models;

namespace api.Data
{
    public interface IRClient:IRepository<Client>{
        Task<IEnumerable<Client>> GetAllAsync();
    }
}