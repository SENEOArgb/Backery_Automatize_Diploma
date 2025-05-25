using App_Automatize_Backery.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace App_Automatize_Backery.Helper
{
    public class ProductsDisposalService
    {
        private readonly MinBakeryDbContext _context;

        public ProductsDisposalService(MinBakeryDbContext context)
        {
            _context = context;
        }

        public async Task DisposeExpiredProductsAsync()
        {
            try
            {
                var expirationDate = DateTime.Today;
                var expiredProducts = _context.RawMaterialsWarehousesProducts
                    .Include(p => p.Product)
                    .Where(p => p.ProductId != null && p.DateSupplyOrProduction.Date < expirationDate)
                    .ToList();

                if (expiredProducts.Any())
                {
                    var names = string.Join("\n", expiredProducts.Select(p => $"{p.Product.ProductName} - {p.RawMaterialCount} шт."));
                    _context.RawMaterialsWarehousesProducts.RemoveRange(expiredProducts);
                    await _context.SaveChangesAsync();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Утилизированы изделия:\n{names}", "Утилизация изделий", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Ошибка утилизации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }
    }
}
