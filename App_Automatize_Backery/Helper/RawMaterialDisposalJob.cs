using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App_Automatize_Backery.Helper
{
    public class RawMaterialDisposalJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            RawMaterialDisposalService.DisposeExpiredRawMaterials();
            return Task.CompletedTask;
        }
    }
}
