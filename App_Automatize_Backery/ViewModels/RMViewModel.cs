﻿using App_Automatize_Backery.Helper;
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
using System.Windows.Input;

namespace App_Automatize_Backery.ViewModels
{
    public class RMViewModel : INotifyPropertyChanged
    {
        private RawMaterial _selectedRawMaterial;

        public ObservableCollection<RawMaterial> RawMaterials { get; set; }
        public RawMaterial SelectedRawMaterial
        {
            get => _selectedRawMaterial;
            set
            {
                _selectedRawMaterial = value;
                OnPropertyChanged();
            }
        }

        public ICommand CreateCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public RMViewModel()
        {
            LoadRawMaterials();
            CreateCommand = new RelayCommand(_ => OpenCreateEditWindow(null));
            EditCommand = new RelayCommand(_ => OpenCreateEditWindow(SelectedRawMaterial), _ => SelectedRawMaterial != null);
            DeleteCommand = new RelayCommand(_ => DeleteRawMaterial(), _ => SelectedRawMaterial != null);
        }

        private void LoadRawMaterials()
        {
            RawMaterials = new ObservableCollection<RawMaterial>(App.DbContext.RawMaterials.Include(mu => mu.MeasurementUnit).ToList());
            OnPropertyChanged(nameof(RawMaterials));
        }

        private void OpenCreateEditWindow(RawMaterial rawMaterial)
        {
            var window = new RawMaterialCreateEditWindow
            {
                DataContext = new RawMaterialCreateEditViewModel(rawMaterial ?? new RawMaterial())
            };

            if (window.ShowDialog() == true)
            {
                LoadRawMaterials();
            }
        }

        private void DeleteRawMaterial()
        {
            if (SelectedRawMaterial == null) return;

            App.DbContext.RawMaterials.Remove(SelectedRawMaterial);
            App.DbContext.SaveChanges();

            RawMaterials.Remove(SelectedRawMaterial);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
