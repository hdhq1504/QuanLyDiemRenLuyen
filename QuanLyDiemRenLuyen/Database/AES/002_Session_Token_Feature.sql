-- ============================================================================
-- Tính năng AES 1: Mã hóa Token Phiên
-- Mã hóa token phiên người dùng lưu trong database để bảo mật
-- ============================================================================

-- ============================================================================
-- Gói Quản lý Token Phiên với Mã hóa AES
-- ============================================================================

CREATE OR REPLACE PACKAGE PKG_SESSION_TOKEN AS
    G_SESSION_DURATION_HOURS CONSTANT NUMBER := 24;
    
    -- p_user_id là MAND (VARCHAR2)
    FUNCTION CREATE_SESSION_TOKEN(p_user_id IN VARCHAR2) RETURN VARCHAR2;
    FUNCTION VALIDATE_SESSION_TOKEN(p_user_id IN VARCHAR2, p_token IN VARCHAR2) RETURN NUMBER;
    PROCEDURE CLEAR_SESSION_TOKEN(p_user_id IN VARCHAR2);
    FUNCTION GET_USER_BY_TOKEN(p_token IN VARCHAR2) RETURN VARCHAR2;
    
END PKG_SESSION_TOKEN;
/

CREATE OR REPLACE PACKAGE BODY PKG_SESSION_TOKEN AS

    FUNCTION GENERATE_TOKEN RETURN VARCHAR2 IS
    BEGIN
        RETURN RAWTOHEX(DBMS_CRYPTO.RANDOMBYTES(16));
    END GENERATE_TOKEN;

    FUNCTION CREATE_SESSION_TOKEN(p_user_id IN VARCHAR2) RETURN VARCHAR2 IS
        v_token VARCHAR2(64);
        v_encrypted_token RAW(256);
        v_expiry TIMESTAMP;
    BEGIN
        v_token := GENERATE_TOKEN();
        v_encrypted_token := PKG_AES_CRYPTO.ENCRYPT_WITH_SYSTEM_KEY(v_token);
        v_expiry := SYSTIMESTAMP + INTERVAL '1' HOUR * G_SESSION_DURATION_HOURS;
        
        UPDATE USERS
        SET SESSION_TOKEN_ENCRYPTED = v_encrypted_token,
            SESSION_EXPIRES_AT = v_expiry
        WHERE MAND = p_user_id;
        
        COMMIT;
        RETURN v_token;
    END CREATE_SESSION_TOKEN;

    FUNCTION VALIDATE_SESSION_TOKEN(p_user_id IN VARCHAR2, p_token IN VARCHAR2) RETURN NUMBER IS
        v_encrypted_token RAW(256);
        v_stored_token VARCHAR2(64);
        v_expiry TIMESTAMP;
    BEGIN
        SELECT SESSION_TOKEN_ENCRYPTED, SESSION_EXPIRES_AT
        INTO v_encrypted_token, v_expiry
        FROM USERS
        WHERE MAND = p_user_id;
        
        IF v_expiry IS NULL OR v_expiry < SYSTIMESTAMP THEN
            RETURN 0;
        END IF;
        
        v_stored_token := PKG_AES_CRYPTO.DECRYPT_WITH_SYSTEM_KEY(v_encrypted_token);
        
        IF v_stored_token = p_token THEN
            RETURN 1;
        ELSE
            RETURN 0;
        END IF;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN RETURN 0;
        WHEN OTHERS THEN RETURN 0;
    END VALIDATE_SESSION_TOKEN;

    PROCEDURE CLEAR_SESSION_TOKEN(p_user_id IN VARCHAR2) IS
    BEGIN
        UPDATE USERS
        SET SESSION_TOKEN_ENCRYPTED = NULL,
            SESSION_EXPIRES_AT = NULL
        WHERE MAND = p_user_id;
        COMMIT;
    END CLEAR_SESSION_TOKEN;

    FUNCTION GET_USER_BY_TOKEN(p_token IN VARCHAR2) RETURN VARCHAR2 IS
        v_stored_token VARCHAR2(64);
    BEGIN
        FOR rec IN (
            SELECT MAND, SESSION_TOKEN_ENCRYPTED
            FROM USERS
            WHERE SESSION_EXPIRES_AT > SYSTIMESTAMP
              AND SESSION_TOKEN_ENCRYPTED IS NOT NULL
        ) LOOP
            BEGIN
                v_stored_token := PKG_AES_CRYPTO.DECRYPT_WITH_SYSTEM_KEY(rec.SESSION_TOKEN_ENCRYPTED);
                IF v_stored_token = p_token THEN
                    RETURN rec.MAND;
                END IF;
            EXCEPTION
                WHEN OTHERS THEN NULL;
            END;
        END LOOP;
        RETURN NULL;
    END GET_USER_BY_TOKEN;

END PKG_SESSION_TOKEN;
/

-- Cấp quyền
GRANT EXECUTE ON PKG_SESSION_TOKEN TO ROLE_ADMIN;
GRANT EXECUTE ON PKG_SESSION_TOKEN TO ROLE_LECTURER;
GRANT EXECUTE ON PKG_SESSION_TOKEN TO ROLE_STUDENT;

COMMIT;
