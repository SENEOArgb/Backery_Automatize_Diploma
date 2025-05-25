using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App_Automatize_Backery.Helper
{
    public class RppReportWordGenerator
    {
        public void GenerateRppReport(DateTime from, DateTime to, decimal expenses, decimal income, decimal profit, int produced, int sold, int unsold)
        {
            string path = $@"D:\ДИПЛОМ\ARM_Proj\Backery_Automatize\App_Automatize_Backery\reports\RPP_{DateTime.Now:yyyyMMdd_HHmm}.docx";

            using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            body.Append(CreateParagraph($"РПП-отчёт за период: {from:d} - {to:d}", true, 24));
            body.Append(CreateParagraph($"Дата формирования: {DateTime.Now:G}"));
            body.Append(CreateParagraph(""));

            body.Append(CreateParagraph("Общие показатели:", true));
            body.Append(CreateParagraph($"Произведено изделий: {produced}"));
            body.Append(CreateParagraph($"Продано изделий: {sold}"));
            body.Append(CreateParagraph($"Утилизировано изделий: {unsold}"));
            body.Append(CreateParagraph($"Общие расходы на поставки: {expenses} руб."));
            body.Append(CreateParagraph($"Общий приход от продаж: {income} руб."));
            body.Append(CreateParagraph($"Чистая прибыль (с учётом утилизации): {profit} руб."));

            mainPart.Document.Append(body);
            mainPart.Document.Save();
            Process.Start("explorer.exe", path);
        }

        private Paragraph CreateParagraph(string text, bool bold = false, int fontSize = 20)
        {
            RunProperties props = new RunProperties();
            if (bold) props.Append(new Bold());
            props.Append(new FontSize { Val = (fontSize * 2).ToString() });

            var run = new Run(props, new Text(text));
            return new Paragraph(run);
        }
    }
}
