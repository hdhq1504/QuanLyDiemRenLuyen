-- =========================================================
-- PKG_AUDIT_EVENTS - Gói Audit Thống nhất
-- =========================================================
-- Mục đích: Ghi log sự kiện audit thống nhất cho SYSTEM, BUSINESS, SECURITY
-- Chạy với: QLDiemRenLuyen
-- =========================================================

SET SERVEROUTPUT ON;

-- =========================================================
-- Tạo gói thống nhất cho sự kiện audit
-- =========================================================

CREATE OR REPLACE PACKAGE PKG_AUDIT_EVENTS AS
    -- Ghi log sự kiện hệ thống (đăng nhập, đăng xuất, truy cập trang)
    PROCEDURE LOG_SYSTEM_EVENT(
        p_event_type IN VARCHAR2,
        p_performed_by IN VARCHAR2,
        p_description IN VARCHAR2 DEFAULT NULL,
        p_client_ip IN VARCHAR2 DEFAULT NULL,
        p_user_agent IN VARCHAR2 DEFAULT NULL,
        p_status IN VARCHAR2 DEFAULT 'SUCCESS'
    );
    
    -- Ghi log sự kiện nghiệp vụ (phê duyệt, từ chối, cập nhật)
    PROCEDURE LOG_BUSINESS_EVENT(
        p_event_type IN VARCHAR2,
        p_performed_by IN VARCHAR2,
        p_entity_type IN VARCHAR2,
        p_entity_id IN VARCHAR2,
        p_description IN VARCHAR2 DEFAULT NULL,
        p_details IN CLOB DEFAULT NULL,
        p_client_ip IN VARCHAR2 DEFAULT NULL,
        p_status IN VARCHAR2 DEFAULT 'SUCCESS',
        p_error_message IN VARCHAR2 DEFAULT NULL
    );
    
    -- Ghi log sự kiện bảo mật (mã hóa, giải mã, truy cập)
    PROCEDURE LOG_SECURITY_EVENT(
        p_event_type IN VARCHAR2,
        p_performed_by IN VARCHAR2,
        p_entity_type IN VARCHAR2 DEFAULT NULL,
        p_entity_id IN VARCHAR2 DEFAULT NULL,
        p_description IN VARCHAR2 DEFAULT NULL,
        p_sensitive_details IN VARCHAR2 DEFAULT NULL,  -- Sẽ được mã hóa
        p_client_ip IN VARCHAR2 DEFAULT NULL
    );
    
    -- Lấy sự kiện theo người dùng
    FUNCTION GET_USER_EVENTS(
        p_user_id IN VARCHAR2,
        p_limit IN NUMBER DEFAULT 100
    ) RETURN SYS_REFCURSOR;
    
    -- Lấy sự kiện theo thực thể
    FUNCTION GET_ENTITY_EVENTS(
        p_entity_type IN VARCHAR2,
        p_entity_id IN VARCHAR2
    ) RETURN SYS_REFCURSOR;
    
END PKG_AUDIT_EVENTS;
/

CREATE OR REPLACE PACKAGE BODY PKG_AUDIT_EVENTS AS

    PROCEDURE LOG_SYSTEM_EVENT(
        p_event_type IN VARCHAR2,
        p_performed_by IN VARCHAR2,
        p_description IN VARCHAR2 DEFAULT NULL,
        p_client_ip IN VARCHAR2 DEFAULT NULL,
        p_user_agent IN VARCHAR2 DEFAULT NULL,
        p_status IN VARCHAR2 DEFAULT 'SUCCESS'
    ) IS
    BEGIN
        INSERT INTO AUDIT_EVENTS (
            EVENT_CATEGORY, EVENT_TYPE, PERFORMED_BY, EVENT_AT_UTC,
            DESCRIPTION, CLIENT_IP, USER_AGENT, STATUS
        ) VALUES (
            'SYSTEM', p_event_type, p_performed_by, SYS_EXTRACT_UTC(SYSTIMESTAMP),
            p_description, p_client_ip, p_user_agent, p_status
        );
        COMMIT;
    END LOG_SYSTEM_EVENT;

    PROCEDURE LOG_BUSINESS_EVENT(
        p_event_type IN VARCHAR2,
        p_performed_by IN VARCHAR2,
        p_entity_type IN VARCHAR2,
        p_entity_id IN VARCHAR2,
        p_description IN VARCHAR2 DEFAULT NULL,
        p_details IN CLOB DEFAULT NULL,
        p_client_ip IN VARCHAR2 DEFAULT NULL,
        p_status IN VARCHAR2 DEFAULT 'SUCCESS',
        p_error_message IN VARCHAR2 DEFAULT NULL
    ) IS
    BEGIN
        INSERT INTO AUDIT_EVENTS (
            EVENT_CATEGORY, EVENT_TYPE, PERFORMED_BY, EVENT_AT_UTC,
            ENTITY_TYPE, ENTITY_ID, DESCRIPTION, DETAILS,
            CLIENT_IP, STATUS, ERROR_MESSAGE
        ) VALUES (
            'BUSINESS', p_event_type, p_performed_by, SYS_EXTRACT_UTC(SYSTIMESTAMP),
            p_entity_type, p_entity_id, p_description, p_details,
            p_client_ip, p_status, p_error_message
        );
        COMMIT;
    END LOG_BUSINESS_EVENT;

    PROCEDURE LOG_SECURITY_EVENT(
        p_event_type IN VARCHAR2,
        p_performed_by IN VARCHAR2,
        p_entity_type IN VARCHAR2 DEFAULT NULL,
        p_entity_id IN VARCHAR2 DEFAULT NULL,
        p_description IN VARCHAR2 DEFAULT NULL,
        p_sensitive_details IN VARCHAR2 DEFAULT NULL,
        p_client_ip IN VARCHAR2 DEFAULT NULL
    ) IS
        v_encrypted RAW(2000);
        v_is_encrypted NUMBER := 0;
    BEGIN
        -- Mã hóa chi tiết nhạy cảm nếu có và PKG_AES_CRYPTO tồn tại
        IF p_sensitive_details IS NOT NULL THEN
            BEGIN
                v_encrypted := PKG_AES_CRYPTO.ENCRYPT_WITH_SYSTEM_KEY(p_sensitive_details);
                v_is_encrypted := 1;
            EXCEPTION
                WHEN OTHERS THEN
                    v_encrypted := NULL;
                    v_is_encrypted := 0;
            END;
        END IF;
        
        INSERT INTO AUDIT_EVENTS (
            EVENT_CATEGORY, EVENT_TYPE, PERFORMED_BY, EVENT_AT_UTC,
            ENTITY_TYPE, ENTITY_ID, DESCRIPTION,
            DETAILS_ENCRYPTED, IS_DETAILS_ENCRYPTED, CLIENT_IP
        ) VALUES (
            'SECURITY', p_event_type, p_performed_by, SYS_EXTRACT_UTC(SYSTIMESTAMP),
            p_entity_type, p_entity_id, p_description,
            v_encrypted, v_is_encrypted, p_client_ip
        );
        COMMIT;
    END LOG_SECURITY_EVENT;

    FUNCTION GET_USER_EVENTS(
        p_user_id IN VARCHAR2,
        p_limit IN NUMBER DEFAULT 100
    ) RETURN SYS_REFCURSOR IS
        v_cursor SYS_REFCURSOR;
    BEGIN
        OPEN v_cursor FOR
            SELECT ID, EVENT_CATEGORY, EVENT_TYPE, PERFORMED_BY, EVENT_AT_UTC,
                   ENTITY_TYPE, ENTITY_ID, DESCRIPTION, CLIENT_IP, STATUS
            FROM AUDIT_EVENTS
            WHERE PERFORMED_BY = p_user_id
            ORDER BY EVENT_AT_UTC DESC
            FETCH FIRST p_limit ROWS ONLY;
        RETURN v_cursor;
    END GET_USER_EVENTS;

    FUNCTION GET_ENTITY_EVENTS(
        p_entity_type IN VARCHAR2,
        p_entity_id IN VARCHAR2
    ) RETURN SYS_REFCURSOR IS
        v_cursor SYS_REFCURSOR;
    BEGIN
        OPEN v_cursor FOR
            SELECT ID, EVENT_CATEGORY, EVENT_TYPE, PERFORMED_BY, EVENT_AT_UTC,
                   DESCRIPTION, CLIENT_IP, STATUS
            FROM AUDIT_EVENTS
            WHERE ENTITY_TYPE = p_entity_type AND ENTITY_ID = p_entity_id
            ORDER BY EVENT_AT_UTC DESC;
        RETURN v_cursor;
    END GET_ENTITY_EVENTS;

END PKG_AUDIT_EVENTS;
/

-- Cấp quyền
GRANT EXECUTE ON PKG_AUDIT_EVENTS TO ROLE_ADMIN;
GRANT EXECUTE ON PKG_AUDIT_EVENTS TO ROLE_LECTURER;
GRANT EXECUTE ON PKG_AUDIT_EVENTS TO ROLE_STUDENT;

PROMPT '✓ Đã tạo gói PKG_AUDIT_EVENTS';
COMMIT;

PROMPT '';
PROMPT '========================================';
PROMPT '✓ AUDIT_EVENTS đã tạo thành công!';
PROMPT '========================================';
