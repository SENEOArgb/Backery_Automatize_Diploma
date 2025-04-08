using App_Automatize_Backery.Helper;
using App_Automatize_Backery.Models;
using App_Automatize_Backery.View.UserControls_Pages_;
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
    internal class RecipesViewModel : INotifyPropertyChanged
    {
        private MainViewModel _mainViewModel;

        public ObservableCollection<Recipe> Recipes { get; set; }
        public Recipe SelectedRecipe { get; set; }

        public object CurrentContent { get; set; }

        public ICommand CreateRecipeCommand { get; }
        public ICommand DeleteRecipeCommand { get; }

        public ICommand EditRecipeCommand { get; }
        public ICommand ViewRecipeDetailsCommand { get; }

        public RecipesViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            Recipes = new ObservableCollection<Recipe>();

            CreateRecipeCommand = new RelayCommand(CreateRecipe);
            DeleteRecipeCommand = new RelayCommand(DeleteRecipe);
            EditRecipeCommand = new RelayCommand(EditRecipe);
            ViewRecipeDetailsCommand = new RelayCommand(ViewRecipeDetails);

            LoadRecipes();
        }

        private void EditRecipe(object obj)
        {
            if (obj is Recipe recipe)
            {
                var window = new RecipeCreateEditWindow();
                window.DataContext = new RecipeCreateEditViewModel(_mainViewModel, recipe);
                window.ShowDialog();
                LoadRecipes();
            }
        }

        private async void LoadRecipes()
        {
            var recipes = await App.DbContext.Recipes
                .Include(r => r.Product)
                .Include(r => r.RawMaterialMeasurementUnitRecipes)
                    .ThenInclude(rm => rm.RawMaterial)
                .Include(r => r.RawMaterialMeasurementUnitRecipes)
                    .ThenInclude(rm => rm.MeasurementUnit)
                .ToListAsync();

            Recipes.Clear();
            foreach (var recipe in recipes)
                Recipes.Add(recipe);
        }

        private void CreateRecipe(object obj)
        {
            var window = new RecipeCreateEditWindow();
            window.DataContext = new RecipeCreateEditViewModel(_mainViewModel);
            window.ShowDialog();
            LoadRecipes();
        }

        private void DeleteRecipe(object obj)
        {
            if (obj is Recipe recipe)
            {
                App.DbContext.Recipes.Remove(recipe);
                App.DbContext.SaveChanges();
                Recipes.Remove(recipe);
            }
        }

        private void ViewRecipeDetails(object obj)
        {
            if (obj is Recipe recipe)
            {
                var vm = new RMMeasurementUnitRecipeViewModel(recipe, _mainViewModel);
                CurrentContent = new RMMeasurementUnitRecipeUC { DataContext = vm };
                OnPropertyChanged(nameof(CurrentContent));
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
