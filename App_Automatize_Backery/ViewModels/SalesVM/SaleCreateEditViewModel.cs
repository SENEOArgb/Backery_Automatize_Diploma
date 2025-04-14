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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;
using App_Automatize_Backery.ViewModels.SupportViewModel;
using App_Automatize_Backery.View.Windows.Sales;

namespace App_Automatize_Backery.ViewModels.SalesVM
{
    public class SaleCreateEditViewModel : INotifyPropertyChanged
    {
        private string _selectedSaleType;
        private decimal? _totalCost;
        private Product _selectedProduct;
        private int _selectedProductQuantity;
        private DateTime? _selectedOrderDate;
        internal RMWarehouseViewModel _rmWarehouseVM;
        private DateTime? _selectedOrderTime;
        private CancellationTokenSource _cancellationTokenSource;
        private MainViewModel vmMain;
        internal ProductionCreateViewModel vmProdCreate;
        public Action<bool>? CloseAction { get; set; }

        private readonly Action _closeAction;

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

        private SaleCreateEditWindow _parentWindow;
        public DateTime? SelectedOrderDate
        {
            get => _selectedOrderDate;
            set
            {
                _selectedOrderDate = value ;
                //UpdateDateTimeSale();
                OnPropertyChanged(nameof(SelectedOrderDate));
            }
        }

        public DateTime? SelectedOrderTime
        {
            get => _selectedOrderTime;
            set
            {
                _selectedOrderTime = value;
                //UpdateDateTimeSale();
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

        public SaleCreateEditViewModel(MinBakeryDbContext context, MainViewModel mainViewModel, Action closeAction, SaleCreateEditWindow window, Sale? sale = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            vmMain = mainViewModel;
            _closeAction = closeAction;
            _parentWindow = window;
            Sale = sale ?? new Sale();

            SelectedSaleType = Sale.TypeSale;

            // если дата/время в Sale заданы, берём их; иначе — по умолчанию
            if (Sale.DateTimeSale != default)
            {
                SelectedOrderDate = Sale.DateTimeSale.Date;
                SelectedOrderTime = Sale.DateTimeSale;
            }
            else
            {
                SelectedOrderDate = DateTime.Now.Date;
                SelectedOrderTime = DateTime.Now;
            }


            Products = new ObservableCollection<Product>(_context.Products.Where(p => p.StatusProduct != "В архиве").AsNoTracking().ToList());
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
            if (result == true)
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
                if (!ValidateSale())
                    return;

                Sale.UserId = vmMain.CurrentUser.UserId;
                Sale.SaleStatus = SelectedSaleType == "Прямая продажа" ? "Завершена" : "В процессе";
                SetSaleTypeAndDateTime();

                Sale.TypeSale = SelectedSaleType;

                if (Sale.DateTimeSale.TimeOfDay >= new TimeSpan(19, 0, 0))
                {
                    decimal discount = 0.20m;
                    TotalCost = TotalCost * (1 - discount);
                    Sale.CoastSale = TotalCost;
                }
                Sale.SaleId = 0;
                await _context.Sales.AddAsync(Sale);
                await _context.SaveChangesAsync();

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {

                        foreach (var saleProduct in SaleProducts)
                        {
                            saleProduct.SaleId = Sale.SaleId;
                            saleProduct.Product = null;

                            _context.Entry(saleProduct).State = saleProduct.SaleProductId == 0
                                ? EntityState.Added
                                : EntityState.Modified;
                        }

                        await _context.SaveChangesAsync();

                        // 1. Создаём Parish (приход по продаже)
                        var parish = new Parish
                        {
                            ParisheDateTime = Sale.DateTimeSale,
                            SaleId = Sale.SaleId,
                            ParisheSize = (decimal)Sale.CoastSale
                        };

                        await _context.Parishes.AddAsync(parish);
                        await _context.SaveChangesAsync(); // нужно получить parish.ParishId

                        // 2. Создаём Report
                        var report = new Report
                        {
                            ReportDate = DateTime.Now,
                            UserId = Sale.UserId,
                            ReportType = $"Продажа {Sale.SaleId}"
                        };

                        await _context.Reports.AddAsync(report);
                        await _context.SaveChangesAsync(); // нужно получить report.ReportId

                        // 3. Ассоциируем Report <-> Parish через ExpencesReportsParishes
                        var link = new ExpencesReportsParish
                        {
                            ParisheId = parish.ParisheId,
                            ReportId = report.ReportId
                        };

                        await _context.ExpencesReportsParishes.AddAsync(link);
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
                        _parentWindow.DialogResult = true;
                        _parentWindow.Close();
                        bool productionCreated = await CreateProductionForOrderAsync(Sale.SaleId, tempContext);
                        if (productionCreated)
                        {
                            _closeAction?.Invoke();
                            //await UpdateSaleTaskAsync(Sale);
                        }
                        else
                        {
                            MessageBox.Show("Создание производства было отменено. Продажа сохранена, но производство не запущено.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                            _closeAction?.Invoke();
                        }
                    }
                }
                else
                {
                    using (var tempCont = new MinBakeryDbContext())
                    {
                        await ProcessImmediateSaleAsync();
                        _closeAction?.Invoke();
                    }
                }
                //_.RefreshWarehousesRM();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении продажи: {ex.Message}\n\n{ex.InnerException?.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"[ERROR] Ошибка при сохранении продажи: {ex}");
            }
        }

        private void SetSaleTypeAndDateTime()
        {
            Sale.TypeSale = SelectedSaleType;

            Debug.WriteLine($"SelectedOrderDate: {SelectedOrderDate}, SelectedOrderTime: {SelectedOrderTime}");
            if (SelectedSaleType == "Заказ")
            {
                if (SelectedOrderDate != null && SelectedOrderTime != null && SelectedOrderDate.HasValue && SelectedOrderTime.HasValue)
                {
                    var date = SelectedOrderDate.Value.Date;
                    Debug.WriteLine($"Дата: {date}");
                    var time = SelectedOrderTime.Value.TimeOfDay;
                    Debug.WriteLine($"Время: {time}");
                    DateTime orderDateTime = date + time;
                    Debug.WriteLine($"SelectedOrderDate: {SelectedOrderDate}, SelectedOrderTime: {SelectedOrderTime}");
                    Sale.TypeSale = SelectedSaleType;
                    Sale.DateTimeSale = orderDateTime;

                    Debug.WriteLine($"Установлено время продажи: {Sale.DateTimeSale}");
                    Debug.WriteLine($"Final Sale DateTimeSale: {Sale.DateTimeSale}");
                }
                else
                {
                    MessageBox.Show("Пожалуйста, выберите корректную дату и время для заказа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                // Если "Прямая продажа", используем текущую дату и время
            }
            else
            {
                // Если другой тип продажи (например, "Заказ"), используем выбранные пользователем дату и время
                Sale.DateTimeSale = DateTime.Now;
            }
            Debug.WriteLine($"Final Sale DateTimeSale: {Sale.DateTimeSale}");
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

        private bool ValidateSale()
        {
            if (string.IsNullOrWhiteSpace(SelectedSaleType))
            {
                MessageBox.Show("Выберите тип продажи.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (Sale == null)
            {
                MessageBox.Show("Объект продажи не инициализирован.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (SaleProducts == null || SaleProducts.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один продукт в продажу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (vmMain?.CurrentUser == null)
            {
                MessageBox.Show("Пользователь не авторизован.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (SelectedSaleType == "Заказ")
            {
                if (!SelectedOrderDate.HasValue || !SelectedOrderTime.HasValue)
                {
                    MessageBox.Show("Пожалуйста, выберите дату и время заказа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                var orderDateTime = SelectedOrderDate.Value.Date + SelectedOrderTime.Value.TimeOfDay;

                if (orderDateTime < DateTime.Now)
                {
                    MessageBox.Show("Дата и время заказа не могут быть в прошлом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // 🔒 Проверка: дата не позже, чем через 7 дней
                if (SelectedOrderDate.Value.Date > DateTime.Today.AddDays(7))
                {
                    MessageBox.Show("Дата заказа не может быть позже, чем через 7 дней от сегодняшней.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // 🔒 Проверка: дата не раньше сегодняшнего дня
                if (SelectedOrderDate.Value.Date < DateTime.Today)
                {
                    MessageBox.Show("Дата заказа не может быть раньше сегодняшней.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            if (TotalCost <= 0)
            {
                MessageBox.Show("Общая стоимость продажи должна быть больше 0.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            var time = Sale.DateTimeSale.TimeOfDay;
            if (time < new TimeSpan(8, 0, 0) || time > new TimeSpan(21, 0, 0))
            {
                MessageBox.Show("Продажи возможны только с 08:00 до 21:00.", "Недопустимое время", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (Sale.DateTimeSale.Date < DateTime.Today || Sale.DateTimeSale.Date > DateTime.Today.AddDays(7))
            {
                MessageBox.Show("Дата продажи должна быть от сегодня и не позже, чем через 7 дней.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            foreach (var prod in SaleProducts)
            {
                if (prod.ProductId <= 0)
                {
                    MessageBox.Show("Один или несколько продуктов в продаже не имеют корректного ID.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                if (prod.CountProductSale <= 0)
                {
                    MessageBox.Show($"Количество для продукта '{prod?.Product?.ProductName}' должно быть больше 0.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                if (prod.CoastToProduct <= 0)
                {
                    MessageBox.Show($"Цена продукта '{prod?.Product?.ProductName}' должна быть больше 0.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}