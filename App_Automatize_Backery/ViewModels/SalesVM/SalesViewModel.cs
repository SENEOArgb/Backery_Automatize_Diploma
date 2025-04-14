using App_Automatize_Backery.Helper;
using App_Automatize_Backery.Models;
using App_Automatize_Backery.View.UserControls_Pages_.WorkerManufactury;
using App_Automatize_Backery.View.Windows.Sales;
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
        public ICommand EditSaleCommand { get; }
        public ICommand ViewSaleDetailsCommand { get; }

        public ICommand DeleteSaleCommand { get; }

        public SalesViewModel(MinBakeryDbContext context, MainViewModel mainViewModel, Action action, SaleCreateEditWindow parentWindow)
        {
            _mainViewModel = mainViewModel;
            _closeAction = action;
            CreateSaleCommand = new RelayCommand(_ => CreateSale());
            ViewSaleDetailsCommand = new RelayCommand(sale => ToggleSaleDetails(sale as Sale), sale => sale is Sale);
            DeleteSaleCommand = new RelayCommand(sale => DeleteSaleAsync(sale as Sale), sale => sale is Sale);

            _context = context;
            LoadSales();
            _parentWindow = parentWindow;
        }

        private void CreateSale()
        {
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

        private async Task DeleteSaleAsync(Sale sale)
        {
            if (sale == null) return;

            var result = MessageBox.Show(
                "Вы уверены, что хотите удалить эту продажу и вернуть продукты на склад (если это заказ)?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                // Возвращаем продукты на склад
                //await ReturnProductsToStock(sale);

                // Отмена задачи на списание, если она была назначена
                if (_saleCancellationTokens.TryGetValue(sale.SaleId, out var existingTokenSource))
                {
                    existingTokenSource.Cancel();
                    _saleCancellationTokens.Remove(sale.SaleId);
                    Debug.WriteLine($"[INFO] Задача на списание заказа {sale.SaleId} отменена.");
                }

                // Удаляем продукты и саму продажу
                var saleProducts = _context.SaleProducts
                    .Where(sp => sp.SaleId == sale.SaleId)
                    .ToList();

                _context.SaleProducts.RemoveRange(saleProducts);
                _context.Sales.Remove(sale);
                await _context.SaveChangesAsync();

                Sales.Remove(sale); // Обновление UI

                if (SelectedSale == sale)
                {
                    SelectedSaleDetails = null;
                    _isSaleDetailsVisible = false;
                    SelectedSale = null;
                }

                Debug.WriteLine($"[INFO] Продажа {sale.SaleId} успешно удалена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
