using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models;

namespace api.Data
{
    public class RClient:Repository<Client>,IRClient
    {
        public RClient(ApplicationDbContext db): base(db) 
        { 
            
        }
        public async Task<IEnumerable<Client>> GetAllAsync() 
        { 
            return await Task.Run(()=>GetAll());
        }
    }
}