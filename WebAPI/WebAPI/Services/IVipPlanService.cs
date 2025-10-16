using System.Collections.Generic;
using WebAPI.DTOs;

namespace WebAPI.Services
{
    public interface IVipPlanService
    {
        IEnumerable<VipPlanDTO> GetAll();
        VipPlanDTO? GetById(int id);
        VipPlanDTO Create(VipPlanDTO dto);
        VipPlanDTO? Update(int id, VipPlanDTO dto);
        bool Delete(int id);
    }
}
