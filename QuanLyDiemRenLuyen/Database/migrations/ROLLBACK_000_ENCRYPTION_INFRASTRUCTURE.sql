-- =========================================================
-- ROLLBACK/CLEANUP SCRIPT
-- File: ROLLBACK_000_ENCRYPTION_INFRASTRUCTURE.sql
-- Description: Xóa toàn bộ encryption infrastructure để chạy lại từ đầu
-- Run with: QLDiemRenLuyen connection
-- =========================================================

SET SERVEROUTPUT ON;

BEGIN
    DBMS_OUTPUT.PUT_LINE('=== Starting Rollback/Cleanup ===');
    DBMS_OUTPUT.PUT_LINE('');
END;
/

-- =========================================================
-- 1) Drop Packages (nếu tồn tại)
-- =========================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('1) Dropping packages...');
    
    -- Drop PKG_STUDENT_ENCRYPTION
    BEGIN
        EXECUTE IMMEDIATE 'DROP PACKAGE PKG_STUDENT_ENCRYPTION';
        DBMS_OUTPUT.PUT_LINE('   ✓ Dropped PKG_STUDENT_ENCRYPTION');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE = -4043 THEN
                DBMS_OUTPUT.PUT_LINE('   - PKG_STUDENT_ENCRYPTION does not exist (OK)');
            ELSE
                DBMS_OUTPUT.PUT_LINE('   ! Error dropping PKG_STUDENT_ENCRYPTION: ' || SQLERRM);
            END IF;
    END;
    
    -- Drop PKG_RSA_CRYPTO
    BEGIN
        EXECUTE IMMEDIATE 'DROP PACKAGE PKG_RSA_CRYPTO';
        DBMS_OUTPUT.PUT_LINE('   ✓ Dropped PKG_RSA_CRYPTO');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE = -4043 THEN
                DBMS_OUTPUT.PUT_LINE('   - PKG_RSA_CRYPTO does not exist (OK)');
            ELSE
                DBMS_OUTPUT.PUT_LINE('   ! Error dropping PKG_RSA_CRYPTO: ' || SQLERRM);
            END IF;
    END;
    
    DBMS_OUTPUT.PUT_LINE('');
END;
/

-- =========================================================
-- 2) Drop Foreign Key Constraints trên STUDENTS (nếu có)
-- =========================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('2) Dropping foreign key constraints...');
    
    BEGIN
        EXECUTE IMMEDIATE 'ALTER TABLE STUDENTS DROP CONSTRAINT FK_STUDENTS_ENCKEY';
        DBMS_OUTPUT.PUT_LINE('   ✓ Dropped FK_STUDENTS_ENCKEY');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE = -2443 THEN
                DBMS_OUTPUT.PUT_LINE('   - FK_STUDENTS_ENCKEY does not exist (OK)');
            ELSE
                DBMS_OUTPUT.PUT_LINE('   ! Error: ' || SQLERRM);
            END IF;
    END;
    
    DBMS_OUTPUT.PUT_LINE('');
END;
/

-- =========================================================
-- 3) Drop Columns từ STUDENTS (nếu có)
-- =========================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('3) Dropping columns from STUDENTS...');
    
    -- Drop PHONE_ENCRYPTED
    BEGIN
        EXECUTE IMMEDIATE 'ALTER TABLE STUDENTS DROP COLUMN PHONE_ENCRYPTED';
        DBMS_OUTPUT.PUT_LINE('   ✓ Dropped PHONE_ENCRYPTED');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE = -904 THEN
                DBMS_OUTPUT.PUT_LINE('   - PHONE_ENCRYPTED does not exist (OK)');
            ELSE
                DBMS_OUTPUT.PUT_LINE('   ! Error: ' || SQLERRM);
            END IF;
    END;
    
    -- Drop ADDRESS_ENCRYPTED
    BEGIN
        EXECUTE IMMEDIATE 'ALTER TABLE STUDENTS DROP COLUMN ADDRESS_ENCRYPTED';
        DBMS_OUTPUT.PUT_LINE('   ✓ Dropped ADDRESS_ENCRYPTED');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE = -904 THEN
                DBMS_OUTPUT.PUT_LINE('   - ADDRESS_ENCRYPTED does not exist (OK)');
            ELSE
                DBMS_OUTPUT.PUT_LINE('   ! Error: ' || SQLERRM);
            END IF;
    END;
    
    -- Drop ID_CARD_NUMBER
    BEGIN
        EXECUTE IMMEDIATE 'ALTER TABLE STUDENTS DROP COLUMN ID_CARD_NUMBER';
        DBMS_OUTPUT.PUT_LINE('   ✓ Dropped ID_CARD_NUMBER');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE = -904 THEN
                DBMS_OUTPUT.PUT_LINE('   - ID_CARD_NUMBER does not exist (OK)');
            ELSE
                DBMS_OUTPUT.PUT_LINE('   ! Error: ' || SQLERRM);
            END IF;
    END;
    
    -- Drop ID_CARD_ENCRYPTED
    BEGIN
        EXECUTE IMMEDIATE 'ALTER TABLE STUDENTS DROP COLUMN ID_CARD_ENCRYPTED';
        DBMS_OUTPUT.PUT_LINE('   ✓ Dropped ID_CARD_ENCRYPTED');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE = -904 THEN
                DBMS_OUTPUT.PUT_LINE('   - ID_CARD_ENCRYPTED does not exist (OK)');
            ELSE
                DBMS_OUTPUT.PUT_LINE('   ! Error: ' || SQLERRM);
            END IF;
    END;
    
    -- Drop ENCRYPTED_AT
    BEGIN
        EXECUTE IMMEDIATE 'ALTER TABLE STUDENTS DROP COLUMN ENCRYPTED_AT';
        DBMS_OUTPUT.PUT_LINE('   ✓ Dropped ENCRYPTED_AT');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE = -904 THEN
                DBMS_OUTPUT.PUT_LINE('   - ENCRYPTED_AT does not exist (OK)');
            ELSE
                DBMS_OUTPUT.PUT_LINE('   ! Error: ' || SQLERRM);
            END IF;
    END;
    
    -- Drop ENCRYPTION_KEY_ID
    BEGIN
        EXECUTE IMMEDIATE 'ALTER TABLE STUDENTS DROP COLUMN ENCRYPTION_KEY_ID';
        DBMS_OUTPUT.PUT_LINE('   ✓ Dropped ENCRYPTION_KEY_ID');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE = -904 THEN
                DBMS_OUTPUT.PUT_LINE('   - ENCRYPTION_KEY_ID does not exist (OK)');
            ELSE
                DBMS_OUTPUT.PUT_LINE('   ! Error: ' || SQLERRM);
            END IF;
    END;
    
    DBMS_OUTPUT.PUT_LINE('');
END;
/

-- =========================================================
-- 4) Drop Indexes trên ENCRYPTION_KEYS (nếu tồn tại)
-- =========================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('4) Dropping indexes on ENCRYPTION_KEYS...');
    
    -- Drop IX_ENCKEY_NAME
    BEGIN
        EXECUTE IMMEDIATE 'DROP INDEX IX_ENCKEY_NAME';
        DBMS_OUTPUT.PUT_LINE('   ✓ Dropped IX_ENCKEY_NAME');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE = -1418 THEN
                DBMS_OUTPUT.PUT_LINE('   - IX_ENCKEY_NAME does not exist (OK)');
            ELSE
                DBMS_OUTPUT.PUT_LINE('   ! Error: ' || SQLERRM);
            END IF;
    END;
    
    -- Drop IX_ENCKEY_ACTIVE
    BEGIN
        EXECUTE IMMEDIATE 'DROP INDEX IX_ENCKEY_ACTIVE';
        DBMS_OUTPUT.PUT_LINE('   ✓ Dropped IX_ENCKEY_ACTIVE');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE = -1418 THEN
                DBMS_OUTPUT.PUT_LINE('   - IX_ENCKEY_ACTIVE does not exist (OK)');
            ELSE
                DBMS_OUTPUT.PUT_LINE('   ! Error: ' || SQLERRM);
            END IF;
    END;
    
    -- Drop IX_ENCKEY_CREATED
    BEGIN
        EXECUTE IMMEDIATE 'DROP INDEX IX_ENCKEY_CREATED';
        DBMS_OUTPUT.PUT_LINE('   ✓ Dropped IX_ENCKEY_CREATED');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE = -1418 THEN
                DBMS_OUTPUT.PUT_LINE('   - IX_ENCKEY_CREATED does not exist (OK)');
            ELSE
                DBMS_OUTPUT.PUT_LINE('   ! Error: ' || SQLERRM);
            END IF;
    END;
    
    DBMS_OUTPUT.PUT_LINE('');
END;
/

-- =========================================================
-- 5) Drop Table ENCRYPTION_KEYS (nếu tồn tại)
-- =========================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('5) Dropping ENCRYPTION_KEYS table...');
    
    BEGIN
        EXECUTE IMMEDIATE 'DROP TABLE ENCRYPTION_KEYS CASCADE CONSTRAINTS';
        DBMS_OUTPUT.PUT_LINE('   ✓ Dropped ENCRYPTION_KEYS table');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE = -942 THEN
                DBMS_OUTPUT.PUT_LINE('   - ENCRYPTION_KEYS table does not exist (OK)');
            ELSE
                DBMS_OUTPUT.PUT_LINE('   ! Error dropping ENCRYPTION_KEYS: ' || SQLERRM);
            END IF;
    END;
    
    DBMS_OUTPUT.PUT_LINE('');
END;
/

-- =========================================================
-- 6) Verification
-- =========================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('6) Verification - checking if cleanup was successful...');
    DBMS_OUTPUT.PUT_LINE('');
END;
/

-- Check tables
SELECT 'Tables remaining:' AS INFO FROM DUAL;
SELECT table_name 
FROM user_tables 
WHERE table_name IN ('ENCRYPTION_KEYS');

-- Check packages
SELECT 'Packages remaining:' AS INFO FROM DUAL;
SELECT object_name, object_type 
FROM user_objects 
WHERE object_name IN ('PKG_RSA_CRYPTO', 'PKG_STUDENT_ENCRYPTION');

-- Check STUDENTS columns
SELECT 'STUDENTS encrypted columns remaining:' AS INFO FROM DUAL;
SELECT column_name 
FROM user_tab_columns 
WHERE table_name = 'STUDENTS' 
AND column_name LIKE '%ENCRYPTED%' OR column_name = 'ENCRYPTION_KEY_ID';

BEGIN
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('=== Cleanup Completed ===');
    DBMS_OUTPUT.PUT_LINE('If verification shows no results, system is clean and ready for fresh installation.');
    DBMS_OUTPUT.PUT_LINE('');
END;
/
