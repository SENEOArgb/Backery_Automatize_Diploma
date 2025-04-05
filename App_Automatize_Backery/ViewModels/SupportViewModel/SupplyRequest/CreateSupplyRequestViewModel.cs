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
using System.Diagnostics;

namespace App_Automatize_Backery.ViewModels.SupportViewModel.SupplyRequest
{
    internal class CreateSupplyRequestViewModel : INotifyPropertyChanged
    {
        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                _isEditing = value;
                OnPropertyChanged(nameof(IsEditing));
            }
        }

        public DateTime SupplyRequestDate { get; set; } = DateTime.Today; // По умолчанию - сегодня
        public Status SelectedStatus { get; set; }
        public ObservableCollection<Status> Statuses { get; set; }
        public MainViewModel VmMain { get; set; }
        public ICommand SaveSupplyRequestCommand { get; set; }

        private Models.SupplyRequest EditingRequest { get; set; }

        public CreateSupplyRequestViewModel(MainViewModel mainViewModel, Models.SupplyRequest existingRequest)
        {
            VmMain = mainViewModel;
            SaveSupplyRequestCommand = new RelayCommand(SaveEditedSupplyRequest);
            LoadStatuses();

            // Заполняем поля данными заявки
            SupplyRequestDate = existingRequest.SupplyRequestDate;
            SelectedStatus = existingRequest.Status;
            EditingRequest = existingRequest;
            IsEditing = true;
        }

        public CreateSupplyRequestViewModel(MainViewModel mainViewModel)
        {
            VmMain = mainViewModel;
            SaveSupplyRequestCommand = new RelayCommand(SaveSupplyRequest);
            LoadStatuses();

            IsEditing = false;

            // Устанавливаем статус по умолчанию (ID = 1, "В ожидании")
            SelectedStatus = Statuses.FirstOrDefault(s => s.StatusId == 1);
        }

        private void LoadStatuses()
        {
            var statuses = App.DbContext.Statuses.Where(s => s.StatusId == 1).ToList();
            Statuses = new ObservableCollection<Status>(statuses);
        }

        private void SaveEditedSupplyRequest(object obj)
        {
            if (EditingRequest != null)
            {
                EditingRequest.SupplyRequestDate = SupplyRequestDate;
                EditingRequest.Status = SelectedStatus;

                try
                {
                    App.DbContext.SupplyRequests.Update(EditingRequest);
                    App.DbContext.SaveChanges();
                    VmMain.LoadSupplyRequests();
                    MessageBox.Show("Заявка обновлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    CloseWindow();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении заявки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveSupplyRequest(object obj)
        {
            var currentUser = VmMain.CurrentUser;
            if (currentUser == null)
            {
                MessageBox.Show("Пользователь не авторизован!");
                return;
            }

            var newRequest = new Models.SupplyRequest
            {
                SupplyRequestDate = SupplyRequestDate,
                StatusId = SelectedStatus?.StatusId ?? 0,
                UserId = currentUser.UserId
            };

            try
            {
                App.DbContext.SupplyRequests.Add(newRequest);
                App.DbContext.SaveChanges();
                VmMain.LoadSupplyRequests();

                MessageBox.Show("Заявка успешно создана!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseWindow();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении заявки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseWindow()
        {
            var window = Application.Current.Windows.OfType<CreateSupplyRequestWindow>().FirstOrDefault();
            window?.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
