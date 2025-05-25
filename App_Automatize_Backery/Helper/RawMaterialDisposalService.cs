using App_Automatize_Backery.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App_Automatize_Backery.Helper
{
    public static class RawMaterialDisposalService
    {
        public static void DisposeExpiredRawMaterials()
        {
            using (var context = new MinBakeryDbContext())
            {
                var now = DateTime.Now;
                var expiredMaterials = context.RawMaterialsWarehousesProducts
                    .Where(p => p.DateSupplyOrProduction.AddDays(p.RawMaterial.ShelfLifeDays) < now)
                    .Include(p => p.RawMaterial) // обязательно, иначе ShelfLifeDays не увидит
                    .ToList();

                if (expiredMaterials.Any())
                {
                    context.RawMaterialsWarehousesProducts.RemoveRange(expiredMaterials);
                    context.SaveChanges();
                }
            }
        }
    }
}
