-- ============================================
-- SCRIPT THIẾT LẬP SYSDBA
-- Chạy với: SYSDBA (sys/sys)
-- Mục đích: Tạo user schema và cấp quyền
-- ============================================

SET SERVEROUTPUT ON;

PROMPT '========================================';
PROMPT 'Thiết lập SYSDBA - Tạo User & Quyền';
PROMPT '========================================';

-- ============================================
-- 1. CREATE USER QLDiemRenLuyen
-- ============================================

-- Drop user if exists
DECLARE
    user_exists NUMBER;
BEGIN
    SELECT COUNT(*) INTO user_exists FROM DBA_USERS WHERE USERNAME = 'QLDIEMRENLUYEN';
    IF user_exists > 0 THEN
        EXECUTE IMMEDIATE 'DROP USER QLDiemRenLuyen CASCADE';
        DBMS_OUTPUT.PUT_LINE('Dropped existing user QLDiemRenLuyen');
    END IF;
END;
/

-- Create user with appropriate privileges
CREATE USER QLDiemRenLuyen 
    IDENTIFIED BY "123"
    DEFAULT TABLESPACE USERS
    QUOTA UNLIMITED ON USERS;

PROMPT '✓ Created user QLDiemRenLuyen';

-- ============================================
-- 2. GRANT SYSTEM PRIVILEGES
-- ============================================

-- Basic privileges
GRANT CONNECT TO QLDiemRenLuyen;
GRANT RESOURCE TO QLDiemRenLuyen;
GRANT CREATE SESSION TO QLDiemRenLuyen;
GRANT CREATE TABLE TO QLDiemRenLuyen;
GRANT CREATE VIEW TO QLDiemRenLuyen;
GRANT CREATE PROCEDURE TO QLDiemRenLuyen;
GRANT CREATE SEQUENCE TO QLDiemRenLuyen;
GRANT CREATE TRIGGER TO QLDiemRenLuyen;
GRANT CREATE SYNONYM TO QLDiemRenLuyen;

-- Required for encryption
GRANT EXECUTE ON DBMS_CRYPTO TO QLDiemRenLuyen;
GRANT EXECUTE ON UTL_RAW TO QLDiemRenLuyen;
GRANT EXECUTE ON DBMS_RANDOM TO QLDiemRenLuyen;

PROMPT '✓ Granted system privileges';

-- ============================================
-- 3. VERIFICATION
-- ============================================

PROMPT '';
PROMPT '========================================';
PROMPT 'VERIFICATION';
PROMPT '========================================';

SELECT USERNAME, ACCOUNT_STATUS, DEFAULT_TABLESPACE
FROM DBA_USERS
WHERE USERNAME = 'QLDIEMRENLUYEN';

PROMPT '';
PROMPT '✓ SYSDBA Setup Complete!';
PROMPT 'Next: Run Tablespace/Profile/Session scripts';
PROMPT '========================================';