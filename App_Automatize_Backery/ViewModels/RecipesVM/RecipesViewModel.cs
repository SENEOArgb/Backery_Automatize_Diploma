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
using System.Windows;
using System.Windows.Input;

namespace App_Automatize_Backery.ViewModels.RecipesVM
{
    internal class RecipesViewModel : INotifyPropertyChanged
    {
        private MainViewModel _mainViewModel;
        private bool _showArchive;

        public ObservableCollection<Recipe> Recipes { get; set; } = new();
        public Recipe SelectedRecipe { get; set; }

        public object CurrentContent { get; set; }

        public ICommand CreateRecipeCommand { get; }
        public ICommand DeleteRecipeCommand { get; }

        public ICommand EditRecipeCommand { get; }
        public ICommand ViewRecipeDetailsCommand { get; }

        public ICommand ArchiveCommand { get; }
        public ICommand RestoreCommand { get; }
        public ICommand ShowArchiveCommand { get; }

        public bool IsManager => _mainViewModel.CurrentUser?.UserRoleId == 2;  // Зав. производством
        public bool IsWorker => _mainViewModel.CurrentUser?.UserRoleId == 3;   // Сотрудник на производстве
        public bool IsTechnolog => _mainViewModel.CurrentUser?.UserRoleId == 1;

        // Видимость
        public Visibility ManagerMenuVisibility => IsManager ? Visibility.Visible : Visibility.Collapsed;
        public Visibility WorkerMenuVisibility => IsWorker ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TechnologistVisibility => IsTechnolog ? Visibility.Visible : Visibility.Hidden;

        public bool IsArchiveVisible => _showArchive;
        public RecipesViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            CreateRecipeCommand = new RelayCommand(CreateRecipe);
            DeleteRecipeCommand = new RelayCommand(DeleteRecipe);
            EditRecipeCommand = new RelayCommand(EditRecipe);
            ViewRecipeDetailsCommand = new RelayCommand(ViewRecipeDetails);

            ArchiveCommand = new RelayCommand(param => ArchiveRecipe(param as Recipe), param => param is Recipe);
            RestoreCommand = new RelayCommand(param => RestoreRecipe(param as Recipe), param => param is Recipe);
            ShowArchiveCommand = new RelayCommand(_ => ToggleArchive());

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
            SelectedRecipe = null;
            Recipes.Clear();

            var query = App.DbContext.Recipes
                .Include(r => r.Product)
                .Include(r => r.RawMaterialMeasurementUnitRecipes)
                    .ThenInclude(rm => rm.RawMaterial)
                .Include(r => r.RawMaterialMeasurementUnitRecipes)
                    .ThenInclude(rm => rm.MeasurementUnit)
                .AsQueryable();

            if (!_showArchive)
                query = query.Where(r => r.StatusRecipe == "Активна");
            else
                query = query.Where(r => r.StatusRecipe == "В архиве");

            foreach (var recipe in query.ToList())
                Recipes.Add(recipe);

            OnPropertyChanged(nameof(Recipes));
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
                var result = MessageBox.Show("Вы уверены, что хотите удалить эту номенклатуру?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;

                App.DbContext.Recipes.Remove(recipe);
                App.DbContext.SaveChanges();
                Recipes.Remove(recipe);
            }
        }

        private void ArchiveRecipe(Recipe recipe)
        {
            if (recipe == null) return;

            var result = MessageBox.Show("Вы уверены, что хотите архивировать эту номенклатуру?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            recipe.StatusRecipe = "В архиве";
            App.DbContext.Update(recipe);
            App.DbContext.SaveChanges();
            LoadRecipes();
        }

        private void RestoreRecipe(Recipe recipe)
        {
            if (recipe == null) return;

            var result = MessageBox.Show("Вы хотите вернуть номенклатуру рецепта в список активных?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            recipe.StatusRecipe = "Активна";
            App.DbContext.Update(recipe);
            App.DbContext.SaveChanges();
            LoadRecipes();
        }

        private void ToggleArchive()
        {
            _showArchive = !_showArchive;
            LoadRecipes();
            OnPropertyChanged(nameof(IsArchiveVisible));
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
