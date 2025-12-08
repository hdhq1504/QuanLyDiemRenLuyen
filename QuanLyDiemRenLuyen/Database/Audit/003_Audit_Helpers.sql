-- =========================================================
-- KIỂM TOÁN - PHẦN HELPERS (Chạy với SYSDBA trước, sau đó QLDiemRenLuyen)
-- =========================================================
-- Kết nối: Bắt đầu với SYSDBA, sau đó chuyển sang QLDiemRenLuyen
-- Mục đích: Tạo context và các thủ tục hỗ trợ
-- Điều kiện: Chạy các phần trước đó trước!
-- =========================================================

SET SERVEROUTPUT ON;
SET LINESIZE 200;

PROMPT '========================================';
PROMPT 'KIỂM TOÁN - Helpers';
PROMPT '========================================';

-- =========================================================
-- PHẦN A: CHẠY VỚI SYSDBA
-- =========================================================

PROMPT '';
PROMPT '=== PHẦN A: Chạy với SYSDBA ===';
PROMPT '';

-- Tạo Application Context
PROMPT 'Đang tạo AUDIT_CTX context...';

CREATE OR REPLACE CONTEXT AUDIT_CTX USING QLDIEMRENLUYEN.PKG_AUDIT_CONTEXT ACCESSED GLOBALLY;

PROMPT '✓ Đã tạo AUDIT_CTX context';

-- Cấp quyền
GRANT CREATE ANY CONTEXT TO QLDIEMRENLUYEN;

PROMPT '✓ Đã cấp quyền CREATE ANY CONTEXT cho QLDiemRenLuyen';

-- =========================================================
-- PHẦN B: CHẠY VỚI QLDiemRenLuyen
-- =========================================================

PROMPT '';
PROMPT '=== PHẦN B: Context Package ===';
PROMPT 'Đang tạo gói PKG_AUDIT_CONTEXT...';

-- Gói quản lý context
CREATE OR REPLACE PACKAGE PKG_AUDIT_CONTEXT AS
    -- Đặt audit context trước khi thực hiện thay đổi
    PROCEDURE SET_CONTEXT(
        p_user_id       IN VARCHAR2,
        p_justification IN VARCHAR2 DEFAULT NULL,
        p_client_ip     IN VARCHAR2 DEFAULT NULL
    );
    
    -- Xóa context sau khi hoàn thành
    PROCEDURE CLEAR_CONTEXT;
    
    -- Lấy giá trị context hiện tại
    FUNCTION GET_USER_ID RETURN VARCHAR2;
    FUNCTION GET_JUSTIFICATION RETURN VARCHAR2;
END PKG_AUDIT_CONTEXT;
/

CREATE OR REPLACE PACKAGE BODY PKG_AUDIT_CONTEXT AS
    
    PROCEDURE SET_CONTEXT(
        p_user_id       IN VARCHAR2,
        p_justification IN VARCHAR2 DEFAULT NULL,
        p_client_ip     IN VARCHAR2 DEFAULT NULL
    ) AS
    BEGIN
        DBMS_SESSION.SET_CONTEXT('AUDIT_CTX', 'USER_ID', p_user_id);
        
        IF p_justification IS NOT NULL THEN
            DBMS_SESSION.SET_CONTEXT('AUDIT_CTX', 'JUSTIFICATION', p_justification);
        END IF;
        
        IF p_client_ip IS NOT NULL THEN
            DBMS_SESSION.SET_CONTEXT('AUDIT_CTX', 'CLIENT_IP', p_client_ip);
        END IF;
        
        DBMS_SESSION.SET_CONTEXT('AUDIT_CTX', 'SET_AT', TO_CHAR(SYSTIMESTAMP, 'YYYY-MM-DD HH24:MI:SS'));
    END SET_CONTEXT;
    
    PROCEDURE CLEAR_CONTEXT AS
    BEGIN
        DBMS_SESSION.CLEAR_CONTEXT('AUDIT_CTX');
    END CLEAR_CONTEXT;
    
    FUNCTION GET_USER_ID RETURN VARCHAR2 AS
    BEGIN
        RETURN SYS_CONTEXT('AUDIT_CTX', 'USER_ID');
    END GET_USER_ID;
    
    FUNCTION GET_JUSTIFICATION RETURN VARCHAR2 AS
    BEGIN
        RETURN SYS_CONTEXT('AUDIT_CTX', 'JUSTIFICATION');
    END GET_JUSTIFICATION;
    
END PKG_AUDIT_CONTEXT;
/

PROMPT '✓ Đã tạo gói PKG_AUDIT_CONTEXT';

-- =========================================================
-- BƯỚC 2: TẠO CÁC THỦ TỤC HỖ TRỢ
-- =========================================================

PROMPT '';
PROMPT 'Đang tạo các thủ tục hỗ trợ...';

-- Thủ tục ghi log hành động nghiệp vụ
CREATE OR REPLACE PROCEDURE SP_LOG_BUSINESS_ACTION(
    p_action_type   IN VARCHAR2,
    p_action_desc   IN VARCHAR2,
    p_entity_type   IN VARCHAR2 DEFAULT NULL,
    p_entity_id     IN VARCHAR2 DEFAULT NULL,
    p_performed_by  IN VARCHAR2 DEFAULT NULL,
    p_details       IN CLOB DEFAULT NULL,
    p_status        IN VARCHAR2 DEFAULT 'SUCCESS'
) AS
    v_user_id VARCHAR2(50);
BEGIN
    v_user_id := NVL(p_performed_by, SYS_CONTEXT('AUDIT_CTX', 'USER_ID'));
    
    INSERT INTO AUDIT_BUSINESS_ACTIONS(
        ACTION_TYPE, ACTION_DESC, ENTITY_TYPE, ENTITY_ID,
        PERFORMED_BY, CLIENT_IP, DETAILS, STATUS
    ) VALUES (
        p_action_type, p_action_desc, p_entity_type, p_entity_id,
        v_user_id, SYS_CONTEXT('USERENV', 'IP_ADDRESS'), p_details, p_status
    );
    
    COMMIT;
END SP_LOG_BUSINESS_ACTION;
/

PROMPT '✓ Đã tạo SP_LOG_BUSINESS_ACTION';

-- Thủ tục lấy lịch sử bản ghi
CREATE OR REPLACE PROCEDURE SP_GET_RECORD_HISTORY(
    p_table_name    IN VARCHAR2,
    p_record_id     IN VARCHAR2,
    p_result        OUT SYS_REFCURSOR
) AS
BEGIN
    OPEN p_result FOR
        SELECT 
            ID,
            OPERATION,
            OLD_VALUES,
            NEW_VALUES,
            CHANGED_COLUMNS,
            PERFORMED_BY,
            PERFORMED_AT,
            JUSTIFICATION
        FROM AUDIT_CHANGE_LOGS
        WHERE TABLE_NAME = UPPER(p_table_name)
          AND RECORD_ID = p_record_id
        ORDER BY PERFORMED_AT DESC;
END SP_GET_RECORD_HISTORY;
/

PROMPT '✓ Đã tạo SP_GET_RECORD_HISTORY';

-- Hàm đếm số lượng audit của một bản ghi
CREATE OR REPLACE FUNCTION FN_GET_AUDIT_COUNT(
    p_table_name    IN VARCHAR2,
    p_record_id     IN VARCHAR2
) RETURN NUMBER AS
    v_count NUMBER;
BEGIN
    SELECT COUNT(*)
    INTO v_count
    FROM AUDIT_CHANGE_LOGS
    WHERE TABLE_NAME = UPPER(p_table_name)
      AND RECORD_ID = p_record_id;
    
    RETURN v_count;
END FN_GET_AUDIT_COUNT;
/

PROMPT '✓ Đã tạo FN_GET_AUDIT_COUNT';

-- Thủ tục đặt lý do (wrapper)
CREATE OR REPLACE PROCEDURE SP_SET_AUDIT_JUSTIFICATION(
    p_user_id       IN VARCHAR2,
    p_justification IN VARCHAR2
) AS
BEGIN
    PKG_AUDIT_CONTEXT.SET_CONTEXT(
        p_user_id       => p_user_id,
        p_justification => p_justification
    );
END SP_SET_AUDIT_JUSTIFICATION;
/

PROMPT '✓ Đã tạo SP_SET_AUDIT_JUSTIFICATION';

-- Thủ tục xóa lý do
CREATE OR REPLACE PROCEDURE SP_CLEAR_AUDIT_CONTEXT AS
BEGIN
    PKG_AUDIT_CONTEXT.CLEAR_CONTEXT;
END SP_CLEAR_AUDIT_CONTEXT;
/

PROMPT '✓ Đã tạo SP_CLEAR_AUDIT_CONTEXT';

-- =========================================================
-- BƯỚC 3: TẠO THỦ TỤC BÁO CÁO AUDIT
-- =========================================================

PROMPT '';
PROMPT 'Đang tạo các thủ tục báo cáo audit...';

CREATE OR REPLACE PROCEDURE SP_AUDIT_REPORT(
    p_table_name    IN VARCHAR2 DEFAULT NULL,
    p_start_date    IN DATE DEFAULT SYSDATE - 30,
    p_end_date      IN DATE DEFAULT SYSDATE,
    p_user_id       IN VARCHAR2 DEFAULT NULL,
    p_result        OUT SYS_REFCURSOR
) AS
BEGIN
    OPEN p_result FOR
        SELECT 
            TABLE_NAME,
            RECORD_ID,
            OPERATION,
            PERFORMED_BY,
            PERFORMED_AT,
            CHANGED_COLUMNS,
            JUSTIFICATION
        FROM AUDIT_CHANGE_LOGS
        WHERE (p_table_name IS NULL OR TABLE_NAME = UPPER(p_table_name))
          AND PERFORMED_AT >= p_start_date
          AND PERFORMED_AT < p_end_date + 1
          AND (p_user_id IS NULL OR PERFORMED_BY = p_user_id)
        ORDER BY PERFORMED_AT DESC;
END SP_AUDIT_REPORT;
/

PROMPT '✓ Đã tạo SP_AUDIT_REPORT';

-- Hàm tổng hợp theo ngày
CREATE OR REPLACE FUNCTION FN_AUDIT_DAILY_SUMMARY(
    p_date IN DATE DEFAULT SYSDATE
) RETURN SYS_REFCURSOR AS
    v_result SYS_REFCURSOR;
BEGIN
    OPEN v_result FOR
        SELECT 
            TABLE_NAME,
            OPERATION,
            COUNT(*) AS CHANGE_COUNT,
            COUNT(DISTINCT PERFORMED_BY) AS UNIQUE_USERS
        FROM AUDIT_CHANGE_LOGS
        WHERE TRUNC(PERFORMED_AT) = TRUNC(p_date)
        GROUP BY TABLE_NAME, OPERATION
        ORDER BY TABLE_NAME, OPERATION;
    
    RETURN v_result;
END FN_AUDIT_DAILY_SUMMARY;
/

PROMPT '✓ Đã tạo FN_AUDIT_DAILY_SUMMARY';

-- =========================================================
-- XÁC MINH
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'XÁC MINH - Các đối tượng Helper';
PROMPT '========================================';

PROMPT '';
PROMPT 'Packages:';
SELECT OBJECT_NAME, OBJECT_TYPE, STATUS
FROM USER_OBJECTS
WHERE OBJECT_NAME = 'PKG_AUDIT_CONTEXT';

PROMPT '';
PROMPT 'Procedures:';
SELECT OBJECT_NAME, OBJECT_TYPE, STATUS
FROM USER_OBJECTS
WHERE OBJECT_TYPE = 'PROCEDURE'
  AND OBJECT_NAME LIKE '%AUDIT%'
ORDER BY OBJECT_NAME;

PROMPT '';
PROMPT 'Functions:';
SELECT OBJECT_NAME, OBJECT_TYPE, STATUS
FROM USER_OBJECTS
WHERE OBJECT_TYPE = 'FUNCTION'
  AND OBJECT_NAME LIKE '%AUDIT%'
ORDER BY OBJECT_NAME;

PROMPT '';
PROMPT '========================================';
PROMPT '✓ HOÀN THÀNH AUDIT HELPERS!';
PROMPT 'Đã tạo:';
PROMPT '  - PKG_AUDIT_CONTEXT (quản lý context)';
PROMPT '  - SP_LOG_BUSINESS_ACTION';
PROMPT '  - SP_GET_RECORD_HISTORY';
PROMPT '  - SP_SET_AUDIT_JUSTIFICATION';
PROMPT '  - SP_CLEAR_AUDIT_CONTEXT';
PROMPT '  - SP_AUDIT_REPORT';
PROMPT '  - FN_GET_AUDIT_COUNT';
PROMPT '  - FN_AUDIT_DAILY_SUMMARY';
PROMPT '========================================';
