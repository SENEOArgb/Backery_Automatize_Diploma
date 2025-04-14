using App_Automatize_Backery.Helper;
using App_Automatize_Backery.Models;
using App_Automatize_Backery.View.UserControls_Pages_;
using App_Automatize_Backery.View.UserControls_Pages_.TechnologPages;
using App_Automatize_Backery.ViewModels.ProductSupportVM;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace App_Automatize_Backery.ViewModels
{
    public class ProductsViewModel : INotifyPropertyChanged
    {
        private Product _selectedProduct;
        private bool _showArchive;
        public ObservableCollection<Product> Products { get; set; } = new();
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested(); // Обновляет доступность кнопок
            }
        }

        public ICommand CreateProductCommand { get; }
        public ICommand EditProductCommand { get; }
        public ICommand ArchiveProductCommand { get; }

        public ICommand RestoreCommand { get; }

        public ICommand ShowArchiveCommand { get; }

        public MainViewModel _vmMain;

        public bool IsManager => _vmMain.CurrentUser?.UserRoleId == 2;  // Зав. производством
        public bool IsWorker => _vmMain.CurrentUser?.UserRoleId == 3;   // Сотрудник на производстве

        public bool IsTechnolog => _vmMain.CurrentUser?.UserRoleId == 1;

        // Свойство для скрытия кнопки меню, если нет доступа
        public Visibility ManagerMenuVisibility => IsManager ? Visibility.Visible : Visibility.Collapsed;
        public Visibility WorkerMenuVisibility => IsWorker ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TechnologistVisibility => IsTechnolog ? Visibility.Visible : Visibility.Hidden;


        public bool IsArchiveVisible => _showArchive;
        public ProductsViewModel(MainViewModel mainViewModel)
        {
            _vmMain = mainViewModel;
            CreateProductCommand = new RelayCommand(_ => OpenCreateEditWindow(null));
            EditProductCommand = new RelayCommand(_ => OpenCreateEditWindow(SelectedProduct), _ => SelectedProduct != null);
            ArchiveProductCommand = new RelayCommand(param => ArchiveProduct(param as Product), param => param is Product);
            RestoreCommand = new RelayCommand(param => RestoreProduct(param as Product), param => param is Product);
            ShowArchiveCommand = new RelayCommand(_ => ToggleArchive());
            LoadProducts();
        }

        private void LoadProducts()
        {
            SelectedProduct = null;
            Products.Clear();

            var query = App.DbContext.Products
                .Include(p => p.TypeProduct)
                .AsQueryable();

            if (!_showArchive)
                query = query.Where(rm => rm.StatusProduct == "Активна");
            else
                query = query.Where(rm => rm.StatusProduct == "В архиве");

            foreach (var product in query.ToList())
                Products.Add(product);

            OnPropertyChanged(nameof(Products));
        }

        private void OpenCreateEditWindow(Product product)
        {
            var window = new ProductCreateEditWindow
            {
                DataContext = new ProductCreateEditViewModel(product ?? new Product())
            };

            if (window.ShowDialog() == true)
            {
                LoadProducts();
            }
        }

        private void ArchiveProduct(Product product)
        {
            if (product == null) return;

            var result = MessageBox.Show(
                "Вы уверены, что хотите архивировать этот продукт?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                product.StatusProduct = "В архиве";
                App.DbContext.Update(product);
                App.DbContext.SaveChanges();
                LoadProducts();
            }
        }

        private void RestoreProduct(Product product)
        {
            if (product == null) return;

            var result = MessageBox.Show("Вы уверены, что хотите восстановить в список активных эту номенклатуру?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            product.StatusProduct = "Активна";
            App.DbContext.Update(product);
            App.DbContext.SaveChanges();
            LoadProducts();
        }

        private void ToggleArchive()
        {
            _showArchive = !_showArchive;
            LoadProducts();
            OnPropertyChanged(nameof(IsArchiveVisible));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
