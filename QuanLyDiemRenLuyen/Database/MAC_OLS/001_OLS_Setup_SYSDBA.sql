-- =========================================================
-- MAC + OLS - PHẦN A (Chạy với SYSDBA)
-- =========================================================
-- Kết nối: SYSDBA (sys/sys)
-- Mục đích: Thiết lập chính sách Oracle Label Security (OLS)
-- =========================================================
--
-- ĐIỀU KIỆN TIÊN QUYẾT:
-- 1. Oracle Enterprise Edition với tùy chọn OLS được bật
-- 2. Chạy: SELECT * FROM V$OPTION WHERE PARAMETER = 'Oracle Label Security';
--    Kết quả phải là VALUE = 'TRUE'
-- 3. User LBACSYS phải được mở khóa
--
-- =========================================================

SET SERVEROUTPUT ON;
SET LINESIZE 200;

PROMPT '========================================';
PROMPT 'MAC + OLS PHẦN A - Thiết lập Chính sách OLS';
PROMPT 'Đang thực thi với: SYSDBA';
PROMPT '========================================';

-- =========================================================
-- STEP 0: VERIFY OLS IS AVAILABLE
-- =========================================================

PROMPT '';
PROMPT 'Checking OLS availability...';

DECLARE
    v_ols_enabled VARCHAR2(10);
BEGIN
    SELECT VALUE INTO v_ols_enabled
    FROM V$OPTION
    WHERE PARAMETER = 'Oracle Label Security';
    
    IF v_ols_enabled = 'TRUE' THEN
        DBMS_OUTPUT.PUT_LINE('✓ Oracle Label Security is ENABLED');
    ELSE
        DBMS_OUTPUT.PUT_LINE('✗ Oracle Label Security is NOT enabled');
        DBMS_OUTPUT.PUT_LINE('Please enable OLS before continuing');
        RAISE_APPLICATION_ERROR(-20001, 'OLS not enabled');
    END IF;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        DBMS_OUTPUT.PUT_LINE('✗ Oracle Label Security option not found');
        DBMS_OUTPUT.PUT_LINE('This feature requires Oracle Enterprise Edition');
        RAISE_APPLICATION_ERROR(-20001, 'OLS not available');
END;
/

-- =========================================================
-- STEP 1: UNLOCK AND SETUP LBACSYS USER
-- =========================================================

PROMPT '';
PROMPT 'Setting up LBACSYS user...';

-- Unlock LBACSYS if locked
ALTER USER LBACSYS ACCOUNT UNLOCK;
ALTER USER LBACSYS IDENTIFIED BY "lbacsys123";

-- Grant INHERIT PRIVILEGES to fix ORA-06598
GRANT INHERIT PRIVILEGES ON USER SYS TO LBACSYS;
GRANT INHERIT PRIVILEGES ON USER LBACSYS TO SYS;

PROMPT '✓ LBACSYS user unlocked and privileges granted';

-- =========================================================
-- STEP 2: DROP EXISTING POLICY (if re-running)
-- =========================================================

PROMPT '';
PROMPT 'Checking for existing policy...';

DECLARE
    v_policy_exists NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_policy_exists
    FROM DBA_SA_POLICIES
    WHERE POLICY_NAME = 'OLS_DRL_POLICY';
    
    IF v_policy_exists > 0 THEN
        -- Drop policy from all tables first
        BEGIN
            SA_POLICY_ADMIN.REMOVE_TABLE_POLICY(
                policy_name => 'OLS_DRL_POLICY',
                schema_name => 'QLDIEMRENLUYEN',
                table_name  => 'FEEDBACKS'
            );
        EXCEPTION WHEN OTHERS THEN NULL;
        END;
        
        BEGIN
            SA_POLICY_ADMIN.REMOVE_TABLE_POLICY(
                policy_name => 'OLS_DRL_POLICY',
                schema_name => 'QLDIEMRENLUYEN',
                table_name  => 'PROOFS'
            );
        EXCEPTION WHEN OTHERS THEN NULL;
        END;
        
        BEGIN
            SA_POLICY_ADMIN.REMOVE_TABLE_POLICY(
                policy_name => 'OLS_DRL_POLICY',
                schema_name => 'QLDIEMRENLUYEN',
                table_name  => 'SCORE_AUDIT_SIGNATURES'
            );
        EXCEPTION WHEN OTHERS THEN NULL;
        END;
        
        -- Drop policy
        SA_SYSDBA.DROP_POLICY(
            policy_name => 'OLS_DRL_POLICY',
            drop_column => TRUE
        );
        DBMS_OUTPUT.PUT_LINE('✓ Dropped existing OLS_DRL_POLICY');
    ELSE
        DBMS_OUTPUT.PUT_LINE('No existing policy to drop');
    END IF;
END;
/

-- =========================================================
-- STEP 3: CREATE OLS POLICY
-- =========================================================

PROMPT '';
PROMPT 'Creating OLS Policy...';

BEGIN
    SA_SYSDBA.CREATE_POLICY(
        policy_name => 'OLS_DRL_POLICY',
        column_name => 'OLS_LABEL'
    );
    DBMS_OUTPUT.PUT_LINE('✓ Created OLS_DRL_POLICY');
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Error creating policy: ' || SQLERRM);
        RAISE;
END;
/

-- =========================================================
-- STEP 4: CREATE SECURITY LEVELS
-- =========================================================

PROMPT '';
PROMPT 'Creating Security Levels...';

-- Level 100: PUBLIC (Công khai) - Thấp nhất
BEGIN
    SA_COMPONENTS.CREATE_LEVEL(
        policy_name  => 'OLS_DRL_POLICY',
        level_num    => 100,
        short_name   => 'PUB',
        long_name    => 'PUBLIC'
    );
    DBMS_OUTPUT.PUT_LINE('✓ Created Level: PUBLIC (100)');
END;
/

-- Level 200: INTERNAL (Nội bộ) - Trung bình
BEGIN
    SA_COMPONENTS.CREATE_LEVEL(
        policy_name  => 'OLS_DRL_POLICY',
        level_num    => 200,
        short_name   => 'INT',
        long_name    => 'INTERNAL'
    );
    DBMS_OUTPUT.PUT_LINE('✓ Created Level: INTERNAL (200)');
END;
/

-- Level 300: CONFIDENTIAL (Bí mật) - Cao nhất
BEGIN
    SA_COMPONENTS.CREATE_LEVEL(
        policy_name  => 'OLS_DRL_POLICY',
        level_num    => 300,
        short_name   => 'CONF',
        long_name    => 'CONFIDENTIAL'
    );
    DBMS_OUTPUT.PUT_LINE('✓ Created Level: CONFIDENTIAL (300)');
END;
/

-- =========================================================
-- STEP 5: CREATE COMPARTMENTS
-- =========================================================

PROMPT '';
PROMPT 'Creating Compartments...';

-- Compartment for Feedback data
BEGIN
    SA_COMPONENTS.CREATE_COMPARTMENT(
        policy_name      => 'OLS_DRL_POLICY',
        comp_num         => 10,
        short_name       => 'FB',
        long_name        => 'FEEDBACK'
    );
    DBMS_OUTPUT.PUT_LINE('✓ Created Compartment: FEEDBACK (FB)');
END;
/

-- Compartment for Evidence/Proof data
BEGIN
    SA_COMPONENTS.CREATE_COMPARTMENT(
        policy_name      => 'OLS_DRL_POLICY',
        comp_num         => 20,
        short_name       => 'EV',
        long_name        => 'EVIDENCE'
    );
    DBMS_OUTPUT.PUT_LINE('✓ Created Compartment: EVIDENCE (EV)');
END;
/

-- Compartment for Audit data
BEGIN
    SA_COMPONENTS.CREATE_COMPARTMENT(
        policy_name      => 'OLS_DRL_POLICY',
        comp_num         => 30,
        short_name       => 'AU',
        long_name        => 'AUDIT'
    );
    DBMS_OUTPUT.PUT_LINE('✓ Created Compartment: AUDIT (AU)');
END;
/

-- =========================================================
-- STEP 6: CREATE GROUPS (Hierarchical)
-- =========================================================

PROMPT '';
PROMPT 'Creating Groups...';

-- Top level: UNIVERSITY (Toàn trường)
BEGIN
    SA_COMPONENTS.CREATE_GROUP(
        policy_name  => 'OLS_DRL_POLICY',
        group_num    => 1000,
        short_name   => 'UNI',
        long_name    => 'UNIVERSITY',
        parent_name  => NULL
    );
    DBMS_OUTPUT.PUT_LINE('✓ Created Group: UNIVERSITY (UNI)');
END;
/

-- Mid level: DEPARTMENT (Cấp Khoa)
BEGIN
    SA_COMPONENTS.CREATE_GROUP(
        policy_name  => 'OLS_DRL_POLICY',
        group_num    => 2000,
        short_name   => 'DEPT',
        long_name    => 'DEPARTMENT',
        parent_name  => 'UNI'
    );
    DBMS_OUTPUT.PUT_LINE('✓ Created Group: DEPARTMENT (DEPT) -> UNI');
END;
/

-- Low level: CLASS (Cấp Lớp) 
BEGIN
    SA_COMPONENTS.CREATE_GROUP(
        policy_name  => 'OLS_DRL_POLICY',
        group_num    => 3000,
        short_name   => 'CLS',
        long_name    => 'CLASS',
        parent_name  => 'DEPT'
    );
    DBMS_OUTPUT.PUT_LINE('✓ Created Group: CLASS (CLS) -> DEPT');
END;
/

-- =========================================================
-- STEP 7: CREATE DATA LABELS
-- =========================================================

PROMPT '';
PROMPT 'Creating Data Labels...';

-- Label for PUBLIC data at CLASS level (Students can see their own)
BEGIN
    SA_LABEL_ADMIN.CREATE_LABEL(
        policy_name  => 'OLS_DRL_POLICY',
        label_tag    => 1000,
        label_value  => 'PUB::CLS',
        data_label   => TRUE
    );
    DBMS_OUTPUT.PUT_LINE('✓ Created Label: PUB::CLS (tag 1000)');
END;
/

-- Label for INTERNAL FEEDBACK at DEPARTMENT level
BEGIN
    SA_LABEL_ADMIN.CREATE_LABEL(
        policy_name  => 'OLS_DRL_POLICY',
        label_tag    => 2010,
        label_value  => 'INT:FB:DEPT',
        data_label   => TRUE
    );
    DBMS_OUTPUT.PUT_LINE('✓ Created Label: INT:FB:DEPT (tag 2010)');
END;
/

-- Label for INTERNAL EVIDENCE at DEPARTMENT level
BEGIN
    SA_LABEL_ADMIN.CREATE_LABEL(
        policy_name  => 'OLS_DRL_POLICY',
        label_tag    => 2020,
        label_value  => 'INT:EV:DEPT',
        data_label   => TRUE
    );
    DBMS_OUTPUT.PUT_LINE('✓ Created Label: INT:EV:DEPT (tag 2020)');
END;
/

-- Label for INTERNAL AUDIT at DEPARTMENT level
BEGIN
    SA_LABEL_ADMIN.CREATE_LABEL(
        policy_name  => 'OLS_DRL_POLICY',
        label_tag    => 2030,
        label_value  => 'INT:AU:DEPT',
        data_label   => TRUE
    );
    DBMS_OUTPUT.PUT_LINE('✓ Created Label: INT:AU:DEPT (tag 2030)');
END;
/

-- Label for CONFIDENTIAL FEEDBACK at UNIVERSITY level
BEGIN
    SA_LABEL_ADMIN.CREATE_LABEL(
        policy_name  => 'OLS_DRL_POLICY',
        label_tag    => 3010,
        label_value  => 'CONF:FB:UNI',
        data_label   => TRUE
    );
    DBMS_OUTPUT.PUT_LINE('✓ Created Label: CONF:FB:UNI (tag 3010)');
END;
/

-- Label for CONFIDENTIAL AUDIT at UNIVERSITY level
BEGIN
    SA_LABEL_ADMIN.CREATE_LABEL(
        policy_name  => 'OLS_DRL_POLICY',
        label_tag    => 3030,
        label_value  => 'CONF:AU:UNI',
        data_label   => TRUE
    );
    DBMS_OUTPUT.PUT_LINE('✓ Created Label: CONF:AU:UNI (tag 3030)');
END;
/

-- =========================================================
-- STEP 8: GRANT POLICY ADMIN TO SCHEMA OWNER
-- =========================================================

PROMPT '';
PROMPT 'Granting policy administration to QLDiemRenLuyen...';

BEGIN
    SA_USER_ADMIN.SET_USER_PRIVS(
        policy_name  => 'OLS_DRL_POLICY',
        user_name    => 'QLDIEMRENLUYEN',
        privileges   => 'FULL'
    );
    DBMS_OUTPUT.PUT_LINE('✓ Granted FULL privileges to QLDiemRenLuyen');
END;
/

-- =========================================================
-- VERIFICATION
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'VERIFICATION - OLS Components Created';
PROMPT '========================================';

PROMPT '';
PROMPT 'Policy:';
SELECT POLICY_NAME, COLUMN_NAME, STATUS
FROM DBA_SA_POLICIES
WHERE POLICY_NAME = 'OLS_DRL_POLICY';

PROMPT '';
PROMPT 'Levels:';
SELECT LEVEL_NUM, SHORT_NAME, LONG_NAME
FROM DBA_SA_LEVELS
WHERE POLICY_NAME = 'OLS_DRL_POLICY'
ORDER BY LEVEL_NUM;

PROMPT '';
PROMPT 'Compartments:';
SELECT COMP_NUM, SHORT_NAME, LONG_NAME
FROM DBA_SA_COMPARTMENTS
WHERE POLICY_NAME = 'OLS_DRL_POLICY'
ORDER BY COMP_NUM;

PROMPT '';
PROMPT 'Groups:';
SELECT GROUP_NUM, SHORT_NAME, LONG_NAME, PARENT_NAME
FROM DBA_SA_GROUPS
WHERE POLICY_NAME = 'OLS_DRL_POLICY'
ORDER BY GROUP_NUM;

PROMPT '';
PROMPT 'Labels:';
SELECT LABEL_TAG, LABEL
FROM DBA_SA_LABELS
WHERE POLICY_NAME = 'OLS_DRL_POLICY'
ORDER BY LABEL_TAG;

PROMPT '';
PROMPT '========================================';
PROMPT '✓ PART A COMPLETED SUCCESSFULLY!';
PROMPT 'Next: Run Part B as QLDiemRenLuyen';
PROMPT '========================================';
