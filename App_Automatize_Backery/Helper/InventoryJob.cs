using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using Quartz;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Threading.Tasks;
using App_Automatize_Backery.Models;

namespace App_Automatize_Backery.Helper
{
    public class InventoryJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                using (var dbContext = new MinBakeryDbContext())
                {
                    // Создание отчёта
                    var report = new Report
                    {
                        ReportDate = DateTime.Now,
                        ReportType = $"Автоматическая инвентаризация склада сырья от {DateTime.Now:dd.MM.yyyy}",
                        UserId = 6 // Можно настроить системного пользователя с ID 1
                    };
                    dbContext.Reports.Add(report);
                    await dbContext.SaveChangesAsync();

                    var reportId = report.ReportId;
                    string reportsDirectory = @"D:\ДИПЛОМ\ARM_Proj\Backery_Automatize\App_Automatize_Backery\reports";
                    if (!Directory.Exists(reportsDirectory))
                        Directory.CreateDirectory(reportsDirectory);

                    string filePath = Path.Combine(reportsDirectory, $"Report_{reportId}.docx");

                    var groupedItems = dbContext.RawMaterialsWarehousesProducts
                        .Include(r => r.RawMaterial)
                        .Include(p => p.Product)
                        .Include(u => u.MeasurementUnit)
                        .Where(r => r.RawMaterial != null)
                        .AsEnumerable()
                        .GroupBy(r => r.RawMaterial.RawMaterialName)
                        .Select(g => new
                        {
                            RawMaterialName = g.Key,
                            Supplies = g.ToList()
                        })
                        .ToList();

                    using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(filePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
                    {
                        var mainPart = wordDocument.AddMainDocumentPart();
                        mainPart.Document = new Document();
                        var body = new Body();

                        body.Append(CreateParagraph($"Автоматический отчет по инвентаризации", true, 24));
                        body.Append(CreateParagraph($"Дата формирования отчёта: {DateTime.Now:G}"));
                        body.Append(CreateParagraph($"Пользователь: СИСТЕМА"));
                        body.Append(CreateParagraph(""));

                        decimal totalInventoryValue = 0;

                        foreach (var group in groupedItems)
                        {
                            body.Append(CreateParagraph($"Сырьё: {group.RawMaterialName}", true, 22));

                            var groupTableData = group.Supplies.Select(s => (
                                s.RawMaterial?.RawMaterialName ?? "Не указано",
                                $"{Math.Round(s.RawMaterialCount, 2):F2} {s.MeasurementUnit?.MeasurementUnitName ?? ""}",
                                $"{s.RawMaterialCount * (double)(s.RawMaterial?.RawMaterialCoast ?? 0m):F2} руб."
                            )).ToList();

                            body.Append(CreateStyledTable(groupTableData, "Сырье (Источник)", "Количество", "Стоимость"));

                            decimal groupTotal = (decimal)group.Supplies.Sum(item => item.RawMaterialCount * (double)(item.RawMaterial?.RawMaterialCoast ?? 0m));
                            totalInventoryValue += groupTotal;

                            body.Append(CreateParagraph($"Итого по \"{group.RawMaterialName}\": {groupTotal:F2} руб.", false, 18));
                            body.Append(CreateParagraph(""));
                        }

                        body.Append(CreateParagraph("Итоги:", true, 22));
                        body.Append(CreateParagraph($"Общая стоимость инвентаризации: {totalInventoryValue} руб."));

                        mainPart.Document.Append(body);
                        mainPart.Document.Save();
                    }

                    Debug.WriteLine($"Инвентаризация выполнена и отчет создан: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка автоматической инвентаризации: {ex.Message}");
            }
        }

        private Paragraph CreateParagraph(string text, bool bold = false, int fontSize = 20)
        {
            RunProperties runProperties = new RunProperties();
            if (bold)
                runProperties.Append(new Bold());
            runProperties.Append(new FontSize() { Val = (fontSize * 2).ToString() });

            Run run = new Run(runProperties, new Text(text));
            return new Paragraph(run);
        }

        private Table CreateStyledTable(List<(string, string, string)> tableData, params string[] headers)
        {
            var table = new Table();
            table.AppendChild(new TableProperties(new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 6 },
                new BottomBorder { Val = BorderValues.Single, Size = 6 },
                new LeftBorder { Val = BorderValues.Single, Size = 6 },
                new RightBorder { Val = BorderValues.Single, Size = 6 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 6 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 6 }
            )));

            var headerRow = new TableRow();
            foreach (var header in headers)
                headerRow.Append(CreateCell(header, true));
            table.Append(headerRow);

            foreach (var row in tableData)
            {
                var tableRow = new TableRow();
                tableRow.Append(CreateCell(row.Item1));
                tableRow.Append(CreateCell(row.Item2));
                tableRow.Append(CreateCell(row.Item3));
                table.Append(tableRow);
            }

            return table;
        }

        private TableCell CreateCell(string text, bool bold = false)
        {
            var run = new Run();
            if (bold)
                run.Append(new RunProperties(new Bold()));
            run.Append(new Text(text));
            var paragraph = new Paragraph(run);
            return new TableCell(paragraph);
        }
    }
}