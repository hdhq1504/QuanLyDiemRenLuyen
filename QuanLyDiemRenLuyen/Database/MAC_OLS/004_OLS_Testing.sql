-- =========================================================
-- MAC + OLS - PHẦN D (Chạy với SYSDBA)
-- =========================================================
-- Kết nối: SYSDBA (sys as sysdba)
-- Mục đích: Kiểm thử chính sách OLS
-- Điều kiện: Chạy script 001, 002, 003 trước!
-- =========================================================

SET SERVEROUTPUT ON;
SET LINESIZE 200;

PROMPT '========================================';
PROMPT 'MAC + OLS PHẦN D - Kiểm thử OLS Policies';
PROMPT 'Đang thực thi với: SYSDBA';
PROMPT '========================================';

-- =========================================================
-- STEP 1: VERIFY POLICY IS APPLIED
-- =========================================================

PROMPT '';
PROMPT 'Verifying policy application...';

SELECT SCHEMA_NAME, TABLE_NAME, POLICY_NAME, STATUS
FROM DBA_SA_TABLE_POLICIES
WHERE POLICY_NAME = 'OLS_DRL_POLICY';

-- =========================================================
-- STEP 2: CHECK LABEL COLUMN EXISTS
-- =========================================================

PROMPT '';
PROMPT 'Checking OLS_LABEL columns...';

SELECT OWNER, TABLE_NAME, COLUMN_NAME, DATA_TYPE
FROM DBA_TAB_COLUMNS
WHERE COLUMN_NAME = 'OLS_LABEL' AND OWNER = 'QLDIEMRENLUYEN'
ORDER BY TABLE_NAME;

-- =========================================================
-- STEP 3: CHECK USER LEVELS
-- =========================================================

PROMPT '';
PROMPT 'User Levels configured:';

SELECT USER_NAME, MAX_LEVEL, MIN_LEVEL, DEF_LEVEL
FROM DBA_SA_USER_LEVELS
WHERE POLICY_NAME = 'OLS_DRL_POLICY'
ORDER BY USER_NAME;

-- =========================================================
-- STEP 4: CHECK LABELS CREATED
-- =========================================================

PROMPT '';
PROMPT 'Labels available:';

SELECT LABEL_TAG, LABEL
FROM DBA_SA_LABELS
WHERE POLICY_NAME = 'OLS_DRL_POLICY'
ORDER BY LABEL_TAG;

-- =========================================================
-- STEP 5: CHECK DATA DISTRIBUTION BY LABEL
-- =========================================================

PROMPT '';
PROMPT 'Data distribution by label...';

PROMPT '';
PROMPT 'FEEDBACKS by label:';
BEGIN
    FOR rec IN (
        SELECT OLS_LABEL, COUNT(*) as CNT
        FROM QLDIEMRENLUYEN.FEEDBACKS
        GROUP BY OLS_LABEL
        ORDER BY OLS_LABEL
    ) LOOP
        DBMS_OUTPUT.PUT_LINE('Label ' || NVL(TO_CHAR(rec.OLS_LABEL), 'NULL') || ': ' || rec.CNT || ' records');
    END LOOP;
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('FEEDBACKS: ' || SQLERRM);
END;
/

PROMPT '';
PROMPT 'PROOFS by label:';
BEGIN
    FOR rec IN (
        SELECT OLS_LABEL, COUNT(*) as CNT
        FROM QLDIEMRENLUYEN.PROOFS
        GROUP BY OLS_LABEL
        ORDER BY OLS_LABEL
    ) LOOP
        DBMS_OUTPUT.PUT_LINE('Label ' || NVL(TO_CHAR(rec.OLS_LABEL), 'NULL') || ': ' || rec.CNT || ' records');
    END LOOP;
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('PROOFS: ' || SQLERRM);
END;
/

PROMPT '';
PROMPT 'SCORE_AUDIT_SIGNATURES by label:';
BEGIN
    FOR rec IN (
        SELECT OLS_LABEL, COUNT(*) as CNT
        FROM QLDIEMRENLUYEN.SCORE_AUDIT_SIGNATURES
        GROUP BY OLS_LABEL
        ORDER BY OLS_LABEL
    ) LOOP
        DBMS_OUTPUT.PUT_LINE('Label ' || NVL(TO_CHAR(rec.OLS_LABEL), 'NULL') || ': ' || rec.CNT || ' records');
    END LOOP;
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('SCORE_AUDIT_SIGNATURES: ' || SQLERRM);
END;
/

-- =========================================================
-- STEP 6: TEST SESSION LABEL SETTING
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'Trigger and Session Procedure Tests';
PROMPT '========================================';

PROMPT '';
PROMPT 'Triggers status:';
SELECT OWNER, TRIGGER_NAME, TABLE_NAME, STATUS
FROM DBA_TRIGGERS
WHERE OWNER = 'QLDIEMRENLUYEN' AND TRIGGER_NAME LIKE 'TRG_%OLS%';

PROMPT '';
PROMPT 'Session procedure exists:';
SELECT OBJECT_NAME, OBJECT_TYPE, STATUS
FROM DBA_OBJECTS
WHERE OWNER = 'QLDIEMRENLUYEN' AND OBJECT_NAME = 'SP_SET_OLS_SESSION';

-- =========================================================
-- SUMMARY
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'OLS TESTING COMPLETE';
PROMPT '========================================';
PROMPT '';
PROMPT 'OLS Configuration Summary:';
PROMPT '';
PROMPT 'Security Levels:';
PROMPT '  - PUB (100): Public - Student access';
PROMPT '  - INT (200): Internal - Lecturer access';
PROMPT '  - CONF (300): Confidential - Admin access';
PROMPT '';
PROMPT 'Compartments:';
PROMPT '  - FB: Feedback data';
PROMPT '  - EV: Evidence/Proof data';
PROMPT '  - AU: Audit data';
PROMPT '';
PROMPT 'Groups:';
PROMPT '  - UNI: University (top level)';
PROMPT '  - DEPT: Department (mid level)';
PROMPT '  - CLS: Class (low level)';
PROMPT '';
PROMPT 'Tables Protected:';
PROMPT '  - FEEDBACKS';
PROMPT '  - PROOFS';
PROMPT '  - SCORE_AUDIT_SIGNATURES';
PROMPT '';
PROMPT 'Access Control:';
PROMPT '  - STUDENT: PUB level only';
PROMPT '  - LECTURER: PUB + INT levels';
PROMPT '  - ADMIN: All levels (PUB + INT + CONF)';
PROMPT '';
PROMPT '========================================';
PROMPT 'Integration Note:';
PROMPT 'Call QLDIEMRENLUYEN.SP_SET_OLS_SESSION(user_id, role)';
PROMPT 'after login to set the session label.';
PROMPT '========================================';
