using App_Automatize_Backery.Helper;
using App_Automatize_Backery.Models;
using App_Automatize_Backery.View.Windows.SupplyRequest;
using App_Automatize_Backery.ViewModels.SupportViewModel.SupplyRequest;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace App_Automatize_Backery.ViewModels
{
    internal class SupplyRequestWarehouseRMViewModel : INotifyPropertyChanged
    {
        private MainViewModel _mainViewModel;
        internal SupplyRequest _supplyRequest;
        private ObservableCollection<SupplyRequestsRawMaterial> _supplyRequestsRawMaterials;

        public ObservableCollection<SupplyRequestsRawMaterial> SupplyRequestsRawMaterials
        {
            get { return _supplyRequestsRawMaterials; }
            set
            {
                _supplyRequestsRawMaterials = value;
                OnPropertyChanged(nameof(SupplyRequestsRawMaterials));
            }
        }

        public SupplyRequestsRawMaterial SelectedRawMaterial { get; set; }
        public ICommand AddRawMaterialToRequestCommand { get; set; }
        public ICommand DeleteRawMaterialCommand { get; set; }
        public ICommand EditRawMaterialCommand { get; set; }

        public bool IsManager => _mainViewModel.CurrentUser?.UserRoleId == 2;
        public bool IsWorker => _mainViewModel.CurrentUser?.UserRoleId == 3;

        public bool IsRequestAccepted => _supplyRequest.StatusId == 2;

        public Visibility ManagerVisibility => IsManager ? Visibility.Visible : Visibility.Collapsed;

        public bool CanAddRawMaterial => IsManager && !IsRequestAccepted;
        public bool CanEditRawMaterial => IsManager && !IsRequestAccepted;
        public bool CanDeleteRawMaterial => IsManager && !IsRequestAccepted;

        public SupplyRequestWarehouseRMViewModel(SupplyRequest supplyRequest, MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _supplyRequest = supplyRequest;
            SupplyRequestsRawMaterials = new ObservableCollection<SupplyRequestsRawMaterial>();

            LoadSupplyRequestRawMaterials();

            AddRawMaterialToRequestCommand = new RelayCommand(OpenAddRawMaterialWindow);
            DeleteRawMaterialCommand = new RelayCommand(DeleteRawMaterial);
            EditRawMaterialCommand = new RelayCommand(EditRawMaterial);
        }

        internal async Task LoadSupplyRequestRawMaterials()
        {
            try
            {
                var rawMaterials = await App.DbContext.SupplyRequestsRawMaterials
                    .Include(srm => srm.RawMaterial)
                    .Include(srm => srm.Warehouse)
                    .Where(srm => srm.SupplyRequestId == _supplyRequest.SupplyRequestId)
                    .ToListAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    SupplyRequestsRawMaterials.Clear();
                    foreach (var item in rawMaterials)
                    {
                        item.SupplyCoastToMaterial = item.CountRawMaterial * item.RawMaterial.RawMaterialCoast;
                        SupplyRequestsRawMaterials.Add(item);
                    }
                });

                OnPropertyChanged(nameof(SupplyRequestsRawMaterials));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки сырья: {ex}");
            }
        }

        private void OpenAddRawMaterialWindow(object obj)
        {
            if (CanAddRawMaterial)
            {
                var window = new CreateRawMaterialToRequestWindow();
                var viewModel = new CreateRMToRequestViewModel(_mainViewModel, _supplyRequest, this);
                window.DataContext = viewModel;
                window.ShowDialog();
                UpdateTotalSalary();// Update list after adding
                LoadSupplyRequestRawMaterials();
            }
        }

        private async void DeleteRawMaterial(object obj)
        {
            if (obj is SupplyRequestsRawMaterial rawMaterialToDelete && CanDeleteRawMaterial)
            {
                var result = MessageBox.Show($"Вы действительно хотите удалить \"{rawMaterialToDelete.RawMaterial.RawMaterialName}\"?",
                                             "Подтверждение удаления",
                                             MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    App.DbContext.SupplyRequestsRawMaterials.Remove(rawMaterialToDelete);
                    await App.DbContext.SaveChangesAsync();
                    UpdateTotalSalary();
                    await LoadSupplyRequestRawMaterials(); // Async update
                }
            }
        }

        private async void EditRawMaterial(object obj)
        {
            if (obj is SupplyRequestsRawMaterial rawMaterialToEdit && CanEditRawMaterial)
            {
                var window = new CreateRawMaterialToRequestWindow();
                window.DataContext = new CreateRMToRequestViewModel(_mainViewModel, _supplyRequest, this, rawMaterialToEdit);
                window.ShowDialog();
                UpdateTotalSalary();
                await LoadSupplyRequestRawMaterials(); // Async update
            }
        }

        internal void UpdateTotalSalary()
        {
            var supplyRequest = App.DbContext.SupplyRequests
                .Include(r => r.SupplyRequestsRawMaterials)
                .ThenInclude(rm => rm.RawMaterial)
                .FirstOrDefault(r => r.SupplyRequestId == _supplyRequest.SupplyRequestId);

            if (supplyRequest != null)
            {
                supplyRequest.TotalSalary = supplyRequest.SupplyRequestsRawMaterials
                    .Sum(rm => rm.CountRawMaterial * rm.RawMaterial.RawMaterialCoast);

                App.DbContext.SaveChangesAsync();
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
