using App_Automatize_Backery.Helper;
using App_Automatize_Backery.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.Collections.Specialized;
using System.ComponentModel;
using App_Automatize_Backery.View.Windows.Productions;
using App_Automatize_Backery.View.UserControls_Pages_.WorkerManufactury;
using App_Automatize_Backery.ViewModels.ProductionsVM.SupportVM;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Diagnostics;

namespace App_Automatize_Backery.ViewModels.ProductionsVM
{
    public class ProductionViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Production> Productions { get; set; }

        private Production _currentProductionDetails;
        public object CurrentDetailsView { get; set; }

        internal readonly StockService _stockService;
        public ICommand CreateProductionCommand { get; }
        public ICommand ViewProductionDetailsCommand { get; }

        public MinBakeryDbContext _context;
        public ProductionViewModel(MinBakeryDbContext context, StockService stockService)
        {
            _context = context;
            _stockService = stockService;
            Productions = new ObservableCollection<Production>(GetAllProductions());
            CreateProductionCommand = new RelayCommand(_ => OpenCreateProduction());
            ViewProductionDetailsCommand = new RelayCommand(p => ShowDetails(p as Production));
        }

        private void OpenCreateProduction()
        {
            var now = DateTime.Now.TimeOfDay;
            var startRestriction = new TimeSpan(21, 0, 0);
            var endRestriction = new TimeSpan(8, 0, 0);

            // Проверяем: либо после 21:00, либо до 8:00
            if (now >= startRestriction || now < endRestriction)
            {
                MessageBox.Show("Создание записей производства недоступно с 21:00 до 8:00.",
                                "Ограничение по времени",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            var window = new ProductionCreateEditWindow
            {
                DataContext = new ProductionCreateViewModel(App.DbContext, _stockService)
            };

            Debug.WriteLine("Создание записи производства");
            if (window.ShowDialog() == true)
            {
                RefreshProductions();
            }
        }

        private void ShowDetails(Production production)
        {
            if (production == null)
                return;

            if (_currentProductionDetails == production)
            {
                // Если повторно нажали на тот же элемент — скрываем
                CurrentDetailsView = null;
                _currentProductionDetails = null;
            }
            else
            {
                // Иначе отображаем детали
                CurrentDetailsView = new ProductionDetailsUC
                {
                    DataContext = new ProductionDetailsViewModel(production)
                };
                _currentProductionDetails = production;
            }

            OnPropertyChanged(nameof(CurrentDetailsView));
        }

        private ObservableCollection<Production> GetAllProductions()
        {
            using (var context = new MinBakeryDbContext()) // Создаём новый контекст для получения данных
            {
                return new ObservableCollection<Production>(context.Productions.ToList());
            }
        }

        private void RefreshProductions()
        {
            Productions = new ObservableCollection<Production>(GetAllProductions());
            OnPropertyChanged(nameof(Productions));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}