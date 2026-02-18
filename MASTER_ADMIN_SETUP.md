# Master Admin Setup Guide

## Overview
This guide explains how to set up the master admin user for the Service Management System.

## Master Admin Credentials
- **Email**: master@theory@gmail.com
- **Password**: master!1!2!3
- **Mobile**: 9999999999 (can be changed)
- **Role**: MasterAdmin

## Step 1: Generate Password Hash

You need to generate the password hash and salt for the master admin user. You can do this by:

### Option A: Using the C# Script
1. Compile and run the `scripts/GenerateMasterAdminHash.cs` file
2. Copy the generated SQL from the console output

### Option B: Using the API (Recommended)
1. Start the API server
2. Use the PasswordHasher service to generate the hash
3. Or use the following SQL with a pre-generated hash (see below)

## Step 2: Create Master Admin User

Run the following SQL script in your PostgreSQL database:

```sql
-- First, ensure MasterAdmin role exists (RoleId = 1)
INSERT INTO roles (id, name, description, is_active, created_at, updated_at)
VALUES (1, 'MasterAdmin', 'Master Administrator', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (id) DO UPDATE SET name = 'MasterAdmin', description = 'Master Administrator', is_active = true;

-- Create master admin user
-- NOTE: Replace 'REPLACE_WITH_HASH' and 'REPLACE_WITH_SALT' with actual values from PasswordHasher
INSERT INTO users (id, name, email, mobile_number, password_hash, password_salt, password_slug, role_id, status_id, verification_status_id, created_at, updated_at)
SELECT 
  gen_random_uuid(),
  'Master Admin',
  'master@theory@gmail.com',
  '9999999999',
  'REPLACE_WITH_HASH', -- Replace with actual hash from PasswordHasher
  'REPLACE_WITH_SALT', -- Replace with actual salt from PasswordHasher
  gen_random_uuid()::text,
  1, -- RoleId for MasterAdmin
  1, -- StatusId for Active
  1, -- VerificationStatusId for Approved
  CURRENT_TIMESTAMP,
  CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM users WHERE email = 'master@theory@gmail.com');

-- Create UsersExtraInfo entry for master admin
INSERT INTO users_extra_info (user_id, full_name, email, phone_number, user_type, role_id, is_verified, verification_status, is_completed, created_at, updated_at)
SELECT 
  u.id,
  'Master Admin',
  u.email,
  u.mobile_number,
  'masteradmin',
  u.role_id,
  true,
  'approved',
  true,
  CURRENT_TIMESTAMP,
  CURRENT_TIMESTAMP
FROM users u
WHERE u.email = 'master@theory@gmail.com'
  AND NOT EXISTS (SELECT 1 FROM users_extra_info WHERE user_id = u.id);
```

## Step 3: Generate Password Hash Using C# Code

If you need to generate the hash manually, use this C# code:

```csharp
using System;
using System.Security.Cryptography;

string password = "master!1!2!3";
const int SaltSize = 16;
const int KeySize = 32;
const int Iterations = 100_000;

// Generate salt
var salt = RandomNumberGenerator.GetBytes(SaltSize);
var saltBase64 = Convert.ToBase64String(salt);

// Hash password
using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
var hash = pbkdf2.GetBytes(KeySize);
var hashBase64 = Convert.ToBase64String(hash);

Console.WriteLine($"Salt: {saltBase64}");
Console.WriteLine($"Hash: {hashBase64}");
```

## Step 4: Verify Master Admin User

After creating the user, verify it exists:

```sql
SELECT u.id, u.email, u.name, r.name as role_name, u.status_id
FROM users u
JOIN roles r ON u.role_id = r.id
WHERE u.email = 'master@theory@gmail.com';
```

## Step 5: Login

1. Start the frontend application
2. Navigate to the login page
3. Use the credentials:
   - Mobile Number: 9999999999 (or the mobile number you set)
   - Password: master!1!2!3
4. You should be redirected to `/super-admin` dashboard

## API Endpoints

All master admin endpoints are available under `/api/MasterAdmin`:

- `GET /api/MasterAdmin/dashboard/stats` - Get dashboard statistics
- `GET /api/MasterAdmin/users` - Get all users
- `GET /api/MasterAdmin/admins` - Get all admins
- `POST /api/MasterAdmin/admins` - Create new admin
- `PUT /api/MasterAdmin/admins/{adminId}/states` - Update admin state assignments
- `DELETE /api/MasterAdmin/admins/{adminId}` - Delete admin
- `GET /api/MasterAdmin/categories` - Get all categories
- `POST /api/MasterAdmin/categories` - Create category
- `PUT /api/MasterAdmin/categories/{categoryId}` - Update category
- `DELETE /api/MasterAdmin/categories/{categoryId}` - Delete category
- `GET /api/MasterAdmin/services` - Get all services
- `POST /api/MasterAdmin/services` - Create service
- `PUT /api/MasterAdmin/services/{serviceId}` - Update service
- `DELETE /api/MasterAdmin/services/{serviceId}` - Delete service
- `GET /api/MasterAdmin/states` - Get all states
- `POST /api/MasterAdmin/states` - Create state
- `PUT /api/MasterAdmin/states/{stateId}` - Update state
- `DELETE /api/MasterAdmin/states/{stateId}` - Delete state
- `GET /api/MasterAdmin/cities` - Get all cities
- `POST /api/MasterAdmin/cities` - Create city
- `PUT /api/MasterAdmin/cities/{cityId}` - Update city
- `DELETE /api/MasterAdmin/cities/{cityId}` - Delete city
- `GET /api/MasterAdmin/verifications` - Get all verifications
- `GET /api/MasterAdmin/bookings` - Get all bookings

## Frontend Changes

The frontend has been updated to:
1. Use API endpoints instead of Supabase for master admin operations
2. Use API authentication (JWT tokens) instead of Supabase auth
3. Check user roles from API user object instead of Supabase queries

## Troubleshooting

### Login Issues
- Ensure the password hash and salt are correctly generated
- Verify the user exists in the database
- Check that the role_id matches the MasterAdmin role (should be 1)

### API Access Issues
- Ensure the API server is running
- Check that `NEXT_PUBLIC_API_BASE_URL` is set in the frontend `.env.local`
- Verify JWT token is being sent in Authorization header

### Permission Issues
- Ensure the user has the MasterAdmin role
- Check that `isSuperAdminRole()` function recognizes "MasterAdmin" role
- Verify the role is normalized correctly (MasterAdmin -> masteradmin)
