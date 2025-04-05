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
using App_Automatize_Backery.ViewModels.SupportViewModel.SupplyRequest;
using App_Automatize_Backery.View.UserControls_Pages_;
using System.Windows.Data;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using App_Automatize_Backery.View.UserControls_Pages_.General_WorkerPages;
using Azure.Core;

namespace App_Automatize_Backery.ViewModels
{
    public class SupplyRequestViewModel : INotifyPropertyChanged
    {
        public bool IsManager => _mainViewModel.CurrentUser?.UserRoleId == 2;  // Зав. производством
        public bool IsWorker => _mainViewModel.CurrentUser?.UserRoleId == 3;   // Сотрудник на производстве

        // Свойство для скрытия кнопки меню, если нет доступа
        public Visibility ManagerVisibility => IsManager ? Visibility.Visible : Visibility.Collapsed;
        public Visibility WorkerVisibility => IsWorker ? Visibility.Visible : Visibility.Collapsed;
        public Visibility AcceptButtonVisibility => IsWorker ? Visibility.Visible : Visibility.Collapsed;

        private MainViewModel _mainViewModel;

        private SupplyRequest _selectedRequest;
        public SupplyRequest SelectedRequest
        {
            get { return _selectedRequest; }
            set
            {
                _selectedRequest = value;
                OnPropertyChanged(nameof(SelectedRequest));
            }
        }

        public ICommand CreateSupplyRequestCommand { get; set; }
        public ICommand AddRawMaterialToRequestCommand { get; set; }
        public ICommand DeleteSupplyRequestCommand { get; set; }
        public ICommand ViewSupplyRequestMaterialsCommand { get; set; }
        public ICommand EditSupplyRequestCommand { get; set; }
        public ICommand AcceptRequestCommand { get; }

        private ObservableCollection<SupplyRequest> _supplyRequests;
        public ObservableCollection<SupplyRequest> SupplyRequests
        {
            get { return _supplyRequests; }
            set
            {
                _supplyRequests = value;
                OnPropertyChanged(nameof(SupplyRequests));
            }
        }

        private ObservableCollection<SupplyRequestsRawMaterial> _supplyRequestsRawMaterial;
        public ObservableCollection<SupplyRequestsRawMaterial> SupplyRequestsRawMaterial
        {
            get { return _supplyRequestsRawMaterial; }
            set
            {
                _supplyRequestsRawMaterial = value;
                OnPropertyChanged(nameof(SupplyRequestsRawMaterial));
            }
        }

        private object _currentContent;
        public object CurrentContent
        {
            get { return _currentContent; }
            set
            {
                _currentContent = value;
                OnPropertyChanged(nameof(CurrentContent));
            }
        }

        public SupplyRequestViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            SupplyRequests = new ObservableCollection<SupplyRequest>();

            LoadSupplyRequestsAsync();

            CreateSupplyRequestCommand = new RelayCommand(CreateSupplyRequestForm);
            DeleteSupplyRequestCommand = new RelayCommand(DeleteSupplyRequest);
            ViewSupplyRequestMaterialsCommand = new RelayCommand(ViewSupplyRequestMaterials);
            EditSupplyRequestCommand = new RelayCommand(EditSupplyRequest);
            AcceptRequestCommand = new RelayCommand(AcceptRequest);

            Debug.WriteLine("AcceptRequestCommand initialized");
        }

        private async Task LoadSupplyRequestsAsync()
        {
            try
            {
                var requests = await App.DbContext.SupplyRequests
                    .Include(sr => sr.Status)
                    .Include(sr => sr.User)
                    .Include(sr => sr.SupplyRequestsRawMaterials)
                    .ToListAsync();

                SupplyRequests.Clear();

                foreach (var request in requests)
                {
                    request.TotalSalary = request.SupplyRequestsRawMaterials
                        .Sum(rm => rm.SupplyCoastToMaterial); // если Price nullable
                    SupplyRequests.Add(request);
                }

                Debug.WriteLine($"Загружено заявок: {SupplyRequests.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки заявок: {ex}");
            }
        }

        private void CreateSupplyRequestForm(object obj)
        {
            var window = new CreateSupplyRequestWindow();
            window.DataContext = new CreateSupplyRequestViewModel(_mainViewModel);
            window.ShowDialog();
        }

        private void EditSupplyRequest(object obj)
        {
            if (obj is SupplyRequest requestToEdit)
            {
                var window = new CreateSupplyRequestWindow();
                window.DataContext = new CreateSupplyRequestViewModel(_mainViewModel, requestToEdit); // Передаем заявку
                window.ShowDialog();
            }
        }

        private async void DeleteSupplyRequest(object obj)
        {
            if (obj is SupplyRequest requestToDelete)
            {
                var result = MessageBox.Show(
                    $"Вы действительно хотите удалить заявку от {requestToDelete.SupplyRequestDate:d}?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        App.DbContext.SupplyRequests.Remove(requestToDelete);
                        await App.DbContext.SaveChangesAsync();
                        await LoadSupplyRequestsAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        private async void AcceptRequest(object obj)
        {
            if (obj is SupplyRequest request)
            {
                if (request.StatusId == 2) // Если заявка уже принята
                {
                    return;  // Прерываем выполнение, если заявка уже принята
                }

                var result = MessageBox.Show("Вы уверены, что хотите принять заявку?",
                                             "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await AcceptSupplyRequest(request);
                }
            }
        }

        private async Task AcceptSupplyRequest(SupplyRequest request)
        {
            var materials = await App.DbContext.SupplyRequestsRawMaterials
                .Where(m => m.SupplyRequestId == request.SupplyRequestId)
                .ToListAsync();  // Получаем все материалы заявки

            if (!materials.Any())
            {
                return;
            }

            foreach (var material in materials)
            {
                // Загружаем все продукты на клиентскую сторону
                var existingMaterial = App.DbContext.RawMaterialsWarehousesProducts
                    .AsEnumerable()  // Переводим на клиентскую сторону
                    .Where(wp => wp.WarehouseId == material.WarehouseId
                                 && wp.RawMaterialId == material.RawMaterialId
                                 && wp.DateSupplyOrProduction == request.SupplyRequestDate)  // Фильтруем по дате на клиентской стороне
                    .FirstOrDefault();

                if (existingMaterial != null)
                {
                    existingMaterial.RawMaterialCount += material.CountRawMaterial;  // Обновляем количество для этой партии
                    request.TotalSalary += material.CountRawMaterial * material.CountRawMaterial;
                }
                else
                {
                    // Если материал не существует для этой партии, создаем новый
                    App.DbContext.RawMaterialsWarehousesProducts.Add(new RawMaterialsWarehousesProduct
                    {
                        WarehouseId = material.WarehouseId,
                        RawMaterialId = material.RawMaterialId,
                        RawMaterialCount = material.CountRawMaterial,
                        DateSupplyOrProduction = request.SupplyRequestDate  // Указываем дату поставки для новой партии
                    });
                }
            }

            // Обновление статуса заявки на "Принята"
            var requestToUpdate = await App.DbContext.SupplyRequests.FindAsync(request.SupplyRequestId);
            if (requestToUpdate != null)
            {
                requestToUpdate.StatusId = 2; // Статус "Принята"
            }

            await App.DbContext.SaveChangesAsync();
            await LoadSupplyRequestsAsync(); // Перезагружаем заявки
        }

        private void ViewSupplyRequestMaterials(object obj)
        {
            if (obj is SupplyRequest selectedRequest)
            {
                // Если уже открыт UserControl для этой же заявки — скрываем
                if (CurrentContent is SupplyRequestsWarehousesRM currentView &&
                    currentView.DataContext is SupplyRequestWarehouseRMViewModel currentViewModel &&
                    currentViewModel._supplyRequest.SupplyRequestId == selectedRequest.SupplyRequestId)
                {
                    CurrentContent = null;
                }
                else
                {
                    // Открываем новый UserControl
                    CurrentContent = new SupplyRequestsWarehousesRM
                    {
                        DataContext = new SupplyRequestWarehouseRMViewModel(selectedRequest, _mainViewModel)
                    };
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
