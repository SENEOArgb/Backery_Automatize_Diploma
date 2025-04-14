using App_Automatize_Backery.Helper;
using App_Automatize_Backery.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace App_Automatize_Backery.ViewModels.SupportViewModel
{
    internal class PWarehouseViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<RawMaterialsWarehousesProduct> _warehouseItems;
        public ObservableCollection<RawMaterialsWarehousesProduct> WarehouseItems
        {
            get => _warehouseItems;
            set
            {
                _warehouseItems = value;
                OnPropertyChanged(nameof(WarehouseItems));
            }
        }

        private readonly MinBakeryDbContext _context;
        private Timer _cleanupTimer;

        public PWarehouseViewModel()
        {
            _context = App.DbContext;
            LoadProducts();
            RefreshCommand = new RelayCommand(async (param) => await RefreshWarehousesP());
            StartCleanupTask();  // Инициализируем задачу очистки
        }

        internal void LoadProducts()
        {
            //_warehouseItems.Clear();
            WarehouseItems = new ObservableCollection<RawMaterialsWarehousesProduct>(
                _context.RawMaterialsWarehousesProducts
                    .Include(r => r.Product)
                    .Where(r => r.Product != null)
                    .ToList()
            );
        }

        public ICommand RefreshCommand;

        public async Task RefreshWarehousesP()
        {
            var productWarehouses = _context.RawMaterialsWarehousesProducts
                .Include(r => r.Product)
                .Where(r => r.Product != null)
                .ToList();

            WarehouseItems.Clear();
            foreach (var item in productWarehouses)
                WarehouseItems.Add(item);
        }

        private void StartCleanupTask()
        {
            // Таймер для выполнения задачи очистки каждый день в полночь
            var timeToFirstRun = GetTimeUntilMidnight();
            _cleanupTimer = new Timer(CleanupOldRecords, null, timeToFirstRun, TimeSpan.FromDays(1));
        }

        private TimeSpan GetTimeUntilMidnight()
        {
            var now = DateTime.Now;
            var midnight = DateTime.Today.AddDays(1); // Следующая полночь
            return midnight - now;
        }

        private async void CleanupOldRecords(object state)
        {
            var expirationDate = DateTime.Now.AddDays(-1);  // Удаляем записи старше одного дня

            var expiredProducts = _context.RawMaterialsWarehousesProducts
                .Where(p => p.ProductId != null & p.DateSupplyOrProduction < expirationDate)
                .ToList();

            if (expiredProducts.Any())
            {
                _context.RawMaterialsWarehousesProducts.RemoveRange(expiredProducts);
                await _context.SaveChangesAsync();
                LoadProducts(); // Перезагружаем данные после удаления
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
