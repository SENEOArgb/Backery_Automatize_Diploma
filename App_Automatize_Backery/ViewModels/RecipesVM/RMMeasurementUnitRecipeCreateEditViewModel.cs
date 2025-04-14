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

namespace App_Automatize_Backery.ViewModels.RecipesVM
{
    internal class RMMeasurementUnitRecipeCreateEditViewModel : INotifyPropertyChanged
    {
        public RawMaterialMeasurementUnitRecipe Ingredient { get; set; }

        public ObservableCollection<RawMaterial> RawMaterials { get; set; }
        public ObservableCollection<MeasurementUnit> MeasurementUnits { get; set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public Window Window { get; set; }

        public RMMeasurementUnitRecipeCreateEditViewModel(RawMaterialMeasurementUnitRecipe ingredient)
        {
            Ingredient = ingredient;

            RawMaterials = new ObservableCollection<RawMaterial>(App.DbContext.RawMaterials.Where(r => r.StatusRawMaterial != "В архиве").ToList());
            MeasurementUnits = new ObservableCollection<MeasurementUnit>(App.DbContext.MeasurementUnits.ToList());

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void Save(object obj)
        {
            if (Ingredient.RawMaterial == null || Ingredient.MeasurementUnit == null || Ingredient.CountRawMaterial <= 0)
            {
                MessageBox.Show("Заполните все поля корректно!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Window.DialogResult = true;
            Window.Close();
        }

        private void Cancel(object obj)
        {
            Window.DialogResult = false;
            Window.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
