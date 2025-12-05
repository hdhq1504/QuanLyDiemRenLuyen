-- =========================================================
-- MAC + VPD IMPLEMENTATION - PART A (Run as SYSDBA)
-- =========================================================
-- Connection: SYSDBA (sys/sys)
-- Purpose: Create Application Context for VPD
-- =========================================================

SET SERVEROUTPUT ON;

PROMPT '========================================';
PROMPT 'MAC + VPD PART A - Creating Application Context';
PROMPT 'Executing as: SYSDBA';
PROMPT '========================================';

-- =========================================================
-- STEP 1: GRANT PRIVILEGES TO QLDiemRenLuyen
-- =========================================================

-- Grant EXECUTE on DBMS_RLS (required for VPD policies)
GRANT EXECUTE ON DBMS_RLS TO QLDiemRenLuyen;

-- Grant CREATE ANY CONTEXT (required to create application context)
GRANT CREATE ANY CONTEXT TO QLDiemRenLuyen;

-- Grant ADMINISTER DATABASE TRIGGER (optional, for session management)
GRANT ADMINISTER DATABASE TRIGGER TO QLDiemRenLuyen;

PROMPT '✓ Granted VPD-related privileges to QLDiemRenLuyen';

-- =========================================================
-- STEP 2: CREATE APPLICATION CONTEXT
-- =========================================================
-- Note: Context must be created by SYSDBA or user with CREATE ANY CONTEXT

-- Drop existing context if exists (silent fail if not exists)
BEGIN
    EXECUTE IMMEDIATE 'DROP CONTEXT VPD_SCORES_CTX';
    DBMS_OUTPUT.PUT_LINE('✓ Dropped existing VPD_SCORES_CTX');
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('No existing context to drop');
END;
/

-- Create Application Context
-- ACCESSED GLOBALLY allows the context to be shared across sessions
-- (useful for connection pooling in web applications)
CREATE OR REPLACE CONTEXT VPD_SCORES_CTX USING QLDiemRenLuyen.PKG_VPD_CONTEXT ACCESSED GLOBALLY;

PROMPT '✓ Created Application Context VPD_SCORES_CTX';

-- =========================================================
-- VERIFICATION
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'VERIFICATION - Context Created';
PROMPT '========================================';

SELECT NAMESPACE, SCHEMA, PACKAGE, TYPE
FROM DBA_CONTEXT
WHERE NAMESPACE = 'VPD_SCORES_CTX';

PROMPT '';
PROMPT 'Privileges Granted to QLDiemRenLuyen:';
SELECT PRIVILEGE
FROM DBA_SYS_PRIVS
WHERE GRANTEE = 'QLDIEMRENLUYEN'
AND PRIVILEGE IN ('EXECUTE', 'CREATE ANY CONTEXT', 'ADMINISTER DATABASE TRIGGER');

PROMPT '';
PROMPT '✓ PART A COMPLETED SUCCESSFULLY!';
PROMPT 'Next: Run Part B as QLDiemRenLuyen';
PROMPT '========================================';
