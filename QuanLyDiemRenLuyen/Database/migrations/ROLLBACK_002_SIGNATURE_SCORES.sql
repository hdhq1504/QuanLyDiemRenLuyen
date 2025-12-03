-- =========================================================
-- ROLLBACK MIGRATION 002: Digital Signature SCORES
-- Cleanup script - chạy TRƯỚC khi chạy lại migration 002
-- =========================================================

PROMPT 'Starting rollback migration 002...';

-- 1) Drop audit table (nếu tồn tại)
BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE SCORE_AUDIT_SIGNATURES CASCADE CONSTRAINTS';
    DBMS_OUTPUT.PUT_LINE('✓ Dropped SCORE_AUDIT_SIGNATURES table');
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE = -942 THEN  -- Table does not exist
            DBMS_OUTPUT.PUT_LINE('- SCORE_AUDIT_SIGNATURES does not exist (OK)');
        ELSE
            RAISE;
        END IF;
END;
/

-- 2) Drop package body
BEGIN
    EXECUTE IMMEDIATE 'DROP PACKAGE BODY PKG_SCORE_SIGNATURE';
    DBMS_OUTPUT.PUT_LINE('✓ Dropped PKG_SCORE_SIGNATURE body');
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE = -4043 THEN  -- Object does not exist
            DBMS_OUTPUT.PUT_LINE('- PKG_SCORE_SIGNATURE body does not exist (OK)');
        ELSE
            RAISE;
        END IF;
END;
/

-- 3) Drop package spec
BEGIN
    EXECUTE IMMEDIATE 'DROP PACKAGE PKG_SCORE_SIGNATURE';
    DBMS_OUTPUT.PUT_LINE('✓ Dropped PKG_SCORE_SIGNATURE package');
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE = -4043 THEN
            DBMS_OUTPUT.PUT_LINE('- PKG_SCORE_SIGNATURE does not exist (OK)');
        ELSE
            RAISE;
        END IF;
END;
/

-- 4) Drop indexes on SCORES (nếu tồn tại)
BEGIN
    EXECUTE IMMEDIATE 'DROP INDEX IX_SCORES_SIGKEY';
    DBMS_OUTPUT.PUT_LINE('✓ Dropped IX_SCORES_SIGKEY');
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE = -1418 THEN  -- Index does not exist
            DBMS_OUTPUT.PUT_LINE('- IX_SCORES_SIGKEY does not exist (OK)');
        ELSE
            RAISE;
        END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP INDEX IX_SCORES_SIGNED';
    DBMS_OUTPUT.PUT_LINE('✓ Dropped IX_SCORES_SIGNED');
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE = -1418 THEN
            DBMS_OUTPUT.PUT_LINE('- IX_SCORES_SIGNED does not exist (OK)');
        ELSE
            RAISE;
        END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP INDEX IX_SCORES_VERIFIED';
    DBMS_OUTPUT.PUT_LINE('✓ Dropped IX_SCORES_VERIFIED');
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE = -1418 THEN
            DBMS_OUTPUT.PUT_LINE('- IX_SCORES_VERIFIED does not exist (OK)');
        ELSE
            RAISE;
        END IF;
END;
/

-- 5) Drop columns from SCORES (nếu tồn tại)
DECLARE
    v_column_exists NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_column_exists
    FROM USER_TAB_COLUMNS
    WHERE TABLE_NAME = 'SCORES' AND COLUMN_NAME = 'DIGITAL_SIGNATURE';
    
    IF v_column_exists > 0 THEN
        EXECUTE IMMEDIATE 'ALTER TABLE SCORES DROP COLUMN DIGITAL_SIGNATURE';
        DBMS_OUTPUT.PUT_LINE('✓ Dropped DIGITAL_SIGNATURE column');
    ELSE
        DBMS_OUTPUT.PUT_LINE('- DIGITAL_SIGNATURE column does not exist (OK)');
    END IF;
END;
/

DECLARE
    v_column_exists NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_column_exists
    FROM USER_TAB_COLUMNS
    WHERE TABLE_NAME = 'SCORES' AND COLUMN_NAME = 'SIGNATURE_ALGORITHM';
    
    IF v_column_exists > 0 THEN
        EXECUTE IMMEDIATE 'ALTER TABLE SCORES DROP COLUMN SIGNATURE_ALGORITHM';
        DBMS_OUTPUT.PUT_LINE('✓ Dropped SIGNATURE_ALGORITHM column');
    ELSE
        DBMS_OUTPUT.PUT_LINE('- SIGNATURE_ALGORITHM column does not exist (OK)');
    END IF;
END;
/

DECLARE
    v_column_exists NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_column_exists
    FROM USER_TAB_COLUMNS
    WHERE TABLE_NAME = 'SCORES' AND COLUMN_NAME = 'SIGNED_DATA_HASH';
    
    IF v_column_exists > 0 THEN
        EXECUTE IMMEDIATE 'ALTER TABLE SCORES DROP COLUMN SIGNED_DATA_HASH';
        DBMS_OUTPUT.PUT_LINE('✓ Dropped SIGNED_DATA_HASH column');
    ELSE
        DBMS_OUTPUT.PUT_LINE('- SIGNED_DATA_HASH column does not exist (OK)');
    END IF;
END;
/

DECLARE
    v_column_exists NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_column_exists
    FROM USER_TAB_COLUMNS
    WHERE TABLE_NAME = 'SCORES' AND COLUMN_NAME = 'SIGNATURE_KEY_ID';
    
    IF v_column_exists > 0 THEN
        EXECUTE IMMEDIATE 'ALTER TABLE SCORES DROP COLUMN SIGNATURE_KEY_ID';
        DBMS_OUTPUT.PUT_LINE('✓ Dropped SIGNATURE_KEY_ID column');
    ELSE
        DBMS_OUTPUT.PUT_LINE('- SIGNATURE_KEY_ID column does not exist (OK)');
    END IF;
END;
/

DECLARE
    v_column_exists NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_column_exists
    FROM USER_TAB_COLUMNS
    WHERE TABLE_NAME = 'SCORES' AND COLUMN_NAME = 'SIGNATURE_VERIFIED';
    
    IF v_column_exists > 0 THEN
        EXECUTE IMMEDIATE 'ALTER TABLE SCORES DROP COLUMN SIGNATURE_VERIFIED';
        DBMS_OUTPUT.PUT_LINE('✓ Dropped SIGNATURE_VERIFIED column');
    ELSE
        DBMS_OUTPUT.PUT_LINE('- SIGNATURE_VERIFIED column does not exist (OK)');
    END IF;
END;
/

DECLARE
    v_column_exists NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_column_exists
    FROM USER_TAB_COLUMNS
    WHERE TABLE_NAME = 'SCORES' AND COLUMN_NAME = 'LAST_VERIFIED_AT';
    
    IF v_column_exists > 0 THEN
        EXECUTE IMMEDIATE 'ALTER TABLE SCORES DROP COLUMN LAST_VERIFIED_AT';
        DBMS_OUTPUT.PUT_LINE('✓ Dropped LAST_VERIFIED_AT column');
    ELSE
        DBMS_OUTPUT.PUT_LINE('- LAST_VERIFIED_AT column does not exist (OK)');
    END IF;
END;
/

DECLARE
    v_column_exists NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_column_exists
    FROM USER_TAB_COLUMNS
    WHERE TABLE_NAME = 'SCORES' AND COLUMN_NAME = 'SIGNED_BY';
    
    IF v_column_exists > 0 THEN
        EXECUTE IMMEDIATE 'ALTER TABLE SCORES DROP COLUMN SIGNED_BY';
        DBMS_OUTPUT.PUT_LINE('✓ Dropped SIGNED_BY column');
    ELSE
        DBMS_OUTPUT.PUT_LINE('- SIGNED_BY column does not exist (OK)');
    END IF;
END;
/

DECLARE
    v_column_exists NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_column_exists
    FROM USER_TAB_COLUMNS
    WHERE TABLE_NAME = 'SCORES' AND COLUMN_NAME = 'SIGNED_AT';
    
    IF v_column_exists > 0 THEN
        EXECUTE IMMEDIATE 'ALTER TABLE SCORES DROP COLUMN SIGNED_AT';
        DBMS_OUTPUT.PUT_LINE('✓ Dropped SIGNED_AT column');
    ELSE
        DBMS_OUTPUT.PUT_LINE('- SIGNED_AT column does not exist (OK)');
    END IF;
END;
/

PROMPT 'Rollback migration 002 completed successfully!';
PROMPT 'Now you can run 002_ADD_DIGITAL_SIGNATURE_SCORES.sql again';
