---
name: Credit Note System Architecture (Updated)
overview: Design and implement a rollback-safe, idempotent credit note system for admin-only use. Credit notes are issued against invoices for refunds/adjustments via bank transfer. No approval workflow required. Credit notes do not affect invoice payment status - they are tracked separately for reporting.
requirements:
  - Only admin can generate credit notes
  - No approval workflow (direct creation)
  - Refund method: Bank transfer
  - Support partial credits (no limits)
  - Admin-only visibility and creation
  - Credit notes don't affect invoice payment status
  - Basic reporting needed
todos:
  - id: create_migration
    content: Create SQL migration script (012_create_credit_note_tables.sql) with simplified schema (no approval fields)
    status: pending
  - id: create_domain_entities
    content: Create domain entity classes (CreditNoteMaster, CreditNoteTax, CreditNoteDiscount, CreditNoteAddOn, CreditNoteAuditHistory, CreditNoteApplication)
    status: pending
  - id: update_dbcontext
    content: Update AppDbContext.cs to add DbSets and configure relationships
    status: pending
    dependencies:
      - create_domain_entities
  - id: create_service_interface
    content: Create ICreditNoteService interface
    status: pending
    dependencies:
      - create_domain_entities
  - id: create_dtos
    content: Create DTOs (CreateCreditNoteRequest, CreditNoteResponse, CreditNoteReportResponse)
    status: pending
    dependencies:
      - create_domain_entities
  - id: implement_service
    content: Implement CreditNoteService with idempotent operations, transaction safety, and audit trail
    status: pending
    dependencies:
      - create_service_interface
      - create_dtos
      - update_dbcontext
  - id: create_controller
    content: Create CreditNoteController with admin-only authorization
    status: pending
    dependencies:
      - implement_service
  - id: add_pdf_generation
    content: Extend PDF generation service for credit note PDFs
    status: pending
    dependencies:
      - implement_service
  - id: add_basic_reports
    content: Add basic reporting endpoints (summary, by date range, by customer)
    status: pending
    dependencies:
      - implement_service
---

# Credit Note System Architecture (Updated Based on Requirements)

## Overview

Design a credit note system where:
- **Only admins** can create and view credit notes
- **No approval workflow** - credit notes are created directly
- **Bank transfer** is the refund method
- Credit notes are **tracked separately** from invoice payment status
- Supports **partial and full credits**
- **Basic reporting** for credit notes

## Database Schema Design (Simplified)

### Core Tables

#### 1. `credit_note_master` (Main Credit Note Table)
- `credit_note_id` (INTEGER, Primary Key, Auto-increment) - Matches invoice_master pattern
- `credit_note_number` (VARCHAR(50), Unique) - Format: CN-{YYYYMMDD}-{####}
- `invoice_id` (INTEGER, Foreign Key → invoice_master) - Original invoice
- `booking_id` (UUID, Foreign Key → booking_requests) - Reference to booking
- `customer_id` (UUID, Foreign Key → users)
- `service_provider_id` (UUID, Foreign Key → users, nullable)
- `service_id` (INTEGER, Foreign Key → services)
- `credit_type` (VARCHAR(20)) - 'Full', 'Partial'
- `credit_reason` (TEXT) - Required reason for credit note
- `credit_note_date` (TIMESTAMP WITH TIME ZONE)
- `subtotal` (DECIMAL(18, 2))
- `total_tax_amount` (DECIMAL(18, 2))
- `total_discount_amount` (DECIMAL(18, 2))
- `total_addon_amount` (DECIMAL(18, 2))
- `total_amount` (DECIMAL(18, 2)) - Total credit amount
- `status` (VARCHAR(50)) - 'Issued', 'Applied', 'Cancelled' (no Draft, no Approval)
- `pdf_path` (TEXT, nullable)
- `notes` (TEXT, nullable)
- `created_by` (UUID, Foreign Key → users) - Admin who created
- `created_at` (TIMESTAMP WITH TIME ZONE)
- `updated_at` (TIMESTAMP WITH TIME ZONE)

#### 2. `credit_note_taxes` (Tax Credits)
- `credit_note_tax_id` (INTEGER, Primary Key)
- `credit_note_id` (INTEGER, Foreign Key → credit_note_master)
- `tax_id` (INTEGER, Foreign Key → tax_master)
- `tax_name` (VARCHAR(100))
- `tax_percentage` (DECIMAL(5, 2))
- `taxable_amount` (DECIMAL(18, 2))
- `tax_amount` (DECIMAL(18, 2))
- `created_at` (TIMESTAMP WITH TIME ZONE)

#### 3. `credit_note_discounts` (Discount Credits)
- `credit_note_discount_id` (INTEGER, Primary Key)
- `credit_note_id` (INTEGER, Foreign Key → credit_note_master)
- `discount_id` (INTEGER, Foreign Key → discount_master, nullable)
- `discount_name` (VARCHAR(255))
- `discount_type` (VARCHAR(50))
- `discount_value` (DECIMAL(18, 2))
- `discount_amount` (DECIMAL(18, 2))
- `created_at` (TIMESTAMP WITH TIME ZONE)

#### 4. `credit_note_add_ons` (Add-On Credits)
- `credit_note_addon_id` (INTEGER, Primary Key)
- `credit_note_id` (INTEGER, Foreign Key → credit_note_master)
- `addon_name` (VARCHAR(255))
- `addon_description` (TEXT)
- `quantity` (INTEGER, default 1)
- `unit_price` (DECIMAL(18, 2))
- `total_price` (DECIMAL(18, 2))
- `created_at` (TIMESTAMP WITH TIME ZONE)

#### 5. `credit_note_audit_history` (Audit Trail)
- `audit_id` (INTEGER, Primary Key)
- `credit_note_id` (INTEGER, Foreign Key → credit_note_master)
- `action` (VARCHAR(50)) - 'Created', 'Updated', 'StatusChanged', 'Cancelled', 'Applied'
- `old_status` (VARCHAR(50), nullable)
- `new_status` (VARCHAR(50), nullable)
- `changed_by` (UUID, Foreign Key → users)
- `change_description` (TEXT, nullable)
- `created_at` (TIMESTAMP WITH TIME ZONE)

#### 6. `credit_note_application` (Bank Transfer Tracking)
- `application_id` (INTEGER, Primary Key)
- `credit_note_id` (INTEGER, Foreign Key → credit_note_master)
- `invoice_id` (INTEGER, Foreign Key → invoice_master)
- `applied_amount` (DECIMAL(18, 2))
- `application_date` (TIMESTAMP WITH TIME ZONE)
- `bank_account_number` (VARCHAR(50), nullable) - Customer bank account
- `bank_name` (VARCHAR(255), nullable)
- `transaction_reference` (VARCHAR(100), nullable) - Bank transaction reference
- `applied_by` (UUID, Foreign Key → users) - Admin who processed refund
- `notes` (TEXT, nullable)
- `created_at` (TIMESTAMP WITH TIME ZONE)

## Key Design Decisions

1. **No Approval Workflow**: Credit notes are created with status 'Issued' directly
2. **Admin Only**: All endpoints use `[AuthorizeAdmin]` attribute
3. **Separate from Payment Status**: Credit notes don't modify invoice payment_status
4. **Bank Transfer Tracking**: credit_note_application table tracks refund details
5. **Idempotent**: Check for existing credit note before creation
6. **Audit Trail**: All changes logged to credit_note_audit_history

## API Endpoints

### Credit Note Management (Admin Only)
- `POST /api/creditnote/create` - Create credit note for an invoice
- `GET /api/creditnote/{creditNoteId}` - Get credit note details
- `GET /api/creditnote/invoice/{invoiceId}` - Get all credit notes for an invoice
- `GET /api/creditnote/list` - List all credit notes (with filters)
- `PUT /api/creditnote/{creditNoteId}/apply` - Mark credit note as applied (with bank transfer details)
- `PUT /api/creditnote/{creditNoteId}/cancel` - Cancel a credit note
- `GET /api/creditnote/{creditNoteId}/pdf` - Download credit note PDF

### Basic Reports (Admin Only)
- `GET /api/creditnote/reports/summary` - Summary report (total credits, count, by status)
- `GET /api/creditnote/reports/by-date` - Credits by date range
- `GET /api/creditnote/reports/by-customer` - Credits by customer

## Status Workflow

```
Created → Issued → Applied (with bank transfer) / Cancelled
```

No draft or approval states.

## Implementation Notes

- Use INTEGER IDs to match invoice_master pattern (not UUID)
- Credit note number format: CN-{YYYYMMDD}-{####}
- All operations wrapped in transactions
- Audit trail automatically created on status changes
- PDF generation reuses invoice PDF service pattern
- Bank transfer details stored when credit note is applied
