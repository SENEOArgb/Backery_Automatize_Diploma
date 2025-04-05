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
using System.Runtime.CompilerServices;

namespace App_Automatize_Backery.ViewModels.ProductionsVM
{
    public class ProductionDetailsViewModel : INotifyPropertyChanged
    {
        private readonly Production _production;
        public Production Production => _production;

        private ObservableCollection<ProductionsRawMaterialsMeasurementUnitRecipe> _productionIngredients;
        public ObservableCollection<ProductionsRawMaterialsMeasurementUnitRecipe> ProductionIngredients
        {
            get => _productionIngredients;
            set
            {
                _productionIngredients = value;
                OnPropertyChanged(nameof(ProductionIngredients));
            }
        }

        private ObservableCollection<ProductionProductInfo> _producedProducts;
        public ObservableCollection<ProductionProductInfo> ProducedProducts
        {
            get => _producedProducts;
            set
            {
                _producedProducts = value;
                OnPropertyChanged(nameof(ProducedProducts));
            }
        }

        public ProductionDetailsViewModel(Production production)
        {
            _production = production;
            LoadIngredients();
            LoadProducedProducts();
        }

        private void LoadIngredients()
        {
            using var context = new MinBakeryDbContext();
            var ingredients = context.ProductionsRawMaterialsMeasurementUnitRecipes
                .Where(p => p.ProductionId == _production.ProductionId)
                .ToList();

            ProductionIngredients = new ObservableCollection<ProductionsRawMaterialsMeasurementUnitRecipe>(ingredients);
        }

        private void LoadProducedProducts()
        {
            using var context = new MinBakeryDbContext();

            var products = context.ProductionsRawMaterialsMeasurementUnitRecipes
                .Where(p => p.ProductionId == _production.ProductionId)
                .GroupBy(pr => pr.RawMaterialMeasurementUnitRecipe.RecipeId) // Группировка по рецепту
                .Select(group => new ProductionProductInfo
                {
                    ProductName = context.Recipes
                        .Where(r => r.RecipeId == group.Key)
                        .Select(r => r.Product.ProductName)
                        .FirstOrDefault(),
                    CountProduct = group.First().CountProduct // Берем количество из первой записи
                })
                .ToList();

            ProducedProducts = new ObservableCollection<ProductionProductInfo>(products);
        }


        /*private void RemoveIngredient(object parameter)
        {
            if (parameter is not ProductionsRawMaterialsMeasurementUnitRecipe ingredient) return;

            if (MessageBox.Show("Удалить этот ингредиент?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                using var context = new MinBakeryDbContext();
                var item = context.ProductionsRawMaterialsMeasurementUnitRecipes.FirstOrDefault(p => p.Id == ingredient.Id);
                if (item != null)
                {
                    context.ProductionsRawMaterialsMeasurementUnitRecipes.Remove(item);
                    context.SaveChanges();
                    ProductionIngredients.Remove(ingredient);
                }
            }
        }*/

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public class ProductionProductInfo
        {
            public string ProductName { get; set; }  // Название продукта
            public int CountProduct { get; set; }    // Количество произведённого продукта
        }


    }
}
