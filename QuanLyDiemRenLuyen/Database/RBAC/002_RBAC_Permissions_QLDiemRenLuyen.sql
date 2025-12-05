-- =========================================================
-- RBAC IMPLEMENTATION - PART B (Run as QLDiemRenLuyen)
-- =========================================================
-- Connection: QLDiemRenLuyen (schema owner)
-- Purpose: Grant object privileges and create supporting objects
-- Prerequisite: Run 001A_RBAC_Roles_SYSDBA.sql first!
-- =========================================================

SET SERVEROUTPUT ON;

PROMPT '========================================';
PROMPT 'RBAC PART B - Granting Permissions';
PROMPT 'Executing as: QLDiemRenLuyen';
PROMPT '========================================';

-- =========================================================
-- STEP 1: GRANT PERMISSIONS TO ROLE_STUDENT
-- =========================================================

-- Activities - Read only approved activities
GRANT SELECT ON ACTIVITIES TO ROLE_STUDENT;

-- Registrations - Can register and manage own registrations
GRANT SELECT, INSERT, UPDATE ON REGISTRATIONS TO ROLE_STUDENT;

-- Proofs - Can upload and view own proofs
GRANT SELECT, INSERT ON PROOFS TO ROLE_STUDENT;

-- Scores - Can view own scores (VPD will filter later)
GRANT SELECT ON SCORES TO ROLE_STUDENT;

-- Feedbacks - Can create and view feedbacks
GRANT SELECT, INSERT ON FEEDBACKS TO ROLE_STUDENT;

-- Users - Can view basic user info
GRANT SELECT ON USERS TO ROLE_STUDENT;

-- Students - Can view own student record (VPD will filter later)
GRANT SELECT ON STUDENTS TO ROLE_STUDENT;

-- Reference tables - Read only
GRANT SELECT ON CLASSES TO ROLE_STUDENT;
GRANT SELECT ON DEPARTMENTS TO ROLE_STUDENT;
GRANT SELECT ON TERMS TO ROLE_STUDENT;

-- Notifications - Can view own notifications
GRANT SELECT ON NOTIFICATIONS TO ROLE_STUDENT;
GRANT SELECT, INSERT, UPDATE ON NOTIFICATION_READS TO ROLE_STUDENT;

PROMPT '✓ Granted permissions to ROLE_STUDENT';

-- =========================================================
-- STEP 2: GRANT PERMISSIONS TO ROLE_LECTURER
-- =========================================================

-- Inherit student permissions
GRANT ROLE_STUDENT TO ROLE_LECTURER;

-- Activities - Full control over own activities
GRANT SELECT, INSERT, UPDATE, DELETE ON ACTIVITIES TO ROLE_LECTURER;

-- Registrations - Can view and approve registrations
GRANT SELECT, UPDATE ON REGISTRATIONS TO ROLE_LECTURER;

-- Proofs - Can view all proofs, update status
GRANT SELECT, UPDATE ON PROOFS TO ROLE_LECTURER;

-- Scores - Can create and manage scores
GRANT SELECT, INSERT, UPDATE, DELETE ON SCORES TO ROLE_LECTURER;
GRANT SELECT, INSERT ON SCORE_AUDIT_SIGNATURES TO ROLE_LECTURER;

-- Feedbacks - Can create, view and respond to feedbacks
GRANT SELECT, INSERT, UPDATE, DELETE ON FEEDBACKS TO ROLE_LECTURER;
GRANT SELECT, INSERT ON FEEDBACK_ACCESS_LOG TO ROLE_LECTURER;
GRANT SELECT, INSERT, DELETE ON FEEDBACK_ATTACHMENTS TO ROLE_LECTURER;

-- Students - Can view students in their department (VPD will filter later)
GRANT SELECT ON STUDENTS TO ROLE_LECTURER;

-- Class Lecturers - Can view class assignments
GRANT SELECT ON CLASS_LECTURER_ASSIGNMENTS TO ROLE_LECTURER;

PROMPT '✓ Granted permissions to ROLE_LECTURER';

-- =========================================================
-- STEP 3: GRANT PERMISSIONS TO ROLE_ADMIN
-- =========================================================

-- Full control over all main tables
GRANT ALL ON USERS TO ROLE_ADMIN;
GRANT ALL ON STUDENTS TO ROLE_ADMIN;
GRANT ALL ON CLASSES TO ROLE_ADMIN;
GRANT ALL ON DEPARTMENTS TO ROLE_ADMIN;
GRANT ALL ON TERMS TO ROLE_ADMIN;
GRANT ALL ON ACTIVITIES TO ROLE_ADMIN;
GRANT ALL ON REGISTRATIONS TO ROLE_ADMIN;
GRANT ALL ON PROOFS TO ROLE_ADMIN;
GRANT ALL ON SCORES TO ROLE_ADMIN;
GRANT ALL ON SCORE_AUDIT_SIGNATURES TO ROLE_ADMIN;
GRANT ALL ON FEEDBACKS TO ROLE_ADMIN;
GRANT ALL ON FEEDBACK_ACCESS_LOG TO ROLE_ADMIN;
GRANT ALL ON FEEDBACK_ATTACHMENTS TO ROLE_ADMIN;
GRANT ALL ON NOTIFICATIONS TO ROLE_ADMIN;
GRANT ALL ON NOTIFICATION_READS TO ROLE_ADMIN;
GRANT ALL ON CLASS_LECTURER_ASSIGNMENTS TO ROLE_ADMIN;

-- System tables
GRANT ALL ON AUDIT_TRAIL TO ROLE_ADMIN;
GRANT ALL ON PASSWORD_RESET_TOKENS TO ROLE_ADMIN;
GRANT ALL ON ENCRYPTION_KEYS TO ROLE_ADMIN;

PROMPT '✓ Granted permissions to ROLE_ADMIN';

-- =========================================================
-- STEP 4: GRANT PERMISSIONS TO ROLE_READONLY
-- =========================================================

-- Read-only access for reporting and analytics
GRANT SELECT ON USERS TO ROLE_READONLY;
GRANT SELECT ON STUDENTS TO ROLE_READONLY;
GRANT SELECT ON CLASSES TO ROLE_READONLY;
GRANT SELECT ON DEPARTMENTS TO ROLE_READONLY;
GRANT SELECT ON TERMS TO ROLE_READONLY;
GRANT SELECT ON ACTIVITIES TO ROLE_READONLY;
GRANT SELECT ON REGISTRATIONS TO ROLE_READONLY;
GRANT SELECT ON PROOFS TO ROLE_READONLY;
GRANT SELECT ON SCORES TO ROLE_READONLY;
GRANT SELECT ON SCORE_AUDIT_SIGNATURES TO ROLE_READONLY;
GRANT SELECT ON FEEDBACKS TO ROLE_READONLY;
GRANT SELECT ON FEEDBACK_ACCESS_LOG TO ROLE_READONLY;
GRANT SELECT ON NOTIFICATIONS TO ROLE_READONLY;
GRANT SELECT ON NOTIFICATION_READS TO ROLE_READONLY;
GRANT SELECT ON CLASS_LECTURER_ASSIGNMENTS TO ROLE_READONLY;
GRANT SELECT ON AUDIT_TRAIL TO ROLE_READONLY;

PROMPT '✓ Granted permissions to ROLE_READONLY';

-- =========================================================
-- STEP 5: CREATE TABLE FOR DB USER CREDENTIALS
-- =========================================================

-- Check if table already exists
DECLARE
    table_exists NUMBER;
BEGIN
    SELECT COUNT(*) INTO table_exists 
    FROM USER_TABLES 
    WHERE TABLE_NAME = 'DB_USER_CREDENTIALS';
    
    IF table_exists > 0 THEN
        EXECUTE IMMEDIATE 'DROP TABLE DB_USER_CREDENTIALS CASCADE CONSTRAINTS';
        DBMS_OUTPUT.PUT_LINE('✓ Dropped existing DB_USER_CREDENTIALS table');
    END IF;
END;
/

-- Create table
CREATE TABLE DB_USER_CREDENTIALS (
    ID VARCHAR2(32) DEFAULT RAWTOHEX(SYS_GUID()) PRIMARY KEY,
    APP_USER_MAND VARCHAR2(50) NOT NULL,
    DB_USERNAME VARCHAR2(50) NOT NULL UNIQUE,
    DB_PASSWORD_HASH VARCHAR2(512) NOT NULL,
    DB_ROLE VARCHAR2(50) NOT NULL,
    CREATED_AT TIMESTAMP DEFAULT SYSTIMESTAMP,
    LAST_LOGIN TIMESTAMP,
    IS_ACTIVE NUMBER(1) DEFAULT 1,
    CONSTRAINT FK_DBCRED_USER FOREIGN KEY (APP_USER_MAND) REFERENCES USERS(MAND),
    CONSTRAINT CK_DBCRED_ROLE CHECK (DB_ROLE IN ('ROLE_STUDENT','ROLE_LECTURER','ROLE_ADMIN','ROLE_READONLY')),
    CONSTRAINT CK_DBCRED_ACTIVE CHECK (IS_ACTIVE IN (0,1))
);

CREATE INDEX IX_DBCRED_MAND ON DB_USER_CREDENTIALS(APP_USER_MAND);
-- Note: IX_DBCRED_USERNAME not needed - UNIQUE constraint on DB_USERNAME creates automatic index

PROMPT '✓ Created DB_USER_CREDENTIALS table';

-- =========================================================
-- STEP 6: CREATE STORED PROCEDURE TO CREATE DB USERS
-- =========================================================

CREATE OR REPLACE PROCEDURE SP_CREATE_DB_USER_FOR_APP_USER(
    p_mand IN VARCHAR2,
    p_result OUT VARCHAR2,
    p_db_username OUT VARCHAR2,
    p_db_password OUT VARCHAR2
) AS
    v_role VARCHAR2(50);
    v_db_role VARCHAR2(50);
    v_username VARCHAR2(50);
    v_password VARCHAR2(50);
    v_user_exists NUMBER;
    v_password_hash VARCHAR2(512);
BEGIN
    -- Get user's application role
    SELECT ROLE_NAME INTO v_role
    FROM USERS
    WHERE MAND = p_mand;
    
    -- Map application role to database role
    CASE v_role
        WHEN 'STUDENT' THEN
            v_db_role := 'ROLE_STUDENT';
        WHEN 'LECTURER' THEN
            v_db_role := 'ROLE_LECTURER';
        WHEN 'ADMIN' THEN
            v_db_role := 'ROLE_ADMIN';
        ELSE
            v_db_role := 'ROLE_READONLY';
    END CASE;
    
    -- Generate DB username
    v_username := 'APP_' || p_mand;
    
    -- Check if DB user already exists
    SELECT COUNT(*) INTO v_user_exists
    FROM DB_USER_CREDENTIALS
    WHERE APP_USER_MAND = p_mand;
    
    IF v_user_exists > 0 THEN
        p_result := 'DB user already exists for this app user';
        SELECT DB_USERNAME INTO p_db_username
        FROM DB_USER_CREDENTIALS
        WHERE APP_USER_MAND = p_mand;
        p_db_password := NULL;
        RETURN;
    END IF;
    
    -- Generate random password
    v_password := DBMS_RANDOM.STRING('X', 20);
    
    -- Hash password
    v_password_hash := RAWTOHEX(DBMS_CRYPTO.HASH(
        UTL_RAW.CAST_TO_RAW(v_password),
        DBMS_CRYPTO.HASH_SH256
    ));
    
    -- Store credentials
    -- NOTE: Actual Oracle user creation requires SYSDBA privileges
    -- This procedure stores metadata only
    INSERT INTO DB_USER_CREDENTIALS(APP_USER_MAND, DB_USERNAME, DB_PASSWORD_HASH, DB_ROLE)
    VALUES (p_mand, v_username, v_password_hash, v_db_role);
    
    COMMIT;
    
    -- Return results
    p_result := 'SUCCESS';
    p_db_username := v_username;
    p_db_password := v_password;
    
    DBMS_OUTPUT.PUT_LINE('Registered DB user: ' || v_username || ' with role: ' || v_db_role);
    DBMS_OUTPUT.PUT_LINE('NOTE: Actual Oracle user creation requires SYSDBA privileges');
    
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        p_result := 'ERROR: Application user not found';
        ROLLBACK;
    WHEN OTHERS THEN
        p_result := 'ERROR: ' || SQLERRM;
        ROLLBACK;
END;
/

PROMPT '✓ Created SP_CREATE_DB_USER_FOR_APP_USER procedure';

-- =========================================================
-- STEP 7: CREATE STORED PROCEDURE TO DROP DB USER
-- =========================================================

CREATE OR REPLACE PROCEDURE SP_DROP_DB_USER(
    p_mand IN VARCHAR2,
    p_result OUT VARCHAR2
) AS
    v_username VARCHAR2(50);
BEGIN
    -- Get DB username
    SELECT DB_USERNAME INTO v_username
    FROM DB_USER_CREDENTIALS
    WHERE APP_USER_MAND = p_mand;
    
    -- Delete from credentials table
    DELETE FROM DB_USER_CREDENTIALS
    WHERE APP_USER_MAND = p_mand;
    
    COMMIT;
    
    p_result := 'SUCCESS';
    DBMS_OUTPUT.PUT_LINE('Removed DB user registration: ' || v_username);
    DBMS_OUTPUT.PUT_LINE('NOTE: Actual Oracle user drop requires SYSDBA privileges');
    
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        p_result := 'ERROR: DB user not found for this app user';
    WHEN OTHERS THEN
        p_result := 'ERROR: ' || SQLERRM;
        ROLLBACK;
END;
/

PROMPT '✓ Created SP_DROP_DB_USER procedure';

-- =========================================================
-- STEP 8: CREATE VIEW TO SHOW ROLE ASSIGNMENTS
-- =========================================================

-- Create view with error handling
BEGIN
    EXECUTE IMMEDIATE 'CREATE OR REPLACE VIEW V_USER_ROLE_ASSIGNMENTS AS
SELECT 
    u.MAND,
    u.FULL_NAME,
    u.EMAIL,
    u.ROLE_NAME as APP_ROLE,
    c.DB_USERNAME,
    c.DB_ROLE,
    c.CREATED_AT as DB_USER_CREATED_AT,
    c.LAST_LOGIN,
    c.IS_ACTIVE as DB_USER_ACTIVE
FROM USERS u
LEFT JOIN DB_USER_CREDENTIALS c ON u.MAND = c.APP_USER_MAND
WHERE u.IS_ACTIVE = 1';
    
    DBMS_OUTPUT.PUT_LINE('✓ Created V_USER_ROLE_ASSIGNMENTS view');
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Warning: Could not create view - ' || SQLERRM);
        DBMS_OUTPUT.PUT_LINE('Creating simplified version...');
        -- Create simplified view without JOIN if needed
        EXECUTE IMMEDIATE 'CREATE OR REPLACE VIEW V_USER_ROLE_ASSIGNMENTS AS
SELECT 
    u.MAND,
    u.FULL_NAME,
    u.EMAIL,
    u.ROLE_NAME as APP_ROLE,
    '''' as DB_USERNAME,
    '''' as DB_ROLE,
    NULL as DB_USER_CREATED_AT,
    NULL as LAST_LOGIN,
    0 as DB_USER_ACTIVE
FROM USERS u
WHERE u.IS_ACTIVE = 1';
        DBMS_OUTPUT.PUT_LINE('✓ Created simplified V_USER_ROLE_ASSIGNMENTS view');
END;
/

-- =========================================================
-- STEP 9: GRANT EXECUTE PERMISSIONS ON PROCEDURES
-- =========================================================

-- Grant privileges with error handling
BEGIN
    EXECUTE IMMEDIATE 'GRANT EXECUTE ON SP_CREATE_DB_USER_FOR_APP_USER TO ROLE_ADMIN';
    EXECUTE IMMEDIATE 'GRANT EXECUTE ON SP_DROP_DB_USER TO ROLE_ADMIN';
    
    -- Try to grant on view
    BEGIN
        EXECUTE IMMEDIATE 'GRANT SELECT ON V_USER_ROLE_ASSIGNMENTS TO ROLE_ADMIN';
    EXCEPTION
        WHEN OTHERS THEN
            DBMS_OUTPUT.PUT_LINE('Warning: Could not grant on view - ' || SQLERRM);
    END;
    
    DBMS_OUTPUT.PUT_LINE('✓ Granted execute permissions to ROLE_ADMIN');
END;
/

-- =========================================================
-- VERIFICATION
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'RBAC IMPLEMENTATION SUMMARY';
PROMPT '========================================';

PROMPT '';
PROMPT 'Total Privileges Granted to Roles:';
SELECT GRANTEE as ROLE, COUNT(*) as PRIVILEGE_COUNT
FROM USER_TAB_PRIVS_MADE
WHERE GRANTEE IN ('ROLE_STUDENT', 'ROLE_LECTURER', 'ROLE_ADMIN', 'ROLE_READONLY')
GROUP BY GRANTEE
ORDER BY
 GRANTEE;

PROMPT '';
PROMPT 'Objects Created:';
SELECT OBJECT_NAME, OBJECT_TYPE, STATUS
FROM USER_OBJECTS
WHERE OBJECT_NAME IN ('DB_USER_CREDENTIALS', 
                      'SP_CREATE_DB_USER_FOR_APP_USER',
                      'SP_DROP_DB_USER',
                      'V_USER_ROLE_ASSIGNMENTS')
ORDER BY OBJECT_TYPE, OBJECT_NAME;

PROMPT '';
PROMPT '✓ RBAC PART B COMPLETED SUCCESSFULLY!';
PROMPT '========================================';
