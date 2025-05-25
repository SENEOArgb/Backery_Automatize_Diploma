using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Xceed.Document.NET;
using Xceed.Words.NET;
using App_Automatize_Backery.Models;
using Microsoft.EntityFrameworkCore;
using Image = Xceed.Document.NET.Image;
using SkiaSharp;
using App_Automatize_Backery.ViewModels;

namespace App_Automatize_Backery.Helper
{
    class RppReportService
    {
        private readonly MinBakeryDbContext _context;

        public RppReportService(MinBakeryDbContext context)
        {
            _context = context;
        }

        // Метод для генерации графика
        public MemoryStream GenerateChart(Dictionary<DateTime, (decimal Income, decimal Expenses)> dataByDate)
        {
            int width = 900;
            int height = 500;
            var bitmap = new Bitmap(width, height);
            var g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);

            System.Drawing.Font font = new System.Drawing.Font("Arial", 10);
            var brush = Brushes.Black;
            var penIncome = new Pen(Color.Green, 2);
            var penExpenses = new Pen(Color.Red, 2);

            int marginLeft = 80;
            int marginRight = 40;
            int marginTop = 40;
            int marginBottom = 100;

            int chartWidth = width - marginLeft - marginRight;
            int chartHeight = height - marginTop - marginBottom;

            var dates = dataByDate.Keys.OrderBy(d => d).ToList();

            if (dates.Count < 2)
            {
                g.DrawString("Недостаточно данных для построения графика", font, brush, 100, height / 2);
            }
            else
            {
                decimal maxValue = Math.Max(1, dataByDate.Values.SelectMany(v => new[] { v.Income, v.Expenses }).Max());

                // Оси
                g.DrawLine(Pens.Black, marginLeft, marginTop, marginLeft, height - marginBottom); // ось Y
                g.DrawLine(Pens.Black, marginLeft, height - marginBottom, width - marginRight, height - marginBottom); // ось X

                // Подпись к оси Y (вертикальная, левее и выше)
                g.TranslateTransform(-2, height / 2); // Левее и выше
                g.RotateTransform(-90);
                g.DrawString("Сумма, руб.", font, brush, 0, 0);
                g.ResetTransform();

                // Подпись к оси X
                g.DrawString("Дата", font, brush, width / 2 - 20, height - marginBottom + 80);

                // Деления и подписи по Y
                for (int i = 0; i <= 5; i++)
                {
                    decimal val = maxValue * i / 5;
                    int y = height - marginBottom - (int)(val / maxValue * chartHeight);
                    var valStr = val.ToString("0.##");
                    var valSize = g.MeasureString(valStr, font);
                    g.DrawString(valStr, font, brush, marginLeft - valSize.Width - 5, y - valSize.Height / 2);
                }

                // Деления и подписи по X
                int xStep = chartWidth / (dates.Count - 1);
                for (int i = 0; i < dates.Count; i++)
                {
                    int x = marginLeft + xStep * i;
                    g.DrawLine(Pens.Black, x, height - marginBottom, x, height - marginBottom + 5);
                    g.DrawString(dates[i].ToString("dd.MM"), font, brush, x - 10, height - marginBottom + 10);
                }

                // Графики (доход и расходы)
                var lastIncome = 0m;
                var lastExpenses = 0m;

                for (int i = 0; i < dates.Count; i++)
                {
                    int x = marginLeft + xStep * i;
                    int yIncome = height - marginBottom - (int)((dataByDate[dates[i]].Income / maxValue) * chartHeight);
                    int yExpenses = height - marginBottom - (int)((dataByDate[dates[i]].Expenses / maxValue) * chartHeight);

                    if (i > 0)
                    {
                        g.DrawLine(penIncome, marginLeft + xStep * (i - 1), (int)lastIncome, x, yIncome);
                        g.DrawLine(penExpenses, marginLeft + xStep * (i - 1), (int)lastExpenses, x, yExpenses);
                    }

                    lastIncome = yIncome;
                    lastExpenses = yExpenses;
                }
            }

            var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return stream;
        }

        // Генерация отчета РПП
        public void GenerateRPPReport(DateTime from, DateTime to)
        {
            var supplies = _context.SupplyRequests
                .Include(s => s.SupplyRequestsRawMaterials)
                .Where(s => s.SupplyRequestDate >= from && s.SupplyRequestDate <= to)
                .ToList();

            var sales = _context.Sales
                .Include(s => s.SaleProducts)
                .ThenInclude(sp => sp.Product)
                .Where(s => s.DateTimeSale >= from && s.DateTimeSale <= to)
                .ToList();

            var productions = _context.Productions
                .Include(p => p.ProductionsRawMaterialsMeasurementUnitRecipes)
                    .ThenInclude(pr => pr.RawMaterialMeasurementUnitRecipe)
                .Include(p => p.ProductionsRawMaterialsMeasurementUnitRecipes)
                    .ThenInclude(pr => pr.RawMaterialMeasurementUnitRecipe)
                        .ThenInclude(r => r.RawMaterial)
                        .ThenInclude(r => r.MeasurementUnit)
                .Include(p => p.ProductionsRawMaterialsMeasurementUnitRecipes)
                    .ThenInclude(pr => pr.RawMaterialMeasurementUnitRecipe)
                        .ThenInclude(r => r.Recipe)
                        .ThenInclude(r => r.Product)
                .Where(p => p.DateTimeStart >= from && p.DateTimeStart <= to)
                .ToList();

            // 1. Суммарные значения
            decimal totalExpenses = supplies.Sum(s => s.SupplyRequestsRawMaterials.Sum(rm => rm.SupplyCoastToMaterial));
            decimal totalIncome = sales.Sum(s => s.CoastSale ?? 0m);
            int totalProduced = productions.Sum(p => p.ProductionsRawMaterialsMeasurementUnitRecipes.Sum(r => r.CountProduct));
            int totalSold = sales.Sum(s => s.SaleProducts.Sum(p => p.CountProductSale));
            int unsold = totalProduced - totalSold;

            // Чистая прибыль (с учётом "потери" из-за нереализованной продукции)
            decimal lostProfit = 0m;
            decimal netProfit = 0m;

            try
            {
                decimal unitCost = productions
                    .SelectMany(p => p.ProductionsRawMaterialsMeasurementUnitRecipes)
                    .Where(r => r?.RawMaterialMeasurementUnitRecipe?.Recipe?.Product != null)
                    .Select(r => r.RawMaterialMeasurementUnitRecipe.Recipe.Product.ProductCoast)
                    .FirstOrDefault();
                lostProfit = unitCost * unsold;
                netProfit = totalIncome - totalExpenses - lostProfit;
            }
            catch { }

            var conversions = _context.MeasurementConversions
                .ToList()
                .ToDictionary(c => (From: c.FromMeasureUnitId, To: c.ToMeasureUnitId), c => c.ConversionFactor);

            // Таблицы:
            var soldProductsGrouped = sales
                .SelectMany(s => s.SaleProducts)
                .GroupBy(sp => sp.Product)
                .Select(g => new
                {
                    Product = g.Key,
                    Count = g.Sum(p => p.CountProductSale),
                    Total = g.Sum(p => p.CountProductSale * (p.Product.ProductCoast))
                }).ToList();


            var producedProductsGrouped = productions
                .SelectMany(p => p.ProductionsRawMaterialsMeasurementUnitRecipes)
                .GroupBy(r => r.RawMaterialMeasurementUnitRecipe.Recipe.Product)
                .Select(g => new
                {
                    Product = g.Key,
                    Count = g.Sum(x => x.CountProduct),
                    Total = g.Sum(x => x.CountProduct * (x.RawMaterialMeasurementUnitRecipe.Recipe.Product.ProductCoast))
                }).ToList();

            var unsoldProducts = producedProductsGrouped
                .GroupJoin(soldProductsGrouped,
                    prod => prod.Product.ProductId,
                    sold => sold.Product.ProductId,
                    (prod, soldGroup) =>
                    {
                        var sold = soldGroup.FirstOrDefault();
                        int soldCount = sold?.Count ?? 0;
                        int unsoldCount = prod.Count - soldCount;
                        decimal unsoldValue = unsoldCount * (prod.Product.ProductCoast);
                        return new
                        {
                            Product = prod.Product,
                            Count = unsoldCount,
                            Total = unsoldValue
                        };
                    })
                .Where(x => x.Count > 0)
                .ToList();

            // Даты
            var supplyDates = supplies.Select(s => s.SupplyRequestDate.Date);
            var saleDates = sales.Select(s => s.DateTimeSale.Date);
            var allDates = supplyDates.Union(saleDates).Distinct().OrderBy(d => d).ToList();

            var dataByDay = allDates.ToDictionary(
                date => date,
                date => (
                    Income: sales.Where(s => s.DateTimeSale.Date == date).Sum(s => s.CoastSale ?? 0m),
                    Expenses: supplies.Where(s => s.SupplyRequestDate.Date == date).Sum(s => s.SupplyRequestsRawMaterials.Sum(rm => rm.SupplyCoastToMaterial))
                )
            );

            // Генерация документа (отчет)
            var doc = DocX.Create(new MemoryStream());

            // Заголовок
            var header = doc.InsertParagraph();
            header.Append("РПП Отчет").Bold().FontSize(16).Alignment = Alignment.center;

            // Вставка графика
            var chartStream = GenerateChart(dataByDay);
            using (var img = new System.IO.MemoryStream(chartStream.ToArray()))
            {
                var image = doc.AddImage(chartStream);
                var picture = image.CreatePicture();
                doc.InsertParagraph().AppendPicture(picture); // Исправляем ошибку на AddPicture
                var picPara = doc.InsertParagraph();
                picPara.AppendPicture(picture).Alignment = Alignment.center;
            }

            // Добавление таблицы с проданными товарами
            var table = doc.InsertTable(soldProductsGrouped.Count + 1, 3);
            table.Rows[0].Cells[0].Paragraphs.First().Append("Продукт");
            table.Rows[0].Cells[1].Paragraphs.First().Append("Количество");
            table.Rows[0].Cells[2].Paragraphs.First().Append("Общая стоимость");

            foreach (var product in soldProductsGrouped)
            {
                table.InsertRow();
                table.Rows[table.Rows.Count - 1].Cells[0].Paragraphs.First().Append(product.Product.ProductName);
                table.Rows[table.Rows.Count - 1].Cells[1].Paragraphs.First().Append(product.Count.ToString());
                table.Rows[table.Rows.Count - 1].Cells[2].Paragraphs.First().Append(product.Total.ToString("C"));
            }

            string directory = @"D:\ДИПЛОМ\ARM_Proj\Backery_Automatize\App_Automatize_Backery\reports";

            string filePath = Path.Combine(directory, $"RPPReport_{DateTime.Now:yyyyMMdd_HHmmss}.docx");
            doc.Save(filePath);
        }

        public void GenerateAndSaveRPPReport(DateTime from, DateTime to)
        {
            // Генерируем отчёт
            GenerateRPPReport(from, to);

            // Сохраняем его в Reports (заглушка, можешь адаптировать под свою систему отчётов)
            var report = new Report
            {
                ReportType = $"РПП за период с {from:dd.MM.yyyy} по {to:dd.MM.yyyy}",
                ReportDate = DateTime.Now,
                UserId = 6 // Здесь ты можешь добавить путь к файлу или бинарное содержимое отчета, если требуется.
            };

            _context.Reports.Add(report);
            _context.SaveChanges();
        }
    }
}
