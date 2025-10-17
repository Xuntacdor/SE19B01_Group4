using System.Collections.Generic;
using System.Linq;
using WebAPI.Data;
using WebAPI.Models;

namespace WebAPI.Repositories
{
    public class VipPlanRepository : IVipPlanRepository
    {
        private readonly ApplicationDbContext _db;

        public VipPlanRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public IEnumerable<VipPlan> GetAll()
        {
            return _db.VipPlans
                .OrderBy(p => p.Price)
                .ToList();
        }

        public VipPlan? GetById(int id)
        {
            return _db.VipPlans.FirstOrDefault(p => p.VipPlanId == id);
        }

        public void Add(VipPlan plan)
        {
            _db.VipPlans.Add(plan);
        }

        public void Update(VipPlan plan)
        {
            _db.VipPlans.Update(plan);
        }

        public void Delete(VipPlan plan)
        {
            _db.VipPlans.Remove(plan);
        }

        public void SaveChanges()
        {
            _db.SaveChanges();
        }
    }
}
