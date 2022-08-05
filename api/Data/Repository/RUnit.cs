using System;
using System.Collections.Generic;
using System.Text;
using api.Models;
namespace api.Data{
    public class RUnit : IRUnit
    {
        private readonly ApplicationDbContext _db;
        public RUnit(ApplicationDbContext db)
        {
            _db = db;
            Company = new RCompany(_db);
            Client = new RClient(_db);
        }
        public IRCompany Company { get; private set; }
        public IRClient Client { get; private set; }
        public void Dispose()
        {
            _db.Dispose();
        }
        public bool isSave(string data)
        {
            return string.IsNullOrEmpty(data);
        }
        public string GetNewID()
        {
            return Guid.NewGuid().ToString().ToUpper();
        }
        public void SaveChanges()
        {
            _db.SaveChanges();
        }
        public void SaveChangesAsync()
        {
            _db.SaveChangesAsync();
        }
    }
}