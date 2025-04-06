using App_Automatize_Backery.Helper;
using App_Automatize_Backery.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.Collections.Specialized;
using System.ComponentModel;
using App_Automatize_Backery.View.Windows.Productions;
using App_Automatize_Backery.View.UserControls_Pages_.WorkerManufactury;
using App_Automatize_Backery.ViewModels.ProductionsVM.SupportVM;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Diagnostics;

namespace App_Automatize_Backery.ViewModels.ProductionsVM
{
    public class ProductionViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Production> Productions { get; set; }
        public Production SelectedProduction { get; set; }


        private Production _currentProductionDetails;
        public Sale sale { get; set; }
        public object CurrentDetailsView { get; set; }

        internal readonly StockService _stockService;
        public ICommand CreateProductionCommand { get; }
        public ICommand ViewProductionDetailsCommand { get; }
        public ICommand DeleteProductionCommand { get; }
        public ICommand EditProductionCommand { get; }

        public MinBakeryDbContext _context;
        public ProductionViewModel(MinBakeryDbContext context, StockService stockService)
        {
            _context = context;
            _stockService = stockService;
            Productions = new ObservableCollection<Production>(GetAllProductions());
            CreateProductionCommand = new RelayCommand(_ => OpenCreateProduction());
            ViewProductionDetailsCommand = new RelayCommand(p => ShowDetails(p as Production));
            DeleteProductionCommand = new RelayCommand(p => DeleteProduction(p as Production));
            //EditProductionCommand = new RelayCommand(EditProduction, CanEditProduction);
        }

        private void OpenCreateProduction()
        {

            var window = new ProductionCreateEditWindow
            {
                DataContext = new ProductionCreateViewModel(App.DbContext, _stockService) // Передаем новый контекст
            };

            Debug.WriteLine("Создание записи производства");
            if (window.ShowDialog() == true)
            {
                RefreshProductions();
            }
        }

        private bool CanEditProduction(object obj)
        {
            if (obj is not Production production) return false;
            return production.DateTimeEnd > DateTime.Now; // Блокируем редактирование после окончания
        }

        /*private void EditProduction(object obj)
        {
            if (obj is not Production production) return;

            Debug.WriteLine("Редактирование записи производства");

            if (production.DateTimeEnd <= DateTime.Now)
            {
                MessageBox.Show("Производство уже завершено и не может быть изменено!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Исправлено: используем переданный production, а не SelectedProduction
            var editWindow = new ProductionCreateEditWindow(_context, _stockService, production, );
            if (editWindow.ShowDialog() == true)
            {
                RefreshProductions(); // Перезагружаем список после редактирования
            }
        }*/

        private void ShowDetails(Production production)
        {
            if (production == null)
                return;

            if (_currentProductionDetails == production)
            {
                // Если повторно нажали на тот же элемент — скрываем
                CurrentDetailsView = null;
                _currentProductionDetails = null;
            }
            else
            {
                // Иначе отображаем детали
                CurrentDetailsView = new ProductionDetailsUC
                {
                    DataContext = new ProductionDetailsViewModel(production)
                };
                _currentProductionDetails = production;
            }

            OnPropertyChanged(nameof(CurrentDetailsView));
        }

        private async void DeleteProduction(Production production)
        {
            if (production == null) return;

            if (production.DateTimeEnd <= DateTime.Now)
            {
                MessageBox.Show("Производство уже завершено и не может быть удалено!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Вы уверены, что хотите удалить производство?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            try
            {
                using (var context = new MinBakeryDbContext())
                {
                    var productionCreateVM = new ProductionCreateViewModel(context, _stockService);
                    productionCreateVM.CancelScheduledProcessing(production.ProductionId);

                    bool wasProcessingScheduled = productionCreateVM.WasProcessingScheduled(production.ProductionId);

                    if (!wasProcessingScheduled)
                    {
                        var relatedRecords = context.ProductionsRawMaterialsMeasurementUnitRecipes
                            .Where(pr => pr.ProductionId == production.ProductionId)
                            .ToList();
                        context.ProductionsRawMaterialsMeasurementUnitRecipes.RemoveRange(relatedRecords);

                        context.Productions.Remove(production);
                        await context.SaveChangesAsync();
                        Productions.Remove(production);

                        MessageBox.Show("Производство успешно удалено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var productionMaterials = context.ProductionsRawMaterialsMeasurementUnitRecipes
                        .Where(pm => pm.ProductionId == production.ProductionId)
                        .ToList();

                    await _stockService.RollbackProductionAsync(productionMaterials);

                    context.ProductionsRawMaterialsMeasurementUnitRecipes.RemoveRange(productionMaterials);
                    context.Productions.Remove(production);

                    await context.SaveChangesAsync();

                    Productions.Remove(production);

                    MessageBox.Show("Производство успешно удалено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private ObservableCollection<Production> GetAllProductions()
        {
            using (var context = new MinBakeryDbContext()) // Создаём новый контекст для получения данных
            {
                return new ObservableCollection<Production>(context.Productions.ToList());
            }
        }

        private void RefreshProductions()
        {
            Productions = new ObservableCollection<Production>(GetAllProductions());
            OnPropertyChanged(nameof(Productions));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}