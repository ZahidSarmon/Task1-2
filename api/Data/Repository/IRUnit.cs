using System;
using System.Collections.Generic;
using System.Text;
namespace api.Data{
    public interface IRUnit : IDisposable
    {
        
         IRCompany Company { get; }
         IRClient Client { get; }
        void SaveChanges();
        void SaveChangesAsync();
        bool isSave(string data);
        string GetNewID();
    }
}