using System;
using WebAPI.Data;
using WebAPI.Models;

namespace WebAPI.Repositories
{
    public class SpeakingRepository : ISpeakingRepository
    {
        private readonly ApplicationDbContext _context;
        public SpeakingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Speaking? GetById(int id)
        {
            return _context.Speakings.Find(id);
        }

        public IEnumerable<Speaking> GetByExamId(int examId)
        {
            return _context.Speakings
                .Where(s => s.ExamId == examId)
                .OrderBy(s => s.DisplayOrder)
                .ToList();
        }

        public void Add(Speaking entity)
        {
            _context.Speakings.Add(entity);
        }

        public void Update(Speaking entity)
        {
            _context.Speakings.Update(entity);
        }

        public void Delete(Speaking entity)
        {
            _context.Speakings.Remove(entity);
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}
