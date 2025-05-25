using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;

namespace App_Automatize_Backery.Helper
{
    public static class QuartzScheduler
    {
        private static IScheduler scheduler;

        public static async Task StartAsync()
        {
            scheduler = await StdSchedulerFactory.GetDefaultScheduler();

            await ScheduleJobs(scheduler);

            await scheduler.Start();
        }

        private static async Task ScheduleJobs(IScheduler scheduler)
        {
            // Инвентаризация
            var inventoryJob = JobBuilder.Create<InventoryJob>()
                .WithIdentity("InventoryJob", "InventoryGroup")
                .Build();

            var inventoryTrigger = TriggerBuilder.Create()
                .WithIdentity("InventoryTrigger", "InventoryGroup")
                .WithSchedule(CronScheduleBuilder.WeeklyOnDayAndHourAndMinute(DayOfWeek.Tuesday, 20, 0))
                .Build();

            await scheduler.ScheduleJob(inventoryJob, inventoryTrigger);

            // Автоматический отчёт РПП
            var autoCreateRPPJob = JobBuilder.Create<AutoCreateRPPReportJob>()
                .WithIdentity("AutoCreateRPPReportJob", "ReportJobs")
                .Build();

            var autoCreateRPPTrigger = TriggerBuilder.Create()
                .WithIdentity("AutoCreateRPPReportTrigger", "ReportTriggers")
                .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(2, 10))
                .Build();

            await scheduler.ScheduleJob(autoCreateRPPJob, autoCreateRPPTrigger);

            // Утилизация просроченной продукции
            var disposalJob = JobBuilder.Create<ProductsDisposalJob>()
                .WithIdentity("ProductsDisposalJob", "DisposalGroup")
                .Build();

            var disposalTrigger = TriggerBuilder.Create()
                .WithIdentity("ProductsDisposalTrigger", "DisposalGroup")
                .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(20, 55))
                .Build();

            await scheduler.ScheduleJob(disposalJob, disposalTrigger);

            var rawDisposalJob = JobBuilder.Create<App_Automatize_Backery.Helper.RawMaterialDisposalJob>()
                .WithIdentity("RawMaterialDisposalJob", "Maintenance")
                .Build();

            var rawDisposalTrigger = TriggerBuilder.Create()
                .WithIdentity("RawMaterialDisposalTrigger", "Maintenance")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInHours(1) // каждые 1 час
                    .RepeatForever())
                .Build();

            await scheduler.ScheduleJob(rawDisposalJob, rawDisposalTrigger);
        }

        public static async Task StopAsync()
        {
            if (scheduler != null)
            {
                await scheduler.Shutdown();
            }
        }

    }
}
