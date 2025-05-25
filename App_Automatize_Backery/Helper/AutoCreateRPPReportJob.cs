using App_Automatize_Backery.Models;
using App_Automatize_Backery.ViewModels;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App_Automatize_Backery.Helper
{
    public class AutoCreateRPPReportJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                using (var db = new MinBakeryDbContext())
                {
                    var from = DateTime.Today;
                    var to = DateTime.Today.AddDays(1).AddSeconds(-1);

                    bool reportExists = db.Reports.Any(r => r.ReportType.Contains($"РПП за период с {from:dd.MM.yyyy} по {to:dd.MM.yyyy}"));
                    if (reportExists)
                    {
                        System.Diagnostics.Debug.WriteLine("Отчёт за этот день уже существует.");
                        return;
                    }

                    var service = new RppReportService(db);
                    service.GenerateAndSaveRPPReport(from, to);

                    System.Diagnostics.Debug.WriteLine("Отчёт РПП успешно сгенерирован и сохранён.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка автосоздания отчета РПП: {ex}");
            }
        }
    }
}
