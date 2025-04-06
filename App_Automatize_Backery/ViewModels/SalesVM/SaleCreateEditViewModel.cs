using App_Automatize_Backery.Helper;
using App_Automatize_Backery.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using App_Automatize_Backery.View.Windows.Productions;
using App_Automatize_Backery.ViewModels.ProductionsVM.SupportVM;
using System.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace App_Automatize_Backery.ViewModels.SalesVM
{
    public class SaleCreateEditViewModel : INotifyPropertyChanged
    {
        private string _selectedSaleType;
        private decimal? _totalCost;
        private Product _selectedProduct;
        private int _selectedProductQuantity;
        private DateTime _selectedOrderDate;
        private TimeSpan _selectedOrderTime;
        private CancellationTokenSource _cancellationTokenSource;
        private MainViewModel vmMain;
        internal ProductionCreateViewModel vmProdCreate;
        public Action<bool>? CloseAction { get; set; }

        StockService _stockService;

        // Добавляем флаг для контроля выполнения задачи
        private bool _isSaleTaskRunning = false;

        public Sale Sale { get; set; }
        public ObservableCollection<SaleProduct> SaleProducts { get; } = new();
        public ObservableCollection<Product> Products { get; set; }
        public ObservableCollection<string> SaleTypes { get; } = new() { "Заказ", "Прямая продажа" };

        public string SelectedSaleType
        {
            get => _selectedSaleType;
            set
            {
                _selectedSaleType = value;
                OnPropertyChanged(nameof(SelectedSaleType));

                if (_selectedSaleType == "Прямая продажа")
                {
                    SelectedOrderDate = DateTime.Now.Date;
                    SelectedOrderTime = DateTime.Now.TimeOfDay;
                }
            }
        }

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged(nameof(SelectedProduct));
            }
        }

        public int SelectedProductQuantity
        {
            get => _selectedProductQuantity;
            set
            {
                _selectedProductQuantity = value;
                OnPropertyChanged(nameof(SelectedProductQuantity));
            }
        }

        public DateTime SelectedOrderDate
        {
            get => _selectedOrderDate;
            set
            {
                _selectedOrderDate = value;
                Sale.DateTimeSale = _selectedOrderDate.Date + _selectedOrderTime;
                OnPropertyChanged(nameof(SelectedOrderDate));
            }
        }

        public TimeSpan SelectedOrderTime
        {
            get => _selectedOrderTime;
            set
            {
                _selectedOrderTime = value;
                Sale.DateTimeSale = _selectedOrderDate.Date + _selectedOrderTime;
                OnPropertyChanged(nameof(SelectedOrderTime));
            }
        }

        public decimal? TotalCost
        {
            get => _totalCost;
            set
            {
                _totalCost = value;
                OnPropertyChanged(nameof(TotalCost));
            }
        }

        public ICommand AddProductCommand { get; }
        public ICommand RemoveProductCommand { get; }
        public ICommand SaveCommand { get; }

        public readonly MinBakeryDbContext _context;

        public SaleCreateEditViewModel(MinBakeryDbContext context, MainViewModel mainViewModel, Sale? sale = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            vmMain = mainViewModel;

            Sale = sale ?? new Sale();
            SelectedSaleType = Sale.TypeSale ?? "Прямая продажа";
            SelectedOrderDate = Sale.DateTimeSale.Date;
            SelectedOrderTime = Sale.DateTimeSale.TimeOfDay;

            Products = new ObservableCollection<Product>(_context.Products.AsNoTracking().ToList());
            SelectedProduct = _context.Products.FirstOrDefault();

            AddProductCommand = new RelayCommand(_ => AddProduct());
            RemoveProductCommand = new RelayCommand(sp => RemoveProduct(sp as SaleProduct), sp => sp is SaleProduct);
            SaveCommand = new RelayCommand(_ => SaveSale());
        }

        private void AddProduct()
        {
            if (SelectedProduct == null || SelectedProductQuantity <= 0) return;

            var newSaleProduct = new SaleProduct
            {
                ProductId = SelectedProduct.ProductId,
                Product = SelectedProduct,
                CountProductSale = SelectedProductQuantity,
                CoastToProduct = SelectedProduct.ProductCoast * SelectedProductQuantity
            };

            SaleProducts.Add(newSaleProduct);
            UpdateTotalCost(Sale);
        }

        private void RemoveProduct(SaleProduct? saleProduct)
        {
            if (saleProduct != null)
            {
                SaleProducts.Remove(saleProduct);
                UpdateTotalCost(Sale);
            }
        }

        private void UpdateTotalCost(Sale sale)
        {
            TotalCost = SaleProducts.Sum(sp => sp.CoastToProduct);
            Sale.CoastSale = TotalCost;
        }

        private readonly Dictionary<int, CancellationTokenSource> _saleCancellationTokens = new();

        private async Task UpdateSaleTaskAsync(Sale sale)
        {
            if (sale == null)
            {
                Debug.WriteLine("[ERROR] sale is null in UpdateSaleTaskAsync");
                return;
            }

            if (sale.TypeSale != "Заказ")
                return;

            if (_saleCancellationTokens.TryGetValue(sale.SaleId, out var existingTokenSource))
            {
                existingTokenSource.Cancel();
                _saleCancellationTokens.Remove(sale.SaleId);
            }

            var cts = new CancellationTokenSource();
            _saleCancellationTokens[sale.SaleId] = cts;
            var token = cts.Token;

            TimeSpan delay = sale.DateTimeSale - DateTime.Now;

            if (delay.TotalMilliseconds <= 0)
            {
                MessageBox.Show($"Время для выполнения заказа {sale.SaleId} уже прошло!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Debug.WriteLine($"[INFO] Планируем списание заказа {sale.SaleId} на {sale.DateTimeSale}");

                await Task.Delay(delay, token);

                if (token.IsCancellationRequested)
                {
                    Debug.WriteLine($"[INFO] Списание заказа {sale.SaleId} отменено.");
                    return;
                }

                using (var dbContext = new MinBakeryDbContext())
                using (var transaction = await dbContext.Database.BeginTransactionAsync(token))
                {
                    try
                    {
                        var dbSale = await dbContext.Sales.FirstOrDefaultAsync(s => s.SaleId == sale.SaleId, token);
                        if (dbSale == null)
                        {
                            MessageBox.Show($"Продажа с ID {sale.SaleId} была удалена до исполнения.", "Информация", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (dbSale.DateTimeSale != sale.DateTimeSale)
                        {
                            Debug.WriteLine($"[INFO] Время заказа {sale.SaleId} изменилось. Новая задача уже запущена.");
                            return;
                        }

                        var warehousesProducts = await dbContext.RawMaterialsWarehousesProducts
                            .Include(rwp => rwp.Product)
                            .ToListAsync(token);

                        var saleProducts = await dbContext.SaleProducts
                            .Include(sp => sp.Product)
                            .Where(sp => sp.SaleId == sale.SaleId)
                            .ToListAsync(token);

                        foreach (var saleProduct in saleProducts)
                        {
                            int remainingToDeduct = saleProduct.CountProductSale;

                            var targetTime = sale.DateTimeSale.AddMinutes(-1);
                            var roundedTargetTime = new DateTime(targetTime.Year, targetTime.Month, targetTime.Day, targetTime.Hour, targetTime.Minute, 0);

                            var warehouseProducts = await dbContext.RawMaterialsWarehousesProducts
                                .Where(wp =>
                                    wp.ProductId == saleProduct.ProductId &&
                                    wp.DateSupplyOrProduction.Year == roundedTargetTime.Year &&
                                    wp.DateSupplyOrProduction.Month == roundedTargetTime.Month &&
                                    wp.DateSupplyOrProduction.Day == roundedTargetTime.Day &&
                                    wp.DateSupplyOrProduction.Hour == roundedTargetTime.Hour &&
                                    wp.DateSupplyOrProduction.Minute == roundedTargetTime.Minute)
                                .OrderBy(wp => wp.DateSupplyOrProduction)
                                .ToListAsync(token);

                            foreach (var warehouseProduct in warehouseProducts)
                            {
                                if (remainingToDeduct <= 0) break;

                                if (warehouseProduct.RawMaterialCount <= remainingToDeduct)
                                {
                                    remainingToDeduct -= (int)warehouseProduct.RawMaterialCount;
                                    dbContext.RawMaterialsWarehousesProducts.Remove(warehouseProduct);
                                }
                                else
                                {
                                    warehouseProduct.RawMaterialCount -= remainingToDeduct;
                                    remainingToDeduct = 0;
                                }
                            }

                            if (remainingToDeduct > 0)
                            {
                                string productName = saleProduct.Product?.ProductName ?? "[неизвестно]";
                                throw new InvalidOperationException($"Недостаточно товара {productName} на складе!");
                            }
                        }

                        dbSale.SaleStatus = "Завершена";
                        dbContext.Sales.Update(dbSale);
                        await dbContext.SaveChangesAsync(token);
                        await transaction.CommitAsync(token);

                        Debug.WriteLine($"[INFO] Списание заказа {sale.SaleId} успешно выполнено.");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync(token);
                        MessageBox.Show($"Ошибка при выполнении заказа: {ex.Message}\n\n{ex.InnerException?.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        Debug.WriteLine($"[ERROR] Ошибка в транзакции UpdateSaleTaskAsync: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выполнении запланированной продажи: {ex.Message}\n\n{ex.InnerException?.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"[ERROR] Ошибка в UpdateSaleTaskAsync: {ex}");
            }
        }


        private async Task<bool> CreateProductionForOrderAsync(int saleId, MinBakeryDbContext context)
        {
            if (context == null)
            {
                Debug.WriteLine("[ERROR] context is null in CreateProductionForOrderAsync");
                return false;
            }

            var freshSale = await context.Sales
                .Include(s => s.SaleProducts)
                    .ThenInclude(sp => sp.Product)
                .FirstOrDefaultAsync(s => s.SaleId == saleId);

            if (freshSale == null)
            {
                MessageBox.Show("Продажа не найдена при создании производства.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            var startDateTime = freshSale.DateTimeSale.AddMinutes(-5);//-30
            var endDateTime = freshSale.DateTimeSale.AddMinutes(-1);

            // 1. Создаём и сохраняем Production в базу
            var production = new Production
            {
                DateTimeStart = startDateTime,
                DateTimeEnd = endDateTime
            };

            await context.Productions.AddAsync(production);
            await context.SaveChangesAsync();

            // 2. Подгружаем заново сохранённую Production из БД (если нужно)
            var savedProduction = await context.Productions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductionId == production.ProductionId);

            if (savedProduction == null)
            {
                MessageBox.Show("Производство не удалось сохранить.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            StockService stockService = new StockService(context);

            // 3. Создаём ViewModel и заполняем до закрытия контекста!
            var vmProdCreate = new ProductionCreateViewModel(context, stockService, freshSale)
            {
                _stockService = stockService,
                StartTime = startDateTime.TimeOfDay,
                EndTime = endDateTime.TimeOfDay,
                DateTimeStart = startDateTime,
                DateTimeEnd = endDateTime,
                _currentProduction = savedProduction,
                IsReadOnly = true
            };

            vmProdCreate.SelectedRecipes.Clear();

            foreach (var saleProduct in freshSale.SaleProducts)
            {
                var recipe = await context.Recipes
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.ProductId == saleProduct.ProductId);

                if (recipe == null)
                {
                    string name = saleProduct.Product?.ProductName ?? "[неизвестный продукт]";
                    MessageBox.Show($"Не найден рецепт для продукта {name}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    continue;
                }

                var ingredients = await context.RawMaterialMeasurementUnitRecipes
                    .Include(i => i.RawMaterial)
                    .Include(i => i.MeasurementUnit)
                    .Where(r => r.RecipeId == recipe.RecipeId)
                    .ToListAsync();

                var selectedRecipeModel = new SelectedRecipeModel
                {
                    Recipe = recipe,
                    ProductCount = saleProduct.CountProductSale,
                    Ingredients = new ObservableCollection<RawMaterialMeasurementUnitRecipe>(ingredients)
                };

                vmProdCreate.SelectedRecipes.Add(selectedRecipeModel);
            }

            // Загрузим существующие данные пока контекст жив!
            vmProdCreate.LoadExistingProductionData();

            // 5. Открываем окно
            ProductionCreateEditWindow wnProduction = new ProductionCreateEditWindow(_context, _stockService, freshSale)
            {
                DataContext = vmProdCreate
            };

            var result = wnProduction.ShowDialog();
            if(result == true)
            {
                await UpdateSaleTaskAsync(freshSale);
            }
            return result == true;
        }



        public async void SaveSale()
        {
            try
            {
                if (SaleProducts == null || SaleProducts.Count == 0)
                {
                    MessageBox.Show("Добавьте хотя бы один продукт в продажу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (vmMain?.CurrentUser == null)
                {
                    MessageBox.Show("Пользователь не авторизован.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (_saleCancellationTokens.TryGetValue(Sale?.SaleId ?? 0, out var existingTokenSource))
                {
                    existingTokenSource.Cancel();
                    _saleCancellationTokens.Remove(Sale.SaleId);
                }

                SetSaleTypeAndDateTime();
                Sale.UserId = vmMain.CurrentUser.UserId;
                Sale.SaleStatus = SelectedSaleType == "Прямая продажа" ? "Завершена" : "В процессе";

                if (Sale.DateTimeSale.TimeOfDay >= new TimeSpan(19, 0, 0))
                {
                    decimal discount = 0.20m;
                    TotalCost = TotalCost * (1 - discount);
                    Sale.CoastSale = TotalCost;
                }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        bool isNewSale = Sale.SaleId == 0;

                        if (!isNewSale)
                            await RestoreStockFromPreviousSale(Sale.SaleId);

                        if (isNewSale)
                            _context.Sales.Add(Sale);
                        else
                            _context.Sales.Update(Sale);

                        await _context.SaveChangesAsync();

                        foreach (var saleProduct in SaleProducts)
                        {
                            saleProduct.SaleId = Sale.SaleId;
                            saleProduct.Product = null;

                            _context.Entry(saleProduct).State = saleProduct.SaleProductId == 0
                                ? EntityState.Added
                                : EntityState.Modified;
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        MessageBox.Show("Продажа успешно сохранена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        MessageBox.Show($"Ошибка при сохранении продажи: {ex.Message}\n\n{ex.InnerException?.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        Debug.WriteLine($"[ERROR] Ошибка в транзакции SaveSale: {ex}");
                        return;
                    }
                }

                bool closeWindow = false;

                if (SelectedSaleType == "Заказ")
                {
                    using (var tempContext = new MinBakeryDbContext())
                    {
                        bool productionCreated = await CreateProductionForOrderAsync(Sale.SaleId, tempContext);
                        if (productionCreated)
                        {
                            //await UpdateSaleTaskAsync(Sale);
                        }
                        else
                        {
                            MessageBox.Show("Создание производства было отменено. Продажа сохранена, но производство не запущено.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    closeWindow = true;
                }
                else if (SelectedSaleType == "Прямая продажа")
                {
                    await ProcessImmediateSaleAsync();
                    closeWindow = true;
                }

                if (closeWindow == true)
                {
                    CloseWindow(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}\n\n{ex.InnerException?.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"[ERROR] Ошибка SaveSale: {ex}");
            }
        }

        private void SetSaleTypeAndDateTime()
        {
            Sale.TypeSale = SelectedSaleType;

            if (SelectedSaleType == "Прямая продажа")
            {
                Sale.DateTimeSale = DateTime.Now;
            }
            else
            {
                // Здесь комбинируем дату и время в одном объекте DateTime
                Sale.DateTimeSale = SelectedOrderDate.Add(SelectedOrderTime);
            }
        }

        private async Task ProcessImmediateSaleAsync()
        {
            foreach (var saleProduct in SaleProducts)
            {
                int remainingToDeduct = saleProduct.CountProductSale;

                // Получаем все партии товара, отсортированные по дате (FIFO)
                var warehouseProducts = _context.RawMaterialsWarehousesProducts
                    .Where(wp => wp.ProductId == saleProduct.ProductId)
                    .OrderBy(wp => wp.DateSupplyOrProduction)//если не будет работать списание для Прямой - убрать
                    .ToList();

                foreach (var warehouseProduct in warehouseProducts)
                {
                    if (remainingToDeduct <= 0) break;

                    if (warehouseProduct.RawMaterialCount <= remainingToDeduct)
                    {
                        // Списываем всю партию и удаляем
                        remainingToDeduct -= (int)warehouseProduct.RawMaterialCount;
                        _context.RawMaterialsWarehousesProducts.Remove(warehouseProduct);
                    }
                    else
                    {
                        // Списываем часть из партии
                        warehouseProduct.RawMaterialCount -= remainingToDeduct;
                        remainingToDeduct = 0;
                    }
                }

                if (remainingToDeduct > 0)
                {
                    throw new InvalidOperationException($"Недостаточно товара {saleProduct.Product.ProductName} на складе!");
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task RestoreStockFromPreviousSale(int saleId)
        {
            using (var dbContext = new MinBakeryDbContext())
            {
                var previousSaleProducts = await dbContext.SaleProducts
                    .Where(sp => sp.SaleId == saleId)
                    .ToListAsync();

                foreach (var saleProduct in previousSaleProducts)
                {
                    var warehouseProduct = await dbContext.RawMaterialsWarehousesProducts
                        .FirstOrDefaultAsync(wp => wp.ProductId == saleProduct.ProductId);

                    if (warehouseProduct != null)
                    {
                        warehouseProduct.RawMaterialCount += saleProduct.CountProductSale;
                    }
                }

                await dbContext.SaveChangesAsync();
            }
        }

        private async Task ProcessScheduledSaleAsync(Sale sale, List<SaleProduct> saleProducts)
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
                var token = _cancellationTokenSource.Token;

                TimeSpan delay = sale.DateTimeSale - DateTime.Now;

                if (delay.TotalMilliseconds <= 0)
                {
                    MessageBox.Show($"Время выполнения заказа {sale.SaleId} уже прошло!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await Task.Delay(delay, token);

                if (token.IsCancellationRequested)
                {
                    MessageBox.Show($"Запланированная продажа {sale.SaleId} была отменена.", "Отмена", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                using (var dbContext = new MinBakeryDbContext())
                {
                    using (var transaction = await dbContext.Database.BeginTransactionAsync(token))
                    {
                        try
                        {
                            var dbSale = await dbContext.Sales.FirstOrDefaultAsync(s => s.SaleId == sale.SaleId, token);
                            if (dbSale == null)
                            {
                                MessageBox.Show($"Заказ {sale.SaleId} не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            foreach (var saleProduct in saleProducts)
                            {
                                var warehouseProduct = await dbContext.RawMaterialsWarehousesProducts
                                    .FirstOrDefaultAsync(wp => wp.ProductId == saleProduct.ProductId, token);

                                if (warehouseProduct == null || warehouseProduct.RawMaterialCount < saleProduct.CountProductSale)
                                {
                                    throw new InvalidOperationException($"Недостаточно товара {saleProduct.Product.ProductName} на складе!");
                                }

                                warehouseProduct.RawMaterialCount -= saleProduct.CountProductSale;
                                saleProduct.SaleId = dbSale.SaleId;
                                dbContext.SaleProducts.Add(saleProduct);
                            }

                            dbSale.SaleStatus = "Завершена";
                            await dbContext.SaveChangesAsync(token);
                            await transaction.CommitAsync(token);
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync(token);
                            MessageBox.Show($"Ошибка при выполнении заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка выполнения запланированной продажи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseWindow(bool dialogResult)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is View.Windows.Sales.SaleCreateEditWindow createWindow)
                {
                    createWindow.DialogResult = dialogResult;
                    createWindow.Close();
                    break;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}