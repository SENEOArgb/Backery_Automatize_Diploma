using App_Automatize_Backery.Helper;
using App_Automatize_Backery.Models;
using App_Automatize_Backery.View.Windows.Recipes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace App_Automatize_Backery.ViewModels.RecipesVM
{
    internal class RMMeasurementUnitRecipeViewModel : INotifyPropertyChanged
    {
        private readonly MainViewModel _mainVM;
        private readonly Recipe _recipe;

        public ObservableCollection<RawMaterialMeasurementUnitRecipe> Ingredients { get; set; }

        public RawMaterialMeasurementUnitRecipe SelectedIngredient { get; set; }

        public ICommand AddIngredientCommand { get; }
        public ICommand EditIngredientCommand { get; }
        public ICommand DeleteIngredientCommand { get; }

        public RMMeasurementUnitRecipeViewModel(Recipe recipe, MainViewModel mainVM)
        {
            _mainVM = mainVM;
            _recipe = recipe;

            Ingredients = new ObservableCollection<RawMaterialMeasurementUnitRecipe>(_recipe.RawMaterialMeasurementUnitRecipes);

            AddIngredientCommand = new RelayCommand(AddIngredient);
            EditIngredientCommand = new RelayCommand(EditIngredient);
            DeleteIngredientCommand = new RelayCommand(DeleteIngredient);
        }

        private void AddIngredient(object obj)
        {
            var newIngredient = new RawMaterialMeasurementUnitRecipe { RecipeId = _recipe.RecipeId };

            var window = new RecipeIngridientCreateEditWindow(newIngredient);
            if (window.ShowDialog() == true)
            {
                App.DbContext.RawMaterialMeasurementUnitRecipes.Add(newIngredient);
                App.DbContext.SaveChanges();
                Ingredients.Add(newIngredient);
            }
        }

        private void EditIngredient(object obj)
        {
            if (obj is RawMaterialMeasurementUnitRecipe ingredient)
            {
                // Загружаем навигационные свойства, если они не подгружены
                if (ingredient.RawMaterial == null)
                {
                    ingredient.RawMaterial = App.DbContext.RawMaterials
                        .FirstOrDefault(r => r.RawMaterialId == ingredient.RawMaterialId);
                }

                if (ingredient.MeasurementUnit == null)
                {
                    ingredient.MeasurementUnit = App.DbContext.MeasurementUnits
                        .FirstOrDefault(m => m.MeasurementUnitId == ingredient.MeasurementUnitId);
                }

                var window = new RecipeIngridientCreateEditWindow(ingredient);
                if (window.ShowDialog() == true)
                {
                    App.DbContext.SaveChanges();
                    RefreshIngredients();
                }
            }
        }

        private void DeleteIngredient(object obj)
        {
            if (obj is RawMaterialMeasurementUnitRecipe ingredient)
            {
                App.DbContext.RawMaterialMeasurementUnitRecipes.Remove(ingredient);
                App.DbContext.SaveChanges();
                Ingredients.Remove(ingredient);
            }
        }

        private void RefreshIngredients()
        {
            Ingredients.Clear();
            var freshList = App.DbContext.RawMaterialMeasurementUnitRecipes
                .Include(x => x.RawMaterial)
                .Include(x => x.MeasurementUnit)
                .Where(x => x.RecipeId == _recipe.RecipeId)
                .ToList();

            foreach (var ing in freshList)
                Ingredients.Add(ing);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
