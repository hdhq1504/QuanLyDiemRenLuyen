-- =========================================================
-- MAC + VPD IMPLEMENTATION - PART B (Run as QLDiemRenLuyen)
-- =========================================================
-- Connection: QLDiemRenLuyen (schema owner)
-- Purpose: Create VPD Context Package and Policy Functions
-- Prerequisite: Run 001_VPD_Context_SYSDBA.sql first!
-- =========================================================
-- 
-- NGHIỆP VỤ: Xem & Duyệt điểm rèn luyện (SCORES)
-- + STUDENT: Chỉ xem được điểm của chính mình
-- + LECTURER (CVHT): Chỉ xem được điểm các lớp mình phụ trách
-- + ADMIN: Xem/duyệt được tất cả điểm toàn hệ thống
--
-- CHÍNH SÁCH MAC:
-- + Do DBA định nghĩa và quản lý
-- + Người dùng không tự chia sẻ hay thay đổi quyền như DAC
-- + Sử dụng VPD (DBMS_RLS) để tạo chính sách row-level bảo mật
-- =========================================================

SET SERVEROUTPUT ON;

PROMPT '========================================';
PROMPT 'MAC + VPD PART B - VPD Context Package';
PROMPT 'Executing as: QLDiemRenLuyen';
PROMPT '========================================';

-- =========================================================
-- STEP 1: CREATE VPD CONTEXT PACKAGE SPECIFICATION
-- =========================================================

CREATE OR REPLACE PACKAGE PKG_VPD_CONTEXT AS
    -- =========================================================
    -- CONSTANTS
    -- =========================================================
    C_CONTEXT_NAME CONSTANT VARCHAR2(50) := 'VPD_SCORES_CTX';
    
    -- Role constants (phải khớp với USERS.ROLE_NAME)
    C_ROLE_STUDENT  CONSTANT VARCHAR2(20) := 'STUDENT';
    C_ROLE_LECTURER CONSTANT VARCHAR2(20) := 'LECTURER';
    C_ROLE_ADMIN    CONSTANT VARCHAR2(20) := 'ADMIN';
    
    -- =========================================================
    -- CONTEXT MANAGEMENT PROCEDURES
    -- =========================================================
    
    -- Thiết lập security context khi user đăng nhập
    -- Được gọi từ Application Layer sau khi xác thực thành công
    PROCEDURE SET_USER_CONTEXT(
        p_user_id   IN VARCHAR2,  -- USERS.MAND
        p_role      IN VARCHAR2,  -- USERS.ROLE_NAME
        p_client_id IN VARCHAR2 DEFAULT NULL  -- Session identifier
    );
    
    -- Xóa context khi user đăng xuất
    PROCEDURE CLEAR_USER_CONTEXT;
    
    -- =========================================================
    -- CONTEXT GETTER FUNCTIONS
    -- =========================================================
    
    -- Lấy User ID từ context
    FUNCTION GET_USER_ID RETURN VARCHAR2;
    
    -- Lấy Role từ context  
    FUNCTION GET_USER_ROLE RETURN VARCHAR2;
    
    -- Lấy Client ID từ context
    FUNCTION GET_CLIENT_ID RETURN VARCHAR2;
    
    -- =========================================================
    -- VPD POLICY FUNCTIONS
    -- =========================================================
    
    -- Policy function cho SELECT trên bảng SCORES
    -- Trả về predicate SQL để filter rows theo role
    FUNCTION FN_SCORES_SELECT_POLICY(
        p_schema IN VARCHAR2,
        p_object IN VARCHAR2
    ) RETURN VARCHAR2;
    
    -- Policy function cho UPDATE trên bảng SCORES
    -- Chỉ Admin mới được update (duyệt điểm)
    FUNCTION FN_SCORES_UPDATE_POLICY(
        p_schema IN VARCHAR2,
        p_object IN VARCHAR2
    ) RETURN VARCHAR2;
    
END PKG_VPD_CONTEXT;
/

PROMPT '✓ Created PKG_VPD_CONTEXT specification';

-- =========================================================
-- STEP 2: CREATE VPD CONTEXT PACKAGE BODY
-- =========================================================

CREATE OR REPLACE PACKAGE BODY PKG_VPD_CONTEXT AS

    -- =========================================================
    -- SET_USER_CONTEXT
    -- Thiết lập security context khi user đăng nhập
    -- =========================================================
    PROCEDURE SET_USER_CONTEXT(
        p_user_id   IN VARCHAR2,
        p_role      IN VARCHAR2,
        p_client_id IN VARCHAR2 DEFAULT NULL
    ) AS
        v_client_id VARCHAR2(100);
    BEGIN
        -- Validate input
        IF p_user_id IS NULL THEN
            RAISE_APPLICATION_ERROR(-20001, 'User ID cannot be null');
        END IF;
        
        IF p_role IS NULL THEN
            RAISE_APPLICATION_ERROR(-20002, 'Role cannot be null');
        END IF;
        
        -- Validate role
        IF p_role NOT IN (C_ROLE_STUDENT, C_ROLE_LECTURER, C_ROLE_ADMIN) THEN
            RAISE_APPLICATION_ERROR(-20003, 'Invalid role: ' || p_role);
        END IF;
        
        -- Generate client_id if not provided
        v_client_id := NVL(p_client_id, SYS_GUID());
        
        -- Set context values using DBMS_SESSION for globally accessible context
        DBMS_SESSION.SET_CONTEXT(
            namespace => C_CONTEXT_NAME,
            attribute => 'USER_ID',
            value     => p_user_id,
            client_id => v_client_id
        );
        
        DBMS_SESSION.SET_CONTEXT(
            namespace => C_CONTEXT_NAME,
            attribute => 'USER_ROLE',
            value     => p_role,
            client_id => v_client_id
        );
        
        DBMS_SESSION.SET_CONTEXT(
            namespace => C_CONTEXT_NAME,
            attribute => 'CLIENT_ID',
            value     => v_client_id,
            client_id => v_client_id
        );
        
        -- Set CLIENT_IDENTIFIER for connection pooling support
        DBMS_SESSION.SET_IDENTIFIER(v_client_id);
        
        -- Log context setting
        BEGIN
            INSERT INTO AUDIT_TRAIL(WHO, ACTION, EVENT_AT_UTC, CLIENT_IP)
            VALUES (
                p_user_id,
                'VPD_CONTEXT_SET|ROLE=' || p_role || '|CLIENT=' || v_client_id,
                SYSTIMESTAMP,
                SYS_CONTEXT('USERENV', 'IP_ADDRESS')
            );
            COMMIT;
        EXCEPTION
            WHEN OTHERS THEN
                NULL; -- Continue even if audit fails
        END;
        
    END SET_USER_CONTEXT;
    
    -- =========================================================
    -- CLEAR_USER_CONTEXT
    -- Xóa context khi user đăng xuất
    -- =========================================================
    PROCEDURE CLEAR_USER_CONTEXT AS
        v_user_id   VARCHAR2(50);
        v_client_id VARCHAR2(100);
    BEGIN
        -- Get current values for logging
        v_user_id := GET_USER_ID();
        v_client_id := GET_CLIENT_ID();
        
        -- Clear context
        DBMS_SESSION.CLEAR_CONTEXT(
            namespace => C_CONTEXT_NAME,
            client_id => v_client_id
        );
        
        -- Clear client identifier
        DBMS_SESSION.CLEAR_IDENTIFIER;
        
        -- Log context clearing
        IF v_user_id IS NOT NULL THEN
            BEGIN
                INSERT INTO AUDIT_TRAIL(WHO, ACTION, EVENT_AT_UTC)
                VALUES (
                    v_user_id,
                    'VPD_CONTEXT_CLEAR|CLIENT=' || v_client_id,
                    SYSTIMESTAMP
                );
                COMMIT;
            EXCEPTION
                WHEN OTHERS THEN
                    NULL; -- Continue even if audit fails
            END;
        END IF;
        
    END CLEAR_USER_CONTEXT;
    
    -- =========================================================
    -- GET_USER_ID
    -- =========================================================
    FUNCTION GET_USER_ID RETURN VARCHAR2 AS
    BEGIN
        RETURN SYS_CONTEXT(C_CONTEXT_NAME, 'USER_ID');
    END GET_USER_ID;
    
    -- =========================================================
    -- GET_USER_ROLE
    -- =========================================================
    FUNCTION GET_USER_ROLE RETURN VARCHAR2 AS
    BEGIN
        RETURN SYS_CONTEXT(C_CONTEXT_NAME, 'USER_ROLE');
    END GET_USER_ROLE;
    
    -- =========================================================
    -- GET_CLIENT_ID
    -- =========================================================
    FUNCTION GET_CLIENT_ID RETURN VARCHAR2 AS
    BEGIN
        RETURN SYS_CONTEXT(C_CONTEXT_NAME, 'CLIENT_ID');
    END GET_CLIENT_ID;
    
    -- =========================================================
    -- FN_SCORES_SELECT_POLICY
    -- Policy function cho SELECT trên bảng SCORES
    -- =========================================================
    FUNCTION FN_SCORES_SELECT_POLICY(
        p_schema IN VARCHAR2,
        p_object IN VARCHAR2
    ) RETURN VARCHAR2 AS
        v_predicate VARCHAR2(4000);
        v_user_id   VARCHAR2(50);
        v_role      VARCHAR2(20);
    BEGIN
        -- Lấy thông tin từ context
        v_user_id := GET_USER_ID();
        v_role := GET_USER_ROLE();
        
        -- Nếu context chưa được set, không cho xem gì
        IF v_user_id IS NULL OR v_role IS NULL THEN
            -- Trả về điều kiện luôn FALSE
            RETURN '1=0';
        END IF;
        
        -- Áp dụng policy theo role
        CASE v_role
            -- =========================================================
            -- STUDENT: Chỉ xem được điểm của chính mình
            -- =========================================================
            WHEN C_ROLE_STUDENT THEN
                v_predicate := 'STUDENT_ID = ''' || v_user_id || '''';
                
            -- =========================================================
            -- LECTURER (CVHT): Chỉ xem được điểm các lớp mình phụ trách
            -- =========================================================
            WHEN C_ROLE_LECTURER THEN
                v_predicate := 'STUDENT_ID IN (
                    SELECT S.USER_ID 
                    FROM STUDENTS S
                    WHERE S.CLASS_ID IN (
                        SELECT CLA.CLASS_ID 
                        FROM CLASS_LECTURER_ASSIGNMENTS CLA
                        WHERE CLA.LECTURER_ID = ''' || v_user_id || '''
                        AND CLA.IS_ACTIVE = 1
                    )
                )';
                
            -- =========================================================
            -- ADMIN: Xem được tất cả điểm toàn hệ thống
            -- =========================================================
            WHEN C_ROLE_ADMIN THEN
                -- Không có điều kiện lọc = xem tất cả
                v_predicate := '1=1';
                
            ELSE
                -- Role không hợp lệ = không xem được gì
                v_predicate := '1=0';
        END CASE;
        
        RETURN v_predicate;
        
    EXCEPTION
        WHEN OTHERS THEN
            -- Trong trường hợp lỗi, block tất cả access
            RETURN '1=0';
    END FN_SCORES_SELECT_POLICY;
    
    -- =========================================================
    -- FN_SCORES_UPDATE_POLICY
    -- Policy function cho UPDATE trên bảng SCORES
    -- Chỉ Admin mới được duyệt điểm
    -- =========================================================
    FUNCTION FN_SCORES_UPDATE_POLICY(
        p_schema IN VARCHAR2,
        p_object IN VARCHAR2
    ) RETURN VARCHAR2 AS
        v_predicate VARCHAR2(4000);
        v_user_id   VARCHAR2(50);
        v_role      VARCHAR2(20);
    BEGIN
        -- Lấy thông tin từ context
        v_user_id := GET_USER_ID();
        v_role := GET_USER_ROLE();
        
        -- Nếu context chưa được set, không cho update gì
        IF v_user_id IS NULL OR v_role IS NULL THEN
            RETURN '1=0';
        END IF;
        
        -- Chỉ ADMIN mới được UPDATE (duyệt điểm)
        CASE v_role
            WHEN C_ROLE_ADMIN THEN
                -- Admin được update tất cả
                v_predicate := '1=1';
                
            WHEN C_ROLE_LECTURER THEN
                -- Lecturer được update điểm của sinh viên trong lớp phụ trách
                v_predicate := 'STUDENT_ID IN (
                    SELECT S.USER_ID 
                    FROM STUDENTS S
                    WHERE S.CLASS_ID IN (
                        SELECT CLA.CLASS_ID 
                        FROM CLASS_LECTURER_ASSIGNMENTS CLA
                        WHERE CLA.LECTURER_ID = ''' || v_user_id || '''
                        AND CLA.IS_ACTIVE = 1
                    )
                )';
                
            ELSE
                -- Student và các role khác không được update
                v_predicate := '1=0';
        END CASE;
        
        RETURN v_predicate;
        
    EXCEPTION
        WHEN OTHERS THEN
            RETURN '1=0';
    END FN_SCORES_UPDATE_POLICY;
    
END PKG_VPD_CONTEXT;
/

PROMPT '✓ Created PKG_VPD_CONTEXT body';

-- =========================================================
-- STEP 3: VERIFY PACKAGE
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'VERIFICATION - Package Status';
PROMPT '========================================';

SELECT OBJECT_NAME, OBJECT_TYPE, STATUS
FROM USER_OBJECTS
WHERE OBJECT_NAME = 'PKG_VPD_CONTEXT'
ORDER BY OBJECT_TYPE;

-- Check for compilation errors
SELECT NAME, TYPE, LINE, POSITION, TEXT
FROM USER_ERRORS
WHERE NAME = 'PKG_VPD_CONTEXT';

PROMPT '';
PROMPT '✓ PART B COMPLETED SUCCESSFULLY!';
PROMPT 'Next: Run Part C to register VPD policies';
PROMPT '========================================';
