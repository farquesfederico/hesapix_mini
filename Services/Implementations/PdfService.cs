using Hesapix.Data;
using Hesapix.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hesapix.Services.Implementations;

public class PdfService : IPdfService
{
    private readonly ApplicationDbContext _context;

    public PdfService(ApplicationDbContext context)
    {
        _context = context;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateSaleInvoicePdfAsync(int saleId, int userId)
    {
        var sale = await _context.Sales
            .Include(s => s.SaleItems)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == saleId && s.UserId == userId);

        if (sale == null)
        {
            throw new Exception("Satış bulunamadı");
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Text("SATIŞ FATURASI")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(20);

                        // Firma ve Müşteri Bilgileri
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Satıcı Bilgileri").SemiBold();
                                col.Item().Text(sale.User.CompanyName);
                                if (!string.IsNullOrEmpty(sale.User.TaxNumber))
                                    col.Item().Text($"VKN: {sale.User.TaxNumber}");
                                if (!string.IsNullOrEmpty(sale.User.Address))
                                    col.Item().Text(sale.User.Address);
                            });

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Müşteri Bilgileri").SemiBold();
                                col.Item().Text(sale.CustomerName);
                                if (!string.IsNullOrEmpty(sale.CustomerTaxNumber))
                                    col.Item().Text($"VKN: {sale.CustomerTaxNumber}");
                                if (!string.IsNullOrEmpty(sale.CustomerAddress))
                                    col.Item().Text(sale.CustomerAddress);
                            });
                        });

                        // Fatura Detayları
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"Fatura No: {sale.SaleNumber}");
                                col.Item().Text($"Tarih: {sale.SaleDate:dd.MM.yyyy}");
                                col.Item().Text($"Durum: {sale.PaymentStatus}");
                            });
                        });

                        // Ürün Tablosu
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("No");
                                header.Cell().Element(CellStyle).Text("Ürün");
                                header.Cell().Element(CellStyle).Text("Miktar");
                                header.Cell().Element(CellStyle).Text("Birim Fiyat");
                                header.Cell().Element(CellStyle).Text("Toplam");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.BorderBottom(1).BorderColor(Colors.Black).PaddingVertical(5);
                                }
                            });

                            int index = 1;
                            foreach (var item in sale.SaleItems)
                            {
                                table.Cell().Element(CellStyle).Text(index.ToString());
                                table.Cell().Element(CellStyle).Text(item.ProductName);
                                table.Cell().Element(CellStyle).Text(item.Quantity.ToString());
                                table.Cell().Element(CellStyle).Text($"₺{item.UnitPrice:N2}");
                                table.Cell().Element(CellStyle).Text($"₺{item.TotalPrice:N2}");
                                index++;

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                                }
                            }
                        });

                        // Toplam Bilgileri
                        column.Item().AlignRight().Column(col =>
                        {
                            col.Spacing(5);
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Ara Toplam:");
                                row.ConstantItem(100).AlignRight().Text($"₺{sale.SubTotal:N2}");
                            });
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"KDV ({sale.TaxRate}%):");
                                row.ConstantItem(100).AlignRight().Text($"₺{sale.TaxAmount:N2}");
                            });
                            if (sale.DiscountAmount > 0)
                            {
                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("İndirim:");
                                    row.ConstantItem(100).AlignRight().Text($"-₺{sale.DiscountAmount:N2}");
                                });
                            }
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("GENEL TOPLAM:").SemiBold().FontSize(12);
                                row.ConstantItem(100).AlignRight().Text($"₺{sale.TotalAmount:N2}").SemiBold().FontSize(12);
                            });
                        });

                        // Notlar
                        if (!string.IsNullOrEmpty(sale.Notes))
                        {
                            column.Item().Column(col =>
                            {
                                col.Item().Text("Notlar:").SemiBold();
                                col.Item().Text(sale.Notes);
                            });
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Sayfa ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
            });
        });

        return document.GeneratePdf();
    }
}