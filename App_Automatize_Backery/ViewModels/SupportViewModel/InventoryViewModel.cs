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
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Diagnostics;
using System.IO;
using System.Timers;

namespace App_Automatize_Backery.ViewModels.SupportViewModel
{
    public class InventoryViewModel : INotifyPropertyChanged
    {
        private MainViewModel _mainViewModel;

        private DateTime _inventoryDateTime;
        private ObservableCollection<RawMaterialsWarehousesProduct> _warehouseItems;
        private RelayCommand _generateInventoryReportCommand;

        public InventoryViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            InventoryDateTime = DateTime.Now;
            GenerateInventoryReportCommand = new RelayCommand(GenerateInventoryReport);

            // Загрузка сгруппированных данных
            LoadGroupedInventory();
        }

        public ObservableCollection<RawMaterialsWarehousesProduct> WarehouseItems
        {
            get => _warehouseItems;
            set
            {
                _warehouseItems = value;
                OnPropertyChanged(nameof(WarehouseItems));
            }
        }

        public DateTime InventoryDateTime
        {
            get => _inventoryDateTime;
            set
            {
                _inventoryDateTime = value;
                OnPropertyChanged(nameof(InventoryDateTime));
            }
        }

        public ICommand GenerateInventoryReportCommand
        {
            get => _generateInventoryReportCommand;
            set
            {
                _generateInventoryReportCommand = (RelayCommand)value;
                OnPropertyChanged(nameof(GenerateInventoryReportCommand));
            }
        }

        // Генерация отчета по инвентаризации
        private void GenerateInventoryReport(object parameter)
        {
            var result = MessageBox.Show("Вы уверены, что хотите сгенерировать отчет?", "Подтверждение", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                // Логика для генерации отчета
                CreateInventoryReport();
            }
        }

        // Метод для формирования отчета
        private void CreateInventoryReport()
        {
            // Директория для сохранения отчетов
            string reportsDirectory = @"D:\ДИПЛОМ\ARM_Proj\Backery_Automatize\App_Automatize_Backery\reports";

            if (!Directory.Exists(reportsDirectory))
                Directory.CreateDirectory(reportsDirectory);

            int reportId;
            using (var context = new MinBakeryDbContext())
            {
                var report = new Report
                {
                    ReportDate = DateTime.Now,
                    ReportType = $"Инвентаризация на складе сырья от {DateTime.Now:dd.MM.yyyy}",
                    UserId = _mainViewModel.CurrentUser.UserId
                };
                context.Reports.Add(report);
                context.SaveChanges();
                reportId = report.ReportId;
            }

            // Теперь имя файла строго по шаблону
            string filePath = Path.Combine(reportsDirectory, $"Report_{reportId}.docx");

            // Создаем новый Word файл
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(filePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = new Body();

                body.Append(CreateParagraph($"Отчёт по инвентаризации", true, 24));
                body.Append(CreateParagraph($"Дата формирования отчёта: {DateTime.Now:G}"));
                body.Append(CreateParagraph($"Пользователь: {_mainViewModel.CurrentUser?.FullName ?? "Неизвестен"}"));
                body.Append(CreateParagraph(""));

                decimal totalInventoryValue = 0;

                var groupedItems = GroupedWarehouseItems;

                if (groupedItems.Any())
                {
                    body.Append(CreateParagraph("Инвентаризация сырья:", true));

                    foreach (var group in groupedItems)
                    {
                        body.Append(CreateParagraph($"Сырьё: {group.RawMaterialName}", true, 22));

                        var groupTableData = group.Supplies.Select(s => (
                            s.RawMaterial?.RawMaterialName ?? "Не указано",
                            $"{Math.Round(s.RawMaterialCount, 2):F2} {s.MeasurementUnit?.MeasurementUnitName ?? ""}",
                            $"{s.RawMaterialCount * (double)(s.RawMaterial?.RawMaterialCoast ?? 0m):F2} руб."
                        )).ToList();

                        body.Append(CreateStyledTable(groupTableData, "Сырье (Источник)", "Количество", "Стоимость"));

                        // Промежуточный итог для этой группы
                        decimal groupTotal = (decimal)group.Supplies.Sum(item => item.RawMaterialCount * (double)(item.RawMaterial?.RawMaterialCoast ?? 0m));
                        totalInventoryValue += groupTotal;

                        body.Append(CreateParagraph($"Итого по \"{group.RawMaterialName}\": {groupTotal:F2} руб.", false, 18));
                        body.Append(CreateParagraph(""));
                    }
                }

                body.Append(CreateParagraph("Итоги:", true, 22));
                body.Append(CreateParagraph($"Общая стоимость инвентаризации: {totalInventoryValue} руб."));

                mainPart.Document.Append(body);
                mainPart.Document.Save();
            }

            // Открытие отчета сразу после создания
            try
            {
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть отчет: {ex.Message}");
            }

            // Уведомление
            MessageBox.Show("Отчет по инвентаризации сгенерирован и открыт.");
        }

        // Сохранение отчета в базу данных
        private void SaveReportToDatabase()
        {
            var report = new Report
            {
                ReportDate = DateTime.Now,
                ReportType = $"Инвентаризация на складе сырья от {DateTime.Now:dd.MM.yyyy}",
                UserId = _mainViewModel.CurrentUser.UserId // Предположим, что у вас есть текущий пользователь
            };
            using (var context = new MinBakeryDbContext())
            {
                context.Reports.Add(report);
                context.SaveChanges();
            }
        }

        // Получение данных для инвентаризации (замените на реальную логику)
        private IEnumerable<RawMaterialsWarehousesProduct> GetRawMaterialsForInventory()
        {
            using (var context = new MinBakeryDbContext())
            {
                return context.RawMaterialsWarehousesProducts
                    .Include(r => r.RawMaterial)
                    .Include(p => p.Product)
                    .Include(u => u.MeasurementUnit)
                    .ToList();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Создание параграфа с заданным текстом
        private Paragraph CreateParagraph(string text, bool bold = false, int fontSize = 20)
        {
            RunProperties runProperties = new RunProperties();
            if (bold)
                runProperties.Append(new Bold());

            runProperties.Append(new FontSize() { Val = (fontSize * 2).ToString() });

            Run run = new Run(runProperties, new Text(text));
            return new Paragraph(run);
        }

        // Создание таблицы с данными
        private Table CreateStyledTable(List<(string, string, string)> tableData, params string[] headers)
        {
            Table table = new Table();
            table.AppendChild(new TableProperties(new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 6 },
                new BottomBorder { Val = BorderValues.Single, Size = 6 },
                new LeftBorder { Val = BorderValues.Single, Size = 6 },
                new RightBorder { Val = BorderValues.Single, Size = 6 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 6 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 6 }
            )));

            // Заголовки таблицы
            TableRow headerRow = new TableRow();
            foreach (var header in headers)
            {
                headerRow.Append(CreateCell(header, true));
            }
            table.Append(headerRow);

            // Данные таблицы
            foreach (var row in tableData)
            {
                TableRow tableRow = new TableRow();
                tableRow.Append(CreateCell(row.Item1));
                tableRow.Append(CreateCell(row.Item2));
                tableRow.Append(CreateCell(row.Item3));
                table.Append(tableRow);
            }

            return table;
        }

        // Создание ячейки таблицы
        private TableCell CreateCell(string text, bool bold = false)
        {
            Run run = new Run();
            if (bold)
                run.Append(new RunProperties(new Bold()));

            run.Append(new Text(text));
            Paragraph paragraph = new Paragraph(run);
            return new TableCell(paragraph);
        }

        public class RawMaterialInventoryGroup
        {
            public string RawMaterialName { get; set; }
            public ObservableCollection<RawMaterialsWarehousesProduct> Supplies { get; set; }
        }

        private ObservableCollection<RawMaterialInventoryGroup> _groupedWarehouseItems;
        public ObservableCollection<RawMaterialInventoryGroup> GroupedWarehouseItems
        {
            get => _groupedWarehouseItems;
            set
            {
                _groupedWarehouseItems = value;
                OnPropertyChanged(nameof(GroupedWarehouseItems));
            }
        }

        private void LoadGroupedInventory()
        {
            using (var context = new MinBakeryDbContext())
            {
                var items = new ObservableCollection<RawMaterialsWarehousesProduct>(
                    context.RawMaterialsWarehousesProducts
                        .Include(r => r.RawMaterial)
                        .Include(p => p.Product)
                        .Include(u => u.MeasurementUnit)
                        .Where(r => r.RawMaterial != null)
                        .ToList()
                );

                var grouped = items
                    .GroupBy(r => r.RawMaterial.RawMaterialName)
                    .Select(g => new RawMaterialInventoryGroup
                    {
                        RawMaterialName = g.Key,
                        Supplies = new ObservableCollection<RawMaterialsWarehousesProduct>(g.ToList())
                    })
                    .ToList();

                GroupedWarehouseItems = new ObservableCollection<RawMaterialInventoryGroup>(grouped);
            }
        }
    }
}
