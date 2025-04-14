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
using App_Automatize_Backery.View.Windows.SupplyRequest;
using App_Automatize_Backery.View.Windows.Recipes;

namespace App_Automatize_Backery.ViewModels.RecipesVM
{
    internal class RecipeCreateEditViewModel : INotifyPropertyChanged
    {
        private readonly MainViewModel _mainVM;

        private Recipe _editingRecipe;
        private bool _isEditMode;

        public ObservableCollection<Product> Products { get; set; }
        public string RecipeDescription
        {
            get => _recipeDescription;
            set { _recipeDescription = value; 
                OnPropertyChanged(nameof(RecipeDescription)); }
        }
        private string _recipeDescription;

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set { _selectedProduct = value; 
                OnPropertyChanged(nameof(SelectedProduct)); }
        }
        private Product _selectedProduct;

        public ICommand SaveCommand { get; }


        public RecipeCreateEditViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;
            SaveCommand = new RelayCommand(Save);

            LoadProducts();
        }

        public RecipeCreateEditViewModel(MainViewModel mainVM, Recipe recipeToEdit)
    : this(mainVM)
        {
            _editingRecipe = recipeToEdit;
            _isEditMode = true;

            RecipeDescription = _editingRecipe.RecipeDescription;
            SelectedProduct = Products.FirstOrDefault(p => p.ProductId == _editingRecipe.ProductId);
        }

        private void LoadProducts()
        {
            Products = new ObservableCollection<Product>(
                App.DbContext.Products
                    .Where(p => p.StatusProduct != "В архиве")
                    .ToList());

            OnPropertyChanged(nameof(Products));
        }


        private void Save(object obj)
        {
            if (SelectedProduct == null || string.IsNullOrWhiteSpace(RecipeDescription))
            {
                MessageBox.Show("Заполните все поля.");
                return;
            }

            if (_isEditMode)
            {
                _editingRecipe.ProductId = SelectedProduct.ProductId;
                _editingRecipe.RecipeDescription = RecipeDescription;
                App.DbContext.SaveChanges();
            }
            else
            {
                var recipe = new Recipe
                {
                    ProductId = SelectedProduct.ProductId,
                    RecipeDescription = RecipeDescription,
                    StatusRecipe = "Активна"
                };

                App.DbContext.Recipes.Add(recipe);
                App.DbContext.SaveChanges();
            }

            if (obj is Window window)
                window.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
