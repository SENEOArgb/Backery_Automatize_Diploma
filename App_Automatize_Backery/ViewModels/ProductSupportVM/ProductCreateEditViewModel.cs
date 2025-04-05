using App_Automatize_Backery.Helper;
using App_Automatize_Backery.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace App_Automatize_Backery.ViewModels.ProductSupportVM
{
    internal class ProductCreateEditViewModel : INotifyPropertyChanged
    {
        private Product _product;
        public Product Product
        {
            get => _product;
            set
            {
                _product = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<TypesProduct> TypeProducts { get; set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public bool IsEditMode { get; }

        public ProductCreateEditViewModel(Product product)
        {
            IsEditMode = product.ProductId != 0;
            Product = product;

            LoadTypeProducts();

            SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        private void LoadTypeProducts()
        {
            TypeProducts = new ObservableCollection<TypesProduct>(App.DbContext.TypesProducts.ToList());
            OnPropertyChanged(nameof(TypeProducts));
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Product.ProductName) && Product.ProductCoast > 0;
        }

        private void Save()
        {
            if (IsEditMode)
            {
                var existingProduct = App.DbContext.Products.Find(Product.ProductId);
                if (existingProduct != null)
                {
                    existingProduct.ProductName = Product.ProductName;
                    existingProduct.TypeProductId = Product.TypeProductId;
                    existingProduct.ProductCoast = Product.ProductCoast;
                }
                App.DbContext.Products.Update(existingProduct);
            }
            else
            {
                App.DbContext.Products.Add(Product);
            }

            App.DbContext.SaveChanges();
            CloseWindow(true);
        }

        private void Cancel()
        {
            CloseWindow(false);
        }

        private void CloseWindow(bool dialogResult)
        {
            foreach (var window in System.Windows.Application.Current.Windows)
            {
                if (window is System.Windows.Window w && w.DataContext == this)
                {
                    w.DialogResult = dialogResult;
                    w.Close();
                    break;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
