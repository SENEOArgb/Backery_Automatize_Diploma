using App_Automatize_Backery.Helper;
using App_Automatize_Backery.View.Windows.RPP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace App_Automatize_Backery.ViewModels.ProductionsVM.SupportVM
{
    internal class CreateRPPViewModel
    {
        private readonly MainViewModel _parent;

        public bool IsToday { get; set; } = true;
        public bool IsPeriod { get; set; }

        public DateTime FromDate { get; set; } = DateTime.Today;
        public DateTime ToDate { get; set; } = DateTime.Today;

        public ICommand GenerateRPPReportCommand { get; }

        public CreateRPPViewModel(MainViewModel parent)
        {
            _parent = parent;
            GenerateRPPReportCommand = new RelayCommand(_ => GenerateReport());
        }

        private void GenerateReport()
        {
            DateTime start = IsToday ? DateTime.Today : FromDate.Date;
            DateTime end = IsToday ? DateTime.Today.AddDays(1).AddTicks(-1) : ToDate.Date.AddDays(1).AddTicks(-1);

            _parent.GenerateRPPReport(start, end);
            Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is CreateRPPWindow)?.Close();
        }
    }
}
