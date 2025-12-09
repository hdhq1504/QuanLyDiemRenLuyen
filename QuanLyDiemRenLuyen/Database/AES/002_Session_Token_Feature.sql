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
        v_token_hash VARCHAR2(128);
        v_encrypted_token RAW(2000);
        v_expiry TIMESTAMP;
    BEGIN
        v_token := GENERATE_TOKEN();
        v_token_hash := RAWTOHEX(DBMS_CRYPTO.HASH(UTL_RAW.CAST_TO_RAW(v_token), DBMS_CRYPTO.HASH_SH256));
        v_encrypted_token := PKG_AES_CRYPTO.ENCRYPT_WITH_SYSTEM_KEY(v_token);
        v_expiry := SYSTIMESTAMP + INTERVAL '1' HOUR * G_SESSION_DURATION_HOURS;
        
        -- Xóa token cũ của user
        DELETE FROM SESSION_TOKENS WHERE USER_MAND = p_user_id;
        
        -- Tạo token mới
        INSERT INTO SESSION_TOKENS (
            USER_MAND, TOKEN_HASH, TOKEN_ENCRYPTED, 
            EXPIRES_AT, IS_VALID, CLIENT_IP
        ) VALUES (
            p_user_id, v_token_hash, v_encrypted_token,
            v_expiry, 1, SYS_CONTEXT('USERENV', 'IP_ADDRESS')
        );
        
        COMMIT;
        RETURN v_token;
    END CREATE_SESSION_TOKEN;

    FUNCTION VALIDATE_SESSION_TOKEN(p_user_id IN VARCHAR2, p_token IN VARCHAR2) RETURN NUMBER IS
        v_encrypted_token RAW(2000);
        v_stored_token VARCHAR2(64);
        v_expiry TIMESTAMP;
        v_is_valid NUMBER;
    BEGIN
        SELECT TOKEN_ENCRYPTED, EXPIRES_AT, IS_VALID
        INTO v_encrypted_token, v_expiry, v_is_valid
        FROM SESSION_TOKENS
        WHERE USER_MAND = p_user_id
          AND IS_VALID = 1
          AND ROWNUM = 1;
        
        IF v_is_valid = 0 OR v_expiry IS NULL OR v_expiry < SYSTIMESTAMP THEN
            RETURN 0;
        END IF;
        
        v_stored_token := PKG_AES_CRYPTO.DECRYPT_WITH_SYSTEM_KEY(v_encrypted_token);
        
        IF v_stored_token = p_token THEN
            -- Cập nhật LAST_USED_AT
            UPDATE SESSION_TOKENS 
            SET LAST_USED_AT = SYSTIMESTAMP
            WHERE USER_MAND = p_user_id AND IS_VALID = 1;
            COMMIT;
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
        UPDATE SESSION_TOKENS
        SET IS_VALID = 0
        WHERE USER_MAND = p_user_id;
        COMMIT;
    END CLEAR_SESSION_TOKEN;

    FUNCTION GET_USER_BY_TOKEN(p_token IN VARCHAR2) RETURN VARCHAR2 IS
        v_token_hash VARCHAR2(128);
        v_encrypted_token RAW(2000);
        v_stored_token VARCHAR2(64);
        v_user_id VARCHAR2(50);
    BEGIN
        v_token_hash := RAWTOHEX(DBMS_CRYPTO.HASH(UTL_RAW.CAST_TO_RAW(p_token), DBMS_CRYPTO.HASH_SH256));
        
        -- Tìm theo hash trước (nhanh hơn)
        FOR rec IN (
            SELECT USER_MAND, TOKEN_ENCRYPTED
            FROM SESSION_TOKENS
            WHERE TOKEN_HASH = v_token_hash
              AND IS_VALID = 1
              AND EXPIRES_AT > SYSTIMESTAMP
        ) LOOP
            BEGIN
                v_stored_token := PKG_AES_CRYPTO.DECRYPT_WITH_SYSTEM_KEY(rec.TOKEN_ENCRYPTED);
                IF v_stored_token = p_token THEN
                    RETURN rec.USER_MAND;
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
