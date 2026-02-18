using APIServiceManagement.Application.DTOs.Responses;
using APIServiceManagement.Application.Interfaces.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Infrastructure.Services;

public class PdfGenerationService : IPdfGenerationService
{
    public PdfGenerationService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateInvoicePdfAsync(InvoiceResponse invoice, CancellationToken cancellationToken = default)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Column(column =>
                    {
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("INVOICE").FontSize(24).Bold().FontColor(Colors.Blue.Medium);
                                col.Item().Text($"Invoice #: {invoice.InvoiceNumber}").FontSize(10);
                                col.Item().Text($"Date: {invoice.InvoiceDate:dd MMM yyyy}").FontSize(10);
                                if (invoice.DueDate.HasValue)
                                {
                                    col.Item().Text($"Due Date: {invoice.DueDate.Value:dd MMM yyyy}").FontSize(10);
                                }
                            });

                            row.ConstantItem(150).AlignRight().Column(col =>
                            {
                                if (invoice.Company != null && !string.IsNullOrEmpty(invoice.Company.CompanyName))
                                {
                                    col.Item().Text(invoice.Company.CompanyName).FontSize(14).Bold();
                                    if (!string.IsNullOrEmpty(invoice.Company.CompanyAddressLine1))
                                    {
                                        col.Item().Text(invoice.Company.CompanyAddressLine1).FontSize(9);
                                    }
                                    if (!string.IsNullOrEmpty(invoice.Company.CompanyAddressLine2))
                                    {
                                        col.Item().Text(invoice.Company.CompanyAddressLine2).FontSize(9);
                                    }
                                    if (!string.IsNullOrEmpty(invoice.Company.CompanyCity))
                                    {
                                        var cityState = invoice.Company.CompanyCity;
                                        if (!string.IsNullOrEmpty(invoice.Company.CompanyState))
                                        {
                                            cityState += $", {invoice.Company.CompanyState}";
                                        }
                                        if (!string.IsNullOrEmpty(invoice.Company.CompanyPincode))
                                        {
                                            cityState += $" - {invoice.Company.CompanyPincode}";
                                        }
                                        col.Item().Text(cityState).FontSize(9);
                                    }
                                    if (!string.IsNullOrEmpty(invoice.Company.CompanyGstin))
                                    {
                                        col.Item().Text($"GSTIN: {invoice.Company.CompanyGstin}").FontSize(8);
                                    }
                                    if (!string.IsNullOrEmpty(invoice.Company.CompanyPan))
                                    {
                                        col.Item().Text($"PAN: {invoice.Company.CompanyPan}").FontSize(8);
                                    }
                                }
                                else
                                {
                                    col.Item().Text("Service Management").FontSize(14).Bold();
                                    col.Item().Text("System").FontSize(10);
                                }
                            });
                        });
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Company and Customer Information
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Bill To:").FontSize(12).Bold();
                                col.Item().PaddingBottom(5);
                                if (invoice.Customer != null)
                                {
                                    col.Item().Text(invoice.Customer.Name).FontSize(10);
                                    col.Item().Text(invoice.Customer.Email).FontSize(10);
                                    col.Item().Text(invoice.Customer.MobileNumber).FontSize(10);
                                }
                            });

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Service Provider:").FontSize(12).Bold();
                                col.Item().PaddingBottom(5);
                                if (invoice.ServiceProvider != null)
                                {
                                    col.Item().Text(invoice.ServiceProvider.Name).FontSize(10);
                                    col.Item().Text(invoice.ServiceProvider.Email).FontSize(10);
                                    col.Item().Text(invoice.ServiceProvider.MobileNumber).FontSize(10);
                                }
                                else
                                {
                                    col.Item().Text("N/A").FontSize(10).FontColor(Colors.Grey.Medium);
                                }
                            });
                        });

                        column.Item().PaddingTop(10);

                        // Service Details
                        column.Item().Text("Service Details").FontSize(14).Bold();
                        column.Item().PaddingBottom(5);
                        column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5);

                        if (invoice.Service != null)
                        {
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Service Name:").FontSize(10);
                                row.RelativeItem().Text(invoice.Service.ServiceName).FontSize(10).Bold();
                            });
                            if (!string.IsNullOrEmpty(invoice.Service.Description))
                            {
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Description:").FontSize(10);
                                    row.RelativeItem().Text(invoice.Service.Description).FontSize(10);
                                });
                            }
                        }

                        column.Item().PaddingTop(10);

                        // Invoice Items Table
                        column.Item().Text("Invoice Summary").FontSize(14).Bold();
                        column.Item().PaddingBottom(5);
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Description").Bold();
                                header.Cell().Element(CellStyle).AlignRight().Text("Amount").Bold();
                            });

                            // Base Price
                            table.Cell().Element(CellStyle).Text("Base Price");
                            table.Cell().Element(CellStyle).AlignRight().Text($"₹{invoice.BasePrice:N2}");

                            // Location Adjustment
                            if (invoice.LocationAdjustmentAmount.HasValue && invoice.LocationAdjustmentAmount.Value != 0)
                            {
                                table.Cell().Element(CellStyle).Text("Location Adjustment");
                                table.Cell().Element(CellStyle).AlignRight().Text($"₹{invoice.LocationAdjustmentAmount.Value:N2}");
                            }

                            // Discount
                            if (invoice.DiscountAmount.HasValue && invoice.DiscountAmount.Value > 0)
                            {
                                table.Cell().Element(CellStyle).Text("Discount");
                                table.Cell().Element(CellStyle).AlignRight().Text($"-₹{invoice.DiscountAmount.Value:N2}").FontColor(Colors.Green.Medium);
                            }

                            // Service Charge
                            if (invoice.ServiceChargeAmount.HasValue && invoice.ServiceChargeAmount.Value > 0)
                            {
                                table.Cell().Element(CellStyle).Text("Service Charge");
                                table.Cell().Element(CellStyle).AlignRight().Text($"₹{invoice.ServiceChargeAmount.Value:N2}");
                            }

                            // Platform Charge
                            if (invoice.PlatformChargeAmount.HasValue && invoice.PlatformChargeAmount.Value > 0)
                            {
                                table.Cell().Element(CellStyle).Text("Platform Charge");
                                table.Cell().Element(CellStyle).AlignRight().Text($"₹{invoice.PlatformChargeAmount.Value:N2}");
                            }

                            // Subtotal
                            table.Cell().Element(CellStyle).Text("Subtotal").Bold();
                            table.Cell().Element(CellStyle).AlignRight().Text($"₹{invoice.Subtotal:N2}").Bold();

                            // Taxes
                            if (invoice.CgstAmount.HasValue && invoice.CgstAmount.Value > 0)
                            {
                                table.Cell().Element(CellStyle).Text("CGST");
                                table.Cell().Element(CellStyle).AlignRight().Text($"₹{invoice.CgstAmount.Value:N2}");
                            }

                            if (invoice.SgstAmount.HasValue && invoice.SgstAmount.Value > 0)
                            {
                                table.Cell().Element(CellStyle).Text("SGST");
                                table.Cell().Element(CellStyle).AlignRight().Text($"₹{invoice.SgstAmount.Value:N2}");
                            }

                            if (invoice.IgstAmount.HasValue && invoice.IgstAmount.Value > 0)
                            {
                                table.Cell().Element(CellStyle).Text("IGST");
                                table.Cell().Element(CellStyle).AlignRight().Text($"₹{invoice.IgstAmount.Value:N2}");
                            }

                            if (invoice.TotalTaxAmount > 0)
                            {
                                table.Cell().Element(CellStyle).Text("Total Tax");
                                table.Cell().Element(CellStyle).AlignRight().Text($"₹{invoice.TotalTaxAmount:N2}");
                            }

                            // Total
                            table.Cell().Element(CellStyle).Background(Colors.Grey.Lighten3).Padding(5).Text("Total Amount").Bold().FontSize(12);
                            table.Cell().Element(CellStyle).Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"₹{invoice.TotalAmount:N2}").Bold().FontSize(12);
                        });

                        column.Item().PaddingTop(10);

                        // Payment Status
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Payment Status:").FontSize(10);
                            row.RelativeItem().Text(invoice.PaymentStatus).FontSize(10).Bold()
                                .FontColor(invoice.PaymentStatus == "Paid" ? Colors.Green.Medium : Colors.Orange.Medium);
                        });

                        // Notes
                        if (!string.IsNullOrEmpty(invoice.Notes))
                        {
                            column.Item().PaddingTop(10);
                            column.Item().Text("Notes:").FontSize(10).Bold();
                            column.Item().Text(invoice.Notes).FontSize(10);
                        }

                        column.Item().PaddingTop(20);

                        // Footer
                        column.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(10)
                            .AlignCenter();
                        
                        if (invoice.Company != null && !string.IsNullOrEmpty(invoice.Company.InvoiceFooterText))
                        {
                            column.Item().Text(invoice.Company.InvoiceFooterText)
                                .FontSize(10)
                                .Italic()
                                .FontColor(Colors.Grey.Medium);
                        }
                        else
                        {
                            column.Item().Text("Thank you for your business!")
                                .FontSize(10)
                                .Italic()
                                .FontColor(Colors.Grey.Medium);
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
            });
        });

        var pdfBytes = document.GeneratePdf();
        return Task.FromResult(pdfBytes);
    }

    public Task<byte[]> GenerateCreditNotePdfAsync(CreditNoteResponse creditNote, CancellationToken cancellationToken = default)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Column(column =>
                    {
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("CREDIT NOTE").FontSize(24).Bold().FontColor(Colors.Red.Medium);
                                col.Item().Text($"Credit Note #: {creditNote.CreditNoteNumber}").FontSize(10);
                                col.Item().Text($"Date: {creditNote.CreditNoteDate:dd MMM yyyy}").FontSize(10);
                                if (!string.IsNullOrEmpty(creditNote.InvoiceNumber))
                                {
                                    col.Item().Text($"Original Invoice #: {creditNote.InvoiceNumber}").FontSize(10);
                                }
                            });

                            row.ConstantItem(150).AlignRight().Column(col =>
                            {
                                if (creditNote.Company != null && !string.IsNullOrEmpty(creditNote.Company.CompanyName))
                                {
                                    col.Item().Text(creditNote.Company.CompanyName).FontSize(14).Bold();
                                    if (!string.IsNullOrEmpty(creditNote.Company.CompanyAddressLine1))
                                    {
                                        col.Item().Text(creditNote.Company.CompanyAddressLine1).FontSize(9);
                                    }
                                    if (!string.IsNullOrEmpty(creditNote.Company.CompanyAddressLine2))
                                    {
                                        col.Item().Text(creditNote.Company.CompanyAddressLine2).FontSize(9);
                                    }
                                    if (!string.IsNullOrEmpty(creditNote.Company.CompanyCity))
                                    {
                                        var cityState = creditNote.Company.CompanyCity;
                                        if (!string.IsNullOrEmpty(creditNote.Company.CompanyState))
                                        {
                                            cityState += $", {creditNote.Company.CompanyState}";
                                        }
                                        if (!string.IsNullOrEmpty(creditNote.Company.CompanyPincode))
                                        {
                                            cityState += $" - {creditNote.Company.CompanyPincode}";
                                        }
                                        col.Item().Text(cityState).FontSize(9);
                                    }
                                    if (!string.IsNullOrEmpty(creditNote.Company.CompanyGstin))
                                    {
                                        col.Item().Text($"GSTIN: {creditNote.Company.CompanyGstin}").FontSize(8);
                                    }
                                    if (!string.IsNullOrEmpty(creditNote.Company.CompanyPan))
                                    {
                                        col.Item().Text($"PAN: {creditNote.Company.CompanyPan}").FontSize(8);
                                    }
                                }
                                else
                                {
                                    col.Item().Text("Service Management").FontSize(14).Bold();
                                    col.Item().Text("System").FontSize(10);
                                }
                            });
                        });
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Company and Customer Information
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Credit To:").FontSize(12).Bold();
                                col.Item().PaddingBottom(5);
                                if (creditNote.Customer != null)
                                {
                                    col.Item().Text(creditNote.Customer.Name).FontSize(10);
                                    col.Item().Text(creditNote.Customer.Email).FontSize(10);
                                    col.Item().Text(creditNote.Customer.MobileNumber).FontSize(10);
                                }
                            });

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Service Provider:").FontSize(12).Bold();
                                col.Item().PaddingBottom(5);
                                if (creditNote.ServiceProvider != null)
                                {
                                    col.Item().Text(creditNote.ServiceProvider.Name).FontSize(10);
                                    col.Item().Text(creditNote.ServiceProvider.Email).FontSize(10);
                                    col.Item().Text(creditNote.ServiceProvider.MobileNumber).FontSize(10);
                                }
                                else
                                {
                                    col.Item().Text("N/A").FontSize(10).FontColor(Colors.Grey.Medium);
                                }
                            });
                        });

                        column.Item().PaddingTop(10);

                        // Credit Reason
                        column.Item().Text("Credit Reason").FontSize(14).Bold();
                        column.Item().PaddingBottom(5);
                        column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5);
                        column.Item().Text(creditNote.CreditReason).FontSize(10);

                        column.Item().PaddingTop(10);

                        // Service Details
                        if (creditNote.Service != null)
                        {
                            column.Item().Text("Service Details").FontSize(14).Bold();
                            column.Item().PaddingBottom(5);
                            column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5);

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Service Name:").FontSize(10);
                                row.RelativeItem().Text(creditNote.Service.ServiceName).FontSize(10).Bold();
                            });
                            if (!string.IsNullOrEmpty(creditNote.Service.Description))
                            {
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Description:").FontSize(10);
                                    row.RelativeItem().Text(creditNote.Service.Description).FontSize(10);
                                });
                            }
                        }

                        column.Item().PaddingTop(10);

                        // Credit Note Summary Table
                        column.Item().Text("Credit Note Summary").FontSize(14).Bold();
                        column.Item().PaddingBottom(5);
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Description").Bold();
                                header.Cell().Element(CellStyle).AlignRight().Text("Amount").Bold();
                            });

                            // Subtotal
                            table.Cell().Element(CellStyle).Text("Subtotal");
                            table.Cell().Element(CellStyle).AlignRight().Text($"₹{creditNote.Subtotal:N2}");

                            // Taxes
                            if (creditNote.Taxes != null && creditNote.Taxes.Any())
                            {
                                foreach (var tax in creditNote.Taxes)
                                {
                                    table.Cell().Element(CellStyle).Text(tax.TaxName ?? "Tax");
                                    table.Cell().Element(CellStyle).AlignRight().Text($"₹{tax.TaxAmount:N2}");
                                }
                            }

                            if (creditNote.TotalTaxAmount > 0)
                            {
                                table.Cell().Element(CellStyle).Text("Total Tax Credit");
                                table.Cell().Element(CellStyle).AlignRight().Text($"₹{creditNote.TotalTaxAmount:N2}");
                            }

                            // Discounts
                            if (creditNote.Discounts != null && creditNote.Discounts.Any())
                            {
                                foreach (var discount in creditNote.Discounts)
                                {
                                    table.Cell().Element(CellStyle).Text(discount.DiscountName ?? "Discount");
                                    table.Cell().Element(CellStyle).AlignRight().Text($"-₹{discount.DiscountAmount:N2}").FontColor(Colors.Green.Medium);
                                }
                            }

                            if (creditNote.TotalDiscountAmount > 0)
                            {
                                table.Cell().Element(CellStyle).Text("Total Discount Credit");
                                table.Cell().Element(CellStyle).AlignRight().Text($"₹{creditNote.TotalDiscountAmount:N2}");
                            }

                            // Add-ons
                            if (creditNote.AddOns != null && creditNote.AddOns.Any())
                            {
                                foreach (var addOn in creditNote.AddOns)
                                {
                                    table.Cell().Element(CellStyle).Text(addOn.AddonName);
                                    table.Cell().Element(CellStyle).AlignRight().Text($"₹{addOn.TotalPrice:N2}");
                                }
                            }

                            if (creditNote.TotalAddonAmount > 0)
                            {
                                table.Cell().Element(CellStyle).Text("Total Add-on Credit");
                                table.Cell().Element(CellStyle).AlignRight().Text($"₹{creditNote.TotalAddonAmount:N2}");
                            }

                            // Total Credit Amount
                            table.Cell().Element(CellStyle).Background(Colors.Red.Lighten4).Padding(5).Text("Total Credit Amount").Bold().FontSize(12);
                            table.Cell().Element(CellStyle).Background(Colors.Red.Lighten4).Padding(5).AlignRight().Text($"₹{creditNote.TotalAmount:N2}").Bold().FontSize(12);
                        });

                        column.Item().PaddingTop(10);

                        // Status
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Status:").FontSize(10);
                            row.RelativeItem().Text(creditNote.Status).FontSize(10).Bold()
                                .FontColor(creditNote.Status == "Applied" ? Colors.Green.Medium : 
                                          creditNote.Status == "Cancelled" ? Colors.Red.Medium : 
                                          Colors.Orange.Medium);
                        });

                        // Credit Type
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Credit Type:").FontSize(10);
                            row.RelativeItem().Text(creditNote.CreditType).FontSize(10).Bold();
                        });

                        // Notes
                        if (!string.IsNullOrEmpty(creditNote.Notes))
                        {
                            column.Item().PaddingTop(10);
                            column.Item().Text("Notes:").FontSize(10).Bold();
                            column.Item().Text(creditNote.Notes).FontSize(10);
                        }

                        // Bank Transfer Details (if applied)
                        if (creditNote.Applications != null && creditNote.Applications.Any())
                        {
                            column.Item().PaddingTop(10);
                            column.Item().Text("Refund Details").FontSize(12).Bold();
                            foreach (var application in creditNote.Applications)
                            {
                                column.Item().PaddingTop(5);
                                column.Item().Text($"Applied Amount: ₹{application.AppliedAmount:N2}").FontSize(10);
                                if (!string.IsNullOrEmpty(application.BankName))
                                {
                                    column.Item().Text($"Bank: {application.BankName}").FontSize(10);
                                }
                                if (!string.IsNullOrEmpty(application.TransactionReference))
                                {
                                    column.Item().Text($"Transaction Ref: {application.TransactionReference}").FontSize(10);
                                }
                                column.Item().Text($"Applied Date: {application.ApplicationDate:dd MMM yyyy}").FontSize(10);
                            }
                        }

                        column.Item().PaddingTop(20);

                        // Footer
                        column.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(10)
                            .AlignCenter();
                        
                        if (creditNote.Company != null && !string.IsNullOrEmpty(creditNote.Company.InvoiceFooterText))
                        {
                            column.Item().Text(creditNote.Company.InvoiceFooterText)
                                .FontSize(10)
                                .Italic()
                                .FontColor(Colors.Grey.Medium);
                        }
                        else
                        {
                            column.Item().Text("This is a credit note for the above invoice.")
                                .FontSize(10)
                                .Italic()
                                .FontColor(Colors.Grey.Medium);
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
            });
        });

        var pdfBytes = document.GeneratePdf();
        return Task.FromResult(pdfBytes);
    }

    private static IContainer CellStyle(IContainer container)
    {
        return container
            .BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(5)
            .PaddingHorizontal(5);
    }
}
