﻿using App_Automatize_Backery.Helper;
using App_Automatize_Backery.Models;
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

        public ObservableCollection<Product> Products { get; set; }
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
        public ICommand DeleteProductCommand { get; }

        public MainViewModel _vmMain;

        public bool IsManager => _vmMain.CurrentUser?.UserRoleId == 2;  // Зав. производством
        public bool IsWorker => _vmMain.CurrentUser?.UserRoleId == 3;   // Сотрудник на производстве

        public bool IsTechnolog => _vmMain.CurrentUser?.UserRoleId == 1;

        // Свойство для скрытия кнопки меню, если нет доступа
        public Visibility ManagerMenuVisibility => IsManager ? Visibility.Visible : Visibility.Collapsed;
        public Visibility WorkerMenuVisibility => IsWorker ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TechnologistVisibility => IsTechnolog ? Visibility.Visible : Visibility.Hidden;

        public ProductsViewModel(MainViewModel mainViewModel)
        {
            _vmMain = mainViewModel;
            LoadProducts();
            CreateProductCommand = new RelayCommand(_ => OpenCreateEditWindow(null));
            EditProductCommand = new RelayCommand(_ => OpenCreateEditWindow(SelectedProduct), _ => SelectedProduct != null);
            DeleteProductCommand = new RelayCommand(_ => DeleteProduct(), _ => SelectedProduct != null);
        }

        private void LoadProducts()
        {
            Products = new ObservableCollection<Product>(App.DbContext.Products.Include(p => p.TypeProduct).ToList());
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

        private void DeleteProduct()
        {
            if (SelectedProduct == null) return;

            App.DbContext.Products.Remove(SelectedProduct);
            App.DbContext.SaveChanges();

            Products.Remove(SelectedProduct);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
