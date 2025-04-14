using App_Automatize_Backery.Helper;
using App_Automatize_Backery.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using App_Automatize_Backery.View.UserControls_Pages_.General_WorkerPages;

namespace App_Automatize_Backery.ViewModels.ReportsVM
{
    public class ReportViewModel : INotifyPropertyChanged
    {
        private readonly MinBakeryDbContext _context;

        public ObservableCollection<Report> Reports { get; set; }

        public MainViewModel vmMain;

        public ICommand OpenReportsFolderCommand { get; }
        public ICommand GenerateWordReportCommand { get; }
        public ICommand OpenGenerateByPeriodWindowCommand { get; }
        public ReportViewModel(MainViewModel mainViewModel)
        {
            vmMain = mainViewModel;
            _context = App.DbContext;
            GenerateWordReportCommand = new RelayCommand(obj => GenerateWordReport(obj as Report));
            OpenReportsFolderCommand = new RelayCommand(_ => OpenReportsFolder());
            OpenGenerateByPeriodWindowCommand = new RelayCommand(_ => OpenGenerateWindow());
            LoadReports();
        }

        private void OpenReportsFolder()
        {
            string reportsPath = @"D:\ДИПЛОМ\ARM_Proj\Backery_Automatize\App_Automatize_Backery\reports";

            if (!Directory.Exists(reportsPath))
                Directory.CreateDirectory(reportsPath);

            Process.Start("explorer.exe", reportsPath);
        }

        private void OpenGenerateWindow()
        {
            var window = new ReportGenerateWindow 
            {
                DataContext  = new GenerateReportByPeriodViewModel(this)
            };
            window.ShowDialog();
        }

        private void LoadReports()
        {
            var reports = _context.Reports
                .Include(r => r.ExpencesReportsParishes)
                    .ThenInclude(erp => erp.Parishe)
                        .ThenInclude(p => p.Sale)
                            .ThenInclude(s => s.User)
                .Include(r => r.ExpencesReportsParishes)
                    .ThenInclude(erp => erp.Parishe)
                        .ThenInclude(p => p.Sale)
                            .ThenInclude(s => s.SaleProducts)
                                .ThenInclude(sp => sp.Product)
                .Include(r => r.ExpencesReportsParishes)
                    .ThenInclude(erp => erp.Expence)
                        .ThenInclude(s => s.SupplyRequest)
                            .ThenInclude(srrm => srrm.SupplyRequestsRawMaterials)
                                .ThenInclude(srrm => srrm.RawMaterial)
                .Include(r => r.ExpencesReportsParishes)
                    .ThenInclude(erp => erp.Expence)
                        .ThenInclude(s => s.SupplyRequest)
                            .ThenInclude(srrm => srrm.SupplyRequestsRawMaterials)
                                .ThenInclude(srrm => srrm.Warehouse)
                .ToList();

            Reports = new ObservableCollection<Report>(reports);
            OnPropertyChanged(nameof(Reports));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void GenerateWordReport(Report report)
        {
            if (report == null || !report.ExpencesReportsParishes.Any())
                return;

            string reportsDirectory = @"D:\ДИПЛОМ\ARM_Proj\Backery_Automatize\App_Automatize_Backery\reports";
            if (!Directory.Exists(reportsDirectory))
                Directory.CreateDirectory(reportsDirectory);

            string filePath = Path.Combine(reportsDirectory, $"Report_{report.ReportId}.docx");

            if (File.Exists(filePath))
            {
                Process.Start("explorer.exe", filePath);
                return;
            }

            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(filePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = new Body();

                body.Append(CreateParagraph($"Отчёт №{report.ReportId}", true, 24));
                body.Append(CreateParagraph($"Дата формирования отчёта: {report.ReportDate:G}"));
                body.Append(CreateParagraph($"Пользователь: {report.User?.FullName ?? "Неизвестен"}"));
                body.Append(CreateParagraph($"Тип отчёта: {report.ReportType}"));
                body.Append(CreateParagraph(""));

                decimal totalIncome = 0;
                decimal totalExpense = 0;

                foreach (var link in report.ExpencesReportsParishes)
                {
                    if (link.Parishe is { Sale: { } sale })
                    {
                        body.Append(CreateParagraph("Продажа:", true));
                        body.Append(CreateParagraph($"Дата и время: {sale.DateTimeSale:G}"));
                        body.Append(CreateParagraph($"Тип продажи: {sale.TypeSale}"));
                        body.Append(CreateParagraph($"Общая стоимость: {sale.CoastSale ?? 0} руб."));

                        if (sale.SaleProducts?.Any() == true)
                        {
                            body.Append(CreateParagraph("Проданные товары:", true));
                            var tableData = sale.SaleProducts
                                .Select(p => (
                                    p.Product?.ProductName ?? "Неизвестно",
                                    p.CountProductSale.ToString(),
                                    $"{p.CoastToProduct ?? 0} руб.")).ToList();

                            body.Append(CreateStyledTable(tableData, "Продукт", "Количество", "Стоимость"));
                        }

                        decimal parishSize = link.Parishe.ParisheSize;
                        totalIncome += parishSize;
                        body.Append(CreateParagraph($"Доход по этой продаже: {parishSize} руб."));
                        body.Append(CreateParagraph(""));
                    }

                    if (link.Expence?.SupplyRequest is { } supplyRequest)
                    {
                        body.Append(CreateParagraph("Поставка:", true));
                        body.Append(CreateParagraph($"Дата поставки: {supplyRequest.SupplyRequestDate:G}"));
                        body.Append(CreateParagraph($"Ответственный: {supplyRequest.User?.FullName ?? "Неизвестен"}"));
                        body.Append(CreateParagraph($"Сумма поставки: {link.Expence.ExpenceCoast} руб."));

                        var materials = App.DbContext.SupplyRequestsRawMaterials
                            .Include(m => m.RawMaterial)
                            .Where(m => m.SupplyRequestId == supplyRequest.SupplyRequestId)
                            .ToList();

                        if (materials.Any())
                        {
                            body.Append(CreateParagraph("Сырьё в поставке:", true));

                            var tableData = materials
                                .Select(m => (
                                    m.RawMaterial?.RawMaterialName ?? "Неизвестно",
                                    m.CountRawMaterial.ToString(),
                                    $"{m.CountRawMaterial * m.RawMaterial.RawMaterialCoast} руб.")).ToList();

                            body.Append(CreateStyledTable(tableData, "Сырьё", "Количество", "Стоимость"));
                        }

                        totalExpense += link.Expence.ExpenceCoast;
                        body.Append(CreateParagraph(""));
                    }
                }

                body.Append(CreateParagraph("Итоги:", true, 22));
                body.Append(CreateParagraph($"Общий доход: {totalIncome} руб."));
                body.Append(CreateParagraph($"Общие расходы: {totalExpense} руб."));
                body.Append(CreateParagraph($"Итоговая прибыль: {totalIncome - totalExpense} руб."));

                mainPart.Document.Append(body);
                mainPart.Document.Save();
            }

            Process.Start("explorer.exe", filePath);
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
        private TableCell CreateCell(string text, bool bold = false)
        {
            Run run = new Run();
            if (bold)
                run.Append(new RunProperties(new Bold()));

            run.Append(new Text(text));
            Paragraph paragraph = new Paragraph(run);
            return new TableCell(paragraph);
        }

        public void GenerateWordReportByPeriod(Report report, List<Parish>? parishes, List<Expence>? expences)
        {
            if ((parishes == null || !parishes.Any()) && (expences == null || !expences.Any()))
                return;

            string fileName = $"Report_{report.ReportId}_{report.ReportType}_Period.docx";
            string reportsDirectory = @"D:\ДИПЛОМ\ARM_Proj\Backery_Automatize\App_Automatize_Backery\reports";

            if (!Directory.Exists(reportsDirectory))
                Directory.CreateDirectory(reportsDirectory);

            string filePath = Path.Combine(reportsDirectory, fileName);

            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = new Body();

                body.Append(CreateParagraph($"Отчёт №{report.ReportId}", true, 24));
                body.Append(CreateParagraph($"Дата формирования: {report.ReportDate:G}"));
                body.Append(CreateParagraph($"Тип отчёта: {report.ReportType}"));
                body.Append(CreateParagraph($"Сформировал: {report.User?.FullName ?? "Неизвестно"}"));
                body.Append(CreateParagraph(""));

                if (report.ReportType == "Продажи" && parishes?.Any() == true)
                {
                    decimal totalIncome = 0;

                    foreach (var parish in parishes)
                    {
                        var sale = parish.Sale;
                        body.Append(CreateParagraph("Продажа:", true));
                        body.Append(CreateParagraph($"Дата и время: {sale.DateTimeSale:G}"));
                        body.Append(CreateParagraph($"Тип продажи: {sale.TypeSale}"));
                        body.Append(CreateParagraph($"Общая сумма: {sale.CoastSale ?? 0} руб."));

                        if (sale.SaleProducts?.Any() == true)
                        {
                            var tableData = sale.SaleProducts
                                .Select(p => (
                                    p.Product?.ProductName ?? "Неизвестно",
                                    p.CountProductSale.ToString(),
                                    $"{p.CoastToProduct ?? 0} руб.")).ToList();

                            body.Append(CreateStyledTable(tableData, "Продукт", "Количество", "Стоимость"));
                        }

                        totalIncome += parish.ParisheSize;
                        body.Append(CreateParagraph(""));
                    }

                    body.Append(CreateParagraph($"Итоговый доход за период: {totalIncome} руб.", true));
                }

                if (report.ReportType == "Поставки" && expences?.Any() == true)
                {
                    decimal totalSupplyCost = 0;

                    foreach (var expence in expences)
                    {
                        var sr = expence.SupplyRequest;
                        body.Append(CreateParagraph("Поставка:", true));
                        body.Append(CreateParagraph($"Дата поставки: {sr.SupplyRequestDate:G}"));
                        body.Append(CreateParagraph($"Ответственный: {sr.User?.FullName ?? "Неизвестно"}"));
                        body.Append(CreateParagraph($"Сумма: {expence.ExpenceCoast} руб."));

                        var materials = sr.SupplyRequestsRawMaterials;

                        if (materials?.Any() == true)
                        {
                            var tableData = materials
                                .Select(m => (
                                    m.RawMaterial?.RawMaterialName ?? "Неизвестно",
                                    m.CountRawMaterial.ToString(),
                                    $"{m.CountRawMaterial * m.RawMaterial.RawMaterialCoast} руб.")).ToList();

                            body.Append(CreateStyledTable(tableData, "Сырьё", "Количество", "Стоимость"));
                        }

                        totalSupplyCost += expence.ExpenceCoast;
                        body.Append(CreateParagraph(""));
                    }

                    body.Append(CreateParagraph($"Общие затраты за период: {totalSupplyCost} руб.", true));
                }

                mainPart.Document.Append(body);
                mainPart.Document.Save();
            }

            Process.Start("explorer.exe", filePath);
        }

        private Table CreateSalesTable(List<SaleProduct> saleProducts)
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

            TableRow header = new TableRow();
            header.Append(CreateCell("Продукт", true));
            header.Append(CreateCell("Количество", true));
            header.Append(CreateCell("Стоимость", true));
            table.Append(header);

            foreach (var sp in saleProducts)
            {
                TableRow row = new TableRow();
                row.Append(CreateCell(sp.Product?.ProductName ?? "Неизвестно"));
                row.Append(CreateCell(sp.CountProductSale.ToString()));
                row.Append(CreateCell($"{sp.CoastToProduct ?? 0} руб."));
                table.Append(row);
            }

            return table;
        }

        private Table CreateSupplyTable(List<SupplyRequestsRawMaterial> materials)
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

            TableRow header = new TableRow();
            header.Append(CreateCell("Сырьё", true));
            header.Append(CreateCell("Количество", true));
            header.Append(CreateCell("Стоимость", true));
            table.Append(header);

            foreach (var mat in materials)
            {
                TableRow row = new TableRow();
                row.Append(CreateCell(mat.RawMaterial?.RawMaterialName ?? "Неизвестно"));
                row.Append(CreateCell(mat.CountRawMaterial.ToString()));
                row.Append(CreateCell($"{mat.CountRawMaterial * mat.RawMaterial.RawMaterialCoast} руб."));
                table.Append(row);
            }

            return table;
        }

        private Table CreateStyledTable(List<(string, string, string)> rows, string col1Header, string col2Header, string col3Header)
        {
            Table table = new Table();

            TableProperties props = new TableProperties(new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 6 },
                new BottomBorder { Val = BorderValues.Single, Size = 6 },
                new LeftBorder { Val = BorderValues.Single, Size = 6 },
                new RightBorder { Val = BorderValues.Single, Size = 6 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 6 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 6 }));

            table.AppendChild(props);

            TableRow header = new TableRow();
            header.Append(CreateCell(col1Header, true));
            header.Append(CreateCell(col2Header, true));
            header.Append(CreateCell(col3Header, true));
            table.Append(header);

            foreach (var rowData in rows)
            {
                TableRow row = new TableRow();
                row.Append(CreateCell(rowData.Item1));
                row.Append(CreateCell(rowData.Item2));
                row.Append(CreateCell(rowData.Item3));
                table.Append(row);
            }

            return table;
        }

    }
}
