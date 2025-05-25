using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App_Automatize_Backery.Helper
{
    public class ProductsDisposalJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var service = new ProductsDisposalService(App.DbContext);
            await service.DisposeExpiredProductsAsync();
        }
    }
}
