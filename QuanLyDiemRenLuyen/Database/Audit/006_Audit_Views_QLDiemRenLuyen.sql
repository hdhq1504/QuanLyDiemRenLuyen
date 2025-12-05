-- =========================================================
-- AUDITING - PART 6: VIEWS (Run as QLDiemRenLuyen)
-- =========================================================
-- Connection: QLDiemRenLuyen
-- Purpose: Create unified views for audit data
-- Prerequisite: Run Parts 1-5 first!
-- =========================================================

SET SERVEROUTPUT ON;
SET LINESIZE 200;

PROMPT '========================================';
PROMPT 'AUDITING PART 6 - Audit Views';
PROMPT 'Executing as: QLDiemRenLuyen';
PROMPT '========================================';

-- =========================================================
-- STEP 1: CREATE VIEW FOR SCORE HISTORY
-- =========================================================

PROMPT '';
PROMPT 'Creating audit views...';

CREATE OR REPLACE VIEW V_AUDIT_SCORES_HISTORY AS
SELECT 
    acl.ID AS AUDIT_ID,
    acl.RECORD_ID AS SCORE_ID,
    acl.OPERATION,
    acl.OLD_VALUES,
    acl.NEW_VALUES,
    acl.CHANGED_COLUMNS,
    acl.PERFORMED_BY,
    acl.PERFORMED_AT,
    acl.JUSTIFICATION,
    s.STUDENT_ID,
    s.TERM_ID,
    u.FULL_NAME AS PERFORMER_NAME
FROM AUDIT_CHANGE_LOGS acl
LEFT JOIN SCORES s ON TO_CHAR(s.ID) = acl.RECORD_ID
LEFT JOIN USERS u ON u.MAND = acl.PERFORMED_BY
WHERE acl.TABLE_NAME = 'SCORES'
ORDER BY acl.PERFORMED_AT DESC;

COMMENT ON VIEW V_AUDIT_SCORES_HISTORY IS 'History of all changes to SCORES table';

PROMPT '✓ Created V_AUDIT_SCORES_HISTORY';

-- =========================================================
-- STEP 2: CREATE VIEW FOR USER ACTIVITY
-- =========================================================

CREATE OR REPLACE VIEW V_AUDIT_USER_ACTIVITY AS
SELECT 
    acl.PERFORMED_BY AS USER_ID,
    u.FULL_NAME AS USER_NAME,
    u.ROLE_NAME,
    acl.TABLE_NAME,
    acl.OPERATION,
    COUNT(*) AS ACTION_COUNT,
    MIN(acl.PERFORMED_AT) AS FIRST_ACTION,
    MAX(acl.PERFORMED_AT) AS LAST_ACTION
FROM AUDIT_CHANGE_LOGS acl
LEFT JOIN USERS u ON u.MAND = acl.PERFORMED_BY
WHERE acl.PERFORMED_BY IS NOT NULL
GROUP BY acl.PERFORMED_BY, u.FULL_NAME, u.ROLE_NAME, acl.TABLE_NAME, acl.OPERATION
ORDER BY acl.PERFORMED_BY, acl.TABLE_NAME;

COMMENT ON VIEW V_AUDIT_USER_ACTIVITY IS 'Summary of user activities by table and operation';

PROMPT '✓ Created V_AUDIT_USER_ACTIVITY';

-- =========================================================
-- STEP 3: CREATE VIEW FOR DAILY SUMMARY
-- =========================================================

CREATE OR REPLACE VIEW V_AUDIT_DAILY_SUMMARY AS
SELECT 
    TRUNC(PERFORMED_AT) AS AUDIT_DATE,
    TABLE_NAME,
    OPERATION,
    COUNT(*) AS CHANGE_COUNT,
    COUNT(DISTINCT PERFORMED_BY) AS UNIQUE_USERS,
    COUNT(CASE WHEN JUSTIFICATION IS NOT NULL THEN 1 END) AS WITH_JUSTIFICATION
FROM AUDIT_CHANGE_LOGS
GROUP BY TRUNC(PERFORMED_AT), TABLE_NAME, OPERATION
ORDER BY AUDIT_DATE DESC, TABLE_NAME, OPERATION;

COMMENT ON VIEW V_AUDIT_DAILY_SUMMARY IS 'Daily summary of audit activities';

PROMPT '✓ Created V_AUDIT_DAILY_SUMMARY';

-- =========================================================
-- STEP 4: CREATE VIEW FOR RECENT CHANGES
-- =========================================================

CREATE OR REPLACE VIEW V_AUDIT_RECENT_CHANGES AS
SELECT 
    acl.ID AS AUDIT_ID,
    acl.TABLE_NAME,
    acl.RECORD_ID,
    acl.OPERATION,
    acl.CHANGED_COLUMNS,
    acl.PERFORMED_BY,
    u.FULL_NAME AS PERFORMER_NAME,
    acl.PERFORMED_AT,
    acl.JUSTIFICATION,
    acl.CLIENT_IP
FROM AUDIT_CHANGE_LOGS acl
LEFT JOIN USERS u ON u.MAND = acl.PERFORMED_BY
WHERE acl.PERFORMED_AT >= SYSDATE - 7  -- Last 7 days
ORDER BY acl.PERFORMED_AT DESC;

COMMENT ON VIEW V_AUDIT_RECENT_CHANGES IS 'Recent changes in the last 7 days';

PROMPT '✓ Created V_AUDIT_RECENT_CHANGES';

-- =========================================================
-- STEP 5: CREATE VIEW FOR BUSINESS ACTIONS
-- =========================================================

CREATE OR REPLACE VIEW V_AUDIT_BUSINESS_ACTIONS AS
SELECT 
    aba.ID AS ACTION_ID,
    aba.ACTION_TYPE,
    aba.ACTION_DESC,
    aba.ENTITY_TYPE,
    aba.ENTITY_ID,
    aba.PERFORMED_BY,
    u.FULL_NAME AS PERFORMER_NAME,
    aba.PERFORMED_AT,
    aba.STATUS,
    aba.ERROR_MESSAGE
FROM AUDIT_BUSINESS_ACTIONS aba
LEFT JOIN USERS u ON u.MAND = aba.PERFORMED_BY
ORDER BY aba.PERFORMED_AT DESC;

COMMENT ON VIEW V_AUDIT_BUSINESS_ACTIONS IS 'Business-level actions logged by application';

PROMPT '✓ Created V_AUDIT_BUSINESS_ACTIONS';

-- =========================================================
-- STEP 6: CREATE COMBINED AUDIT VIEW
-- =========================================================

CREATE OR REPLACE VIEW V_AUDIT_ALL AS
-- Change logs
SELECT 
    'CHANGE_LOG' AS SOURCE,
    ID AS AUDIT_ID,
    TABLE_NAME,
    RECORD_ID,
    OPERATION,
    PERFORMED_BY,
    PERFORMED_AT,
    JUSTIFICATION,
    OLD_VALUES,
    NEW_VALUES
FROM AUDIT_CHANGE_LOGS

UNION ALL

-- Business actions
SELECT 
    'BUSINESS_ACTION' AS SOURCE,
    ID AS AUDIT_ID,
    ENTITY_TYPE AS TABLE_NAME,
    ENTITY_ID AS RECORD_ID,
    ACTION_TYPE AS OPERATION,
    PERFORMED_BY,
    PERFORMED_AT,
    ACTION_DESC AS JUSTIFICATION,
    NULL AS OLD_VALUES,
    DETAILS AS NEW_VALUES
FROM AUDIT_BUSINESS_ACTIONS;

COMMENT ON VIEW V_AUDIT_ALL IS 'Combined view of all audit sources';

PROMPT '✓ Created V_AUDIT_ALL';

-- =========================================================
-- STEP 7: CREATE VIEW FOR APPROVALS TRACKING
-- =========================================================

CREATE OR REPLACE VIEW V_AUDIT_APPROVALS AS
SELECT 
    acl.ID AS AUDIT_ID,
    acl.TABLE_NAME,
    acl.RECORD_ID,
    acl.PERFORMED_BY AS APPROVER_ID,
    u.FULL_NAME AS APPROVER_NAME,
    acl.PERFORMED_AT AS APPROVED_AT,
    acl.JUSTIFICATION AS APPROVAL_REASON,
    acl.OLD_VALUES,
    acl.NEW_VALUES
FROM AUDIT_CHANGE_LOGS acl
LEFT JOIN USERS u ON u.MAND = acl.PERFORMED_BY
WHERE acl.TABLE_NAME IN ('SCORES', 'ACTIVITIES', 'PROOFS')
  AND acl.OPERATION = 'UPDATE'
  AND (
      acl.CHANGED_COLUMNS LIKE '%STATUS%'
      OR acl.CHANGED_COLUMNS LIKE '%APPROVAL%'
      OR acl.CHANGED_COLUMNS LIKE '%APPROVED%'
  )
ORDER BY acl.PERFORMED_AT DESC;

COMMENT ON VIEW V_AUDIT_APPROVALS IS 'Tracking of all approval actions';

PROMPT '✓ Created V_AUDIT_APPROVALS';

-- =========================================================
-- STEP 8: CREATE VIEW FOR SENSITIVE DATA ACCESS (FGA)
-- =========================================================

-- Note: This view requires SELECT access on DBA_FGA_AUDIT_TRAIL
-- Run as SYSDBA: GRANT SELECT ON DBA_FGA_AUDIT_TRAIL TO QLDIEMRENLUYEN;

CREATE OR REPLACE VIEW V_AUDIT_SENSITIVE_ACCESS AS
SELECT 
    'FGA' AS SOURCE,
    fga.TIMESTAMP AS ACCESS_TIME,
    fga.OBJECT_SCHEMA || '.' || fga.OBJECT_NAME AS OBJECT_NAME,
    fga.POLICY_NAME,
    fga.DB_USER,
    fga.USERHOST AS CLIENT_HOST,
    fga.OS_USER,
    fga.STATEMENT_TYPE,
    fga.SQL_TEXT
FROM DBA_FGA_AUDIT_TRAIL fga
WHERE fga.OBJECT_SCHEMA = 'QLDIEMRENLUYEN'
  AND fga.POLICY_NAME LIKE 'FGA_%'
ORDER BY fga.TIMESTAMP DESC;

COMMENT ON VIEW V_AUDIT_SENSITIVE_ACCESS IS 'FGA audit trail for sensitive data access';

PROMPT '✓ Created V_AUDIT_SENSITIVE_ACCESS';

-- =========================================================
-- VERIFICATION
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'VERIFICATION - Audit Views';
PROMPT '========================================';

PROMPT '';
PROMPT 'Views created:';
SELECT VIEW_NAME
FROM USER_VIEWS
WHERE VIEW_NAME LIKE 'V_AUDIT%'
ORDER BY VIEW_NAME;

PROMPT '';
PROMPT '========================================';
PROMPT '✓ PART 6 COMPLETED!';
PROMPT 'Views created:';
PROMPT '  - V_AUDIT_SCORES_HISTORY';
PROMPT '  - V_AUDIT_USER_ACTIVITY';
PROMPT '  - V_AUDIT_DAILY_SUMMARY';
PROMPT '  - V_AUDIT_RECENT_CHANGES';
PROMPT '  - V_AUDIT_BUSINESS_ACTIONS';
PROMPT '  - V_AUDIT_ALL';
PROMPT '  - V_AUDIT_APPROVALS';
PROMPT '  - V_AUDIT_SENSITIVE_ACCESS';
PROMPT '';
PROMPT 'Next: Run Part 7 for testing';
PROMPT '========================================';
