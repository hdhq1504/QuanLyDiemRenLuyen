-- ============================================================================
-- Tính năng AES 3: Mã hóa Chi tiết Nhật ký Audit
-- Mã hóa các chi tiết nhạy cảm trong bản ghi sự kiện audit
-- ============================================================================

-- ============================================================================
-- Gói Ghi nhật ký Audit có Mã hóa
-- ============================================================================

CREATE OR REPLACE PACKAGE PKG_AUDIT_CRYPTO AS
    
    -- Ghi nhật ký hành động với chi tiết nhạy cảm được mã hóa
    PROCEDURE LOG_ACTION_ENCRYPTED(
        p_who IN VARCHAR2,
        p_action IN VARCHAR2,
        p_sensitive_details IN VARCHAR2 DEFAULT NULL,
        p_client_ip IN VARCHAR2 DEFAULT NULL,
        p_user_agent IN VARCHAR2 DEFAULT NULL
    );
    
    -- Lấy bản ghi audit đã giải mã
    FUNCTION GET_AUDIT_DETAILS(p_audit_id IN VARCHAR2) RETURN VARCHAR2;
    
    -- Lấy lịch sử audit với chi tiết đã giải mã
    FUNCTION GET_AUDIT_HISTORY(
        p_who IN VARCHAR2 DEFAULT NULL,
        p_action_like IN VARCHAR2 DEFAULT NULL,
        p_limit IN NUMBER DEFAULT 100
    ) RETURN SYS_REFCURSOR;

END PKG_AUDIT_CRYPTO;
/

CREATE OR REPLACE PACKAGE BODY PKG_AUDIT_CRYPTO AS

    PROCEDURE LOG_ACTION_ENCRYPTED(
        p_who IN VARCHAR2,
        p_action IN VARCHAR2,
        p_sensitive_details IN VARCHAR2 DEFAULT NULL,
        p_client_ip IN VARCHAR2 DEFAULT NULL,
        p_user_agent IN VARCHAR2 DEFAULT NULL
    ) IS
        v_encrypted_details RAW(2000);
        v_is_encrypted NUMBER := 0;
    BEGIN
        IF p_sensitive_details IS NOT NULL THEN
            v_encrypted_details := PKG_AES_CRYPTO.ENCRYPT_WITH_SYSTEM_KEY(p_sensitive_details);
            v_is_encrypted := 1;
        END IF;
        
        INSERT INTO AUDIT_EVENTS (
            EVENT_CATEGORY,
            EVENT_TYPE,
            PERFORMED_BY,
            EVENT_AT_UTC,
            CLIENT_IP,
            USER_AGENT,
            DETAILS_ENCRYPTED,
            IS_DETAILS_ENCRYPTED
        ) VALUES (
            'SECURITY',
            p_action,
            p_who,
            SYS_EXTRACT_UTC(SYSTIMESTAMP),
            p_client_ip,
            p_user_agent,
            v_encrypted_details,
            v_is_encrypted
        );
        
        COMMIT;
    END LOG_ACTION_ENCRYPTED;

    FUNCTION GET_AUDIT_DETAILS(p_audit_id IN VARCHAR2) RETURN VARCHAR2 IS
        v_encrypted_details RAW(2000);
        v_is_encrypted NUMBER;
    BEGIN
        SELECT DETAILS_ENCRYPTED, NVL(IS_DETAILS_ENCRYPTED, 0)
        INTO v_encrypted_details, v_is_encrypted
        FROM AUDIT_EVENTS
        WHERE ID = p_audit_id;
        
        IF v_is_encrypted = 1 AND v_encrypted_details IS NOT NULL THEN
            RETURN PKG_AES_CRYPTO.DECRYPT_WITH_SYSTEM_KEY(v_encrypted_details);
        ELSE
            RETURN NULL;
        END IF;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN RETURN NULL;
    END GET_AUDIT_DETAILS;

    FUNCTION GET_AUDIT_HISTORY(
        p_who IN VARCHAR2 DEFAULT NULL,
        p_action_like IN VARCHAR2 DEFAULT NULL,
        p_limit IN NUMBER DEFAULT 100
    ) RETURN SYS_REFCURSOR IS
        v_cursor SYS_REFCURSOR;
    BEGIN
        OPEN v_cursor FOR
            SELECT 
                ID,
                PERFORMED_BY AS WHO,
                EVENT_TYPE AS ACTION,
                EVENT_AT_UTC,
                CLIENT_IP,
                USER_AGENT,
                CASE 
                    WHEN IS_DETAILS_ENCRYPTED = 1 AND DETAILS_ENCRYPTED IS NOT NULL THEN 
                        PKG_AES_CRYPTO.DECRYPT_WITH_SYSTEM_KEY(DETAILS_ENCRYPTED)
                    ELSE 
                        NULL
                END AS SENSITIVE_DETAILS
            FROM AUDIT_EVENTS
            WHERE (p_who IS NULL OR PERFORMED_BY = p_who)
              AND (p_action_like IS NULL OR EVENT_TYPE LIKE '%' || p_action_like || '%')
            ORDER BY EVENT_AT_UTC DESC
            FETCH FIRST p_limit ROWS ONLY;
        
        RETURN v_cursor;
    END GET_AUDIT_HISTORY;

END PKG_AUDIT_CRYPTO;
/

-- Cấp quyền
GRANT EXECUTE ON PKG_AUDIT_CRYPTO TO ROLE_ADMIN;
GRANT EXECUTE ON PKG_AUDIT_CRYPTO TO ROLE_LECTURER;

COMMIT;
