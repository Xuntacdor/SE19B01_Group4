using System;
using WebAPI.Data;
using WebAPI.Models;

namespace WebAPI.Repositories
{
    public class SpeakingFeedbackRepository : ISpeakingFeedbackRepository
    {
        private readonly ApplicationDbContext _context;
        public SpeakingFeedbackRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IEnumerable<SpeakingFeedback> GetAll()
        {
            return _context.SpeakingFeedbacks.ToList();
        }

        public SpeakingFeedback? GetById(int id)
        {
            return _context.SpeakingFeedbacks.Find(id);
        }

        public void Add(SpeakingFeedback entity)
        {
            _context.SpeakingFeedbacks.Add(entity);
        }

        public void Update(SpeakingFeedback entity)
        {
            _context.SpeakingFeedbacks.Update(entity);
        }

        public void Delete(SpeakingFeedback entity)
        {
            _context.SpeakingFeedbacks.Remove(entity);
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}
