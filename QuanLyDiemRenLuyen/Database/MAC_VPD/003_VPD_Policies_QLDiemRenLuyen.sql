-- =========================================================
-- MAC + VPD IMPLEMENTATION - PART C (Run as QLDiemRenLuyen)
-- =========================================================
-- Connection: QLDiemRenLuyen (schema owner)
-- Purpose: Register VPD Policies using DBMS_RLS
-- Prerequisite: Run 001 and 002 scripts first!
-- =========================================================
--
-- VPD POLICIES:
-- 1. SCORES_SELECT_POLICY - Controls who can view scores
-- 2. SCORES_UPDATE_POLICY - Controls who can update/approve scores
--
-- =========================================================

SET SERVEROUTPUT ON;

PROMPT '========================================';
PROMPT 'MAC + VPD PART C - Registering VPD Policies';
PROMPT 'Executing as: QLDiemRenLuyen';
PROMPT '========================================';

-- =========================================================
-- STEP 1: DROP EXISTING POLICIES (if re-running)
-- =========================================================

DECLARE
    policy_exists NUMBER;
BEGIN
    -- Check and drop SELECT policy
    SELECT COUNT(*) INTO policy_exists
    FROM USER_POLICIES
    WHERE OBJECT_NAME = 'SCORES' AND POLICY_NAME = 'SCORES_SELECT_POLICY';
    
    IF policy_exists > 0 THEN
        DBMS_RLS.DROP_POLICY(
            object_schema => USER,
            object_name   => 'SCORES',
            policy_name   => 'SCORES_SELECT_POLICY'
        );
        DBMS_OUTPUT.PUT_LINE('✓ Dropped existing SCORES_SELECT_POLICY');
    END IF;
    
    -- Check and drop UPDATE policy
    SELECT COUNT(*) INTO policy_exists
    FROM USER_POLICIES
    WHERE OBJECT_NAME = 'SCORES' AND POLICY_NAME = 'SCORES_UPDATE_POLICY';
    
    IF policy_exists > 0 THEN
        DBMS_RLS.DROP_POLICY(
            object_schema => USER,
            object_name   => 'SCORES',
            policy_name   => 'SCORES_UPDATE_POLICY'
        );
        DBMS_OUTPUT.PUT_LINE('✓ Dropped existing SCORES_UPDATE_POLICY');
    END IF;
    
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Note: ' || SQLERRM);
END;
/

-- =========================================================
-- STEP 2: CREATE SELECT POLICY FOR SCORES TABLE
-- =========================================================
-- Policy Name: SCORES_SELECT_POLICY
-- Purpose: Control row-level access for SELECT queries
-- 
-- Behavior:
-- - STUDENT: Sees only their own scores
-- - LECTURER: Sees scores of students in their assigned classes
-- - ADMIN: Sees all scores
-- =========================================================

BEGIN
    DBMS_RLS.ADD_POLICY(
        object_schema   => USER,                        -- Schema owner (QLDiemRenLuyen)
        object_name     => 'SCORES',                    -- Table name
        policy_name     => 'SCORES_SELECT_POLICY',      -- Policy name
        function_schema => USER,                        -- Schema of policy function
        policy_function => 'PKG_VPD_CONTEXT.FN_SCORES_SELECT_POLICY',  -- Policy function
        statement_types => 'SELECT',                    -- Apply to SELECT only
        policy_type     => DBMS_RLS.CONTEXT_SENSITIVE,  -- Re-evaluate when context changes
        update_check    => FALSE,                       -- No check on UPDATE
        enable          => TRUE,                        -- Enable immediately
        static_policy   => FALSE,                       -- Dynamic policy
        long_predicate  => TRUE                         -- Allow predicates > 4000 chars
    );
    
    DBMS_OUTPUT.PUT_LINE('✓ Created SCORES_SELECT_POLICY');
END;
/

-- =========================================================
-- STEP 3: CREATE UPDATE POLICY FOR SCORES TABLE
-- =========================================================
-- Policy Name: SCORES_UPDATE_POLICY
-- Purpose: Control row-level access for UPDATE queries (duyệt điểm)
--
-- Behavior:
-- - STUDENT: Cannot update any scores
-- - LECTURER: Can update scores of students in their assigned classes
-- - ADMIN: Can update all scores
-- =========================================================

BEGIN
    DBMS_RLS.ADD_POLICY(
        object_schema   => USER,
        object_name     => 'SCORES',
        policy_name     => 'SCORES_UPDATE_POLICY',
        function_schema => USER,
        policy_function => 'PKG_VPD_CONTEXT.FN_SCORES_UPDATE_POLICY',
        statement_types => 'UPDATE',                    -- Apply to UPDATE only
        policy_type     => DBMS_RLS.CONTEXT_SENSITIVE,
        update_check    => TRUE,                        -- Verify after UPDATE
        enable          => TRUE,
        static_policy   => FALSE,
        long_predicate  => TRUE
    );
    
    DBMS_OUTPUT.PUT_LINE('✓ Created SCORES_UPDATE_POLICY');
END;
/

-- =========================================================
-- STEP 4: CREATE HELPER PROCEDURES FOR APPLICATION
-- =========================================================

-- Procedure để duyệt điểm (chỉ Admin/Lecturer)
CREATE OR REPLACE PROCEDURE SP_APPROVE_SCORE(
    p_score_id    IN NUMBER,
    p_approved_by IN VARCHAR2,
    p_result      OUT VARCHAR2
) AS
    v_role        VARCHAR2(20);
    v_student_id  VARCHAR2(50);
    v_class_id    VARCHAR2(32);
    v_is_cvht     NUMBER := 0;
BEGIN
    -- Lấy role của người duyệt
    SELECT ROLE_NAME INTO v_role
    FROM USERS
    WHERE MAND = p_approved_by;
    
    -- Lấy thông tin điểm
    SELECT sc.STUDENT_ID, s.CLASS_ID
    INTO v_student_id, v_class_id
    FROM SCORES sc
    JOIN STUDENTS s ON sc.STUDENT_ID = s.USER_ID
    WHERE sc.ID = p_score_id;
    
    -- Kiểm tra quyền duyệt
    IF v_role = 'ADMIN' THEN
        -- Admin được duyệt tất cả
        NULL;
    ELSIF v_role = 'LECTURER' THEN
        -- Lecturer chỉ được duyệt điểm lớp mình phụ trách
        SELECT COUNT(*) INTO v_is_cvht
        FROM CLASS_LECTURER_ASSIGNMENTS
        WHERE CLASS_ID = v_class_id
        AND LECTURER_ID = p_approved_by
        AND IS_ACTIVE = 1;
        
        IF v_is_cvht = 0 THEN
            p_result := 'ERROR: Bạn không phải CVHT của lớp này';
            RETURN;
        END IF;
    ELSE
        p_result := 'ERROR: Bạn không có quyền duyệt điểm';
        RETURN;
    END IF;
    
    -- Cập nhật trạng thái điểm
    UPDATE SCORES
    SET STATUS = 'APPROVED',
        APPROVED_BY = p_approved_by,
        APPROVED_AT = SYSTIMESTAMP
    WHERE ID = p_score_id
    AND STATUS = 'PROVISIONAL';
    
    IF SQL%ROWCOUNT = 0 THEN
        p_result := 'ERROR: Điểm không tồn tại hoặc đã được duyệt';
        RETURN;
    END IF;
    
    -- Log action
    INSERT INTO AUDIT_TRAIL(WHO, ACTION, EVENT_AT_UTC)
    VALUES (
        p_approved_by,
        'SCORE_APPROVED|SCORE_ID=' || p_score_id || '|STUDENT=' || v_student_id,
        SYSTIMESTAMP
    );
    
    COMMIT;
    p_result := 'SUCCESS';
    
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        p_result := 'ERROR: Không tìm thấy dữ liệu';
        ROLLBACK;
    WHEN OTHERS THEN
        p_result := 'ERROR: ' || SQLERRM;
        ROLLBACK;
END;
/

PROMPT '✓ Created SP_APPROVE_SCORE procedure';

-- Procedure để duyệt điểm hàng loạt theo lớp (chỉ Admin/CVHT)
CREATE OR REPLACE PROCEDURE SP_APPROVE_SCORES_BY_CLASS(
    p_class_id    IN VARCHAR2,
    p_term_id     IN VARCHAR2,
    p_approved_by IN VARCHAR2,
    p_result      OUT VARCHAR2,
    p_count       OUT NUMBER
) AS
    v_role    VARCHAR2(20);
    v_is_cvht NUMBER := 0;
BEGIN
    p_count := 0;
    
    -- Lấy role của người duyệt
    SELECT ROLE_NAME INTO v_role
    FROM USERS
    WHERE MAND = p_approved_by;
    
    -- Kiểm tra quyền
    IF v_role = 'ADMIN' THEN
        NULL; -- Admin được duyệt tất cả
    ELSIF v_role = 'LECTURER' THEN
        SELECT COUNT(*) INTO v_is_cvht
        FROM CLASS_LECTURER_ASSIGNMENTS
        WHERE CLASS_ID = p_class_id
        AND LECTURER_ID = p_approved_by
        AND IS_ACTIVE = 1;
        
        IF v_is_cvht = 0 THEN
            p_result := 'ERROR: Bạn không phải CVHT của lớp này';
            RETURN;
        END IF;
    ELSE
        p_result := 'ERROR: Bạn không có quyền duyệt điểm';
        RETURN;
    END IF;
    
    -- Duyệt tất cả điểm của lớp trong học kỳ
    UPDATE SCORES
    SET STATUS = 'APPROVED',
        APPROVED_BY = p_approved_by,
        APPROVED_AT = SYSTIMESTAMP
    WHERE STUDENT_ID IN (
        SELECT USER_ID FROM STUDENTS WHERE CLASS_ID = p_class_id
    )
    AND TERM_ID = p_term_id
    AND STATUS = 'PROVISIONAL';
    
    p_count := SQL%ROWCOUNT;
    
    -- Log action
    INSERT INTO AUDIT_TRAIL(WHO, ACTION, EVENT_AT_UTC)
    VALUES (
        p_approved_by,
        'BULK_SCORE_APPROVED|CLASS=' || p_class_id || '|TERM=' || p_term_id || '|COUNT=' || p_count,
        SYSTIMESTAMP
    );
    
    COMMIT;
    p_result := 'SUCCESS';
    
EXCEPTION
    WHEN OTHERS THEN
        p_result := 'ERROR: ' || SQLERRM;
        ROLLBACK;
END;
/

PROMPT '✓ Created SP_APPROVE_SCORES_BY_CLASS procedure';

-- =========================================================
-- STEP 5: CREATE VIEW FOR SCORE ACCESS (uses VPD automatically)
-- =========================================================

CREATE OR REPLACE VIEW V_SCORES_WITH_INFO AS
SELECT 
    sc.ID,
    sc.STUDENT_ID,
    u.FULL_NAME AS STUDENT_NAME,
    s.STUDENT_CODE,
    c.NAME AS CLASS_NAME,
    c.CODE AS CLASS_CODE,
    d.NAME AS DEPARTMENT_NAME,
    t.NAME AS TERM_NAME,
    t.YEAR,
    t.TERM_NUMBER,
    sc.TOTAL_SCORE,
    sc.CLASSIFICATION,
    sc.STATUS,
    sc.APPROVED_BY,
    ua.FULL_NAME AS APPROVED_BY_NAME,
    sc.APPROVED_AT,
    sc.CREATED_AT
FROM SCORES sc
JOIN USERS u ON sc.STUDENT_ID = u.MAND
JOIN STUDENTS s ON sc.STUDENT_ID = s.USER_ID
LEFT JOIN CLASSES c ON s.CLASS_ID = c.ID
LEFT JOIN DEPARTMENTS d ON s.DEPARTMENT_ID = d.ID
JOIN TERMS t ON sc.TERM_ID = t.ID
LEFT JOIN USERS ua ON sc.APPROVED_BY = ua.MAND;

PROMPT '✓ Created V_SCORES_WITH_INFO view';

-- =========================================================
-- STEP 6: GRANT PERMISSIONS
-- =========================================================

-- Grant execute on context package to roles
GRANT EXECUTE ON PKG_VPD_CONTEXT TO ROLE_STUDENT;
GRANT EXECUTE ON PKG_VPD_CONTEXT TO ROLE_LECTURER;
GRANT EXECUTE ON PKG_VPD_CONTEXT TO ROLE_ADMIN;

-- Grant execute on approval procedures
GRANT EXECUTE ON SP_APPROVE_SCORE TO ROLE_LECTURER;
GRANT EXECUTE ON SP_APPROVE_SCORE TO ROLE_ADMIN;

GRANT EXECUTE ON SP_APPROVE_SCORES_BY_CLASS TO ROLE_LECTURER;
GRANT EXECUTE ON SP_APPROVE_SCORES_BY_CLASS TO ROLE_ADMIN;

-- Grant select on view
GRANT SELECT ON V_SCORES_WITH_INFO TO ROLE_STUDENT;
GRANT SELECT ON V_SCORES_WITH_INFO TO ROLE_LECTURER;
GRANT SELECT ON V_SCORES_WITH_INFO TO ROLE_ADMIN;

PROMPT '✓ Granted permissions to roles';

-- =========================================================
-- VERIFICATION
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'VERIFICATION - VPD Policies';
PROMPT '========================================';

PROMPT '';
PROMPT 'Registered Policies on SCORES table:';
SELECT OBJECT_NAME, POLICY_NAME, PF_OWNER, PACKAGE || '.' || FUNCTION AS POLICY_FUNCTION, 
       SEL, UPD, ENABLE
FROM USER_POLICIES
WHERE OBJECT_NAME = 'SCORES'
ORDER BY POLICY_NAME;

PROMPT '';
PROMPT 'Objects Created:';
SELECT OBJECT_NAME, OBJECT_TYPE, STATUS
FROM USER_OBJECTS
WHERE OBJECT_NAME IN (
    'PKG_VPD_CONTEXT',
    'SP_APPROVE_SCORE',
    'SP_APPROVE_SCORES_BY_CLASS',
    'V_SCORES_WITH_INFO'
)
ORDER BY OBJECT_TYPE, OBJECT_NAME;

PROMPT '';
PROMPT '========================================';
PROMPT 'MAC + VPD IMPLEMENTATION SUMMARY';
PROMPT '========================================';
PROMPT '';
PROMPT 'Chính sách MAC đã được áp dụng:';
PROMPT '+ STUDENT: Chỉ xem được điểm của chính mình';
PROMPT '+ LECTURER (CVHT): Xem/duyệt điểm các lớp mình phụ trách';
PROMPT '+ ADMIN: Xem/duyệt được tất cả điểm toàn hệ thống';
PROMPT '';
PROMPT 'VPD Policies đã đăng ký:';
PROMPT '+ SCORES_SELECT_POLICY - Kiểm soát quyền xem điểm';
PROMPT '+ SCORES_UPDATE_POLICY - Kiểm soát quyền duyệt điểm';
PROMPT '';
PROMPT '✓ PART C COMPLETED SUCCESSFULLY!';
PROMPT 'Next: Run Part D for testing';
PROMPT '========================================';
