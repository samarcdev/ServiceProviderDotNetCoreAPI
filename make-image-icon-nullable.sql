-- SQL script to make image and icon columns nullable in categories and services tables
-- Run this script on your PostgreSQL database

-- Make categories.image nullable
ALTER TABLE categories 
    ALTER COLUMN image DROP NOT NULL;

-- Make categories.icon nullable
ALTER TABLE categories 
    ALTER COLUMN icon DROP NOT NULL;

-- Make services.image nullable
ALTER TABLE services 
    ALTER COLUMN image DROP NOT NULL;

-- Make services.icon nullable
ALTER TABLE services 
    ALTER COLUMN icon DROP NOT NULL;
