-- =========================================================
-- AUDITING - PART 2: FINE-GRAINED AUDITING (Run as SYSDBA)
-- =========================================================
-- Connection: SYSDBA (sys as sysdba)
-- Purpose: Create FGA policies using DBMS_FGA
-- Prerequisite: Run Part 1 and restart database first!
-- =========================================================
--
-- FGA audits SELECT statements on sensitive data
-- Stored in SYS.FGA_LOG$ / DBA_FGA_AUDIT_TRAIL
--
-- =========================================================

SET SERVEROUTPUT ON;
SET LINESIZE 200;

PROMPT '========================================';
PROMPT 'AUDITING PART 2 - Fine-Grained Auditing';
PROMPT 'Executing as: SYSDBA';
PROMPT '========================================';

-- =========================================================
-- STEP 1: VERIFY AUDIT_TRAIL IS ENABLED
-- =========================================================

PROMPT '';
PROMPT 'Verifying audit trail status...';

DECLARE
    v_audit_trail VARCHAR2(100);
BEGIN
    SELECT VALUE INTO v_audit_trail
    FROM V$PARAMETER
    WHERE NAME = 'audit_trail';
    
    IF v_audit_trail LIKE '%DB%' THEN
        DBMS_OUTPUT.PUT_LINE('✓ AUDIT_TRAIL is enabled: ' || v_audit_trail);
    ELSE
        DBMS_OUTPUT.PUT_LINE('⚠ AUDIT_TRAIL is: ' || v_audit_trail);
        DBMS_OUTPUT.PUT_LINE('Please enable AUDIT_TRAIL=DB,EXTENDED and restart database');
    END IF;
END;
/

-- =========================================================
-- STEP 2: DROP EXISTING FGA POLICIES (if re-running)
-- =========================================================

PROMPT '';
PROMPT 'Cleaning up existing FGA policies...';

BEGIN
    DBMS_FGA.DROP_POLICY(
        object_schema => 'QLDIEMRENLUYEN',
        object_name   => 'STUDENTS',
        policy_name   => 'FGA_STUDENTS_SENSITIVE'
    );
    DBMS_OUTPUT.PUT_LINE('Dropped FGA_STUDENTS_SENSITIVE');
EXCEPTION
    WHEN OTHERS THEN NULL;
END;
/

BEGIN
    DBMS_FGA.DROP_POLICY(
        object_schema => 'QLDIEMRENLUYEN',
        object_name   => 'SCORES',
        policy_name   => 'FGA_SCORES_READ'
    );
    DBMS_OUTPUT.PUT_LINE('Dropped FGA_SCORES_READ');
EXCEPTION
    WHEN OTHERS THEN NULL;
END;
/

BEGIN
    DBMS_FGA.DROP_POLICY(
        object_schema => 'QLDIEMRENLUYEN',
        object_name   => 'FEEDBACKS',
        policy_name   => 'FGA_FEEDBACKS_CONTENT'
    );
    DBMS_OUTPUT.PUT_LINE('Dropped FGA_FEEDBACKS_CONTENT');
EXCEPTION
    WHEN OTHERS THEN NULL;
END;
/

BEGIN
    DBMS_FGA.DROP_POLICY(
        object_schema => 'QLDIEMRENLUYEN',
        object_name   => 'USERS',
        policy_name   => 'FGA_USERS_PASSWORD'
    );
    DBMS_OUTPUT.PUT_LINE('Dropped FGA_USERS_PASSWORD');
EXCEPTION
    WHEN OTHERS THEN NULL;
END;
/

-- =========================================================
-- STEP 3: CREATE FGA POLICY FOR STUDENTS SENSITIVE DATA
-- =========================================================

PROMPT '';
PROMPT 'Creating FGA policies...';

-- Policy: Audit access to student sensitive information
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => 'QLDIEMRENLUYEN',
        object_name     => 'STUDENTS',
        policy_name     => 'FGA_STUDENTS_SENSITIVE',
        audit_column    => 'PHONE,ADDRESS,ID_CARD_NUMBER,PHONE_ENCRYPTED,ADDRESS_ENCRYPTED,ID_CARD_ENCRYPTED',
        audit_condition => NULL,  -- Audit all SELECT
        statement_types => 'SELECT',
        audit_trail     => DBMS_FGA.DB + DBMS_FGA.EXTENDED
    );
    DBMS_OUTPUT.PUT_LINE('✓ Created FGA_STUDENTS_SENSITIVE');
    DBMS_OUTPUT.PUT_LINE('  - Audits: PHONE, ADDRESS, ID_CARD_NUMBER (encrypted fields)');
END;
/

-- =========================================================
-- STEP 4: CREATE FGA POLICY FOR SCORES
-- =========================================================

-- Policy: Audit read access to scores
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => 'QLDIEMRENLUYEN',
        object_name     => 'SCORES',
        policy_name     => 'FGA_SCORES_READ',
        audit_column    => 'TOTAL_SCORE,CLASSIFICATION,STATUS',
        audit_condition => NULL,  -- Audit all SELECT
        statement_types => 'SELECT',
        audit_trail     => DBMS_FGA.DB + DBMS_FGA.EXTENDED
    );
    DBMS_OUTPUT.PUT_LINE('✓ Created FGA_SCORES_READ');
    DBMS_OUTPUT.PUT_LINE('  - Audits: TOTAL_SCORE, CLASSIFICATION, STATUS');
END;
/

-- =========================================================
-- STEP 5: CREATE FGA POLICY FOR FEEDBACKS
-- =========================================================

-- Policy: Audit read access to feedback content
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => 'QLDIEMRENLUYEN',
        object_name     => 'FEEDBACKS',
        policy_name     => 'FGA_FEEDBACKS_CONTENT',
        audit_column    => 'CONTENT,RESPONSE,CONTENT_ENCRYPTED,RESPONSE_ENCRYPTED',
        audit_condition => NULL,  -- Audit all SELECT
        statement_types => 'SELECT',
        audit_trail     => DBMS_FGA.DB + DBMS_FGA.EXTENDED
    );
    DBMS_OUTPUT.PUT_LINE('✓ Created FGA_FEEDBACKS_CONTENT');
    DBMS_OUTPUT.PUT_LINE('  - Audits: CONTENT, RESPONSE (encrypted fields)');
END;
/

-- =========================================================
-- STEP 6: CREATE FGA POLICY FOR USERS PASSWORD
-- =========================================================

-- Policy: Audit access to password-related fields
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => 'QLDIEMRENLUYEN',
        object_name     => 'USERS',
        policy_name     => 'FGA_USERS_PASSWORD',
        audit_column    => 'PASSWORD_HASH,PASSWORD_SALT',
        audit_condition => NULL,  -- Audit all SELECT
        statement_types => 'SELECT',
        audit_trail     => DBMS_FGA.DB + DBMS_FGA.EXTENDED
    );
    DBMS_OUTPUT.PUT_LINE('✓ Created FGA_USERS_PASSWORD');
    DBMS_OUTPUT.PUT_LINE('  - Audits: PASSWORD_HASH, PASSWORD_SALT');
END;
/

-- =========================================================
-- STEP 7: ENABLE FGA POLICIES
-- =========================================================

PROMPT '';
PROMPT 'Enabling FGA policies...';

BEGIN
    DBMS_FGA.ENABLE_POLICY('QLDIEMRENLUYEN', 'STUDENTS', 'FGA_STUDENTS_SENSITIVE');
    DBMS_OUTPUT.PUT_LINE('✓ Enabled FGA_STUDENTS_SENSITIVE');
END;
/

BEGIN
    DBMS_FGA.ENABLE_POLICY('QLDIEMRENLUYEN', 'SCORES', 'FGA_SCORES_READ');
    DBMS_OUTPUT.PUT_LINE('✓ Enabled FGA_SCORES_READ');
END;
/

BEGIN
    DBMS_FGA.ENABLE_POLICY('QLDIEMRENLUYEN', 'FEEDBACKS', 'FGA_FEEDBACKS_CONTENT');
    DBMS_OUTPUT.PUT_LINE('✓ Enabled FGA_FEEDBACKS_CONTENT');
END;
/

BEGIN
    DBMS_FGA.ENABLE_POLICY('QLDIEMRENLUYEN', 'USERS', 'FGA_USERS_PASSWORD');
    DBMS_OUTPUT.PUT_LINE('✓ Enabled FGA_USERS_PASSWORD');
END;
/

-- =========================================================
-- VERIFICATION
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'VERIFICATION - FGA Policies';
PROMPT '========================================';

PROMPT '';
PROMPT 'Configured FGA Policies:';
SELECT 
    OBJECT_SCHEMA,
    OBJECT_NAME,
    POLICY_NAME,
    POLICY_COLUMN,
    ENABLED
FROM DBA_AUDIT_POLICIES
WHERE OBJECT_SCHEMA = 'QLDIEMRENLUYEN'
ORDER BY OBJECT_NAME;

PROMPT '';
PROMPT '========================================';
PROMPT '✓ PART 2 COMPLETED!';
PROMPT 'FGA policies created for:';
PROMPT '  - STUDENTS (sensitive personal info)';
PROMPT '  - SCORES (grade information)';
PROMPT '  - FEEDBACKS (complaint content)';
PROMPT '  - USERS (password fields)';
PROMPT '';
PROMPT 'Next: Run Part 3 as QLDiemRenLuyen';
PROMPT '========================================';
