using App_Automatize_Backery.Models;
using App_Automatize_Backery.ViewModels.ProductionsVM.SupportVM;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows;

namespace App_Automatize_Backery.Helper
{
    public class StockService
    {
        private readonly MinBakeryDbContext _context;

        public StockService(MinBakeryDbContext context)
        {
            _context = context;
        }

        // ✅ Проверка наличия сырья с учетом конверсии
        internal bool CheckAvailabilityWithConversion(List<ProductionsRawMaterialsMeasurementUnitRecipe> requiredMaterials)
        {
            foreach (var required in requiredMaterials)
            {
                double totalAvailable = _context.RawMaterialsWarehousesProducts
                    .Where(r => r.RawMaterialId == required.RawMaterialMeasurementUnitRecipe.RawMaterialId)
                    .Sum(r => (double)r.RawMaterialCount);

                double requiredRawMaterialCount = ConvertToBaseUnit(
                    required.CountProduct * required.RawMaterialMeasurementUnitRecipe.CountRawMaterial,
                    required.RawMaterialMeasurementUnitRecipe.MeasurementUnitId
                );

                if (totalAvailable < requiredRawMaterialCount)
                {
                    MessageBox.Show($"Недостаточно сырья для ID {required.RawMaterialMeasurementUnitRecipe.RawMaterialId}: " +
                                    $"Требуется {requiredRawMaterialCount}, Доступно {totalAvailable}",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            return true;
        }

        // ✅ Списание сырья со склада с учетом количества сырья из рецепта
        public async Task DeductRawMaterialsAsync(List<ProductionsRawMaterialsMeasurementUnitRecipe> materials)
        {
            foreach (var required in materials)
            {
                var requiredAmount = ConvertToBaseUnit(
                    required.CountProduct * required.RawMaterialMeasurementUnitRecipe.CountRawMaterial,
                    required.RawMaterialMeasurementUnitRecipe.MeasurementUnitId
                );

                var stockItems = await _context.RawMaterialsWarehousesProducts
                    .Where(r => r.RawMaterialId == required.RawMaterialMeasurementUnitRecipe.RawMaterialId)
                    .OrderBy(r => r.RawMaterialCount)
                    .ToListAsync();

                double totalAvailable = stockItems.Sum(s => (double)s.RawMaterialCount);
                if (totalAvailable < requiredAmount)
                {
                    throw new InvalidOperationException($"Недостаточно сырья ID {required.RawMaterialMeasurementUnitRecipe.RawMaterialId}: " +
                                                        $"Требуется {requiredAmount}, Доступно {totalAvailable}");
                }

                foreach (var stock in stockItems)
                {
                    if (requiredAmount <= 0) break;

                    if (stock.RawMaterialCount >= requiredAmount)
                    {
                        stock.RawMaterialCount -= requiredAmount;
                        requiredAmount = 0;
                    }
                    else
                    {
                        requiredAmount -= stock.RawMaterialCount;
                        stock.RawMaterialCount = 0;
                    }

                    _context.RawMaterialsWarehousesProducts.Update(stock);
                }
            }

            await _context.SaveChangesAsync();
        }

        // ✅ Добавление готовой продукции на склад
        public async Task AddFinishedProductAsync(int productId, int quantity, int warehouseId)
        {
            var currentDateTime = DateTime.Now;

            // Найдем все записи для этого продукта на складе
            var existingProducts = await _context.RawMaterialsWarehousesProducts
                .Where(p => p.ProductId == productId && p.WarehouseId == warehouseId)
                .ToListAsync();

            // Попробуем найти записи с одинаковыми датой и временем
            var matchingProduct = existingProducts
                .FirstOrDefault(p => p.DateSupplyOrProduction.Date == currentDateTime.Date && p.DateSupplyOrProduction.Hour == currentDateTime.Hour && p.DateSupplyOrProduction.Minute == currentDateTime.Minute);

            if (matchingProduct != null)
            {
                // Если нашли подходящую запись с одинаковой датой и временем, прибавляем количество
                matchingProduct.RawMaterialCount += quantity;
                _context.RawMaterialsWarehousesProducts.Update(matchingProduct);
            }
            else
            {
                // Если такой записи нет, создаем новую с уникальной датой и временем
                await _context.RawMaterialsWarehousesProducts.AddAsync(new RawMaterialsWarehousesProduct
                {
                    ProductId = productId,
                    RawMaterialCount = quantity,
                    WarehouseId = warehouseId,
                    DateSupplyOrProduction = currentDateTime  // Записываем текущее время
                });
            }

            await _context.SaveChangesAsync();
        }

        // Конвертирует в базовые единицы (кг, л)
        private double ConvertToBaseUnit(double quantity, int fromUnitId)
        {
            return fromUnitId switch
            {
                2 => quantity / 1000, // г → кг
                4 => quantity / 1000, // мл → л
                _ => quantity // Если уже в базовой единице (1 - кг, 3 - л), либо шт.
            };
        }

        // ✅ Откат списания сырья
        public async Task RollbackProductionAsync(List<ProductionsRawMaterialsMeasurementUnitRecipe> materials)
        {
            if (!materials.Any()) return;

            // Откат списанного сырья
            foreach (var required in materials)
            {
                var warehouseIds = _context.RawMaterialsWarehousesProducts
                    .Where(p => p.RawMaterialId == required.RawMaterialMeasurementUnitRecipe.RawMaterial.RawMaterialId)
                    .Select(p => p.WarehouseId)
                    .Distinct()
                    .ToList();
                var warehouseId = warehouseIds.FirstOrDefault();
                var rollbackAmount = ConvertToBaseUnit(
                    required.CountProduct * required.RawMaterialMeasurementUnitRecipe.CountRawMaterial,
                    required.RawMaterialMeasurementUnitRecipe.MeasurementUnitId
                );

                var stockItems = _context.RawMaterialsWarehousesProducts
                    .Where(r => r.RawMaterialId == required.RawMaterialMeasurementUnitRecipe.RawMaterialId &&
                                r.WarehouseId == 2) // Учитываем склад
                    .OrderByDescending(r => r.RawMaterialCount)
                    .ToList();

                foreach (var stock in stockItems)
                {
                    if (rollbackAmount <= 0) break;

                    var toRestore = Math.Min(rollbackAmount, stock.RawMaterialCount);
                    stock.RawMaterialCount += toRestore;
                    rollbackAmount -= toRestore;
                }
            }

            // Удаление готовой продукции
            foreach (var required in materials)
            {
                var productId = required.RawMaterialMeasurementUnitRecipe.Recipe.ProductId;
                if (productId == null) continue;

                var productStockItems = _context.RawMaterialsWarehousesProducts
                    .Where(p => p.ProductId == productId && p.WarehouseId == 2) // Убираем только с нужного склада
                    .OrderBy(p => p.RawMaterialCount) // Убираем сначала с наименьших остатков
                    .ToList();

                var countToRemove = required.CountProduct;

                foreach (var stock in productStockItems)
                {
                    if (countToRemove <= 0) break;

                    var toRemove = Math.Min(stock.RawMaterialCount, countToRemove);
                    stock.RawMaterialCount -= toRemove;
                    countToRemove -= (int)toRemove;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateStockForProduction(List<ProductionsRawMaterialsMeasurementUnitRecipe> materials, Dictionary<int, int> productCounts)
        {
            Debug.WriteLine("[STOCK] Начинаем обновление склада...");

            if (!CheckAvailabilityWithConversion(materials))
            {
                Debug.WriteLine("[FAIL] Недостаточно сырья!");
                return false;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await DeductRawMaterialsAsync(materials);
                foreach (var (productId, count) in productCounts)
                {
                    await AddFinishedProductAsync(productId, count, warehouseId: 1);
                }

                await transaction.CommitAsync();
                Debug.WriteLine("[SUCCESS] Склад обновлен!");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Ошибка обновления склада: {ex.Message}");
                await transaction.RollbackAsync();
                return false;
            }
        }
    }
}
