-- ========================================
-- QUẢN LÝ PHÂN CÔNG CVHT (CỐ VẤN HỌC TẬP)
-- Script: 002_Class_Advisor_Management.sql
-- Chạy với: QLDiemRenLuyen
-- ========================================

SET SERVEROUTPUT ON;

BEGIN
    DBMS_OUTPUT.PUT_LINE('========================================');
    DBMS_OUTPUT.PUT_LINE('CLASS ADVISOR MANAGEMENT - Database Setup');
    DBMS_OUTPUT.PUT_LINE('Executing as: ' || USER);
    DBMS_OUTPUT.PUT_LINE('========================================');
    DBMS_OUTPUT.PUT_LINE('');
END;
/

-- ========================================
-- 1. CREATE STORED PROCEDURE: SP_ASSIGN_CLASS_ADVISOR
-- ========================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('Creating SP_ASSIGN_CLASS_ADVISOR procedure...');
END;
/

CREATE OR REPLACE PROCEDURE SP_ASSIGN_CLASS_ADVISOR(
    p_class_id IN VARCHAR2,
    p_lecturer_id IN VARCHAR2,
    p_assigned_by IN VARCHAR2,
    p_notes IN VARCHAR2 DEFAULT NULL,
    p_result OUT VARCHAR2
) AS
    v_existing_lecturer VARCHAR2(50);
    v_class_name VARCHAR2(100);
    v_lecturer_name VARCHAR2(255);
    v_existing_name VARCHAR2(255);
BEGIN
    -- Validate inputs
    IF p_class_id IS NULL OR p_lecturer_id IS NULL OR p_assigned_by IS NULL THEN
        p_result := 'ERROR: Missing required parameters';
        RETURN;
    END IF;
    
    -- Get class name
    BEGIN
        SELECT NAME INTO v_class_name FROM CLASSES WHERE ID = p_class_id;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            p_result := 'ERROR: Class not found';
            RETURN;
    END;
    
    -- Get lecturer name and validate role
    BEGIN
        SELECT FULL_NAME INTO v_lecturer_name 
        FROM USERS 
        WHERE MAND = p_lecturer_id AND ROLE_NAME = 'LECTURER' AND IS_ACTIVE = 1;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            p_result := 'ERROR: Lecturer not found or inactive';
            RETURN;
    END;
    
    -- Check if class already has CVHT
    BEGIN
        SELECT cl.LECTURER_ID, u.FULL_NAME 
        INTO v_existing_lecturer, v_existing_name
        FROM CLASS_LECTURER_ASSIGNMENTS cl
        JOIN USERS u ON cl.LECTURER_ID = u.MAND
        WHERE cl.CLASS_ID = p_class_id AND cl.IS_ACTIVE = 1;
        
        -- Same lecturer - no change needed
        IF v_existing_lecturer = p_lecturer_id THEN
            p_result := 'INFO: ' || v_lecturer_name || ' is already CVHT for this class';
            RETURN;
        END IF;
        
        -- Remove old assignment (set IS_ACTIVE = 0)
        UPDATE CLASS_LECTURER_ASSIGNMENTS
        SET IS_ACTIVE = 0,
            REMOVED_AT = SYSTIMESTAMP,
            REMOVED_BY = p_assigned_by
        WHERE CLASS_ID = p_class_id AND IS_ACTIVE = 1;
        
        DBMS_OUTPUT.PUT_LINE('Changed CVHT from ' || v_existing_name || ' to ' || v_lecturer_name);
        
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            -- No existing CVHT
            NULL;
    END;
    
    -- Create new assignment (for both new and replacement)
    INSERT INTO CLASS_LECTURER_ASSIGNMENTS(
        CLASS_ID, LECTURER_ID, ASSIGNED_BY, NOTES, IS_ACTIVE
    ) VALUES (
        p_class_id, p_lecturer_id, p_assigned_by, p_notes, 1
    );
    
    DBMS_OUTPUT.PUT_LINE('Assigned ' || v_lecturer_name || ' as CVHT');
    
    -- Audit trail
    INSERT INTO AUDIT_TRAIL(WHO, ACTION, EVENT_AT_UTC, CLIENT_IP)
    VALUES (
        p_assigned_by,
        'ASSIGN_CVHT|CLASS=' || p_class_id || '|LECTURER=' || p_lecturer_id || '|CLASS_NAME=' || v_class_name,
        SYSTIMESTAMP,
        SYS_CONTEXT('USERENV', 'IP_ADDRESS')
    );
    
    COMMIT;
    p_result := 'SUCCESS|Assigned ' || v_lecturer_name || ' as CVHT for ' || v_class_name;
    
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        p_result := 'ERROR: ' || SQLERRM;
        DBMS_OUTPUT.PUT_LINE('Error: ' || SQLERRM);
END SP_ASSIGN_CLASS_ADVISOR;
/

BEGIN
    DBMS_OUTPUT.PUT_LINE('✓ Created SP_ASSIGN_CLASS_ADVISOR procedure');
    DBMS_OUTPUT.PUT_LINE('');
END;
/

-- ========================================
-- 3. CREATE STORED PROCEDURE: SP_REMOVE_CLASS_ADVISOR
-- ========================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('Creating SP_REMOVE_CLASS_ADVISOR procedure...');
END;
/

CREATE OR REPLACE PROCEDURE SP_REMOVE_CLASS_ADVISOR(
    p_class_id IN VARCHAR2,
    p_removed_by IN VARCHAR2,
    p_notes IN VARCHAR2 DEFAULT NULL,
    p_result OUT VARCHAR2
) AS
    v_lecturer_id VARCHAR2(50);
    v_lecturer_name VARCHAR2(255);
    v_class_name VARCHAR2(100);
BEGIN
    -- Validate inputs
    IF p_class_id IS NULL OR p_removed_by IS NULL THEN
        p_result := 'ERROR: Missing required parameters';
        RETURN;
    END IF;
    
    -- Get class name
    BEGIN
        SELECT NAME INTO v_class_name FROM CLASSES WHERE ID = p_class_id;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            p_result := 'ERROR: Class not found';
            RETURN;
    END;
    
    -- Check for active assignment
    BEGIN
        SELECT LECTURER_ID INTO v_lecturer_id
        FROM CLASS_LECTURER_ASSIGNMENTS
        WHERE CLASS_ID = p_class_id AND IS_ACTIVE = 1;
        
        SELECT FULL_NAME INTO v_lecturer_name
        FROM USERS WHERE MAND = v_lecturer_id;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            p_result := 'ERROR: No active CVHT assignment for this class';
            RETURN;
    END;
    
    -- Remove assignment (set IS_ACTIVE = 0)
    UPDATE CLASS_LECTURER_ASSIGNMENTS
    SET IS_ACTIVE = 0,
        REMOVED_AT = SYSTIMESTAMP,
        REMOVED_BY = p_removed_by,
        NOTES = CASE WHEN p_notes IS NOT NULL THEN p_notes ELSE NOTES END
    WHERE CLASS_ID = p_class_id AND IS_ACTIVE = 1;
    
    -- Audit trail
    INSERT INTO AUDIT_TRAIL(WHO, ACTION, EVENT_AT_UTC, CLIENT_IP)
    VALUES (
        p_removed_by,
        'REMOVE_CVHT|CLASS=' || p_class_id || '|LECTURER=' || v_lecturer_id || '|CLASS_NAME=' || v_class_name,
        SYSTIMESTAMP,
        SYS_CONTEXT('USERENV', 'IP_ADDRESS')
    );
    
    COMMIT;
    p_result := 'SUCCESS|Removed ' || v_lecturer_name || ' as CVHT from ' || v_class_name;
    
    DBMS_OUTPUT.PUT_LINE('Removed ' || v_lecturer_name || ' from ' || v_class_name);
    
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        p_result := 'ERROR: ' || SQLERRM;
        DBMS_OUTPUT.PUT_LINE('Error: ' || SQLERRM);
END SP_REMOVE_CLASS_ADVISOR;
/

BEGIN
    DBMS_OUTPUT.PUT_LINE('✓ Created SP_REMOVE_CLASS_ADVISOR procedure');
    DBMS_OUTPUT.PUT_LINE('');
END;
/

-- ========================================
-- 4. CREATE HELPER VIEW: V_CLASS_ADVISORS
-- ========================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('Creating V_CLASS_ADVISORS view...');
END;
/

CREATE OR REPLACE VIEW V_CLASS_ADVISORS AS
SELECT 
    c.ID as CLASS_ID,
    c.CODE as CLASS_CODE,
    c.NAME as CLASS_NAME,
    c.DEPARTMENT_ID,
    d.NAME as DEPARTMENT_NAME,
    cl.ID as ASSIGNMENT_ID,
    cl.LECTURER_ID,
    u.FULL_NAME as CVHT_NAME,
    u.EMAIL as CVHT_EMAIL,
    (SELECT COUNT(*) FROM STUDENTS WHERE CLASS_ID = c.ID) as STUDENT_COUNT,
    cl.ASSIGNED_AT as ASSIGNED_DATE,
    cl.ASSIGNED_BY
FROM CLASSES c
LEFT JOIN CLASS_LECTURER_ASSIGNMENTS cl ON c.ID = cl.CLASS_ID AND cl.IS_ACTIVE = 1
LEFT JOIN USERS u ON cl.LECTURER_ID = u.MAND
LEFT JOIN DEPARTMENTS d ON c.DEPARTMENT_ID = d.ID;

BEGIN
    DBMS_OUTPUT.PUT_LINE('✓ Created V_CLASS_ADVISORS view');
    DBMS_OUTPUT.PUT_LINE('');
END;
/

-- ========================================
-- 5. GRANT PERMISSIONS TO ROLES
-- ========================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('Granting permissions to roles...');
END;
/

-- Grant to ROLE_ADMIN (full access)
GRANT SELECT, INSERT, UPDATE, DELETE ON CLASS_LECTURER_ASSIGNMENTS TO ROLE_ADMIN;
GRANT EXECUTE ON SP_ASSIGN_CLASS_ADVISOR TO ROLE_ADMIN;
GRANT EXECUTE ON SP_REMOVE_CLASS_ADVISOR TO ROLE_ADMIN;
GRANT SELECT ON V_CLASS_ADVISORS TO ROLE_ADMIN;

-- Grant to ROLE_LECTURER (read-only for viewing their assignments)
GRANT SELECT ON CLASS_LECTURER_ASSIGNMENTS TO ROLE_LECTURER;
GRANT SELECT ON V_CLASS_ADVISORS TO ROLE_LECTURER;

-- Grant to ROLE_STUDENT (read-only for viewing class advisor info)
GRANT SELECT ON V_CLASS_ADVISORS TO ROLE_STUDENT;

BEGIN
    DBMS_OUTPUT.PUT_LINE('✓ Granted permissions to ROLE_ADMIN, ROLE_LECTURER, ROLE_STUDENT');
    DBMS_OUTPUT.PUT_LINE('');
END;
/

-- ========================================
-- 6. VERIFICATION & SUMMARY
-- ========================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('========================================');
    DBMS_OUTPUT.PUT_LINE('CLASS ADVISOR MANAGEMENT - SUMMARY');
    DBMS_OUTPUT.PUT_LINE('========================================');
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('Objects Created:');
END;
/

SELECT OBJECT_NAME, OBJECT_TYPE, STATUS
FROM USER_OBJECTS
WHERE OBJECT_NAME IN (
    'CLASS_LECTURER_HISTORY',
    'SP_ASSIGN_CLASS_ADVISOR',
    'SP_REMOVE_CLASS_ADVISOR',
    'V_CLASS_ADVISORS'
)
ORDER BY OBJECT_TYPE, OBJECT_NAME;

BEGIN
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('Permissions Granted:');
END;
/

SELECT GRANTEE, TABLE_NAME, COUNT(*) as PRIVILEGE_COUNT
FROM USER_TAB_PRIVS_MADE
WHERE TABLE_NAME IN (
    'CLASS_LECTURER_HISTORY',
    'SP_ASSIGN_CLASS_ADVISOR',
    'SP_REMOVE_CLASS_ADVISOR',
    'V_CLASS_ADVISORS'
)
GROUP BY GRANTEE, TABLE_NAME
ORDER BY GRANTEE, TABLE_NAME;

BEGIN
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('✓ CLASS ADVISOR MANAGEMENT DATABASE SETUP COMPLETED!');
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('Features Enabled:');
    DBMS_OUTPUT.PUT_LINE('- CVHT assignment and removal');
    DBMS_OUTPUT.PUT_LINE('- Complete assignment history tracking');
    DBMS_OUTPUT.PUT_LINE('- Audit trail integration');
    DBMS_OUTPUT.PUT_LINE('- Helper view for reporting');
    DBMS_OUTPUT.PUT_LINE('========================================');
END;
/
