-- =========================================================
-- MAC + OLS - PHẦN C (Chạy với SYSDBA)
-- =========================================================
-- Kết nối: SYSDBA (sys as sysdba)
-- Mục đích: Thiết lập nhãn người dùng cho OLS policy
-- Điều kiện: Chạy script 001 và 002 trước!
-- =========================================================
--
-- LƯU Ý: Sử dụng SHORT_NAME cho levels/groups:
--   Levels: CONF, INT, PUB (không phải CONFIDENTIAL, INTERNAL, PUBLIC)
--   Groups: UNI, DEPT, CLS (không phải UNIVERSITY, DEPARTMENT, CLASS)
--
-- PHÂN QUYỀN NHÃN NGƯỜI DÙNG:
-- STUDENT:  Chỉ đọc được dữ liệu cấp PUB
-- LECTURER: Đọc được dữ liệu cấp PUB + INT
-- ADMIN:    Đọc được tất cả cấp (PUB, INT, CONF)
--
-- =========================================================

SET SERVEROUTPUT ON;
SET LINESIZE 200;

PROMPT '========================================';
PROMPT 'MAC + OLS PHẦN C - Gán nhãn người dùng';
PROMPT 'Đang thực thi với: SYSDBA';
PROMPT '========================================';

-- =========================================================
-- STEP 1: CREATE PROCEDURE TO SET USER LABELS BY ROLE
-- =========================================================

PROMPT '';
PROMPT 'Creating user label management procedure...';

CREATE OR REPLACE PROCEDURE SYS.SP_SET_OLS_USER_LABEL(
    p_username IN VARCHAR2,
    p_role     IN VARCHAR2
) AUTHID DEFINER AS
BEGIN
    CASE p_role
        -- =========================================================
        -- STUDENT: Can only read PUB level data
        -- =========================================================
        WHEN 'STUDENT' THEN
            SA_USER_ADMIN.SET_LEVELS(
                policy_name  => 'OLS_DRL_POLICY',
                user_name    => p_username,
                max_level    => 'PUB',
                min_level    => 'PUB',
                def_level    => 'PUB',
                row_level    => 'PUB'
            );
            
            SA_USER_ADMIN.SET_GROUPS(
                policy_name  => 'OLS_DRL_POLICY',
                user_name    => p_username,
                read_groups  => 'CLS',
                write_groups => 'CLS',
                def_groups   => 'CLS',
                row_groups   => NULL
            );
            
            DBMS_OUTPUT.PUT_LINE('✓ Set STUDENT labels for ' || p_username);
            
        -- =========================================================
        -- LECTURER: Can read PUB + INT level data
        -- =========================================================
        WHEN 'LECTURER' THEN
            SA_USER_ADMIN.SET_LEVELS(
                policy_name  => 'OLS_DRL_POLICY',
                user_name    => p_username,
                max_level    => 'INT',
                min_level    => 'PUB',
                def_level    => 'INT',
                row_level    => 'PUB'
            );
            
            SA_USER_ADMIN.SET_COMPARTMENTS(
                policy_name  => 'OLS_DRL_POLICY',
                user_name    => p_username,
                read_comps   => 'FB,EV',
                write_comps  => 'FB,EV',
                def_comps    => NULL,
                row_comps    => NULL
            );
            
            SA_USER_ADMIN.SET_GROUPS(
                policy_name  => 'OLS_DRL_POLICY',
                user_name    => p_username,
                read_groups  => 'DEPT',
                write_groups => 'DEPT',
                def_groups   => 'DEPT',
                row_groups   => NULL
            );
            
            DBMS_OUTPUT.PUT_LINE('✓ Set LECTURER labels for ' || p_username);
            
        -- =========================================================
        -- ADMIN: Can read all levels
        -- =========================================================
        WHEN 'ADMIN' THEN
            SA_USER_ADMIN.SET_LEVELS(
                policy_name  => 'OLS_DRL_POLICY',
                user_name    => p_username,
                max_level    => 'CONF',
                min_level    => 'PUB',
                def_level    => 'CONF',
                row_level    => 'PUB'
            );
            
            SA_USER_ADMIN.SET_COMPARTMENTS(
                policy_name  => 'OLS_DRL_POLICY',
                user_name    => p_username,
                read_comps   => 'FB,EV,AU',
                write_comps  => 'FB,EV,AU',
                def_comps    => NULL,
                row_comps    => NULL
            );
            
            SA_USER_ADMIN.SET_GROUPS(
                policy_name  => 'OLS_DRL_POLICY',
                user_name    => p_username,
                read_groups  => 'UNI',
                write_groups => 'UNI',
                def_groups   => 'UNI',
                row_groups   => NULL
            );
            
            DBMS_OUTPUT.PUT_LINE('✓ Set ADMIN labels for ' || p_username);
            
        ELSE
            DBMS_OUTPUT.PUT_LINE('Unknown role: ' || p_role);
    END CASE;
    
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Error setting labels: ' || SQLERRM);
END;
/

PROMPT '✓ Created SP_SET_OLS_USER_LABEL procedure';

GRANT EXECUTE ON SYS.SP_SET_OLS_USER_LABEL TO QLDIEMRENLUYEN;

-- =========================================================
-- STEP 2: CREATE PROCEDURE TO SET SESSION LABEL
-- =========================================================

PROMPT '';
PROMPT 'Creating session label procedure in schema...';

CREATE OR REPLACE PROCEDURE QLDIEMRENLUYEN.SP_SET_OLS_SESSION(
    p_user_id IN VARCHAR2,
    p_role    IN VARCHAR2
) AS
BEGIN
    CASE p_role
        WHEN 'STUDENT' THEN
            SA_SESSION.SET_LABEL('OLS_DRL_POLICY', 'PUB::CLS');
        WHEN 'LECTURER' THEN
            SA_SESSION.SET_LABEL('OLS_DRL_POLICY', 'INT:FB,EV:DEPT');
        WHEN 'ADMIN' THEN
            SA_SESSION.SET_LABEL('OLS_DRL_POLICY', 'CONF:FB,EV,AU:UNI');
        ELSE
            SA_SESSION.SET_LABEL('OLS_DRL_POLICY', 'PUB::CLS');
    END CASE;
    
    -- Log the session setup
    BEGIN
        INSERT INTO QLDIEMRENLUYEN.AUDIT_TRAIL(WHO, ACTION, EVENT_AT_UTC)
        VALUES (p_user_id, 'OLS_SESSION_SET|ROLE=' || p_role, SYSTIMESTAMP);
        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN NULL;
    END;
    
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Error setting session label: ' || SQLERRM);
END;
/

PROMPT '✓ Created SP_SET_OLS_SESSION procedure';

-- =========================================================
-- STEP 3: SET LABELS FOR DATABASE ROLES (if they exist)
-- Note: OLS labels for roles may not work in all Oracle versions
-- =========================================================

PROMPT '';
PROMPT 'Setting labels for database roles...';

-- ROLE_STUDENT
BEGIN
    SA_USER_ADMIN.SET_LEVELS(
        policy_name  => 'OLS_DRL_POLICY',
        user_name    => 'ROLE_STUDENT',
        max_level    => 'PUB',
        min_level    => 'PUB',
        def_level    => 'PUB',
        row_level    => 'PUB'
    );
    DBMS_OUTPUT.PUT_LINE('✓ Set labels for ROLE_STUDENT');
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Note: ROLE_STUDENT - ' || SQLERRM);
END;
/

-- ROLE_LECTURER
BEGIN
    SA_USER_ADMIN.SET_LEVELS(
        policy_name  => 'OLS_DRL_POLICY',
        user_name    => 'ROLE_LECTURER',
        max_level    => 'INT',
        min_level    => 'PUB',
        def_level    => 'INT',
        row_level    => 'PUB'
    );
    DBMS_OUTPUT.PUT_LINE('✓ Set labels for ROLE_LECTURER');
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Note: ROLE_LECTURER - ' || SQLERRM);
END;
/

-- ROLE_ADMIN
BEGIN
    SA_USER_ADMIN.SET_LEVELS(
        policy_name  => 'OLS_DRL_POLICY',
        user_name    => 'ROLE_ADMIN',
        max_level    => 'CONF',
        min_level    => 'PUB',
        def_level    => 'CONF',
        row_level    => 'PUB'
    );
    
    SA_USER_ADMIN.SET_COMPARTMENTS(
        policy_name  => 'OLS_DRL_POLICY',
        user_name    => 'ROLE_ADMIN',
        read_comps   => 'FB,EV,AU',
        write_comps  => 'FB,EV,AU',
        def_comps    => NULL,
        row_comps    => NULL
    );
    
    SA_USER_ADMIN.SET_GROUPS(
        policy_name  => 'OLS_DRL_POLICY',
        user_name    => 'ROLE_ADMIN',
        read_groups  => 'UNI',
        write_groups => 'UNI',
        def_groups   => 'UNI',
        row_groups   => NULL
    );
    
    DBMS_OUTPUT.PUT_LINE('✓ Set labels for ROLE_ADMIN');
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Note: ROLE_ADMIN - ' || SQLERRM);
END;
/

-- =========================================================
-- VERIFICATION
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'VERIFICATION - User Labels';
PROMPT '========================================';

PROMPT '';
PROMPT 'User Levels:';
SELECT USER_NAME, MAX_LEVEL, MIN_LEVEL, DEF_LEVEL, ROW_LEVEL
FROM DBA_SA_USER_LEVELS
WHERE POLICY_NAME = 'OLS_DRL_POLICY';

PROMPT '';
PROMPT 'User Compartments:';
SELECT USER_NAME, COMP_NAME
FROM DBA_SA_USER_COMPARTMENTS
WHERE POLICY_NAME = 'OLS_DRL_POLICY';

PROMPT '';
PROMPT 'User Groups:';
SELECT USER_NAME, GROUP_NAME
FROM DBA_SA_USER_GROUPS
WHERE POLICY_NAME = 'OLS_DRL_POLICY';

PROMPT '';
PROMPT '========================================';
PROMPT '✓ PART C COMPLETED!';
PROMPT 'Next: Run Part D for testing';
PROMPT '========================================';
