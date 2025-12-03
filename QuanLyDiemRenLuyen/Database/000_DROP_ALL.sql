-- =========================================================
-- DATABASE DROP SCRIPT
-- File: 000_DROP_ALL.sql
-- Description: Xóa toàn bộ objects trong database để reset
-- WARNING: Script này sẽ XÓA TẤT CẢ DỮ LIỆU!
-- =========================================================

PROMPT '========================================';
PROMPT 'WARNING: Dropping ALL database objects!';
PROMPT 'This will DELETE ALL DATA!';
PROMPT 'Press Ctrl+C to cancel...';
PROMPT '========================================';

-- Pause for 5 seconds to give chance to cancel
BEGIN
    DBMS_LOCK.SLEEP(5);
END;
/

PROMPT 'Starting drop sequence...';

-- =========================================================
-- 1) Drop Oracle Packages (nếu có)
-- =========================================================
PROMPT 'Dropping packages...';

BEGIN
    EXECUTE IMMEDIATE 'DROP PACKAGE PKG_FEEDBACK_ENCRYPTION';
    DBMS_OUTPUT.PUT_LINE('✓ Dropped PKG_FEEDBACK_ENCRYPTION');
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -4043 THEN RAISE; END IF; -- Ignore if not exists
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP PACKAGE PKG_SCORE_SIGNATURE';
    DBMS_OUTPUT.PUT_LINE('✓ Dropped PKG_SCORE_SIGNATURE');
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -4043 THEN RAISE; END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP PACKAGE PKG_STUDENT_ENCRYPTION';
    DBMS_OUTPUT.PUT_LINE('✓ Dropped PKG_STUDENT_ENCRYPTION');
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -4043 THEN RAISE; END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP PACKAGE PKG_RSA_CRYPTO';
    DBMS_OUTPUT.PUT_LINE('✓ Dropped PKG_RSA_CRYPTO');
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -4043 THEN RAISE; END IF;
END;
/

-- =========================================================
-- 2) Drop Tables (theo thứ tự ngược với foreign keys)
-- =========================================================
PROMPT 'Dropping tables...';

-- Drop child tables first
DROP TABLE FEEDBACK_ATTACHMENTS CASCADE CONSTRAINTS PURGE;
DROP TABLE FEEDBACK_ACCESS_LOG CASCADE CONSTRAINTS PURGE;
DROP TABLE FEEDBACKS CASCADE CONSTRAINTS PURGE;
DROP TABLE PROOFS CASCADE CONSTRAINTS PURGE;
DROP TABLE SCORE_AUDIT_SIGNATURES CASCADE CONSTRAINTS PURGE;
DROP TABLE SCORES CASCADE CONSTRAINTS PURGE;
DROP TABLE NOTIFICATION_READS CASCADE CONSTRAINTS PURGE;
DROP TABLE NOTIFICATIONS CASCADE CONSTRAINTS PURGE;
DROP TABLE REGISTRATIONS CASCADE CONSTRAINTS PURGE;
DROP TABLE ACTIVITIES CASCADE CONSTRAINTS PURGE;
DROP TABLE STUDENTS CASCADE CONSTRAINTS PURGE;
DROP TABLE CLASS_LECTURERS CASCADE CONSTRAINTS PURGE;
DROP TABLE CLASSES CASCADE CONSTRAINTS PURGE;
DROP TABLE TERMS CASCADE CONSTRAINTS PURGE;
DROP TABLE DEPARTMENTS CASCADE CONSTRAINTS PURGE;
DROP TABLE PASSWORD_RESET_TOKENS CASCADE CONSTRAINTS PURGE;
DROP TABLE AUDIT_TRAIL CASCADE CONSTRAINTS PURGE;
DROP TABLE USERS CASCADE CONSTRAINTS PURGE;
DROP TABLE ENCRYPTION_KEYS CASCADE CONSTRAINTS PURGE;

PROMPT '✓ All tables dropped';

-- =========================================================
-- 3) Drop Sequences (if any)
-- =========================================================
PROMPT 'Dropping sequences...';

-- Note: SCORES.ID uses IDENTITY, không cần drop sequence riêng
-- Nếu có sequences khác, thêm vào đây

-- =========================================================
-- 4) Purge Recycle Bin
-- =========================================================
PROMPT 'Purging recycle bin...';
PURGE RECYCLEBIN;

PROMPT '========================================';
PROMPT 'Database cleanup completed!';
PROMPT 'All objects have been dropped.';
PROMPT '========================================';

-- Verification: Show remaining objects
PROMPT 'Remaining user objects:';
SELECT object_type, COUNT(*) as count
FROM user_objects
GROUP BY object_type
ORDER BY object_type;
