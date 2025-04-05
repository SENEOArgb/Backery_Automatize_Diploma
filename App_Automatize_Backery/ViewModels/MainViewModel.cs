using App_Automatize_Backery.Helper;
using App_Automatize_Backery.Models;
using App_Automatize_Backery.View;
using App_Automatize_Backery.View.UserControls_Pages_;
using App_Automatize_Backery.View.UserControls_Pages_.General_WorkerPages;
using App_Automatize_Backery.View.UserControls_Pages_.WorkerManufactury;
using App_Automatize_Backery.View.Windows;
using App_Automatize_Backery.ViewModels.ProductionsVM;
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
using System.Windows.Input;
using SupplyRequestsUC = App_Automatize_Backery.View.UserControls_Pages_.General_WorkerPages.SupplyRequestsUC;

namespace App_Automatize_Backery.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {

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
        public bool IsManager => CurrentUser?.UserRoleId == 1;  // Зав. производством
        public bool IsWorker => CurrentUser?.UserRoleId == 2;   // Сотрудник на производстве

        // Свойство для скрытия кнопки меню, если нет доступа
        public Visibility ManagerMenuVisibility => IsManager ? Visibility.Visible : Visibility.Collapsed;
        public Visibility WorkerMenuVisibility => IsWorker ? Visibility.Visible : Visibility.Collapsed;

        public User CurrentUser { get; set; }  // Новое свойство для текущего пользователя

        public ICommand ShowRawMaterialsCommand { get; }
        public ICommand ShowWarehouseCommand { get; }
        public ICommand ShowProductsCommand { get; }
        public ICommand ShowSupplyCommand { get; }
        public ICommand ShowUserCommand { get; }
        public ICommand ShowProductionCommand { get; }
        public ICommand ShowSaleCommand { get; }

        public ICommand LogoutCommand { get; }

        public MinBakeryDbContext _context;
        internal readonly StockService _stockService;

        public string RoleName => CurrentUser?.UserRole?.UserRoleName ?? "Неизвестно";  // Возвращаем имя роли

        private SupplyRequestViewModel _supplyRequestViewModel;
        private SupplyRequestWarehouseRMViewModel _supplyRequestRMViewModel;

        public ProductionViewModel ProductionVM { get; set; }

        public MainViewModel(User user)
        {
            LoadConfig();

            _context = App.DbContext;
            _stockService = new StockService(_context);

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
                    var rawMaterialsUC = new RawMaterialsUC();
                    rawMaterialsUC.DataContext = new RMViewModel(); // Привязка ViewModel
                    CurrentView = rawMaterialsUC;
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
                    warehouseUC.DataContext = new WarehouseViewModel(); // Привязка ViewModel
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
                    var productsUC = new ProductsUC();
                    productsUC.DataContext = new ProductsViewModel(); // Привязка ViewModel
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
                    saleUC.DataContext = new SalesViewModel(_context, this);
                    CurrentView = saleUC;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при изменении CurrentView: {ex.Message}");
                }
            });
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
                if (_dailyCompletion != value)
                {
                    _dailyCompletion = value;
                    OnPropertyChanged(nameof(DailyCompletion));
                }
            }
        }

        // Текст для отображения статуса выполнения
        private string _dailyCompletionText;
        public string DailyCompletionText
        {
            get => _dailyCompletionText;
            set
            {
                if (_dailyCompletionText != value)
                {
                    _dailyCompletionText = value;
                    OnPropertyChanged(nameof(DailyCompletionText));
                }
            }
        }

        private void UpdateCompletion()
        {
            if (DailyNorm > 0)
            {
                // Процент выполнения нормы на основе произведенных изделий
                double productionProgress = (double)DailyProduction / DailyNorm * 100;

                // Процент выполнения нормы на основе проданных изделий
                double salesProgress = DailyProduction > 0 ? (double)DailySale / DailyProduction * 100 : 0;

                // Финальный процент выполнения нормы — это среднее значение из этих двух
                double completion = Math.Min(productionProgress, salesProgress); // Не может быть больше 100%

                DailyCompletion = completion;
                DailyCompletionText = $"{DailyCompletion:F2}%";
            }
            else
            {
                DailyCompletion = 0;
                DailyCompletionText = "Норма не установлена";
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
    }
}
