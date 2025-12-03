-- =========================================================
-- DATABASE REBUILD SCRIPT
-- File: REBUILD_DATABASE.sql
-- Description: Chạy tất cả scripts theo thứ tự để tạo lại database
-- =========================================================

PROMPT '========================================';
PROMPT 'REBUILD DATABASE - QuanLyDiemRenLuyen';
PROMPT 'Starting full database rebuild...';
PROMPT '========================================';

-- =========================================================
-- Step 1: Drop all existing objects
-- =========================================================
PROMPT '';
PROMPT 'Step 1/6: Dropping existing objects...';
@@000_DROP_ALL.sql

-- =========================================================
-- Step 2: Create base schema
-- =========================================================
PROMPT '';
PROMPT 'Step 2/6: Creating base schema...';
@@db.sql

-- =========================================================
-- Step 3: Run migration 000 - Encryption Infrastructure
-- =========================================================
PROMPT '';
PROMPT 'Step 3/6: Creating encryption infrastructure...';
@@migrations/000_CREATE_ENCRYPTION_INFRASTRUCTURE.sql

-- =========================================================
-- Step 4: Run migration 001 - Encrypted Student Columns
-- =========================================================
PROMPT '';
PROMPT 'Step 4/6: Adding encrypted columns to STUDENTS...';
@@migrations/001_ADD_ENCRYPTED_COLUMNS_STUDENTS.sql

-- =========================================================
-- Step 5: Run migration 002 - Digital Signatures
-- =========================================================
PROMPT '';
PROMPT 'Step 5/6: Adding digital signatures for SCORES...';
@@migrations/002_ADD_DIGITAL_SIGNATURE_SCORES.sql

-- =========================================================
-- Step 6: Run migration 003 - Encrypted Feedbacks
-- =========================================================
PROMPT '';
PROMPT 'Step 6/6: Adding encrypted feedbacks...';
@@migrations/003_ADD_ENCRYPTED_FEEDBACKS.sql

-- =========================================================
-- Verification
-- =========================================================
PROMPT '';
PROMPT '========================================';
PROMPT 'DATABASE REBUILD COMPLETED!';
PROMPT '========================================';
PROMPT '';
PROMPT 'Verification Results:';
PROMPT '';

-- Count tables
SELECT 'Tables: ' || COUNT(*) as SUMMARY
FROM USER_TABLES;

-- Count packages
SELECT 'Packages: ' || COUNT(*) as SUMMARY  
FROM USER_OBJECTS
WHERE OBJECT_TYPE = 'PACKAGE';

-- List all tables
PROMPT '';
PROMPT 'Created Tables:';
SELECT TABLE_NAME 
FROM USER_TABLES
ORDER BY TABLE_NAME;

PROMPT '';
PROMPT 'Created Packages:';
SELECT OBJECT_NAME
FROM USER_OBJECTS  
WHERE OBJECT_TYPE = 'PACKAGE'
AND STATUS = 'VALID'
ORDER BY OBJECT_NAME;

PROMPT '';
PROMPT '========================================';
PROMPT 'Ready for use!';
PROMPT '========================================';
