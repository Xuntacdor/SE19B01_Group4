using System;
using System.Collections.Generic;
using System.Linq;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Repositories;

namespace WebAPI.Services
{
    public class VipPlanService : IVipPlanService
    {
        private readonly IVipPlanRepository _repo;

        public VipPlanService(IVipPlanRepository repo)
        {
            _repo = repo;
        }

    
        public IEnumerable<VipPlanDTO> GetAll()
        {
            return _repo.GetAll().Select(p => new VipPlanDTO
            {
                VipPlanId = p.VipPlanId,
                PlanName = p.PlanName,
                Price = p.Price,
                DurationDays = p.DurationDays,
                Description = p.Description,
                CreatedAt = p.CreatedAt
            });
        }


        public VipPlanDTO? GetById(int id)
        {
            var p = _repo.GetById(id);
            if (p == null) return null;

            return new VipPlanDTO
            {
                VipPlanId = p.VipPlanId,
                PlanName = p.PlanName,
                Price = p.Price,
                DurationDays = p.DurationDays,
                Description = p.Description,
                CreatedAt = p.CreatedAt
            };
        }


        public VipPlanDTO Create(VipPlanDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.PlanName))
                throw new ArgumentException("Plan name cannot be empty.");

            var entity = new VipPlan
            {
                PlanName = dto.PlanName,
                Price = dto.Price,
                DurationDays = dto.DurationDays,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            _repo.Add(entity);
            _repo.SaveChanges();

  
            return new VipPlanDTO
            {
                VipPlanId = entity.VipPlanId,
                PlanName = entity.PlanName,
                Price = entity.Price,
                DurationDays = entity.DurationDays,
                Description = entity.Description,
                CreatedAt = entity.CreatedAt
            };
        }


        public VipPlanDTO? Update(int id, VipPlanDTO dto)
        {
            var existing = _repo.GetById(id);
            if (existing == null)
                return null;

            existing.PlanName = dto.PlanName ?? existing.PlanName;
            existing.Price = dto.Price != 0 ? dto.Price : existing.Price;
            existing.DurationDays = dto.DurationDays != 0 ? dto.DurationDays : existing.DurationDays;
            existing.Description = dto.Description ?? existing.Description;

            _repo.Update(existing);
            _repo.SaveChanges();

            return new VipPlanDTO
            {
                VipPlanId = existing.VipPlanId,
                PlanName = existing.PlanName,
                Price = existing.Price,
                DurationDays = existing.DurationDays,
                Description = existing.Description,
                CreatedAt = existing.CreatedAt
            };
        }

       
        public bool Delete(int id)
        {
            var existing = _repo.GetById(id);
            if (existing == null)
                return false;

            _repo.Delete(existing);
            _repo.SaveChanges();
            return true;
        }
    }
}
