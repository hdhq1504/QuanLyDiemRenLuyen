-- =========================================================
-- AUDITING - PART 1: STANDARD AUDIT SETUP (Run as SYSDBA)
-- =========================================================
-- Connection: SYSDBA (sys as sysdba)
-- Purpose: Enable AUDIT_TRAIL and create AUDIT statements
-- =========================================================
--
-- IMPORTANT: After running this script, you MUST restart 
-- the database for AUDIT_TRAIL changes to take effect!
--
-- =========================================================

SET SERVEROUTPUT ON;
SET LINESIZE 200;

PROMPT '========================================';
PROMPT 'AUDITING PART 1 - Standard Audit Setup';
PROMPT 'Executing as: SYSDBA';
PROMPT '========================================';

-- =========================================================
-- STEP 1: CHECK CURRENT AUDIT SETTINGS
-- =========================================================

PROMPT '';
PROMPT 'Current audit settings:';

SHOW PARAMETER AUDIT_TRAIL;

-- =========================================================
-- STEP 2: ENABLE AUDIT_TRAIL=DB,EXTENDED
-- =========================================================

PROMPT '';
PROMPT 'Enabling AUDIT_TRAIL=DB,EXTENDED...';

-- DB = Store audit records in SYS.AUD$ table
-- EXTENDED = Also capture SQL text and bind variables
ALTER SYSTEM SET AUDIT_TRAIL=DB,EXTENDED SCOPE=SPFILE;

PROMPT '✓ AUDIT_TRAIL=DB,EXTENDED set (requires restart)';

-- =========================================================
-- STEP 3: GRANT NECESSARY PRIVILEGES
-- =========================================================

PROMPT '';
PROMPT 'Granting audit privileges...';

-- Grant SELECT on audit views to schema owner
GRANT SELECT ON SYS.AUD$ TO QLDIEMRENLUYEN;
GRANT SELECT ON SYS.DBA_AUDIT_TRAIL TO QLDIEMRENLUYEN;
GRANT SELECT ON SYS.DBA_FGA_AUDIT_TRAIL TO QLDIEMRENLUYEN;

PROMPT '✓ Granted audit view access to QLDiemRenLuyen';

-- =========================================================
-- STEP 4: CREATE AUDIT POLICIES FOR TABLES
-- =========================================================

PROMPT '';
PROMPT 'Creating audit policies for tables...';

-- Audit SCORES table (high importance)
AUDIT INSERT ON QLDIEMRENLUYEN.SCORES BY ACCESS;
AUDIT UPDATE ON QLDIEMRENLUYEN.SCORES BY ACCESS;
AUDIT DELETE ON QLDIEMRENLUYEN.SCORES BY ACCESS;
PROMPT '✓ Audit enabled for SCORES';

-- Audit USERS table (high importance)
AUDIT INSERT ON QLDIEMRENLUYEN.USERS BY ACCESS;
AUDIT UPDATE ON QLDIEMRENLUYEN.USERS BY ACCESS;
AUDIT DELETE ON QLDIEMRENLUYEN.USERS BY ACCESS;
PROMPT '✓ Audit enabled for USERS';

-- Audit FEEDBACKS table
AUDIT INSERT ON QLDIEMRENLUYEN.FEEDBACKS BY ACCESS;
AUDIT UPDATE ON QLDIEMRENLUYEN.FEEDBACKS BY ACCESS;
AUDIT DELETE ON QLDIEMRENLUYEN.FEEDBACKS BY ACCESS;
PROMPT '✓ Audit enabled for FEEDBACKS';

-- Audit ACTIVITIES table (update only for approval)
AUDIT UPDATE ON QLDIEMRENLUYEN.ACTIVITIES BY ACCESS;
PROMPT '✓ Audit enabled for ACTIVITIES';

-- Audit PROOFS table (update only for status change)
AUDIT UPDATE ON QLDIEMRENLUYEN.PROOFS BY ACCESS;
PROMPT '✓ Audit enabled for PROOFS';

-- Audit CLASS_LECTURER_ASSIGNMENTS table
AUDIT INSERT ON QLDIEMRENLUYEN.CLASS_LECTURER_ASSIGNMENTS BY ACCESS;
AUDIT UPDATE ON QLDIEMRENLUYEN.CLASS_LECTURER_ASSIGNMENTS BY ACCESS;
PROMPT '✓ Audit enabled for CLASS_LECTURER_ASSIGNMENTS';

-- Audit REGISTRATIONS table
AUDIT UPDATE ON QLDIEMRENLUYEN.REGISTRATIONS BY ACCESS;
PROMPT '✓ Audit enabled for REGISTRATIONS';

-- =========================================================
-- STEP 5: CREATE AUDIT POLICIES FOR SYSTEM EVENTS
-- =========================================================

PROMPT '';
PROMPT 'Creating audit policies for system events...';

-- Audit session events
AUDIT CREATE SESSION;
AUDIT ALTER USER;
AUDIT DROP USER;
PROMPT '✓ Audit enabled for session/user events';

-- Audit privilege events
AUDIT GRANT ANY PRIVILEGE;
AUDIT REVOKE ANY PRIVILEGE;
AUDIT GRANT ANY ROLE;
PROMPT '✓ Audit enabled for privilege events';

-- Audit DDL on sensitive objects
AUDIT ALTER ANY TABLE;
AUDIT DROP ANY TABLE;
AUDIT TRUNCATE TABLE;
PROMPT '✓ Audit enabled for DDL events';

-- =========================================================
-- STEP 6: CONFIGURE AUDIT TRAIL CLEANUP (OPTIONAL)
-- =========================================================

PROMPT '';
PROMPT 'Audit cleanup policy info:';
PROMPT 'Use DBMS_AUDIT_MGMT to manage audit trail size';
PROMPT 'Example: DBMS_AUDIT_MGMT.CLEAN_AUDIT_TRAIL(...)';

-- =========================================================
-- VERIFICATION
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'VERIFICATION';
PROMPT '========================================';

PROMPT '';
PROMPT 'Audit options set:';
SELECT OBJECT_NAME, OBJECT_TYPE, INS, UPD, DEL
FROM DBA_OBJ_AUDIT_OPTS
WHERE OWNER = 'QLDIEMRENLUYEN'
ORDER BY OBJECT_NAME;

PROMPT '';
PROMPT 'Statement audit options:';
SELECT AUDIT_OPTION, SUCCESS, FAILURE
FROM DBA_STMT_AUDIT_OPTS
ORDER BY AUDIT_OPTION;

PROMPT '';
PROMPT '========================================';
PROMPT '⚠️  IMPORTANT: RESTART DATABASE NOW!';
PROMPT '========================================';
PROMPT 'Run these commands to restart:';
PROMPT '  SHUTDOWN IMMEDIATE;';
PROMPT '  STARTUP;';
PROMPT '';
PROMPT 'After restart, run Part 2 script.';
PROMPT '========================================';
