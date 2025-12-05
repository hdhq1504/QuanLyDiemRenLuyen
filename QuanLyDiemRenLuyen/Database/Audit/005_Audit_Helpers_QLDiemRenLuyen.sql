-- =========================================================
-- AUDITING - PART 5: HELPERS (Run as SYSDBA first, then QLDiemRenLuyen)
-- =========================================================
-- Connection: Start with SYSDBA, then switch to QLDiemRenLuyen
-- Purpose: Create context and helper procedures
-- Prerequisite: Run Parts 1-4 first!
-- =========================================================

SET SERVEROUTPUT ON;
SET LINESIZE 200;

PROMPT '========================================';
PROMPT 'AUDITING PART 5 - Helpers';
PROMPT '========================================';

-- =========================================================
-- SECTION A: RUN AS SYSDBA
-- =========================================================

PROMPT '';
PROMPT '=== SECTION A: Run as SYSDBA ===';
PROMPT '';

-- Create Application Context
PROMPT 'Creating AUDIT_CTX context...';

CREATE OR REPLACE CONTEXT AUDIT_CTX USING QLDIEMRENLUYEN.PKG_AUDIT_CONTEXT ACCESSED GLOBALLY;

PROMPT '✓ Created AUDIT_CTX context';

-- Grant access
GRANT CREATE ANY CONTEXT TO QLDIEMRENLUYEN;

PROMPT '✓ Granted CREATE ANY CONTEXT to QLDiemRenLuyen';

-- =========================================================
-- SECTION B: RUN AS QLDiemRenLuyen
-- (Comment out Section A and uncomment Section B when running as QLDiemRenLuyen)
-- =========================================================

PROMPT '';
PROMPT '=== SECTION B: Context Package ===';
PROMPT 'Creating PKG_AUDIT_CONTEXT package...';

-- Context management package
CREATE OR REPLACE PACKAGE PKG_AUDIT_CONTEXT AS
    -- Set audit context before performing changes
    PROCEDURE SET_CONTEXT(
        p_user_id       IN VARCHAR2,
        p_justification IN VARCHAR2 DEFAULT NULL,
        p_client_ip     IN VARCHAR2 DEFAULT NULL
    );
    
    -- Clear context after operation
    PROCEDURE CLEAR_CONTEXT;
    
    -- Get current context values
    FUNCTION GET_USER_ID RETURN VARCHAR2;
    FUNCTION GET_JUSTIFICATION RETURN VARCHAR2;
END PKG_AUDIT_CONTEXT;
/

CREATE OR REPLACE PACKAGE BODY PKG_AUDIT_CONTEXT AS
    
    PROCEDURE SET_CONTEXT(
        p_user_id       IN VARCHAR2,
        p_justification IN VARCHAR2 DEFAULT NULL,
        p_client_ip     IN VARCHAR2 DEFAULT NULL
    ) AS
    BEGIN
        DBMS_SESSION.SET_CONTEXT('AUDIT_CTX', 'USER_ID', p_user_id);
        
        IF p_justification IS NOT NULL THEN
            DBMS_SESSION.SET_CONTEXT('AUDIT_CTX', 'JUSTIFICATION', p_justification);
        END IF;
        
        IF p_client_ip IS NOT NULL THEN
            DBMS_SESSION.SET_CONTEXT('AUDIT_CTX', 'CLIENT_IP', p_client_ip);
        END IF;
        
        DBMS_SESSION.SET_CONTEXT('AUDIT_CTX', 'SET_AT', TO_CHAR(SYSTIMESTAMP, 'YYYY-MM-DD HH24:MI:SS'));
    END SET_CONTEXT;
    
    PROCEDURE CLEAR_CONTEXT AS
    BEGIN
        DBMS_SESSION.CLEAR_CONTEXT('AUDIT_CTX');
    END CLEAR_CONTEXT;
    
    FUNCTION GET_USER_ID RETURN VARCHAR2 AS
    BEGIN
        RETURN SYS_CONTEXT('AUDIT_CTX', 'USER_ID');
    END GET_USER_ID;
    
    FUNCTION GET_JUSTIFICATION RETURN VARCHAR2 AS
    BEGIN
        RETURN SYS_CONTEXT('AUDIT_CTX', 'JUSTIFICATION');
    END GET_JUSTIFICATION;
    
END PKG_AUDIT_CONTEXT;
/

PROMPT '✓ Created PKG_AUDIT_CONTEXT package';

-- =========================================================
-- STEP 2: CREATE HELPER PROCEDURES
-- =========================================================

PROMPT '';
PROMPT 'Creating helper procedures...';

-- Procedure to log business actions
CREATE OR REPLACE PROCEDURE SP_LOG_BUSINESS_ACTION(
    p_action_type   IN VARCHAR2,
    p_action_desc   IN VARCHAR2,
    p_entity_type   IN VARCHAR2 DEFAULT NULL,
    p_entity_id     IN VARCHAR2 DEFAULT NULL,
    p_performed_by  IN VARCHAR2 DEFAULT NULL,
    p_details       IN CLOB DEFAULT NULL,
    p_status        IN VARCHAR2 DEFAULT 'SUCCESS'
) AS
    v_user_id VARCHAR2(50);
BEGIN
    v_user_id := NVL(p_performed_by, SYS_CONTEXT('AUDIT_CTX', 'USER_ID'));
    
    INSERT INTO AUDIT_BUSINESS_ACTIONS(
        ACTION_TYPE, ACTION_DESC, ENTITY_TYPE, ENTITY_ID,
        PERFORMED_BY, CLIENT_IP, DETAILS, STATUS
    ) VALUES (
        p_action_type, p_action_desc, p_entity_type, p_entity_id,
        v_user_id, SYS_CONTEXT('USERENV', 'IP_ADDRESS'), p_details, p_status
    );
    
    COMMIT;
END SP_LOG_BUSINESS_ACTION;
/

PROMPT '✓ Created SP_LOG_BUSINESS_ACTION';

-- Procedure to get record history
CREATE OR REPLACE PROCEDURE SP_GET_RECORD_HISTORY(
    p_table_name    IN VARCHAR2,
    p_record_id     IN VARCHAR2,
    p_result        OUT SYS_REFCURSOR
) AS
BEGIN
    OPEN p_result FOR
        SELECT 
            ID,
            OPERATION,
            OLD_VALUES,
            NEW_VALUES,
            CHANGED_COLUMNS,
            PERFORMED_BY,
            PERFORMED_AT,
            JUSTIFICATION
        FROM AUDIT_CHANGE_LOGS
        WHERE TABLE_NAME = UPPER(p_table_name)
          AND RECORD_ID = p_record_id
        ORDER BY PERFORMED_AT DESC;
END SP_GET_RECORD_HISTORY;
/

PROMPT '✓ Created SP_GET_RECORD_HISTORY';

-- Function to get audit count for a record
CREATE OR REPLACE FUNCTION FN_GET_AUDIT_COUNT(
    p_table_name    IN VARCHAR2,
    p_record_id     IN VARCHAR2
) RETURN NUMBER AS
    v_count NUMBER;
BEGIN
    SELECT COUNT(*)
    INTO v_count
    FROM AUDIT_CHANGE_LOGS
    WHERE TABLE_NAME = UPPER(p_table_name)
      AND RECORD_ID = p_record_id;
    
    RETURN v_count;
END FN_GET_AUDIT_COUNT;
/

PROMPT '✓ Created FN_GET_AUDIT_COUNT';

-- Procedure to set justification (wrapper)
CREATE OR REPLACE PROCEDURE SP_SET_AUDIT_JUSTIFICATION(
    p_user_id       IN VARCHAR2,
    p_justification IN VARCHAR2
) AS
BEGIN
    PKG_AUDIT_CONTEXT.SET_CONTEXT(
        p_user_id       => p_user_id,
        p_justification => p_justification
    );
END SP_SET_AUDIT_JUSTIFICATION;
/

PROMPT '✓ Created SP_SET_AUDIT_JUSTIFICATION';

-- Procedure to clear justification
CREATE OR REPLACE PROCEDURE SP_CLEAR_AUDIT_CONTEXT AS
BEGIN
    PKG_AUDIT_CONTEXT.CLEAR_CONTEXT;
END SP_CLEAR_AUDIT_CONTEXT;
/

PROMPT '✓ Created SP_CLEAR_AUDIT_CONTEXT';

-- =========================================================
-- STEP 3: CREATE AUDIT REPORT PROCEDURE
-- =========================================================

PROMPT '';
PROMPT 'Creating audit report procedures...';

CREATE OR REPLACE PROCEDURE SP_AUDIT_REPORT(
    p_table_name    IN VARCHAR2 DEFAULT NULL,
    p_start_date    IN DATE DEFAULT SYSDATE - 30,
    p_end_date      IN DATE DEFAULT SYSDATE,
    p_user_id       IN VARCHAR2 DEFAULT NULL,
    p_result        OUT SYS_REFCURSOR
) AS
BEGIN
    OPEN p_result FOR
        SELECT 
            TABLE_NAME,
            RECORD_ID,
            OPERATION,
            PERFORMED_BY,
            PERFORMED_AT,
            CHANGED_COLUMNS,
            JUSTIFICATION
        FROM AUDIT_CHANGE_LOGS
        WHERE (p_table_name IS NULL OR TABLE_NAME = UPPER(p_table_name))
          AND PERFORMED_AT >= p_start_date
          AND PERFORMED_AT < p_end_date + 1
          AND (p_user_id IS NULL OR PERFORMED_BY = p_user_id)
        ORDER BY PERFORMED_AT DESC;
END SP_AUDIT_REPORT;
/

PROMPT '✓ Created SP_AUDIT_REPORT';

-- Daily summary function
CREATE OR REPLACE FUNCTION FN_AUDIT_DAILY_SUMMARY(
    p_date IN DATE DEFAULT SYSDATE
) RETURN SYS_REFCURSOR AS
    v_result SYS_REFCURSOR;
BEGIN
    OPEN v_result FOR
        SELECT 
            TABLE_NAME,
            OPERATION,
            COUNT(*) AS CHANGE_COUNT,
            COUNT(DISTINCT PERFORMED_BY) AS UNIQUE_USERS
        FROM AUDIT_CHANGE_LOGS
        WHERE TRUNC(PERFORMED_AT) = TRUNC(p_date)
        GROUP BY TABLE_NAME, OPERATION
        ORDER BY TABLE_NAME, OPERATION;
    
    RETURN v_result;
END FN_AUDIT_DAILY_SUMMARY;
/

PROMPT '✓ Created FN_AUDIT_DAILY_SUMMARY';

-- =========================================================
-- VERIFICATION
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'VERIFICATION - Helper Objects';
PROMPT '========================================';

PROMPT '';
PROMPT 'Packages:';
SELECT OBJECT_NAME, OBJECT_TYPE, STATUS
FROM USER_OBJECTS
WHERE OBJECT_NAME = 'PKG_AUDIT_CONTEXT';

PROMPT '';
PROMPT 'Procedures:';
SELECT OBJECT_NAME, OBJECT_TYPE, STATUS
FROM USER_OBJECTS
WHERE OBJECT_TYPE = 'PROCEDURE'
  AND OBJECT_NAME LIKE '%AUDIT%'
ORDER BY OBJECT_NAME;

PROMPT '';
PROMPT 'Functions:';
SELECT OBJECT_NAME, OBJECT_TYPE, STATUS
FROM USER_OBJECTS
WHERE OBJECT_TYPE = 'FUNCTION'
  AND OBJECT_NAME LIKE '%AUDIT%'
ORDER BY OBJECT_NAME;

PROMPT '';
PROMPT '========================================';
PROMPT '✓ PART 5 COMPLETED!';
PROMPT 'Created:';
PROMPT '  - PKG_AUDIT_CONTEXT (context management)';
PROMPT '  - SP_LOG_BUSINESS_ACTION';
PROMPT '  - SP_GET_RECORD_HISTORY';
PROMPT '  - SP_SET_AUDIT_JUSTIFICATION';
PROMPT '  - SP_CLEAR_AUDIT_CONTEXT';
PROMPT '  - SP_AUDIT_REPORT';
PROMPT '  - FN_GET_AUDIT_COUNT';
PROMPT '  - FN_AUDIT_DAILY_SUMMARY';
PROMPT '';
PROMPT 'Next: Run Part 6 to create views';
PROMPT '========================================';
