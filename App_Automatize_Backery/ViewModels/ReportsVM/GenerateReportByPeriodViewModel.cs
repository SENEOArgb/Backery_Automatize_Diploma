using App_Automatize_Backery.Helper;
using App_Automatize_Backery.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace App_Automatize_Backery.ViewModels.ReportsVM
{
    public class GenerateReportByPeriodViewModel : INotifyPropertyChanged
    {
        private readonly MinBakeryDbContext _context;
        public MainViewModel mainVm;
        public ReportViewModel repVm;
        public DateTime StartDate { get; set; } = DateTime.Now.Date;
        public DateTime EndDate { get; set; } = DateTime.Now.Date;
        public string SelectedReportType { get; set; } = "Продажи";

        public ICommand GenerateReportCommand { get; }

        public GenerateReportByPeriodViewModel(ReportViewModel repViewModel)
        {
            repVm = repViewModel;
            _context = App.DbContext;
            GenerateReportCommand = new RelayCommand(_ => GenerateReport(), _ => CanGenerateReport());
        }

        private bool CanGenerateReport()
        {
            // Проверка, что дата окончания позже даты начала
            return EndDate >= StartDate;
        }

        private void GenerateReport()
        {
            var report = CreateReport();
            var links = new List<ExpencesReportsParish>();

            if (SelectedReportType == "Продажи")
            {
                var periodParishes = _context.Parishes
                    .Include(p => p.Sale)
                        .ThenInclude(s => s.SaleProducts)
                            .ThenInclude(sp => sp.Product)
                    .Where(p => p.Sale.DateTimeSale >= StartDate && p.Sale.DateTimeSale < EndDate.AddDays(1))
                    .ToList();

                links.AddRange(periodParishes.Select(p => new ExpencesReportsParish { Report = report, ParisheId = p.ParisheId }));

                report.ExpencesReportsParishes = links;

                _context.Reports.Add(report);
                _context.SaveChanges();

                if (periodParishes.Any())
                {
                    repVm.GenerateWordReportByPeriod(report, periodParishes, null);
                }
            }
            else if (SelectedReportType == "Поставки")
            {
                var periodExpences = _context.Expences
                    .Include(e => e.SupplyRequest)
                        .ThenInclude(sr => sr.SupplyRequestsRawMaterials)
                            .ThenInclude(r => r.RawMaterial)
                    .Include(e => e.SupplyRequest.User)
                    .Where(e => e.SupplyRequest.SupplyRequestDate >= StartDate && e.SupplyRequest.SupplyRequestDate < EndDate.AddDays(1))
                    .ToList();

                links.AddRange(periodExpences.Select(e => new ExpencesReportsParish { Report = report, ExpenceId = e.ExpenceId }));

                report.ExpencesReportsParishes = links;

                _context.Reports.Add(report);
                _context.SaveChanges();

                if (periodExpences.Any())
                {
                    repVm.GenerateWordReportByPeriod(report, null, periodExpences);
                }
            }

            MessageBox.Show("Отчёт за период успешно создан!", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private Report CreateReport()
        {
            return new Report
            {
                ReportDate = DateTime.Now,
                ReportType = SelectedReportType,
                UserId = repVm.vmMain.CurrentUser.UserId
            };
        }

        private IEnumerable<ExpencesReportsParish> GenerateSalesReport(DateTime startDate, DateTime endDate, Report report)
        {
            var reportParishes = _context.Parishes
                .Include(p => p.Sale)
                    .ThenInclude(s => s.SaleProducts)
                        .ThenInclude(sp => sp.Product)
                .Where(p => p.Sale.DateTimeSale >= StartDate && p.Sale.DateTimeSale <= EndDate.AddDays(1).AddTicks(-1))
                .ToList();

            return reportParishes.Select(p => new ExpencesReportsParish
            {
                Report = report,
                ParisheId = p.ParisheId
            });
        }

        private IEnumerable<ExpencesReportsParish> GenerateSupplyReport(DateTime startDate, DateTime endDate, Report report)
        {
            var expences = _context.Expences
                .Include(e => e.SupplyRequest)
                    .ThenInclude(sr => sr.SupplyRequestsRawMaterials)
                        .ThenInclude(r => r.RawMaterial)
                .Where(e => e.SupplyRequest.SupplyRequestDate >= startDate && e.SupplyRequest.SupplyRequestDate < endDate.AddDays(1))
                .ToList();

            return expences.Select(e => new ExpencesReportsParish
            {
                Report = report,
                ExpenceId = e.ExpenceId
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
