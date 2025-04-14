using App_Automatize_Backery.Helper;
using App_Automatize_Backery.Models;
using App_Automatize_Backery.View.Windows.SupplyRequest;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static System.Net.Mime.MediaTypeNames;

namespace App_Automatize_Backery.ViewModels.SupportViewModel.SupplyRequest
{
    internal class CreateRMToRequestViewModel : INotifyPropertyChanged
    {
        private SupplyRequestsRawMaterial _editingRawMaterial;
        internal Models.SupplyRequest _currentRequest;

        private SupplyRequestWarehouseRMViewModel SupplyRequestWarehouseRMViewModel;

        public MainViewModel VmMain { get; set; }
        public ObservableCollection<RawMaterial> RawMaterials { get; set; }
        public RawMaterial SelectedRawMaterial { get; set; }
        public int CountRawMaterial { get; set; }
        public decimal SupplyCoastToMaterial { get; set; }

        public ICommand SaveRawMaterialCommand { get; set; }

        public CreateRMToRequestViewModel(MainViewModel mainViewModel, Models.SupplyRequest supplyRequest, SupplyRequestWarehouseRMViewModel supplyRequestWarehouseRMViewModel, SupplyRequestsRawMaterial editingRawMaterial = null)
        {
            VmMain = mainViewModel;
            _currentRequest = supplyRequest;
            SupplyRequestWarehouseRMViewModel = supplyRequestWarehouseRMViewModel;
            _editingRawMaterial = editingRawMaterial;

            RawMaterials = new ObservableCollection<RawMaterial>(App.DbContext.RawMaterials.Where(p => p.StatusRawMaterial != "В архиве").ToList());

            if (_editingRawMaterial != null)
            {
                // Заполняем поля данными редактируемого сырья
                SelectedRawMaterial = RawMaterials.FirstOrDefault(rm => rm.RawMaterialId == _editingRawMaterial.RawMaterialId);
                CountRawMaterial = _editingRawMaterial.CountRawMaterial;
                SupplyCoastToMaterial = _editingRawMaterial.SupplyCoastToMaterial;
            }

            SaveRawMaterialCommand = new RelayCommand(SaveRawMaterial);
        }

        private void SaveRawMaterial(object obj)
        {
            // Валидация: сырьё выбрано
            if (SelectedRawMaterial == null)
            {
                MessageBox.Show("Пожалуйста, выберите сырьё.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Валидация: количество > 0
            if (CountRawMaterial <= 0)
            {
                MessageBox.Show("Количество сырья должно быть больше 0.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedRawMaterial != null)
            {
                decimal totalCountSupply = 0;
                // Поиск существующей записи сырья в заявке
                var existingRawMaterial = App.DbContext.SupplyRequestsRawMaterials
                    .FirstOrDefault(rm => rm.SupplyRequestId == _currentRequest.SupplyRequestId &&
                                          rm.RawMaterialId == SelectedRawMaterial.RawMaterialId);

                if (existingRawMaterial != null)
                {
                    // Если такое сырье уже есть в заявке → обновляем количество и стоимость
                    existingRawMaterial.CountRawMaterial = CountRawMaterial;
                    existingRawMaterial.SupplyCoastToMaterial = existingRawMaterial.RawMaterial.RawMaterialCoast * CountRawMaterial;
                    App.DbContext.SupplyRequestsRawMaterials.Update(existingRawMaterial);
                    totalCountSupply = existingRawMaterial.SupplyCoastToMaterial;
                }
                else
                {
                    // Если такого сырья нет → создаем новую запись
                    var newRawMaterial = new SupplyRequestsRawMaterial
                    {
                        SupplyRequestId = _currentRequest.SupplyRequestId,
                        WarehouseId = 2,
                        RawMaterialId = SelectedRawMaterial.RawMaterialId,
                        CountRawMaterial = CountRawMaterial,
                        SupplyCoastToMaterial = SelectedRawMaterial.RawMaterialCoast * CountRawMaterial
                    };
                    App.DbContext.SupplyRequestsRawMaterials.Add(newRawMaterial);
                    totalCountSupply += newRawMaterial.SupplyCoastToMaterial;
                }
                _currentRequest.TotalSalary += totalCountSupply;
                App.DbContext.SupplyRequests.Update(_currentRequest);

                // Сохраняем изменения в БД
                App.DbContext.SaveChanges();

                // Закрываем окно
                var window = System.Windows.Application.Current.Windows.OfType<CreateRawMaterialToRequestWindow>().FirstOrDefault();
                window?.Close();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
