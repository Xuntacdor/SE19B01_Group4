using System.Collections.Generic;
using WebAPI.Models;

namespace WebAPI.Repositories
{
    public interface IVipPlanRepository
    {
        IEnumerable<VipPlan> GetAll();
        VipPlan? GetById(int id);
        void Add(VipPlan plan);
        void Update(VipPlan plan);
        void Delete(VipPlan plan);
        void SaveChanges();
    }
}
