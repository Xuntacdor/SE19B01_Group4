using WebAPI.Data;
using WebAPI.Models;
using System.Collections.Generic;
using System.Linq;

namespace WebAPI.Repositories
{
    public class ListeningRepository : IListeningRepository
    {
        private readonly ApplicationDbContext _db;

        public ListeningRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public Listening? GetById(int id) => _db.Listening.FirstOrDefault(r => r.ListeningId == id);

        public List<Listening> GetAll() => _db.Listening.ToList();

        public List<Listening> GetByExamId(int examId) =>
            _db.Listening.Where(r => r.ExamId == examId).ToList();

        public void Add(Listening reading)
        {
            _db.Listening.Add(reading);
            SaveChanges();
        }

        public void Update(Listening reading)
        {
            _db.Listening.Update(reading);
            SaveChanges();
        }

        public void Delete(Listening reading)
        {
            _db.Listening.Remove(reading);
            SaveChanges();
        }

        public void SaveChanges() => _db.SaveChanges();
    }
}
