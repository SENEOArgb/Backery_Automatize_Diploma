using App_Automatize_Backery.Helper;
using App_Automatize_Backery.Models;
using App_Automatize_Backery.View;
using App_Automatize_Backery.View.UserControls_Pages_;
using App_Automatize_Backery.View.UserControls_Pages_.General_WorkerPages;
using App_Automatize_Backery.View.UserControls_Pages_.WorkerManufactury;
using App_Automatize_Backery.View.Windows;
using App_Automatize_Backery.View.Windows.RPP;
using App_Automatize_Backery.View.Windows.Sales;
using App_Automatize_Backery.ViewModels.ProductionsVM;
using App_Automatize_Backery.ViewModels.ProductionsVM.SupportVM;
using App_Automatize_Backery.ViewModels.RecipesVM;
using App_Automatize_Backery.ViewModels.ReportsVM;
using App_Automatize_Backery.ViewModels.SalesVM;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Xceed.Words.NET;
using System.Drawing;
using System.Drawing.Imaging;
using SupplyRequestsUC = App_Automatize_Backery.View.UserControls_Pages_.General_WorkerPages.SupplyRequestsUC;
using Xceed.Document.NET;

namespace App_Automatize_Backery.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly Action _closeAction;
        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set
            {
                if (_currentView != value)
                {
                    Debug.WriteLine($"CurrentView изменён: {value}");
                    _currentView = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool IsManager => CurrentUser?.UserRoleId == 2;  // Зав. производством
        public bool IsWorker => CurrentUser?.UserRoleId == 3;   // Сотрудник на производстве

        public bool IsTechnolog => CurrentUser?.UserRoleId == 1;

        // Свойство для скрытия кнопки меню, если нет доступа
        public Visibility ManagerMenuVisibility => IsManager ? Visibility.Visible : Visibility.Collapsed;
        public Visibility WorkerMenuVisibility => IsWorker ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TechnologistVisibility => IsTechnolog ? Visibility.Visible : Visibility.Collapsed;

        public Visibility RawMaterialsVisibility => IsWorker ? Visibility.Collapsed : Visibility.Visible;
        public Visibility ProductsVisibility => IsWorker ? Visibility.Collapsed : Visibility.Visible;
        public Visibility SalesVisibility => IsTechnolog ? Visibility.Collapsed : Visibility.Visible;
        public Visibility SupplyVisibility => IsTechnolog ? Visibility.Collapsed : Visibility.Visible;
        public Visibility ProductionVisibility => IsTechnolog ? Visibility.Collapsed : Visibility.Visible;
        public Visibility ReportsVisibility =>
    (IsManager || IsWorker) ? Visibility.Visible : Visibility.Collapsed;

        public User CurrentUser { get; set; }  // Новое свойство для текущего пользователя

        public ICommand ShowRawMaterialsCommand { get; }
        public ICommand ShowWarehouseCommand { get; }
        public ICommand ShowProductsCommand { get; }
        public ICommand ShowSupplyCommand { get; }
        public ICommand ShowUserCommand { get; }
        public ICommand ShowProductionCommand { get; }
        public ICommand ShowSaleCommand { get; }
        public ICommand ShowReportsCommand { get; }
        public ICommand ShowRecipeCommand { get; }

        public ICommand LogoutCommand { get; }

        public MinBakeryDbContext _context;
        internal readonly StockService _stockService;
        private SaleCreateEditWindow _parentWindow;
        public string RoleName => CurrentUser?.UserRole?.UserRoleName ?? "Неизвестно";  // Возвращаем имя роли

        private SupplyRequestViewModel _supplyRequestViewModel;
        private SupplyRequestWarehouseRMViewModel _supplyRequestRMViewModel;

        public ProductionViewModel ProductionVM { get; set; }

        public MainViewModel(User user)
        {
            LoadConfig();

            _context = App.DbContext;
            _stockService = new StockService(_context);
            _closeAction = _closeAction;
            _parentWindow = _parentWindow;

            // Загрузка данных о произведенной продукции
            LoadDailyProduction();
            LoadDailySale();

            SetDailyNormCommand = new RelayCommand(SetDailyNorm);

            CurrentUser = user;
            CurrentView = new UserUC(CurrentUser);
            ProductionVM = new ProductionViewModel(_context, _stockService);

            _supplyRequestViewModel = new SupplyRequestViewModel(this);

            LogoutCommand = new RelayCommand(Logout);

            // Создание команд для переключения представлений
            ShowUserCommand = new RelayCommand(o =>
            {
                try
                {
                    var userUC = new UserUC(CurrentUser);
                    userUC.DataContext = this; // Или новый ViewModel, если требуется
                    CurrentView = userUC;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при изменении CurrentView: {ex.Message}");
                }
            });

            ShowRawMaterialsCommand = new RelayCommand(o =>
            {
                try
                {
                    var rawMaterialsUC = new RawMaterialsUC(this);
                    rawMaterialsUC.DataContext = new RMViewModel(this); // Привязка ViewModel
                    CurrentView = rawMaterialsUC;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при изменении CurrentView: {ex.Message}");
                }
            });

            ShowRecipeCommand = new RelayCommand(o =>
            {
                try
                {
                    var recipesUC = new RecipesUC();
                    recipesUC.DataContext = new RecipesViewModel(this); // Привязка ViewModel
                    CurrentView = recipesUC;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при изменении CurrentView: {ex.Message}");
                }
            });

            ShowWarehouseCommand = new RelayCommand(o =>
            {
                try
                {
                    var warehouseUC = new WarehouseUC();
                    warehouseUC.DataContext = new WarehouseViewModel(this); // Привязка ViewModel
                    CurrentView = warehouseUC;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при изменении CurrentView: {ex.Message}");
                }
            });

            ShowProductsCommand = new RelayCommand(o =>
            {
                try
                {
                    var productsUC = new ProductsUC(this);
                    productsUC.DataContext = new ProductsViewModel(this); // Привязка ViewModel
                    CurrentView = productsUC;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при изменении CurrentView: {ex.Message}");
                }
            });
            ShowSupplyCommand = new RelayCommand(o =>
            {
                try
                {
                    var supplyRequestsUC = new SupplyRequestsUC(this);
                    supplyRequestsUC.DataContext = _supplyRequestViewModel;  // Устанавливаем контекст данных
                    CurrentView = supplyRequestsUC;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при изменении CurrentView: {ex.Message}");
                }
            });
            ShowProductionCommand = new RelayCommand(o =>
            {
                try
                {
                    var productionUC = new ProductionsUC();
                    productionUC.DataContext = new ProductionViewModel(_context, _stockService);
                    CurrentView = productionUC;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при изменении CurrentView: {ex.Message}");
                }
            });

            ShowSaleCommand = new RelayCommand(o =>
            {
                try
                {
                    var saleUC = new SalesUC();
                    saleUC.DataContext = new SalesViewModel(_context, this, _closeAction, _parentWindow);
                    CurrentView = saleUC;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при изменении CurrentView: {ex.Message}");
                }
            });
            ShowReportsCommand = new RelayCommand(o =>
            {
                try
                {
                    var reportUC = new ReportUC(this);
                    reportUC.DataContext = new ReportViewModel(this); // или передай контекст
                    CurrentView = reportUC;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при отображении ReportUC: {ex.Message}");
                }
            });

            CreateReportRPP = new RelayCommand(_ => OpenRPPWindow());
        }

        private void Logout(object obj)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Создаем новое окно авторизации
                var loginWindow = new LoginWindow();
                loginWindow.Show();

                // Закрываем главное окно
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is MainWindow)
                    {
                        window.Close();
                        break;
                    }
                }
            });
        }

        private void SetDailyNorm(object parameter)
        {
            var inputDialog = new DailyNormDialog();  // Окно для ввода дневной нормы
            if (inputDialog.ShowDialog() == true)
            {
                DailyNorm = inputDialog.InputNorm;
                // Сохранить норму в базе данных, если необходимо
                // Например, App.DbContext.DailyNorms.Add(new DailyNorm { Norm = DailyNorm });
            }
        }

        private int _dailyProduction;
        public int DailyProduction
        {
            get => _dailyProduction;
            set
            {
                if (_dailyProduction != value)
                {
                    _dailyProduction = value;
                    OnPropertyChanged();
                    UpdateCompletion(); // ← обязательно!
                }
            }
        }

        private void CheckDailyNorm()
        {
            var todayProduction = DailyProduction;
            var todayNorm = DailyNorm;

            if (todayProduction < todayNorm)
            {
                MessageBox.Show($"Норма на день не выполнена. Продукция произведена только на {todayProduction} единиц, что на 20% меньше требуемой нормы ({todayNorm}).", "Оповещение", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Уменьшаем норму на 20% на следующий день
                DailyNorm = (int)(DailyNorm * 0.8);
            }
        }

        private int _dailySale;
        public int DailySale
        {
            get => _dailySale;
            set
            {
                if (_dailySale != value)
                {
                    _dailySale = value;
                    OnPropertyChanged();
                    UpdateCompletion(); // ← обязательно!
                }
            }
        }

        // Свойство для хранения дневной нормы
        private int _dailyNorm;

        private static int _cachedNorm;
        public int DailyNorm
        {
            get => _dailyNorm;
            set
            {
                if (_dailyNorm != value)
                {
                    _dailyNorm = value;
                    _cachedNorm = value; // сохраняем в static поле
                    OnPropertyChanged();
                    UpdateCompletion();
                    SaveConfig();
                }
            }
        }

        // Свойство для отображения процентов выполнения нормы
        private double _dailyCompletion;
        public double DailyCompletion
        {
            get => _dailyCompletion;
            set
            {
                double limitedValue = Math.Min(value, 100); // Ограничиваем значение максимум 100%
                if (Math.Abs(_dailyCompletion - limitedValue) > 0.01)
                {
                    _dailyCompletion = limitedValue;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DailyCompletionText)); // Обновляем текст
                }
            }
        }

        // Текст для отображения статуса выполнения
        private string _dailyCompletionText;
        public string DailyCompletionText => $"{DailyCompletion:F2}%";

        private void UpdateCompletion()
        {
            if (DailyNorm > 0)
            {
                // Процент выполнения нормы на основе произведенных изделий
                double productionProgress = (double)DailyProduction / DailyNorm * 100;

                // Процент выполнения нормы на основе проданных изделий
                double salesProgress = DailyProduction > 0 ? (double)DailySale / DailyProduction * 100 : 0;

                // Финальный процент выполнения нормы — это минимум из этих двух
                double completion = Math.Min(productionProgress, salesProgress);

                DailyCompletion = completion;
            }
            else
            {
                DailyCompletion = 0;
            }
        }

        // Команда для изменения дневной нормы
        public ICommand SetDailyNormCommand { get; }

        public void LoadSupplyRequests()
        {
            _supplyRequestViewModel.SupplyRequests = new ObservableCollection<SupplyRequest>(App.DbContext.SupplyRequests.ToList());
            OnPropertyChanged(nameof(_supplyRequestViewModel.SupplyRequests));
        }

        private void LoadDailyProduction()
        {
            var today = DateTime.Today;
            var productionCount = App.DbContext.Productions
                .Where(p => p.DateTimeStart >= today && p.DateTimeStart < today.AddDays(1)) // Фильтрация по текущему дню
                .Distinct()
                .Sum(p => p.ProductionsRawMaterialsMeasurementUnitRecipes
                    .Sum(pr => pr.CountProduct)); // Суммируем количество произведенных продуктов

            Console.WriteLine($"Произведено изделий за сегодня: {productionCount}");
            DailyProduction = productionCount;
        }

        private void LoadDailySale()
        {
            var today = DateTime.Today;
            var saleCount = App.DbContext.Sales
                .Where(p => p.DateTimeSale >= today && p.DateTimeSale < today.AddDays(1)) // Фильтрация по текущему дню
                .Distinct()
                .Sum(p => p.SaleProducts
                    .Sum(pr => pr.CountProductSale)); // Суммируем количество произведенных продуктов

            Console.WriteLine($"Произведено изделий за сегодня: {saleCount}");
            DailySale = saleCount;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            Console.WriteLine($"Свойство изменено: {prop}");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public class AppConfig
        {
            public int SavedDailyNorm { get; set; } = 0;
        }

        private readonly string _configPath = "/ДИПЛОМ/ARM_Proj/Backery_Automatize/App_Automatize_Backery/res/config.json";
        private AppConfig _config;

        private void LoadConfig()
        {
            if (File.Exists(_configPath))
            {
                string json = File.ReadAllText(_configPath);
                _config = JsonConvert.DeserializeObject<AppConfig>(json);
                DailyNorm = _config.SavedDailyNorm;
            }
            else
            {
                _config = new AppConfig();
            }
        }

        private void SaveConfig()
        {
            _config.SavedDailyNorm = DailyNorm;
            File.WriteAllText(_configPath, JsonConvert.SerializeObject(_config));
        }

        ///////////////////////////

        public ICommand CreateReportRPP { get; }

        private void OpenRPPWindow()
        {
            var window = new CreateRPPWindow
            {
                DataContext = new CreateRPPViewModel(this)
            };
            window.ShowDialog();
        }

        public void GenerateRPPReport(DateTime from, DateTime to)
        {
            var supplies = _context.SupplyRequests
                .Include(s => s.SupplyRequestsRawMaterials)
                .Where(s => s.SupplyRequestDate >= from && s.SupplyRequestDate <= to)
                .ToList();

            var sales = _context.Sales
                .Include(s => s.SaleProducts)
                .ThenInclude(sp => sp.Product)
                .Where(s => s.DateTimeSale >= from && s.DateTimeSale <= to)
                .ToList();

            var productions = _context.Productions
                .Include(p => p.ProductionsRawMaterialsMeasurementUnitRecipes)
                    .ThenInclude(pr => pr.RawMaterialMeasurementUnitRecipe)
                .Include(p => p.ProductionsRawMaterialsMeasurementUnitRecipes)
                    .ThenInclude(pr => pr.RawMaterialMeasurementUnitRecipe)
                        .ThenInclude(r => r.RawMaterial)
                        .ThenInclude(r => r.MeasurementUnit)
                .Include(p => p.ProductionsRawMaterialsMeasurementUnitRecipes)
                    .ThenInclude(pr => pr.RawMaterialMeasurementUnitRecipe)
                        .ThenInclude(r => r.Recipe)
                        .ThenInclude(r => r.Product)
                .Where(p => p.DateTimeStart >= from && p.DateTimeStart <= to)
                .ToList();

            // 1. Суммарные значения
            decimal totalExpenses = supplies.Sum(s => s.SupplyRequestsRawMaterials.Sum(rm => rm.SupplyCoastToMaterial));
            decimal totalIncome = sales.Sum(s => s.CoastSale ?? 0m);
            int totalProduced = productions
                .SelectMany(p => p.ProductionsRawMaterialsMeasurementUnitRecipes
                    .GroupBy(r => r.RawMaterialMeasurementUnitRecipe.Recipe.RecipeId)
                    .Select(g => g.First().CountProduct))
                .Sum();
            int totalSold = sales.Sum(s => s.SaleProducts.Sum(p => p.CountProductSale));
            int unsold = totalProduced - totalSold;

            // Чистая прибыль (с учётом "потери" из-за нереализованной продукции)
            decimal lostProfit = 0m;
            decimal netProfit = 0m;

            try
            {
                decimal unitCost = productions
                    .SelectMany(p => p.ProductionsRawMaterialsMeasurementUnitRecipes)
                    .Where(r => r?.RawMaterialMeasurementUnitRecipe?.Recipe?.Product != null)
                    .Select(r => r.RawMaterialMeasurementUnitRecipe.Recipe.Product.ProductCoast)
                    .FirstOrDefault();
                lostProfit = unitCost * unsold;
                netProfit = totalIncome - totalExpenses - lostProfit;
            }
            catch { }

            var conversions = _context.MeasurementConversions
                .ToList()
                .ToDictionary(c => (From: c.FromMeasureUnitId, To: c.ToMeasureUnitId), c => c.ConversionFactor);

            // Таблицы:
            var soldProductsGrouped = sales
                .SelectMany(s => s.SaleProducts)
                .GroupBy(sp => sp.Product)
                .Select(g => new
                {
                    Product = g.Key,
                    Count = g.Sum(p => p.CountProductSale),
                    Total = g.Sum(p => p.CountProductSale * (p.Product.ProductCoast))
                }).ToList();

            var producedProductsGrouped = productions
                .Select(p => new
                {
                    Product = p.ProductionsRawMaterialsMeasurementUnitRecipes
                        .First().RawMaterialMeasurementUnitRecipe.Recipe.Product,
                    Count = p.ProductionsRawMaterialsMeasurementUnitRecipes
                        .First().CountProduct,
                    Total = p.ProductionsRawMaterialsMeasurementUnitRecipes
                        .First().CountProduct *
                            p.ProductionsRawMaterialsMeasurementUnitRecipes
                            .First().RawMaterialMeasurementUnitRecipe.Recipe.Product.ProductCoast
                })
                .GroupBy(x => x.Product)
                .Select(g => new
                {
                    Product = g.Key,
                    Count = g.Sum(x => x.Count),
                    Total = g.Sum(x => x.Total)
                }).ToList();


            var unsoldProducts = producedProductsGrouped
                .GroupJoin(soldProductsGrouped,
                    prod => prod.Product.ProductId,
                    sold => sold.Product.ProductId,
                    (prod, soldGroup) =>
                    {
                        var sold = soldGroup.FirstOrDefault();
                        int soldCount = sold?.Count ?? 0;
                        int unsoldCount = prod.Count - soldCount;
                        decimal unsoldValue = unsoldCount * (prod.Product.ProductCoast);
                        return new
                        {
                            Product = prod.Product,
                            Count = unsoldCount,
                            Total = unsoldValue
                        };
                    })
                .Where(x => x.Count > 0)
                .ToList();

            // Даты
            var supplyDates = supplies.Select(s => s.SupplyRequestDate.Date);
            var saleDates = sales.Select(s => s.DateTimeSale.Date);
            var allDates = supplyDates.Union(saleDates).Distinct().OrderBy(d => d).ToList();

            var dataByDay = allDates.ToDictionary(
                date => date,
                date => (
                    Income: sales.Where(s => s.DateTimeSale.Date == date).Sum(s => s.CoastSale ?? 0m),
                    Expenses: supplies.Where(s => s.SupplyRequestDate.Date == date).Sum(s => s.SupplyRequestsRawMaterials.Sum(rm => rm.SupplyCoastToMaterial))
                )
            );

            // Генерация Word-отчета
            var reportsDirectory = @"D:\ДИПЛОМ\ARM_Proj\Backery_Automatize\App_Automatize_Backery\reports";
            Directory.CreateDirectory(reportsDirectory);
            var path = Path.Combine(reportsDirectory, $"RPP_Report_{from:yyyyMMdd}_{to:yyyyMMdd}.docx");

            using (var doc = DocX.Create(path))
            {
                doc.InsertParagraph($"📅 Отчёт по производству и продажам с {from:dd.MM.yyyy} по {to:dd.MM.yyyy}").FontSize(16).Bold().SpacingAfter(20);

                doc.InsertParagraph($"💰 Общий доход: {totalIncome:F2} руб.");
                doc.InsertParagraph($"💸 Общие расходы: {totalExpenses:F2} руб.");
                doc.InsertParagraph($"🧾 Чистая прибыль: {netProfit:F2} руб.");
                doc.InsertParagraph($"🏭 Произведено: {totalProduced} шт.");
                doc.InsertParagraph($"🛒 Продано: {totalSold} шт.");
                doc.InsertParagraph($"📦 Осталось на складе: {unsold} шт.");
                doc.InsertParagraph("");

                // Таблица: Проданные изделия
                doc.InsertParagraph("📊 Проданные изделия:").Bold();
                var soldTable = doc.AddTable(soldProductsGrouped.Count + 1, 3);
                soldTable.Design = TableDesign.LightShadingAccent1;
                soldTable.Rows[0].Cells[0].Paragraphs[0].Append("Изделие");
                soldTable.Rows[0].Cells[1].Paragraphs[0].Append("Количество");
                soldTable.Rows[0].Cells[2].Paragraphs[0].Append("Сумма");

                for (int i = 0; i < soldProductsGrouped.Count; i++)
                {
                    soldTable.Rows[i + 1].Cells[0].Paragraphs[0].Append(soldProductsGrouped[i].Product.ProductName);
                    soldTable.Rows[i + 1].Cells[1].Paragraphs[0].Append(soldProductsGrouped[i].Count.ToString());
                    soldTable.Rows[i + 1].Cells[2].Paragraphs[0].Append($"{soldProductsGrouped[i].Total:F2} руб.");
                }
                doc.InsertTable(soldTable);
                doc.InsertParagraph();

                // Таблица: Произведённые изделия
                doc.InsertParagraph("🏭 Произведённые изделия:").Bold();
                var producedTable = doc.AddTable(producedProductsGrouped.Count + 1, 3);
                producedTable.Design = TableDesign.LightList;
                producedTable.Rows[0].Cells[0].Paragraphs[0].Append("Изделие");
                producedTable.Rows[0].Cells[1].Paragraphs[0].Append("Количество");
                producedTable.Rows[0].Cells[2].Paragraphs[0].Append("Сумма");

                for (int i = 0; i < producedProductsGrouped.Count; i++)
                {
                    producedTable.Rows[i + 1].Cells[0].Paragraphs[0].Append(producedProductsGrouped[i].Product.ProductName);
                    producedTable.Rows[i + 1].Cells[1].Paragraphs[0].Append(producedProductsGrouped[i].Count.ToString());
                    producedTable.Rows[i + 1].Cells[2].Paragraphs[0].Append($"{producedProductsGrouped[i].Total:F2} руб.");
                }
                doc.InsertTable(producedTable);
                doc.InsertParagraph();

                // Таблица: Непроданные изделия
                doc.InsertParagraph("📦 Непроданные изделия:").Bold();
                var unsoldTable = doc.AddTable(unsoldProducts.Count + 1, 3);
                unsoldTable.Design = TableDesign.ColorfulGrid;
                unsoldTable.Rows[0].Cells[0].Paragraphs[0].Append("Изделие");
                unsoldTable.Rows[0].Cells[1].Paragraphs[0].Append("Количество");
                unsoldTable.Rows[0].Cells[2].Paragraphs[0].Append("Сумма");

                for (int i = 0; i < unsoldProducts.Count; i++)
                {
                    unsoldTable.Rows[i + 1].Cells[0].Paragraphs[0].Append(unsoldProducts[i].Product.ProductName);
                    unsoldTable.Rows[i + 1].Cells[1].Paragraphs[0].Append(unsoldProducts[i].Count.ToString());
                    unsoldTable.Rows[i + 1].Cells[2].Paragraphs[0].Append($"{unsoldProducts[i].Total:F2} руб.");
                }
                doc.InsertTable(unsoldTable);
                doc.InsertParagraph();

                // Диаграмма
                var chartImageStream = GenerateChart(dataByDay);
                var chartImage = doc.AddImage(chartImageStream);
                var picture = chartImage.CreatePicture();
                picture.Width = 500;
                picture.Height = 300;

                doc.InsertParagraph("📊 Диаграмма доходов и расходов по дням").SpacingBefore(20);
                doc.InsertParagraph().AppendPicture(picture);

                doc.Save();
            }

            // Сохранение отчета в БД
            var report = new Report
            {
                UserId = CurrentUser.UserId,
                ReportType = $"РПП за период с {from:dd.MM.yyyy} по {to:dd.MM.yyyy}",
                ReportDate = DateTime.Now
            };
            App.DbContext.Reports.Add(report);
            App.DbContext.SaveChanges();

            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии отчета: {ex.Message}");
            }

            MessageBox.Show($"Отчёт успешно сохранён:\n{path}", "Отчёт", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private MemoryStream GenerateChart(Dictionary<DateTime, (decimal Income, decimal Expenses)> dataByDate)
        {
            int width = 900;
            int height = 500;
            var bitmap = new Bitmap(width, height);
            var g = Graphics.FromImage(bitmap);
            g.Clear(System.Drawing.Color.White);

            var font = new System.Drawing.Font("Arial", 10);
            var brush = System.Drawing.Brushes.Black;
            var penIncome = new System.Drawing.Pen(System.Drawing.Color.Green, 2);
            var penExpenses = new System.Drawing.Pen(System.Drawing.Color.Red, 2);

            int marginLeft = 80;
            int marginRight = 40;
            int marginTop = 40;
            int marginBottom = 100;

            int chartWidth = width - marginLeft - marginRight;
            int chartHeight = height - marginTop - marginBottom;

            var dates = dataByDate.Keys.OrderBy(d => d).ToList();

            if (dates.Count < 2)
            {
                g.DrawString("Недостаточно данных для построения графика", font, brush, 100, height / 2);
            }
            else
            {
                decimal maxValue = Math.Max(1, dataByDate.Values.SelectMany(v => new[] { v.Income, v.Expenses }).Max());

                // Оси
                g.DrawLine(Pens.Black, marginLeft, marginTop, marginLeft, height - marginBottom); // ось Y
                g.DrawLine(Pens.Black, marginLeft, height - marginBottom, width - marginRight, height - marginBottom); // ось X

                // Подпись к оси Y (вертикальная, левее и выше)
                g.TranslateTransform(-2, height / 2); // Левее (5 вместо 20) и выше
                g.RotateTransform(-90);
                g.DrawString("Сумма, руб.", font, brush, 0, 0);
                g.ResetTransform();

                // Подпись к оси X
                g.DrawString("Дата", font, brush, width / 2 - 20, height - marginBottom + 80);

                // Деления и подписи по Y
                for (int i = 0; i <= 5; i++)
                {
                    decimal val = maxValue * i / 5;
                    int y = height - marginBottom - (int)(val / maxValue * chartHeight);
                    var valStr = val.ToString("0.##");
                    var valSize = g.MeasureString(valStr, font);
                    g.DrawString(valStr, font, brush, marginLeft - valSize.Width - 5, y - valSize.Height / 2);
                    g.DrawLine(Pens.LightGray, marginLeft, y, width - marginRight, y);
                }

                // Деления и подписи по X
                for (int i = 0; i < dates.Count; i++)
                {
                    int x = marginLeft + i * chartWidth / (dates.Count - 1);
                    g.DrawLine(Pens.Gray, x, height - marginBottom, x, height - marginBottom + 5);

                    // Повернутая подпись
                    g.TranslateTransform(x - 20, height - marginBottom + 10);
                    g.RotateTransform(45);
                    g.DrawString(dates[i].ToShortDateString(), font, brush, 0, 0);
                    g.ResetTransform();
                }

                // Точки графиков
                PointF[] incomePoints = dates.Select((d, i) => new PointF(
                    marginLeft + i * chartWidth / (dates.Count - 1),
                    height - marginBottom - (float)(dataByDate[d].Income / maxValue * chartHeight)
                )).ToArray();

                PointF[] expensePoints = dates.Select((d, i) => new PointF(
                    marginLeft + i * chartWidth / (dates.Count - 1),
                    height - marginBottom - (float)(dataByDate[d].Expenses / maxValue * chartHeight)
                )).ToArray();

                g.DrawLines(penIncome, incomePoints);
                g.DrawLines(penExpenses, expensePoints);

                // Легенда
                int legendX = width - 150;
                g.FillRectangle(System.Drawing.Brushes.Green, legendX, 20, 10, 10);
                g.DrawString("Доход", font, brush, legendX + 15, 18);
                g.FillRectangle(System.Drawing.Brushes.Red, legendX, 40, 10, 10);
                g.DrawString("Расход", font, brush, legendX + 15, 38);
            }

            var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
    }
}
