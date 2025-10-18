using System.Linq.Expressions;
using WebAPI.Models;

namespace WebAPI.Repositories
{
    public interface ITransactionRepository
    {
        IQueryable<Transaction> GetAll();
        Transaction? GetById(int id);
        Transaction? GetByReference(string reference);
        void Add(Transaction entity);
        void Update(Transaction entity);
        void SaveChanges();
        IQueryable<Transaction> IncludeUserAndPlan();
    }
}
