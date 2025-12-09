-- =========================================================
-- DAC GIAI ĐOẠN 2: CHIA SẺ ĐIỂM BỞI CVHT
-- =========================================================
-- Kết nối: QLDiemRenLuyen
-- Mục đích: Cho phép CVHT chia sẻ quyền xem điểm tạm thời
-- =========================================================

SET SERVEROUTPUT ON;

PROMPT '========================================';
PROMPT 'DAC GIAI ĐOẠN 2 - Chia sẻ Điểm';
PROMPT 'Đang thực thi với: QLDiemRenLuyen';
PROMPT '========================================';

-- =========================================================
-- BƯỚC 1: TẠO VIEW QUYỀN ĐANG HOẠT ĐỘNG
-- =========================================================

CREATE OR REPLACE VIEW V_ACTIVE_SCORE_PERMISSIONS AS
SELECT 
    csp.*,
    c.NAME as CLASS_NAME,
    c.CODE as CLASS_CODE,
    u_by.FULL_NAME as GRANTED_BY_NAME,
    u_to.FULL_NAME as GRANTED_TO_NAME,
    u_to.ROLE_NAME as GRANTEE_ROLE
FROM CLASS_SCORE_PERMISSIONS csp
JOIN CLASSES c ON csp.CLASS_ID = c.ID
JOIN USERS u_by ON csp.GRANTED_BY = u_by.MAND
JOIN USERS u_to ON csp.GRANTED_TO = u_to.MAND
WHERE csp.IS_ACTIVE = 1
AND csp.REVOKED_AT IS NULL
AND (csp.EXPIRES_AT IS NULL OR csp.EXPIRES_AT > SYSTIMESTAMP);

PROMPT '✓ Created V_ACTIVE_SCORE_PERMISSIONS view';

-- =========================================================
-- STEP 3: CREATE GRANT PERMISSION PROCEDURE
-- =========================================================

CREATE OR REPLACE PROCEDURE SP_GRANT_SCORE_PERMISSION(
    p_class_id IN VARCHAR2,
    p_granted_by IN VARCHAR2,
    p_granted_to IN VARCHAR2,
    p_permission_type IN VARCHAR2,
    p_expires_at IN TIMESTAMP,
    p_notes IN VARCHAR2,
    p_result OUT VARCHAR2,
    p_permission_id OUT VARCHAR2
) AS
    v_is_cvht NUMBER;
    v_permission_id VARCHAR2(32);
    v_existing_count NUMBER;
BEGIN
    -- Verify grantor is CVHT of the class
    SELECT COUNT(*) INTO v_is_cvht
    FROM CLASS_LECTURER_ASSIGNMENTS
    WHERE CLASS_ID = p_class_id AND LECTURER_ID = p_granted_by AND IS_ACTIVE = 1;
    
    IF v_is_cvht = 0 THEN
        p_result := 'ERROR: Only class advisor (CVHT) can grant permissions for this class';
        RETURN;
    END IF;
    
    -- Prevent self-granting
    IF p_granted_by = p_granted_to THEN
        p_result := 'ERROR: Cannot grant permission to yourself';
        RETURN;
    END IF;
    
    -- Check for existing active permission
    SELECT COUNT(*) INTO v_existing_count
    FROM CLASS_SCORE_PERMISSIONS
    WHERE CLASS_ID = p_class_id
    AND GRANTED_TO = p_granted_to
    AND PERMISSION_TYPE = p_permission_type
    AND IS_ACTIVE = 1
    AND REVOKED_AT IS NULL
    AND (EXPIRES_AT IS NULL OR EXPIRES_AT > SYSTIMESTAMP);
    
    IF v_existing_count > 0 THEN
        p_result := 'ERROR: Active permission already exists for this user and permission type';
        RETURN;
    END IF;
    
    -- Create new permission
    v_permission_id := RAWTOHEX(SYS_GUID());
    
    INSERT INTO CLASS_SCORE_PERMISSIONS(
        ID, CLASS_ID, GRANTED_BY, GRANTED_TO, 
        PERMISSION_TYPE, EXPIRES_AT, NOTES
    ) VALUES (
        v_permission_id, p_class_id, p_granted_by, p_granted_to,
        p_permission_type, p_expires_at, p_notes
    );
    
    -- Log to AUDIT_EVENTS
    INSERT INTO AUDIT_EVENTS(EVENT_CATEGORY, EVENT_TYPE, PERFORMED_BY, ENTITY_TYPE, ENTITY_ID, DESCRIPTION, CLIENT_IP)
    VALUES (
        'SECURITY',
        'DAC_GRANT',
        p_granted_by,
        'CLASS_SCORE_PERMISSION',
        v_permission_id,
        'PERMISSION_ID=' || v_permission_id || '|CLASS=' || p_class_id || '|TO=' || p_granted_to || '|TYPE=' || p_permission_type,
        SYS_CONTEXT('USERENV', 'IP_ADDRESS')
    );
    
    COMMIT;
    
    p_result := 'SUCCESS';
    p_permission_id := v_permission_id;
    
    DBMS_OUTPUT.PUT_LINE('Permission granted: ' || v_permission_id);
    
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        p_result := 'ERROR: ' || SQLERRM;
END;
/

PROMPT '✓ Created SP_GRANT_SCORE_PERMISSION procedure';

-- =========================================================
-- STEP 4: CREATE REVOKE PERMISSION PROCEDURE
-- =========================================================

CREATE OR REPLACE PROCEDURE SP_REVOKE_SCORE_PERMISSION(
    p_permission_id IN VARCHAR2,
    p_revoked_by IN VARCHAR2,
    p_result OUT VARCHAR2
) AS
    v_granted_by VARCHAR2(50);
    v_class_id VARCHAR2(32);
    v_granted_to VARCHAR2(50);
    v_permission_type VARCHAR2(20);
BEGIN
    -- Get permission details
    SELECT GRANTED_BY, CLASS_ID, GRANTED_TO, PERMISSION_TYPE
    INTO v_granted_by, v_class_id, v_granted_to, v_permission_type
    FROM CLASS_SCORE_PERMISSIONS
    WHERE ID = p_permission_id;
    
    -- Only grantor can revoke
    IF v_granted_by != p_revoked_by THEN
        p_result := 'ERROR: Only the original grantor can revoke this permission';
        RETURN;
    END IF;
    
    -- Check if already revoked
    DECLARE
        v_already_revoked TIMESTAMP;
    BEGIN
        SELECT REVOKED_AT INTO v_already_revoked
        FROM CLASS_SCORE_PERMISSIONS
        WHERE ID = p_permission_id;
        
        IF v_already_revoked IS NOT NULL THEN
            p_result := 'ERROR: Permission already revoked';
            RETURN;
        END IF;
    END;
    
    -- Revoke permission
    UPDATE CLASS_SCORE_PERMISSIONS
    SET REVOKED_AT = SYSTIMESTAMP,
        REVOKED_BY = p_revoked_by,
        IS_ACTIVE = 0
    WHERE ID = p_permission_id;
    
    -- Log to AUDIT_EVENTS
    INSERT INTO AUDIT_EVENTS(EVENT_CATEGORY, EVENT_TYPE, PERFORMED_BY, ENTITY_TYPE, ENTITY_ID, DESCRIPTION)
    VALUES (
        'SECURITY',
        'DAC_REVOKE',
        p_revoked_by,
        'CLASS_SCORE_PERMISSION',
        p_permission_id,
        'CLASS=' || v_class_id || '|FROM=' || v_granted_to || '|TYPE=' || v_permission_type
    );
    
    COMMIT;
    
    p_result := 'SUCCESS';
    DBMS_OUTPUT.PUT_LINE('Permission revoked: ' || p_permission_id);
    
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        p_result := 'ERROR: Permission not found';
    WHEN OTHERS THEN
        ROLLBACK;
        p_result := 'ERROR: ' || SQLERRM;
END;
/

PROMPT '✓ Created SP_REVOKE_SCORE_PERMISSION procedure';

-- =========================================================
-- STEP 5: CREATE ACCESS CHECK FUNCTION
-- =========================================================

CREATE OR REPLACE FUNCTION FN_CHECK_SCORE_ACCESS(
    p_user_id IN VARCHAR2,
    p_score_id IN NUMBER,
    p_access_type IN VARCHAR2
) RETURN NUMBER AS
    v_class_id VARCHAR2(32);
    v_student_id VARCHAR2(50);
    v_has_direct NUMBER := 0;
    v_has_granted NUMBER := 0;
    v_permission_id VARCHAR2(32);
    v_log_action VARCHAR2(500);
BEGIN
    -- Get class and student from score
    SELECT s.CLASS_ID, sc.STUDENT_ID
    INTO v_class_id, v_student_id
    FROM SCORES sc
    JOIN STUDENTS s ON sc.STUDENT_ID = s.USER_ID
    WHERE sc.ID = p_score_id;
    
    -- Check 1: Is user CVHT of the class? (always has access)
    SELECT COUNT(*) INTO v_has_direct
    FROM CLASS_LECTURER_ASSIGNMENTS
    WHERE CLASS_ID = v_class_id AND LECTURER_ID = p_user_id AND IS_ACTIVE = 1;
    
    IF v_has_direct > 0 THEN
        v_log_action := 'SCORE_ACCESS|CVHT|' || p_access_type || '|SCORE=' || p_score_id || '|SUCCESS';
        
        INSERT INTO AUDIT_EVENTS(EVENT_CATEGORY, EVENT_TYPE, PERFORMED_BY, ENTITY_TYPE, ENTITY_ID, DESCRIPTION)
        VALUES ('BUSINESS', 'SCORE_ACCESS', p_user_id, 'SCORE', TO_CHAR(p_score_id), v_log_action);
        COMMIT;
        
        RETURN 1;
    END IF;
    
    -- Check 2: Does user have granted permission?
    BEGIN
        SELECT COUNT(*), MAX(ID) INTO v_has_granted, v_permission_id
        FROM CLASS_SCORE_PERMISSIONS
        WHERE CLASS_ID = v_class_id
        AND GRANTED_TO = p_user_id
        AND (
            PERMISSION_TYPE = p_access_type OR
            (PERMISSION_TYPE = 'APPROVE' AND p_access_type IN ('EDIT', 'VIEW')) OR
            (PERMISSION_TYPE = 'EDIT' AND p_access_type = 'VIEW')
        )
        AND IS_ACTIVE = 1
        AND REVOKED_AT IS NULL
        AND (EXPIRES_AT IS NULL OR EXPIRES_AT > SYSTIMESTAMP);
        
        IF v_has_granted > 0 THEN
            v_log_action := 'SCORE_ACCESS|GRANTED|' || p_access_type || 
                          '|SCORE=' || p_score_id || '|PERM=' || v_permission_id || '|SUCCESS';
        ELSE
            v_log_action := 'SCORE_ACCESS|DENIED|' || p_access_type || 
                          '|SCORE=' || p_score_id || '|NO_PERMISSION';
        END IF;
        
        -- Log access attempt
        INSERT INTO AUDIT_EVENTS(EVENT_CATEGORY, EVENT_TYPE, PERFORMED_BY, ENTITY_TYPE, ENTITY_ID, DESCRIPTION)
        VALUES ('BUSINESS', 'SCORE_ACCESS', p_user_id, 'SCORE', TO_CHAR(p_score_id), v_log_action);
        COMMIT;
        
        RETURN v_has_granted;
        
    EXCEPTION
        WHEN OTHERS THEN
            RETURN 0;
    END;
    
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RETURN 0;
    WHEN OTHERS THEN
        RETURN 0;
END;
/

PROMPT '✓ Created FN_CHECK_SCORE_ACCESS function';

-- =========================================================
-- STEP 6: GRANT PERMISSIONS TO ROLES
-- =========================================================

-- ROLE_LECTURER can use permission management
GRANT SELECT, INSERT, UPDATE ON CLASS_SCORE_PERMISSIONS TO ROLE_LECTURER;
GRANT SELECT ON V_ACTIVE_SCORE_PERMISSIONS TO ROLE_LECTURER;
GRANT EXECUTE ON SP_GRANT_SCORE_PERMISSION TO ROLE_LECTURER;
GRANT EXECUTE ON SP_REVOKE_SCORE_PERMISSION TO ROLE_LECTURER;
GRANT EXECUTE ON FN_CHECK_SCORE_ACCESS TO ROLE_LECTURER;

-- ROLE_ADMIN has full access
GRANT ALL ON CLASS_SCORE_PERMISSIONS TO ROLE_ADMIN;
GRANT SELECT ON V_ACTIVE_SCORE_PERMISSIONS TO ROLE_ADMIN;
GRANT EXECUTE ON SP_GRANT_SCORE_PERMISSION TO ROLE_ADMIN;
GRANT EXECUTE ON SP_REVOKE_SCORE_PERMISSION TO ROLE_ADMIN;
GRANT EXECUTE ON FN_CHECK_SCORE_ACCESS TO ROLE_ADMIN;

PROMPT '✓ Granted permissions to roles';

-- =========================================================
-- VERIFICATION
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'DAC PHASE 2 IMPLEMENTATION SUMMARY';
PROMPT '========================================';

PROMPT '';
PROMPT 'Objects Created:';
SELECT OBJECT_NAME, OBJECT_TYPE, STATUS
FROM USER_OBJECTS
WHERE OBJECT_NAME IN (
    'CLASS_SCORE_PERMISSIONS',
    'V_ACTIVE_SCORE_PERMISSIONS',
    'SP_GRANT_SCORE_PERMISSION',
    'SP_REVOKE_SCORE_PERMISSION',
    'FN_CHECK_SCORE_ACCESS'
)
ORDER BY OBJECT_TYPE, OBJECT_NAME;

PROMPT '';
PROMPT 'Permissions Granted:';
SELECT GRANTEE, TABLE_NAME, COUNT(*) as PRIVILEGE_COUNT
FROM USER_TAB_PRIVS_MADE
WHERE TABLE_NAME IN ('CLASS_SCORE_PERMISSIONS', 'V_ACTIVE_SCORE_PERMISSIONS')
GROUP BY GRANTEE, TABLE_NAME
ORDER BY GRANTEE, TABLE_NAME;

PROMPT '';
PROMPT '✓ DAC PHASE 2 DATABASE MIGRATION COMPLETED!';
PROMPT 'New Tables: 1 (CLASS_SCORE_PERMISSIONS)';
PROMPT 'New Views: 1 (V_ACTIVE_SCORE_PERMISSIONS)';
PROMPT 'New Procedures: 2 (GRANT, REVOKE)';
PROMPT 'New Functions: 1 (CHECK_ACCESS)';
PROMPT 'Access Logging: Integrated with AUDIT_TRAIL';
PROMPT '========================================';
