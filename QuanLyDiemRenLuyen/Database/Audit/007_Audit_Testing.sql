-- =========================================================
-- AUDITING - PART 7: TESTING (Run as QLDiemRenLuyen)
-- =========================================================
-- Connection: QLDiemRenLuyen
-- Purpose: Test all auditing functionality
-- Prerequisite: Run Parts 1-6 first!
-- =========================================================

SET SERVEROUTPUT ON;
SET LINESIZE 200;

PROMPT '========================================';
PROMPT 'AUDITING PART 7 - Testing';
PROMPT 'Executing as: QLDiemRenLuyen';
PROMPT '========================================';

-- =========================================================
-- TEST 1: VERIFY AUDIT_TRAIL SETTING
-- =========================================================

PROMPT '';
PROMPT '=== TEST 1: Verify AUDIT_TRAIL Setting ===';

DECLARE
    v_audit_trail VARCHAR2(100);
BEGIN
    SELECT VALUE INTO v_audit_trail
    FROM V$PARAMETER
    WHERE NAME = 'audit_trail';
    
    DBMS_OUTPUT.PUT_LINE('AUDIT_TRAIL = ' || v_audit_trail);
    
    IF v_audit_trail LIKE '%DB%' THEN
        DBMS_OUTPUT.PUT_LINE('✓ Standard Auditing is enabled');
    ELSE
        DBMS_OUTPUT.PUT_LINE('⚠ Standard Auditing may not be fully enabled');
    END IF;
END;
/

-- =========================================================
-- TEST 2: VERIFY FGA POLICIES
-- =========================================================

PROMPT '';
PROMPT '=== TEST 2: Verify FGA Policies ===';

SELECT OBJECT_NAME, POLICY_NAME, ENABLED
FROM DBA_AUDIT_POLICIES
WHERE OBJECT_SCHEMA = 'QLDIEMRENLUYEN'
ORDER BY OBJECT_NAME;

-- =========================================================
-- TEST 3: TEST TRIGGER-BASED AUDITING WITH JUSTIFICATION
-- =========================================================

PROMPT '';
PROMPT '=== TEST 3: Test Trigger Auditing ===';

DECLARE
    v_test_score_id NUMBER;
    v_audit_count NUMBER;
BEGIN
    -- Set audit context with justification
    PKG_AUDIT_CONTEXT.SET_CONTEXT(
        p_user_id       => 'TEST_ADMIN',
        p_justification => 'Testing audit functionality'
    );
    DBMS_OUTPUT.PUT_LINE('✓ Set audit context');
    
    -- Get a score ID for testing (if exists)
    BEGIN
        SELECT ID INTO v_test_score_id
        FROM SCORES
        WHERE ROWNUM = 1;
        
        -- Update the score
        UPDATE SCORES
        SET TOTAL_SCORE = TOTAL_SCORE
        WHERE ID = v_test_score_id;
        
        DBMS_OUTPUT.PUT_LINE('✓ Updated score ID: ' || v_test_score_id);
        
        -- Check audit log
        SELECT COUNT(*)
        INTO v_audit_count
        FROM AUDIT_CHANGE_LOGS
        WHERE TABLE_NAME = 'SCORES'
          AND RECORD_ID = TO_CHAR(v_test_score_id);
        
        DBMS_OUTPUT.PUT_LINE('✓ Audit entries for this score: ' || v_audit_count);
        
        ROLLBACK;
        DBMS_OUTPUT.PUT_LINE('✓ Rolled back test changes');
        
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            DBMS_OUTPUT.PUT_LINE('No scores found for testing');
    END;
    
    -- Clear context
    PKG_AUDIT_CONTEXT.CLEAR_CONTEXT;
    DBMS_OUTPUT.PUT_LINE('✓ Cleared audit context');
END;
/

-- =========================================================
-- TEST 4: TEST FGA (SELECT AUDIT)
-- =========================================================

PROMPT '';
PROMPT '=== TEST 4: Test FGA (SELECT Audit) ===';

DECLARE
    v_count NUMBER;
    v_phone VARCHAR2(50);
BEGIN
    -- Select sensitive data to trigger FGA
    BEGIN
        SELECT PHONE INTO v_phone
        FROM STUDENTS
        WHERE ROWNUM = 1;
        
        DBMS_OUTPUT.PUT_LINE('✓ Selected sensitive data (PHONE)');
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            DBMS_OUTPUT.PUT_LINE('No students found');
    END;
    
    -- Note: FGA audit may have slight delay
    DBMS_OUTPUT.PUT_LINE('Note: Check DBA_FGA_AUDIT_TRAIL for FGA entries');
END;
/

-- =========================================================
-- TEST 5: TEST BUSINESS ACTION LOGGING
-- =========================================================

PROMPT '';
PROMPT '=== TEST 5: Test Business Action Logging ===';

BEGIN
    SP_LOG_BUSINESS_ACTION(
        p_action_type   => 'TEST_ACTION',
        p_action_desc   => 'Testing audit business action logging',
        p_entity_type   => 'SYSTEM',
        p_entity_id     => 'TEST_001',
        p_performed_by  => 'TEST_ADMIN',
        p_details       => '{"test": true, "purpose": "verification"}',
        p_status        => 'SUCCESS'
    );
    DBMS_OUTPUT.PUT_LINE('✓ Logged business action');
END;
/

-- Verify
SELECT ACTION_TYPE, ACTION_DESC, PERFORMED_BY, STATUS
FROM AUDIT_BUSINESS_ACTIONS
WHERE ACTION_TYPE = 'TEST_ACTION'
ORDER BY PERFORMED_AT DESC
FETCH FIRST 1 ROW ONLY;

-- =========================================================
-- TEST 6: TEST VIEWS
-- =========================================================

PROMPT '';
PROMPT '=== TEST 6: Test Audit Views ===';

PROMPT '';
PROMPT 'Recent changes (last 7 days):';
SELECT TABLE_NAME, OPERATION, COUNT(*) AS CNT
FROM V_AUDIT_RECENT_CHANGES
GROUP BY TABLE_NAME, OPERATION
ORDER BY TABLE_NAME;

PROMPT '';
PROMPT 'Daily summary:';
SELECT AUDIT_DATE, TABLE_NAME, CHANGE_COUNT
FROM V_AUDIT_DAILY_SUMMARY
WHERE AUDIT_DATE >= SYSDATE - 1
ORDER BY AUDIT_DATE DESC, TABLE_NAME;

-- =========================================================
-- TEST 7: TEST RECORD HISTORY
-- =========================================================

PROMPT '';
PROMPT '=== TEST 7: Test Record History ===';

DECLARE
    v_cursor SYS_REFCURSOR;
    v_id VARCHAR2(32);
    v_operation VARCHAR2(10);
    v_performed_at TIMESTAMP;
    v_justification VARCHAR2(1000);
BEGIN
    -- Get history for a score (if exists)
    BEGIN
        SP_GET_RECORD_HISTORY(
            p_table_name => 'SCORES',
            p_record_id  => '1',
            p_result     => v_cursor
        );
        
        DBMS_OUTPUT.PUT_LINE('History for SCORES record 1:');
        
        LOOP
            FETCH v_cursor INTO v_id, v_operation, v_performed_at, v_justification,
                                v_id, v_id, v_id, v_id; -- Dummy fetches
            EXIT WHEN v_cursor%NOTFOUND;
            DBMS_OUTPUT.PUT_LINE('  ' || v_operation || ' at ' || v_performed_at);
        END LOOP;
        
        CLOSE v_cursor;
    EXCEPTION
        WHEN OTHERS THEN
            DBMS_OUTPUT.PUT_LINE('Could not retrieve history: ' || SQLERRM);
    END;
END;
/

-- =========================================================
-- SUMMARY
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'AUDIT TESTING SUMMARY';
PROMPT '========================================';

PROMPT '';
PROMPT 'Audit Configuration:';

SELECT 'Audit Tables' AS COMPONENT, COUNT(*) AS COUNT
FROM USER_TABLES WHERE TABLE_NAME LIKE 'AUDIT%'
UNION ALL
SELECT 'Audit Triggers', COUNT(*)
FROM USER_TRIGGERS WHERE TRIGGER_NAME LIKE 'TRG_AUDIT%'
UNION ALL
SELECT 'Audit Views', COUNT(*)
FROM USER_VIEWS WHERE VIEW_NAME LIKE 'V_AUDIT%'
UNION ALL
SELECT 'Change Logs', COUNT(*)
FROM AUDIT_CHANGE_LOGS
UNION ALL
SELECT 'Business Actions', COUNT(*)
FROM AUDIT_BUSINESS_ACTIONS;

PROMPT '';
PROMPT 'Trigger Status:';
SELECT TRIGGER_NAME, STATUS
FROM USER_TRIGGERS
WHERE TRIGGER_NAME LIKE 'TRG_AUDIT%'
ORDER BY TRIGGER_NAME;

PROMPT '';
PROMPT '========================================';
PROMPT '✓ AUDITING TESTING COMPLETE!';
PROMPT '========================================';
PROMPT '';
PROMPT 'Summary:';
PROMPT '1. Standard Audit: AUDIT_TRAIL=DB,EXTENDED';
PROMPT '2. FGA Policies: STUDENTS, SCORES, FEEDBACKS, USERS';
PROMPT '3. Triggers: SCORES, USERS, FEEDBACKS, etc.';
PROMPT '4. Views: V_AUDIT_* for reporting';
PROMPT '';
PROMPT 'Usage in Application:';
PROMPT '  // Before making changes:';
PROMPT '  EXEC SP_SET_AUDIT_JUSTIFICATION(user_id, reason);';
PROMPT '  // After changes:';
PROMPT '  EXEC SP_CLEAR_AUDIT_CONTEXT;';
PROMPT '';
PROMPT '  // Log business action:';
PROMPT '  EXEC SP_LOG_BUSINESS_ACTION(type, desc, ...);';
PROMPT '';
PROMPT '  // Get record history:';
PROMPT '  EXEC SP_GET_RECORD_HISTORY(table, id, cursor);';
PROMPT '========================================';
