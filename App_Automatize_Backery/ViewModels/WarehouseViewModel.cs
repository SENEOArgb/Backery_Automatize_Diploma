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
using System.Windows.Controls;
using System.Windows.Data;
using App_Automatize_Backery.View.SupportControls;
using App_Automatize_Backery.ViewModels.SupportViewModel;

namespace App_Automatize_Backery.ViewModels
{
    public class WarehouseViewModel : INotifyPropertyChanged
    {
        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged(nameof(CurrentView));
            }
        }

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                _selectedTabIndex = value;
                OnPropertyChanged(nameof(SelectedTabIndex));
                UpdateIndicatorPosition();
            }
        }

        private string _selectedIndicatorWidth;
        public string SelectedIndicatorWidth
        {
            get => _selectedIndicatorWidth;
            set
            {
                _selectedIndicatorWidth = value;
                OnPropertyChanged(nameof(SelectedIndicatorWidth));
            }
        }

        private string _selectedIndicatorMargin;
        public string SelectedIndicatorMargin
        {
            get => _selectedIndicatorMargin;
            set
            {
                _selectedIndicatorMargin = value;
                OnPropertyChanged(nameof(SelectedIndicatorMargin));
            }
        }

        public ICommand ShowProductsCommand { get; }
        public ICommand ShowRawMaterialsCommand { get; }

        private readonly PWarehouseViewModel _productsViewModel;
        private readonly RMWarehouseViewModel _rawMaterialsViewModel;

        public WarehouseViewModel()
        {
            _productsViewModel = new PWarehouseViewModel();
            _rawMaterialsViewModel = new RMWarehouseViewModel();

            ShowProductsCommand = new RelayCommand(o => ShowProductsView());
            ShowRawMaterialsCommand = new RelayCommand(o => ShowRawMaterialsView());

            // Устанавливаем начальный вид
            ShowProductsView();
        }

        private void ShowProductsView()
        {
            SelectedTabIndex = 0;
            CurrentView = new ProductsWarehouseUC { DataContext = _productsViewModel };
        }

        private void ShowRawMaterialsView()
        {
            SelectedTabIndex = 1;
            CurrentView = new RawMaterialsWarehouseUC { DataContext = _rawMaterialsViewModel };
        }

        private void UpdateIndicatorPosition()
        {
            if (SelectedTabIndex == 0)
            {
                SelectedIndicatorWidth = "100";
                SelectedIndicatorMargin = "0,0,0,0";
            }
            else
            {
                SelectedIndicatorWidth = "100";
                SelectedIndicatorMargin = "110,0,0,0"; // Смещаем вправо
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
