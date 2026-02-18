# Invoice Generation Feature - Production Ready Implementation

## Overview
This document describes the production-ready invoice generation feature that creates invoices for completed service bookings, generates PDF invoices, and provides a foundation for notification services.

## Database Tables Used

### Existing Tables (Leveraged)
1. **booking_requests** - Contains all pricing and service information
2. **users** - Customer and service provider information
3. **services** - Service details
4. **tax_master** - Tax configuration
5. **company_configuration** - Company details (extended for invoices)
6. **discount_master** - Discount information
7. **booking_statuses** - Booking status tracking

### Existing Invoice Tables Used
1. **invoice_master** - Main invoice table storing core invoice information
   - Unique invoice numbers
   - Payment status tracking
   - PDF storage path
   - Summary amounts (subtotal, total_tax_amount, total_discount_amount, total_addon_amount, total_amount)
   - Full audit trail (created_at, updated_at)

2. **invoice_taxes** - Detailed tax breakdown for each invoice
   - Links to tax_master for tax configuration
   - Stores tax name, percentage, taxable amount, and tax amount
   - Supports multiple taxes per invoice (CGST, SGST, IGST, etc.)

3. **invoice_discounts** - Discount details applied to invoices
   - Links to discount_master for discount configuration
   - Stores discount type, value, and calculated amount
   - Supports multiple discounts per invoice

4. **invoice_add_ons** - Additional charges/add-ons for invoices
   - Stores addon name, description, quantity, unit price, and total price
   - Used for location adjustments, service charges, platform charges, etc.
   - Supports multiple add-ons per invoice

## Features Implemented

### 1. Invoice Generation Service (`InvoiceService`)
- ✅ **Transaction-based processing** - Ensures data consistency
- ✅ **Automatic booking completion** - Marks booking as completed when invoice is generated
- ✅ **Duplicate prevention** - Checks for existing invoices before creation
- ✅ **Validation** - Validates pricing information and amounts
- ✅ **Company configuration integration** - Uses company settings for invoice prefix and payment terms
- ✅ **Unique invoice numbering** - Generates sequential invoice numbers with collision detection
- ✅ **Error handling** - Comprehensive error handling with rollback on failure
- ✅ **Logging** - Detailed logging for debugging and monitoring

### 2. PDF Generation Service (`PdfGenerationService`)
- ✅ **Professional invoice layout** - Clean, professional PDF design
- ✅ **Company branding** - Displays company information from configuration
- ✅ **Complete invoice details** - Shows all pricing breakdown, taxes, discounts
- ✅ **Customer and provider information** - Full contact details
- ✅ **GST/PAN display** - Shows tax identification numbers
- ✅ **Customizable footer** - Uses company footer text or default message

### 3. Notification Service (`EmailNotificationService`)
- ✅ **Email service** - SMTP-based email sending
- ✅ **Extensible design** - Ready for SMS and WhatsApp integration
- ✅ **Configuration-based** - Uses appsettings.json for SMTP configuration
- ✅ **Error handling** - Graceful failure handling

### 4. API Endpoints (`InvoiceController`)
- ✅ `POST /api/invoice/generate` - Generate invoice for completed booking
- ✅ `GET /api/invoice/{invoiceId}` - Get invoice details by ID
- ✅ `GET /api/invoice/booking/{bookingId}` - Get invoice by booking ID
- ✅ `GET /api/invoice/{invoiceId}/pdf` - Download invoice PDF

## Database Migrations Required

### Migration 1: Alter Existing Invoice Tables
**File:** `scripts/009_alter_invoice_tables_structure.sql`
- Ensures invoice_master table has all required columns
- Creates invoice_taxes, invoice_discounts, and invoice_add_ons tables if they don't exist
- Sets up foreign key relationships
- Creates indexes for performance
- Adds unique constraints and check constraints
- **Note:** This migration is idempotent - it only adds columns/tables that don't exist

### Migration 2: Extend Company Configuration
**File:** `scripts/010_extend_company_configuration_for_invoices.sql`
- Adds company details (name, address, GST, PAN, etc.)
- Adds invoice configuration (prefix, payment terms, footer text)

## Configuration Required

### 1. Email Settings (appsettings.json)
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "FromEmail": "your-email@gmail.com",
    "FromName": "Service Management System",
    "EnableSsl": "true"
  }
}
```

### 2. Company Configuration (Database)
After running migration `010_extend_company_configuration_for_invoices.sql`, update the `company_configuration` table:

```sql
UPDATE company_configuration 
SET 
    company_name = 'Your Company Name',
    company_address_line1 = '123 Main Street',
    company_city = 'City Name',
    company_pincode = '123456',
    company_phone = '+91-1234567890',
    company_email = 'info@company.com',
    company_gstin = 'GSTIN123456789',
    company_pan = 'PAN1234567',
    invoice_prefix = 'INV',
    payment_terms_days = 30,
    invoice_footer_text = 'Thank you for your business!'
WHERE is_active = true;
```

## Production Readiness Features

### ✅ Data Integrity
- Database transactions ensure atomicity
- Foreign key constraints prevent orphaned records
- Unique constraints prevent duplicate invoices
- Validation prevents invalid data
- **Normalized structure** - Taxes, discounts, and add-ons stored in separate tables to avoid data duplication
- **Referential integrity** - Links to tax_master and discount_master for consistency

### ✅ Error Handling
- Comprehensive try-catch blocks
- Transaction rollback on errors
- Detailed error logging
- User-friendly error messages

### ✅ Performance
- Database indexes on frequently queried fields
- Efficient query patterns with Include()
- AsNoTracking() for read-only queries
- PDF caching (stored in file system)

### ✅ Security
- Authorization checks on endpoints
- Input validation
- SQL injection prevention (EF Core parameterized queries)
- File path validation

### ✅ Monitoring & Logging
- Structured logging with ILogger
- Log levels (Information, Warning, Error)
- Contextual information in logs
- Error tracking ready

### ✅ Scalability
- Stateless service design
- File storage abstraction (can switch to cloud storage)
- Database connection pooling
- Async/await throughout

## Usage Example

### Generate Invoice
```http
POST /api/invoice/generate
Authorization: Bearer {token}
Content-Type: application/json

{
  "bookingId": "guid-here",
  "notes": "Optional notes"
}
```

### Get Invoice PDF
```http
GET /api/invoice/{invoiceId}/pdf
Authorization: Bearer {token}
```

## Future Enhancements

1. **Payment Integration** - Track invoice payments
2. **Email Notifications** - Send invoice PDFs via email
3. **SMS/WhatsApp** - Send invoice links via SMS/WhatsApp
4. **Invoice Status Updates** - Webhook for payment status changes
5. **Bulk Invoice Generation** - Generate multiple invoices at once
6. **Invoice Templates** - Multiple PDF templates
7. **Multi-currency Support** - Handle different currencies
8. **Invoice History** - Track invoice modifications

## Testing Checklist

- [ ] Generate invoice for completed booking
- [ ] Verify invoice number uniqueness
- [ ] Verify PDF generation
- [ ] Verify database transaction rollback on error
- [ ] Verify duplicate invoice prevention
- [ ] Verify booking status update
- [ ] Test with missing company configuration
- [ ] Test with missing pricing information
- [ ] Verify PDF download endpoint
- [ ] Test authorization on all endpoints

## Notes

- Invoice generation automatically marks booking as completed if not already
- PDF is generated and stored in file system (configurable path)
- Company configuration is optional - defaults are used if not configured
- Invoice numbers follow format: `{PREFIX}-{YYYYMMDD}-{####}`
- Payment terms default to 30 days (configurable in company_configuration)
