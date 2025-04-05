using App_Automatize_Backery.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App_Automatize_Backery.ViewModels.SalesVM
{
    public class SaleProductViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<SaleProduct> SaleProducts { get; set; } = new();

        private readonly MinBakeryDbContext _context; // Добавляем поле для хранения контекста

        public SaleProductViewModel(Sale sale, MinBakeryDbContext context)
        {
            _context = context; // Запоминаем переданный контекст
            LoadSaleProducts(sale);
        }

        private void LoadSaleProducts(Sale sale)
        {
            SaleProducts.Clear();
            var products = _context.SaleProducts
                                   .Where(sp => sp.SaleId == sale.SaleId)
                                   .Include(sp => sp.Product) // Загружаем связанную сущность
                                   .ToList();

            foreach (var product in products)
                SaleProducts.Add(product);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
