-- =========================================================
-- RBAC IMPLEMENTATION - PART A (Run as SYSDBA)
-- =========================================================
-- Connection: sysDBA (SYSDBA role)
-- Purpose: Create database roles and grant system privileges
-- =========================================================

SET SERVEROUTPUT ON;

PROMPT '========================================';
PROMPT 'RBAC PART A - Creating Database Roles';
PROMPT 'Executing as: SYSDBA';
PROMPT '========================================';

-- =========================================================
-- STEP 1: DROP EXISTING ROLES (if re-running)
-- =========================================================

DECLARE
    role_exists NUMBER;
BEGIN
    SELECT COUNT(*) INTO role_exists FROM DBA_ROLES WHERE ROLE = 'ROLE_STUDENT';
    IF role_exists > 0 THEN
        EXECUTE IMMEDIATE 'DROP ROLE ROLE_STUDENT';
        DBMS_OUTPUT.PUT_LINE('✓ Dropped existing ROLE_STUDENT');
    END IF;
    
    SELECT COUNT(*) INTO role_exists FROM DBA_ROLES WHERE ROLE = 'ROLE_LECTURER';
    IF role_exists > 0 THEN
        EXECUTE IMMEDIATE 'DROP ROLE ROLE_LECTURER';
        DBMS_OUTPUT.PUT_LINE('✓ Dropped existing ROLE_LECTURER');
    END IF;
    
    SELECT COUNT(*) INTO role_exists FROM DBA_ROLES WHERE ROLE = 'ROLE_ADMIN';
    IF role_exists > 0 THEN
        EXECUTE IMMEDIATE 'DROP ROLE ROLE_ADMIN';
        DBMS_OUTPUT.PUT_LINE('✓ Dropped existing ROLE_ADMIN');
    END IF;
    
    SELECT COUNT(*) INTO role_exists FROM DBA_ROLES WHERE ROLE = 'ROLE_READONLY';
    IF role_exists > 0 THEN
        EXECUTE IMMEDIATE 'DROP ROLE ROLE_READONLY';
        DBMS_OUTPUT.PUT_LINE('✓ Dropped existing ROLE_READONLY');
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('No existing roles to drop');
END;
/

-- =========================================================
-- STEP 2: CREATE DATABASE ROLES
-- =========================================================

CREATE ROLE ROLE_STUDENT;
CREATE ROLE ROLE_LECTURER;
CREATE ROLE ROLE_ADMIN;
CREATE ROLE ROLE_READONLY;

PROMPT '✓ Created 4 database roles';

-- =========================================================
-- STEP 3: GRANT BASIC SYSTEM PRIVILEGES
-- =========================================================

-- All roles need CREATE SESSION to connect
GRANT CREATE SESSION TO ROLE_STUDENT;
GRANT CREATE SESSION TO ROLE_LECTURER;
GRANT CREATE SESSION TO ROLE_ADMIN;
GRANT CREATE SESSION TO ROLE_READONLY;

PROMPT '✓ Granted CREATE SESSION to all roles';

-- =========================================================
-- STEP 4: GRANT ROLES TO SCHEMA OWNER (QLDiemRenLuyen)
-- =========================================================
-- This allows QLDiemRenLuyen to grant object privileges to these roles

GRANT ROLE_STUDENT TO QLDiemRenLuyen WITH ADMIN OPTION;
GRANT ROLE_LECTURER TO QLDiemRenLuyen WITH ADMIN OPTION;
GRANT ROLE_ADMIN TO QLDiemRenLuyen WITH ADMIN OPTION;
GRANT ROLE_READONLY TO QLDiemRenLuyen WITH ADMIN OPTION;

PROMPT '✓ Granted roles to QLDiemRenLuyen with ADMIN OPTION';

-- =========================================================
-- VERIFICATION
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'VERIFICATION - Roles Created';
PROMPT '========================================';

SELECT ROLE FROM DBA_ROLES WHERE ROLE LIKE 'ROLE_%' ORDER BY ROLE;

PROMPT '';
PROMPT '✓ PART A COMPLETED SUCCESSFULLY!';
PROMPT 'Next: Run Part B as QLDiemRenLuyen';
PROMPT '========================================';
