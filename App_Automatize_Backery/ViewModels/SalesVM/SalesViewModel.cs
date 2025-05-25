using App_Automatize_Backery.Helper;
using App_Automatize_Backery.Models;
using App_Automatize_Backery.View.UserControls_Pages_.WorkerManufactury;
using App_Automatize_Backery.View.Windows.Sales;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace App_Automatize_Backery.ViewModels.SalesVM
{
    public class SalesViewModel : INotifyPropertyChanged
    {
        private readonly MainViewModel _mainViewModel;
        public readonly MinBakeryDbContext _context;
        private Sale _selectedSale;
        private SaleProductUC _selectedSaleDetails;
        private bool _isSaleDetailsVisible = false; // Флаг для управления видимостью

        private Dictionary<int, CancellationTokenSource> _saleCancellationTokens = new Dictionary<int, CancellationTokenSource>();
        private readonly Action _closeAction;
        public ObservableCollection<Sale> Sales { get; } = new();

        private SaleCreateEditWindow _parentWindow;

        public Sale SelectedSale
        {
            get => _selectedSale;
            set
            {
                _selectedSale = value;
                OnPropertyChanged(nameof(SelectedSale));
                OnPropertyChanged(nameof(CanEditSale));
            }
        }

        // Кнопка "Редактировать" активна только для заказов, которые еще в процессе
        public bool CanEditSale => SelectedSale != null &&
                                   SelectedSale.TypeSale == "Заказ" &&
                                   SelectedSale.SaleStatus == "В процессе";

        public SaleProductUC SelectedSaleDetails
        {
            get => _selectedSaleDetails;
            set
            {
                _selectedSaleDetails = value;
                OnPropertyChanged(nameof(SelectedSaleDetails));
            }
        }

        public ICommand CreateSaleCommand { get; }
        public ICommand ViewSaleDetailsCommand { get; }

        public SalesViewModel(MinBakeryDbContext context, MainViewModel mainViewModel, Action action, SaleCreateEditWindow parentWindow)
        {
            _mainViewModel = mainViewModel;
            _closeAction = action;
            CreateSaleCommand = new RelayCommand(_ => CreateSale());
            ViewSaleDetailsCommand = new RelayCommand(sale => ToggleSaleDetails(sale as Sale), sale => sale is Sale);

            _context = context;
            LoadSales();
            _parentWindow = parentWindow;

            StartAutoRefreshSales();
        }

        private void CreateSale()
        {
            var now = DateTime.Now.TimeOfDay;
            var startBlockedTime = new TimeSpan(21, 0, 0);
            var endBlockedTime = new TimeSpan(8, 0, 0);

            if (now >= startBlockedTime || now < endBlockedTime)
            {
                MessageBox.Show("Оформление продажи доступно только с 8:00 до 21:00.",
                                "Доступ ограничен",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            var window = new SaleCreateEditWindow(_context, _mainViewModel, _closeAction);
            var vm = new SaleCreateEditViewModel(_context, _mainViewModel, () => window.Close(), window);
            window.DataContext = vm;
            window.ShowDialog();
            LoadSales();
        }

        private void LoadSales()
        {
            Sales.Clear();
            foreach (var sale in _context.Sales)
                Sales.Add(sale);
        }

        private async Task ReturnProductsToStock(Sale sale)
        {
            var saleProducts = _context.SaleProducts
                .Where(sp => sp.SaleId == sale.SaleId)
                .ToList();

            // Если это "Заказ", возвращаем продукты на склад
            if (sale.TypeSale == "Заказ")
            {
                foreach (var sp in saleProducts)
                {
                    var warehouseProduct = _context.RawMaterialsWarehousesProducts
                        .FirstOrDefault(p => p.ProductId == sp.ProductId && p.WarehouseId == 1);

                    if (warehouseProduct != null)
                    {
                        warehouseProduct.RawMaterialCount += sp.CountProductSale;
                    }
                    else
                    {
                        // Если не найдено — создаём новую запись на складе
                        _context.RawMaterialsWarehousesProducts.Add(new RawMaterialsWarehousesProduct
                        {
                            ProductId = sp.ProductId,
                            WarehouseId = 1,
                            RawMaterialCount = sp.CountProductSale
                        });
                    }
                }
            }
            await _context.SaveChangesAsync();
        }

        private CancellationTokenSource _refreshCancellationTokenSource = new CancellationTokenSource();

        private void ToggleSaleDetails(Sale sale)
        {
            if (sale == null) return;

            if (_isSaleDetailsVisible && SelectedSale == sale)
            {
                // Если уже открыто, скрываем
                SelectedSaleDetails = null;
                _isSaleDetailsVisible = false;
            }
            else
            {
                // Если закрыто, открываем
                SelectedSale = sale;
                SelectedSaleDetails = new SaleProductUC { DataContext = new SaleProductViewModel(sale, _context) };
                _isSaleDetailsVisible = true;
            }
        }

        private void StartAutoRefreshSales()
        {
            Task.Run(async () =>
            {
                while (!_refreshCancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), _refreshCancellationTokenSource.Token);
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            RefreshSalesFromDatabase();
                        });
                    }
                    catch (TaskCanceledException)
                    {
                        break; // Задача была отменена
                    }
                }
            });
        }


        private void RefreshSalesFromDatabase()
        {
            var latestSales = _context.Sales.AsNoTracking().ToList();

            foreach (var updatedSale in latestSales)
            {
                var existingSale = Sales.FirstOrDefault(s => s.SaleId == updatedSale.SaleId);
                if (existingSale != null)
                {
                    // Обновляем поля вручную
                    if (existingSale.SaleStatus != updatedSale.SaleStatus)
                    {
                        existingSale.SaleStatus = updatedSale.SaleStatus;
                        OnPropertyChanged(nameof(Sales)); // Если UI не обновляется, можно вызвать вручную
                    }

                    // при необходимости — другие свойства тоже
                }
                else
                {
                    Sales.Add(updatedSale); // новая продажа
                }
            }

            // Удаление тех, кто был удалён из БД
            var toRemove = Sales.Where(localSale => !latestSales.Any(dbSale => dbSale.SaleId == localSale.SaleId)).ToList();
            foreach (var sale in toRemove)
                Sales.Remove(sale);
        }


        public void Dispose()
        {
            _refreshCancellationTokenSource.Cancel();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
