using App_Automatize_Backery.Helper;
using App_Automatize_Backery.Models;
using App_Automatize_Backery.View.UserControls_Pages_.TechnologPages;
using App_Automatize_Backery.ViewModels.Новая_папка;
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
    public class RMViewModel : INotifyPropertyChanged
    {
        private RawMaterial _selectedRawMaterial;
        private bool _showArchive;

        public ObservableCollection<RawMaterial> RawMaterials { get; set; } = new();
        public RawMaterial SelectedRawMaterial
        {
            get => _selectedRawMaterial;
            set
            {
                _selectedRawMaterial = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel _vmMain;

        // Команды
        public ICommand CreateCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ArchiveCommand { get; }
        public ICommand RestoreCommand { get; }
        public ICommand ShowArchiveCommand { get; }

        // Роли
        public bool IsManager => _vmMain.CurrentUser?.UserRoleId == 2;  // Зав. производством
        public bool IsWorker => _vmMain.CurrentUser?.UserRoleId == 3;   // Сотрудник на производстве
        public bool IsTechnolog => _vmMain.CurrentUser?.UserRoleId == 1;

        // Видимость
        public Visibility ManagerMenuVisibility => IsManager ? Visibility.Visible : Visibility.Collapsed;
        public Visibility WorkerMenuVisibility => IsWorker ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TechnologistVisibility => IsTechnolog ? Visibility.Visible : Visibility.Hidden;

        public bool IsArchiveVisible => _showArchive;

        public RMViewModel(MainViewModel mainViewModel)
        {
            _vmMain = mainViewModel;

            CreateCommand = new RelayCommand(_ => OpenCreateEditWindow(null));
            EditCommand = new RelayCommand(_ => OpenCreateEditWindow(SelectedRawMaterial), _ => SelectedRawMaterial != null);
            DeleteCommand = new RelayCommand(_ => DeleteRawMaterial(), _ => SelectedRawMaterial != null);

            ArchiveCommand = new RelayCommand(param => ArchiveRawMaterial(param as RawMaterial), param => param is RawMaterial);
            RestoreCommand = new RelayCommand(param => RestoreRawMaterial(param as RawMaterial), param => param is RawMaterial);
            ShowArchiveCommand = new RelayCommand(_ => ToggleArchive());

            LoadRawMaterials();
        }

        private void LoadRawMaterials()
        {
            SelectedRawMaterial = null;
            RawMaterials.Clear();

            var query = App.DbContext.RawMaterials
                .Include(rm => rm.MeasurementUnit)
                .AsQueryable();

            if (!_showArchive)
                query = query.Where(rm => rm.StatusRawMaterial == "Активна");
            else
                query = query.Where(rm => rm.StatusRawMaterial == "В архиве");

            foreach (var rawMaterial in query.ToList())
                RawMaterials.Add(rawMaterial);

            OnPropertyChanged(nameof(RawMaterials));
        }

        private void OpenCreateEditWindow(RawMaterial rawMaterial)
        {
            var window = new RawMaterialCreateEditWindow
            {
                DataContext = new RawMaterialCreateEditViewModel(rawMaterial ?? new RawMaterial())
            };

            if (window.ShowDialog() == true)
                LoadRawMaterials();
        }

        private void DeleteRawMaterial()
        {
            if (SelectedRawMaterial == null) return;

            var result = MessageBox.Show("Вы уверены, что хотите удалить эту номенклатуру?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            App.DbContext.RawMaterials.Remove(SelectedRawMaterial);
            App.DbContext.SaveChanges();

            RawMaterials.Remove(SelectedRawMaterial);
        }

        private void ArchiveRawMaterial(RawMaterial rawMaterial)
        {
            if (rawMaterial == null) return;

            var result = MessageBox.Show("Вы уверены, что хотите архивировать эту номенклатуру?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            rawMaterial.StatusRawMaterial = "В архиве";
            App.DbContext.Update(rawMaterial);
            App.DbContext.SaveChanges();
            LoadRawMaterials();
        }

        private void RestoreRawMaterial(RawMaterial rawMaterial)
        {
            if (rawMaterial == null) return;

            var result = MessageBox.Show("Вы уверены, что хотите восстановить в список активных эту номенклатуру?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            rawMaterial.StatusRawMaterial = "Активна";
            App.DbContext.Update(rawMaterial);
            App.DbContext.SaveChanges();
            LoadRawMaterials();
        }

        private void ToggleArchive()
        {
            _showArchive = !_showArchive;
            LoadRawMaterials();
            OnPropertyChanged(nameof(IsArchiveVisible));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
