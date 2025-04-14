using App_Automatize_Backery.Helper;
using App_Automatize_Backery.Models;
using App_Automatize_Backery.View.SupportControls;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace App_Automatize_Backery.ViewModels.SupportViewModel
{
    internal class RMWarehouseViewModel : INotifyPropertyChanged
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

        public RMWarehouseViewModel()
        {
            _context = App.DbContext;
            LoadRawMaterials();
            RefreshCommand = new RelayCommand(async (param) => await RefreshWarehousesRM());
            //RefreshWarehousesRM();
            StartCleanupTask();  // Инициализируем задачу очистки
        }

        private void LoadRawMaterials()
        {
            WarehouseItems = new ObservableCollection<RawMaterialsWarehousesProduct>(
                _context.RawMaterialsWarehousesProducts
                    .Include(r => r.RawMaterial)
                    .Where(r => r.RawMaterial != null)
                    .ToList()
            );
        }

        public ICommand RefreshCommand { get; }

        public async Task RefreshWarehousesRM()
        {
            using (var newContext = new MinBakeryDbContext())
            {
                var rawMaterialWarehouses = newContext.RawMaterialsWarehousesProducts
                    .Include(r => r.RawMaterial)
                    .Where(r => r.RawMaterial != null)
                    .ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    WarehouseItems.Clear();
                    foreach (var item in rawMaterialWarehouses)
                    {
                        WarehouseItems.Add(item);
                    }
                });
            }
        }



        private void StartCleanupTask()
        {
            // Таймер для выполнения задачи очистки каждый день в полночь
            var timeToFirstRun = GetTimeUntilMidnight();
            _cleanupTimer = new Timer(CleanupExpiredRecords, null, timeToFirstRun, TimeSpan.FromDays(1));
        }

        private TimeSpan GetTimeUntilMidnight()
        {
            var now = DateTime.Now;
            var midnight = DateTime.Today.AddDays(1); // Следующая полночь
            return midnight - now;
        }

        private async void CleanupExpiredRecords(object state)
        {
            var expiredDate = DateTime.Now; // Проверяем все записи, срок хранения которых истек

            var expiredProducts = _context.RawMaterialsWarehousesProducts
                .Where(p => p.ExpirationDate < expiredDate)  // Убираем записи с истекшим сроком хранения
                .ToList();

            if (expiredProducts.Any())
            {
                _context.RawMaterialsWarehousesProducts.RemoveRange(expiredProducts);
                await _context.SaveChangesAsync();
                LoadRawMaterials(); // Перезагружаем данные после удаления
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
