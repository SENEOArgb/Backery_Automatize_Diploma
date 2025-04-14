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

namespace App_Automatize_Backery.ViewModels.Новая_папка
{
    internal class RawMaterialCreateEditViewModel : INotifyPropertyChanged
    {
        private RawMaterial _rawMaterial;
        public RawMaterial RawMaterial
        {
            get => _rawMaterial;
            set
            {
                _rawMaterial = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<MeasurementUnit> _measurementUnits;
        public ObservableCollection<MeasurementUnit> MeasurementUnits
        {
            get => _measurementUnits;
            set
            {
                _measurementUnits = value;
                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public bool IsEditMode { get; }

        public RawMaterialCreateEditViewModel(RawMaterial rawMaterial)
        {
            IsEditMode = rawMaterial.RawMaterialId != 0;
            RawMaterial = rawMaterial;

            // Загружаем список единиц измерения
            MeasurementUnits = new ObservableCollection<MeasurementUnit>(App.DbContext.MeasurementUnits.ToList());

            SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(RawMaterial.RawMaterialName) &&
                   RawMaterial.RawMaterialCoast > 0 &&
                   RawMaterial.MeasurementUnitId > 0 &&
                   RawMaterial.ShelfLifeDays > 0;
        }

        private void Save()
        {
            if (!IsEditMode)
            {
                // Проверка на дубликат названия
                var duplicate = App.DbContext.RawMaterials
                    .Any(r => r.RawMaterialName.ToLower() == RawMaterial.RawMaterialName.ToLower() && r.StatusRawMaterial != "Удалена");

                if (duplicate)
                {
                    System.Windows.MessageBox.Show("Сырьё с таким названием уже существует.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                RawMaterial.StatusRawMaterial = "Активна";
                App.DbContext.RawMaterials.Add(RawMaterial);
            }
            else
            {
                var existingMaterial = App.DbContext.RawMaterials.Find(RawMaterial.RawMaterialId);
                if (existingMaterial != null)
                {
                    existingMaterial.RawMaterialName = RawMaterial.RawMaterialName;
                    existingMaterial.ShelfLifeDays = RawMaterial.ShelfLifeDays;
                    existingMaterial.RawMaterialCoast = RawMaterial.RawMaterialCoast;
                    existingMaterial.MeasurementUnitId = RawMaterial.MeasurementUnitId;
                    App.DbContext.RawMaterials.Update(existingMaterial);
                }
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
