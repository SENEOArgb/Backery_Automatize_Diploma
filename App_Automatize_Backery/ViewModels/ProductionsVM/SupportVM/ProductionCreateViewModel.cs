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
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Diagnostics;
using App_Automatize_Backery.ViewModels.SupportViewModel;

namespace App_Automatize_Backery.ViewModels.ProductionsVM.SupportVM
{
    public class ProductionCreateViewModel : INotifyPropertyChanged
    {
        private readonly MinBakeryDbContext _context;
        public bool IsReadOnly { get; set; } = false;
        private Recipe _selectedRecipe;
        private ProductionViewModel prViewModel;
        internal StockService _stockService;
        internal RMWarehouseViewModel _rmWarehouseVM;
        internal Production _currentProduction; // Редактируемая запись
        private readonly Dictionary<int, CancellationTokenSource> _scheduledTasks = new();
        private readonly Dictionary<int, Task> _processingTasks = new(); // Отслеживание выполняемых задач
        private DateTime _dateTimeStart;
        private DateTime _dateTimeEnd;
        //internal Sale _existSale;
        private readonly Sale? _saleFromOrder;

        public Production CurrentProduction
        {
            get => _currentProduction;
            set
            {
                _currentProduction = value;
                LoadExistingProductionData();
                OnPropertyChanged(nameof(CurrentProduction));
            }
        }

        public DateTime DateTimeStart
        {
            get => _dateTimeStart;
            set
            {
                _dateTimeStart = value;
                OnPropertyChanged(nameof(DateTimeStart));
            }
        }

        public DateTime DateTimeEnd
        {
            get => _dateTimeEnd;
            set
            {
                _dateTimeEnd = value;
                OnPropertyChanged(nameof(DateTimeEnd));
            }
        }

        private TimeSpan _startTime;
        public TimeSpan StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                DateTimeStart = DateTimeStart.Date + _startTime; // Объединяем дату и время
                OnPropertyChanged(nameof(StartTime));
            }
        }

        private TimeSpan _endTime;
        public TimeSpan EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                DateTimeEnd = DateTimeEnd.Date + _endTime; // Объединяем дату и время
                OnPropertyChanged(nameof(EndTime));
            }
        }


        // Доступные рецепты
        public ObservableCollection<Recipe> Recipes { get; set; }

        public Recipe SelectedRecipe
        {
            get => _selectedRecipe;
            set
            {
                _selectedRecipe = value;
                OnPropertyChanged(nameof(SelectedRecipe));
            }
        }

        // Выбранные рецепты с количеством
        public ObservableCollection<SelectedRecipeModel> SelectedRecipes { get; set; } = new();

        // Команды
        public ICommand AddRecipeCommand { get; }
        public ICommand RemoveRecipeCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        //для редактирования производства
        public ProductionCreateViewModel(MinBakeryDbContext context, StockService stockService, Sale existSale)
    : this(context, stockService)
        {
            _context = context;
            _stockService = stockService;
            _saleFromOrder = existSale;
            //_rmWarehouseVM = new RMWarehouseViewModel();

            LoadExistingProductionData();

            Recipes = new ObservableCollection<Recipe>(_context.Recipes
    .Include(r => r.Product)
    .Where(r => r.Product != null) // Только рецепты с продуктами
    .ToList());

            DateTimeStart = DateTime.Now;
            DateTimeEnd = DateTime.Now.AddHours(1);
            StartTime = DateTimeStart.TimeOfDay;
            EndTime = DateTimeEnd.TimeOfDay;

            // Инициализация команд
            AddRecipeCommand = new RelayCommand(_ => AddRecipe(), _ => SelectedRecipe != null);
            RemoveRecipeCommand = new RelayCommand(obj => RemoveRecipe(obj as SelectedRecipeModel));
            SaveCommand = new RelayCommand(_ => SaveProduction());
            CancelCommand = new RelayCommand(_ => CloseWindow(false));
        }

        public ProductionCreateViewModel(MinBakeryDbContext context, StockService stockService)
        {
            _context = context;
            _stockService = stockService;
            //_rmWarehouseVM = new RMWarehouseViewModel();
            // Загружаем рецепты с продуктами
            Recipes = new ObservableCollection<Recipe>(_context.Recipes
                .Include(r => r.Product)
                .Where(r => r.Product != null & r.StatusRecipe != "В архиве") // Только рецепты с продуктами
                .ToList());

            DateTimeStart = DateTime.Now;
            DateTimeEnd = DateTime.Now.AddHours(1);
            StartTime = DateTimeStart.TimeOfDay;
            EndTime = DateTimeEnd.TimeOfDay;

            // Инициализация команд
            AddRecipeCommand = new RelayCommand(_ => AddRecipe(), _ => SelectedRecipe != null);
            RemoveRecipeCommand = new RelayCommand(obj => RemoveRecipe(obj as SelectedRecipeModel));
            SaveCommand = new RelayCommand(_ => SaveProduction());
            CancelCommand = new RelayCommand(_ => CloseWindow(false));
        }

        internal void LoadExistingProductionData()
        {
            try
            {
                if (_context == null || _currentProduction == null)
                    return;

                var productionRecipes = _context.ProductionsRawMaterialsMeasurementUnitRecipes
                    .Include(pr => pr.RawMaterialMeasurementUnitRecipe)
                        .ThenInclude(rm => rm.RawMaterial)
                    .Include(pr => pr.RawMaterialMeasurementUnitRecipe)
                        .ThenInclude(rm => rm.MeasurementUnit)
                    .Where(pr => pr.ProductionId == _currentProduction.ProductionId)
                    .ToList();

                foreach (var group in productionRecipes.GroupBy(pr => pr.RawMaterialMeasurementUnitRecipe.RecipeId))
                {
                    var ingredients = group.Select(pr => pr.RawMaterialMeasurementUnitRecipe).ToList();
                    var recipe = _context.Recipes
                        .Include(r => r.Product)
                        .FirstOrDefault(r => r.RecipeId == group.Key);

                    if (recipe != null)
                    {
                        SelectedRecipes.Add(new SelectedRecipeModel
                        {
                            Recipe = recipe,
                            ProductCount = group.First().CountProduct,
                            Ingredients = new ObservableCollection<RawMaterialMeasurementUnitRecipe>(ingredients)
                        });
                    }
                }

                OnPropertyChanged(nameof(Recipes));
                OnPropertyChanged(nameof(SelectedRecipes));
                OnPropertyChanged(nameof(SelectedRecipe));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Ошибка при загрузке данных производства: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Ошибка при загрузке данных производства: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Добавление рецепта и загрузка ингредиентов
        private void AddRecipe()
        {
            if (SelectedRecipe == null) return;

            // Проверка на дубликаты
            if (SelectedRecipes.Any(r => r.Recipe.RecipeId == SelectedRecipe.RecipeId))
            {
                MessageBox.Show("Этот рецепт уже добавлен!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Загрузка ингредиентов из RawMaterialMeasurementUnitRecipe
            var ingredients = _context.RawMaterialMeasurementUnitRecipes
                .Include(i => i.RawMaterial)
                .Include(i => i.MeasurementUnit)
                .Where(r => r.RecipeId == SelectedRecipe.RecipeId)
                .ToList();

            // Создание модели с рецептом и ингредиентами
            var selectedRecipeModel = new SelectedRecipeModel
            {
                Recipe = SelectedRecipe,
                ProductCount = 0,
                Ingredients = new ObservableCollection<RawMaterialMeasurementUnitRecipe>(ingredients)
            };

            SelectedRecipes.Add(selectedRecipeModel);
            SelectedRecipe = null; // Сброс выбора
        }

        // Сохранение производства
        private async void SaveProduction()
        {
            if (!Validate()) return;

            try
            {
                bool isNew = _currentProduction == null;

                if (isNew)
                {
                    _currentProduction = new Production
                    {
                        DateTimeStart = DateTimeStart,
                        DateTimeEnd = DateTimeEnd
                    };
                    _context.Productions.Add(_currentProduction);
                }
                else
                {
                    //CancelScheduledProcessing(_currentProduction.ProductionId);

                    _currentProduction.DateTimeStart = DateTimeStart;
                    _currentProduction.DateTimeEnd = DateTimeEnd;
                }

                await _context.SaveChangesAsync();

                var newMaterials = new List<ProductionsRawMaterialsMeasurementUnitRecipe>();
                foreach (var selected in SelectedRecipes)
                {
                    foreach (var ingredient in selected.Ingredients)
                    {
                        newMaterials.Add(new ProductionsRawMaterialsMeasurementUnitRecipe
                        {
                            ProductionId = _currentProduction.ProductionId,
                            RawMaterialMeasurementUnitRecipe = ingredient,
                            CountProduct = selected.ProductCount
                        });
                    }
                }
                _context.ProductionsRawMaterialsMeasurementUnitRecipes.AddRange(newMaterials);
                await _context.SaveChangesAsync();

                if (_saleFromOrder != null)
                {
                    var salesProduction = new SalesProduction
                    {
                        SaleId = _saleFromOrder.SaleId,
                        ProductionId = _currentProduction.ProductionId
                    };

                    _context.SalesProductions.Add(salesProduction);
                    await _context.SaveChangesAsync();
                }

                // Создание записей в ProductionsEquipments
                //CreateProductionEquipments();

                MessageBox.Show("Производство успешно сохранено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                var productCounts = GetProductCounts();

                ScheduleProductionProcessing(_currentProduction, newMaterials, productCounts);

                CloseWindow(true);

                //_rmWarehouseVM.RefreshWarehousesRM();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Удаление рецепта
        private void RemoveRecipe(SelectedRecipeModel recipeModel)
        {
            if (recipeModel != null)
                SelectedRecipes.Remove(recipeModel);
        }

        // Получение списка продуктов и их количества
        private Dictionary<int, int> GetProductCounts()
        {
            return SelectedRecipes.ToDictionary(sr => sr.Recipe.ProductId, sr => sr.ProductCount);
        }

        private bool _isProcessing = false; // Глобальная блокировка

        private void ScheduleProductionProcessing(Production production, List<ProductionsRawMaterialsMeasurementUnitRecipe> materials,
                                                   Dictionary<int, int> productCounts, Sale? sale = null)
        {
            if (sale != null)
            {
                Debug.WriteLine($"[SCHEDULE] Производство {production.ProductionId} (Продажа {sale.SaleId}): Перезапуск запланированной задачи...");
            }
            else
            {
                Debug.WriteLine($"[SCHEDULE] Производство {production.ProductionId}: Перезапуск запланированной задачи...");
            }

            var delay = production.DateTimeEnd - DateTime.Now;
            if (delay <= TimeSpan.Zero)
            {
                Debug.WriteLine($"[EXECUTE NOW] Производство {production.ProductionId}: Немедленное списание.");
                _ = ExecuteStockProcessing(production.ProductionId, materials, productCounts, sale); // Если sale null, передадим null
                return;
            }

            lock (_scheduledTasks)
            {
                _scheduledTasks[production.ProductionId] = new CancellationTokenSource(); // Убрали использование токена, но сохранили управление задачами
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    Debug.WriteLine($"[WAIT] Производство {production.ProductionId}: Ожидание {delay.TotalSeconds} секунд...");
                    await Task.Delay(delay);

                    Debug.WriteLine($"[EXECUTE] Производство {production.ProductionId}: Начинаем списание...");
                    await ExecuteStockProcessing(production.ProductionId, materials, productCounts, sale); // Применяем sale или null
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] Производство {production.ProductionId}: Ошибка: {ex.Message}");
                }
            });
        }

        private async Task ExecuteStockProcessing(int productionId, List<ProductionsRawMaterialsMeasurementUnitRecipe> materials,
                                                   Dictionary<int, int> productCounts, Sale? sale = null)
        {
            Debug.WriteLine($"[PROCESS] Производство {productionId}: Подготовка к списанию сырья...");

            var tcs = new TaskCompletionSource<bool>();

            lock (_processingTasks)
            {
                _processingTasks[productionId] = tcs.Task;
            }

            try
            {
                Debug.WriteLine($"[EXECUTE] Производство {productionId}: Начинаем списание...");

                // Если есть продажа, можно выполнить дополнительные действия, связанные с ней.
                if (sale != null)
                {
                    // Логика работы с продажей, например:
                    Debug.WriteLine($"[SALE] Производство {productionId}: Привязано к продаже {sale.SaleId}.");
                    // Можно, например, списывать товары со склада в контексте продажи или учитывать эту продажу в другой логике.
                }

                // Обновление склада, независимо от наличия продажи
                await _stockService.UpdateStockForProduction(materials, productCounts);

                Debug.WriteLine($"[SUCCESS] Производство {productionId}: Списание завершено.");
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Производство {productionId}: Ошибка: {ex.Message}");
                tcs.SetException(ex);
            }
            finally
            {
                lock (_processingTasks)
                {
                    _processingTasks.Remove(productionId);
                }
            }
        }
        internal bool WasProcessingScheduled(int productionId)
        {
            lock (_scheduledTasks)
            {
                return _scheduledTasks.ContainsKey(productionId);
            }
        }

        // Валидация данных перед сохранением
        private bool Validate()
        {
            if (DateTimeStart == default || DateTimeEnd == default)
            {
                MessageBox.Show("Укажите дату и время начала и окончания производства.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (DateTimeStart >= DateTimeEnd)
            {
                MessageBox.Show("Дата и время начала должны быть раньше даты и времени окончания.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (SelectedRecipes == null || !SelectedRecipes.Any())
            {
                MessageBox.Show("Добавьте хотя бы один рецепт!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            foreach (var recipe in SelectedRecipes)
            {
                if (recipe.ProductCount <= 0)
                {
                    MessageBox.Show($"Количество производимого продукта для рецепта '{recipe.Recipe?.Product.ProductName}' должно быть больше 0!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (recipe.Ingredients == null || !recipe.Ingredients.Any())
                {
                    MessageBox.Show($"Рецепт '{recipe.Recipe?.Product.ProductName}' не содержит ингредиентов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                foreach (var ing in recipe.Ingredients)
                {
                    if (ing.RawMaterial == null || ing.MeasurementUnit == null)
                    {
                        MessageBox.Show($"Один из ингредиентов в рецепте '{recipe.Recipe?.Product.ProductName}' не имеет сырья или единицы измерения!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }

                    if (ing.CountRawMaterial <= 0)
                    {
                        MessageBox.Show($"Количество ингредиента '{ing.RawMaterial?.RawMaterialName}' в рецепте '{recipe.Recipe?.Product.ProductName}' должно быть больше 0!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                }
            }

            return true;
        }

        internal async void CancelScheduledProcessing(int productionId)
        {
            lock (_scheduledTasks)
            {
                if (_scheduledTasks.TryGetValue(productionId, out var cts))
                {
                    Debug.WriteLine($"[CANCEL] Производство {productionId}: Немедленная отмена запланированной задачи...");
                    cts.Cancel();
                    _scheduledTasks.Remove(productionId);
                }
            }

            Task processingTask = null;
            lock (_processingTasks)
            {
                if (_processingTasks.TryGetValue(productionId, out processingTask))
                {
                    Debug.WriteLine($"[CANCEL] Производство {productionId}: Ожидание завершения текущей задачи...");
                    _processingTasks.Remove(productionId);
                }
            }

            if (processingTask != null)
            {
                try
                {
                    await processingTask; // Дожидаемся завершения списания, если оно уже началось
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine($"[CANCELLED] Производство {productionId}: Операция списания прервана.");
                }
            }

            // Теперь вызываем откат сырья и продукции
            Debug.WriteLine($"[ROLLBACK] Производство {productionId}: Восстанавливаем сырье и удаляем продукцию...");

            var materials = _context.ProductionsRawMaterialsMeasurementUnitRecipes
                .Include(m => m.RawMaterialMeasurementUnitRecipe)
                .ThenInclude(rm => rm.RawMaterial)
                .Where(m => m.ProductionId == productionId)
                .ToList();

            await _stockService.RollbackProductionAsync(materials);
        }

        // Закрытие окна
        private void CloseWindow(bool dialogResult)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is View.Windows.Productions.ProductionCreateEditWindow createWindow)
                {
                    createWindow.DialogResult = dialogResult;
                    createWindow.Close();
                    break;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }

    // Модель для хранения информации о выбранных рецептах и их ингредиентах
    public class SelectedRecipeModel : INotifyPropertyChanged
    {
        public Recipe Recipe { get; set; }

        private int _productCount;
        public int ProductCount
        {
            get => _productCount;
            set
            {
                _productCount = value;
                OnPropertyChanged(nameof(ProductCount));
            }
        }

        public ObservableCollection<RawMaterialMeasurementUnitRecipe> Ingredients { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
