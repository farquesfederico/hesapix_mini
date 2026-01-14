using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Hesapix.Data;
using Hesapix.Services.Interfaces;

namespace Hesapix.Services.Implementations
{
    public class PdfService : IPdfService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PdfService> _logger;

        public PdfService(ApplicationDbContext context, ILogger<PdfService> logger)
        {
            _context = context;
            _logger = logger;

            // QuestPDF lisans ayarı (Community License - ücretsiz)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(int saleId, int userId)
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
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(content => ComposeInvoiceContent(content, sale));
                    page.Footer().AlignCenter().Text(x =>
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

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("HESAPIX").FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                    column.Item().Text("Muhasebe Yönetim Sistemi").FontSize(10);
                    column.Item().Text("www.hesapix.com").FontSize(9).FontColor(Colors.Grey.Medium);
                });

                row.RelativeItem().AlignRight().Column(column =>
                {
                    column.Item().Text("SATIŞ FATURASI").FontSize(16).Bold();
                    column.Item().Text($"Tarih: {DateTime.Now:dd/MM/yyyy}").FontSize(9);
                });
            });
        }

        private void ComposeInvoiceContent(IContainer container, Models.Entities.Sale sale)
        {
            container.PaddingVertical(20).Column(column =>
            {
                column.Spacing(15);

                // Fatura Bilgileri
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Fatura Bilgileri").Bold().FontSize(12);
                        col.Item().PaddingTop(5).Text($"Fatura No: {sale.SaleNumber}");
                        col.Item().Text($"Tarih: {sale.SaleDate:dd/MM/yyyy}");
                        col.Item().Text($"Durum: {GetPaymentStatusText(sale.PaymentStatus)}");
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Müşteri Bilgileri").Bold().FontSize(12);
                        col.Item().PaddingTop(5).Text($"Ad Soyad: {sale.CustomerName}");
                        if (!string.IsNullOrEmpty(sale.CustomerPhone))
                            col.Item().Text($"Telefon: {sale.CustomerPhone}");
                        if (!string.IsNullOrEmpty(sale.CustomerEmail))
                            col.Item().Text($"E-posta: {sale.CustomerEmail}");
                        if (!string.IsNullOrEmpty(sale.CustomerTaxNumber))
                            col.Item().Text($"Vergi No: {sale.CustomerTaxNumber}");
                    });
                });

                // Ürün Listesi
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);  // Sıra
                        columns.RelativeColumn(3);    // Ürün Adı
                        columns.RelativeColumn(1);    // Miktar
                        columns.RelativeColumn(1.5f); // Birim Fiyat
                        columns.RelativeColumn(1);    // KDV %
                        columns.RelativeColumn(1);    // İndirim %
                        columns.RelativeColumn(1.5f); // Toplam
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("#").Bold();
                        header.Cell().Element(CellStyle).Text("Ürün Adı").Bold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Miktar").Bold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Birim Fiyat").Bold();
                        header.Cell().Element(CellStyle).AlignRight().Text("KDV %").Bold();
                        header.Cell().Element(CellStyle).AlignRight().Text("İnd. %").Bold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Toplam").Bold();

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.DefaultTextStyle(x => x.SemiBold())
                                .PaddingVertical(5)
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten1);
                        }
                    });

                    // Rows
                    int index = 1;
                    foreach (var item in sale.SaleItems)
                    {
                        table.Cell().Element(CellStyle).Text(index++);
                        table.Cell().Element(CellStyle).Text(item.ProductName);
                        table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity);
                        table.Cell().Element(CellStyle).AlignRight().Text($"{item.UnitPrice:N2} ₺");
                        table.Cell().Element(CellStyle).AlignRight().Text($"%{item.TaxRate}");
                        table.Cell().Element(CellStyle).AlignRight().Text($"%{item.DiscountRate}");
                        table.Cell().Element(CellStyle).AlignRight().Text($"{item.TotalPrice:N2} ₺");

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten3)
                                .PaddingVertical(5);
                        }
                    }
                });

                // Özet Bilgiler
                column.Item().AlignRight().PaddingTop(10).Column(summary =>
                {
                    summary.Item().Row(row =>
                    {
                        row.ConstantItem(120).Text("Ara Toplam:").SemiBold();
                        row.ConstantItem(100).AlignRight().Text($"{sale.SubTotal:N2} ₺");
                    });

                    summary.Item().Row(row =>
                    {
                        row.ConstantItem(120).Text("KDV:").SemiBold();
                        row.ConstantItem(100).AlignRight().Text($"{sale.TaxAmount:N2} ₺");
                    });

                    if (sale.DiscountAmount > 0)
                    {
                        summary.Item().Row(row =>
                        {
                            row.ConstantItem(120).Text("İndirim:").SemiBold();
                            row.ConstantItem(100).AlignRight().Text($"-{sale.DiscountAmount:N2} ₺").FontColor(Colors.Red.Medium);
                        });
                    }

                    summary.Item().PaddingTop(5).Row(row =>
                    {
                        row.ConstantItem(120).Text("GENEL TOPLAM:").Bold().FontSize(12);
                        row.ConstantItem(100).AlignRight().Text($"{sale.TotalAmount:N2} ₺").Bold().FontSize(12).FontColor(Colors.Blue.Medium);
                    });
                });

                // Notlar
                if (!string.IsNullOrEmpty(sale.Notes))
                {
                    column.Item().PaddingTop(15).Column(notes =>
                    {
                        notes.Item().Text("Notlar:").Bold();
                        notes.Item().PaddingTop(5).Text(sale.Notes).FontSize(9);
                    });
                }
            });
        }

        public async Task<byte[]> GenerateSalesReportPdfAsync(DateTime startDate, DateTime endDate, int userId)
        {
            var sales = await _context.Sales
                .Include(s => s.SaleItems)
                .Where(s => s.UserId == userId && s.SaleDate >= startDate && s.SaleDate <= endDate)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(column =>
                    {
                        column.Item().Text("SATIŞ RAPORU").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                        column.Item().Text($"Tarih Aralığı: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}").FontSize(10);
                        column.Item().Text($"Rapor Tarihi: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                    });

                    page.Content().PaddingTop(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);  // Fatura No
                            columns.ConstantColumn(80);  // Tarih
                            columns.RelativeColumn(2);   // Müşteri
                            columns.ConstantColumn(60);  // Ara Toplam
                            columns.ConstantColumn(60);  // KDV
                            columns.ConstantColumn(60);  // İndirim
                            columns.ConstantColumn(80);  // Toplam
                            columns.ConstantColumn(80);  // Durum
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("Fatura No");
                            header.Cell().Element(HeaderStyle).Text("Tarih");
                            header.Cell().Element(HeaderStyle).Text("Müşteri");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Ara Toplam");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("KDV");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("İndirim");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Toplam");
                            header.Cell().Element(HeaderStyle).Text("Durum");

                            static IContainer HeaderStyle(IContainer container)
                            {
                                return container.Background(Colors.Grey.Lighten3)
                                    .Padding(5)
                                    .DefaultTextStyle(x => x.SemiBold());
                            }
                        });

                        foreach (var sale in sales)
                        {
                            table.Cell().Element(CellStyle).Text(sale.SaleNumber);
                            table.Cell().Element(CellStyle).Text(sale.SaleDate.ToString("dd/MM/yyyy"));
                            table.Cell().Element(CellStyle).Text(sale.CustomerName);
                            table.Cell().Element(CellStyle).AlignRight().Text($"{sale.SubTotal:N2}");
                            table.Cell().Element(CellStyle).AlignRight().Text($"{sale.TaxAmount:N2}");
                            table.Cell().Element(CellStyle).AlignRight().Text($"{sale.DiscountAmount:N2}");
                            table.Cell().Element(CellStyle).AlignRight().Text($"{sale.TotalAmount:N2} ₺");
                            table.Cell().Element(CellStyle).Text(GetPaymentStatusText(sale.PaymentStatus));

                            static IContainer CellStyle(IContainer container)
                            {
                                return container.BorderBottom(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5);
                            }
                        }
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Toplam Satış: ").SemiBold();
                        x.Span($"{sales.Sum(s => s.TotalAmount):N2} ₺").Bold().FontColor(Colors.Blue.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerateStockReportPdfAsync(int userId)
        {
            var stocks = await _context.Stocks
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderBy(s => s.Category)
                .ThenBy(s => s.ProductName)
                .ToListAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(column =>
                    {
                        column.Item().Text("STOK RAPORU").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                        column.Item().Text($"Rapor Tarihi: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                        column.Item().Text($"Toplam Ürün Çeşidi: {stocks.Count}").FontSize(9);
                    });

                    page.Content().PaddingTop(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);  // Ürün Kodu
                            columns.RelativeColumn(3);   // Ürün Adı
                            columns.RelativeColumn(1);   // Kategori
                            columns.ConstantColumn(60);  // Miktar
                            columns.ConstantColumn(70);  // Alış Fiyatı
                            columns.ConstantColumn(70);  // Satış Fiyatı
                            columns.ConstantColumn(80);  // Toplam Değer
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("Ürün Kodu");
                            header.Cell().Element(HeaderStyle).Text("Ürün Adı");
                            header.Cell().Element(HeaderStyle).Text("Kategori");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Miktar");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Alış");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Satış");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Toplam Değer");

                            static IContainer HeaderStyle(IContainer container)
                            {
                                return container.Background(Colors.Grey.Lighten3)
                                    .Padding(5)
                                    .DefaultTextStyle(x => x.SemiBold());
                            }
                        });

                        foreach (var stock in stocks)
                        {
                            var totalValue = stock.Quantity * stock.PurchasePrice;
                            var lowStock = stock.MinimumStock.HasValue && stock.Quantity <= stock.MinimumStock.Value;

                            table.Cell().Element(CellStyle).Text(stock.ProductCode);
                            table.Cell().Element(CellStyle).Text(stock.ProductName);
                            table.Cell().Element(CellStyle).Text(stock.Category ?? "-");
                            table.Cell().Element(CellStyle).AlignRight().Text(stock.Quantity.ToString())
                                .FontColor(lowStock ? Colors.Red.Medium : Colors.Black);
                            table.Cell().Element(CellStyle).AlignRight().Text($"{stock.PurchasePrice:N2}");
                            table.Cell().Element(CellStyle).AlignRight().Text($"{stock.SalePrice:N2}");
                            table.Cell().Element(CellStyle).AlignRight().Text($"{totalValue:N2} ₺");

                            static IContainer CellStyle(IContainer container)
                            {
                                return container.BorderBottom(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5);
                            }
                        }
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Toplam Stok Değeri: ").SemiBold();
                        x.Span($"{stocks.Sum(s => s.Quantity * s.PurchasePrice):N2} ₺").Bold().FontColor(Colors.Blue.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static string GetPaymentStatusText(Models.Entities.PaymentStatus status)
        {
            return status switch
            {
                Models.Entities.PaymentStatus.Pending => "Beklemede",
                Models.Entities.PaymentStatus.PartialPaid => "Kısmi Ödendi",
                Models.Entities.PaymentStatus.Paid => "Ödendi",
                Models.Entities.PaymentStatus.Cancelled => "İptal",
                _ => "Bilinmiyor"
            };
        }
    }
}