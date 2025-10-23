using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.Models;

namespace WebAPI.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly ApplicationDbContext _context;

        public TransactionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<Transaction> GetAll()
        {
            return _context.Transactions.AsQueryable();
        }

        public IQueryable<Transaction> IncludeUserAndPlan()
        {
            return _context.Transactions
                           .Include(t => t.User)
                           .Include(t => t.Plan);
        }

        public Transaction? GetById(int id)
        {
            return _context.Transactions
                           .Include(t => t.User)
                           .Include(t => t.Plan)
                           .FirstOrDefault(t => t.TransactionId == id);
        }

        public Transaction? GetByReference(string reference)
        {
            return _context.Transactions
                           .FirstOrDefault(t => t.ProviderTxnId == reference);
        }

        public void Add(Transaction entity)
        {
            _context.Transactions.Add(entity);
        }

        public void Update(Transaction entity)
        {
            _context.Transactions.Update(entity);
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}
