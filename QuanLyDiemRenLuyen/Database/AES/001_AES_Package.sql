-- ============================================================================
-- PKG_AES_CRYPTO: Gói Mã hóa Đối xứng AES-256
-- Sử dụng DBMS_CRYPTO với AES-256-CBC
-- ============================================================================

-- ============================================================================
-- BƯỚC 1: Tạo Package Specification
-- ============================================================================

CREATE OR REPLACE PACKAGE PKG_AES_CRYPTO AS
    -- AES-256 với chế độ CBC và padding PKCS5
    G_ENCRYPTION_TYPE CONSTANT PLS_INTEGER := DBMS_CRYPTO.ENCRYPT_AES256 
                                            + DBMS_CRYPTO.CHAIN_CBC 
                                            + DBMS_CRYPTO.PAD_PKCS5;
    
    -- Tạo khóa
    FUNCTION GENERATE_AES_KEY RETURN RAW;
    FUNCTION GENERATE_IV RETURN RAW;
    
    -- Mã hóa/Giải mã
    FUNCTION AES_ENCRYPT(p_plaintext IN VARCHAR2, p_key IN RAW) RETURN RAW;
    FUNCTION AES_ENCRYPT_RAW(p_data IN RAW, p_key IN RAW) RETURN RAW;
    FUNCTION AES_DECRYPT(p_ciphertext IN RAW, p_key IN RAW) RETURN VARCHAR2;
    FUNCTION AES_DECRYPT_RAW(p_ciphertext IN RAW, p_key IN RAW) RETURN RAW;
    
    -- Quản lý khóa
    PROCEDURE STORE_AES_KEY(
        p_key_name IN VARCHAR2,
        p_key IN RAW,
        p_description IN VARCHAR2 DEFAULT NULL,
        p_created_by IN VARCHAR2 DEFAULT USER
    );
    FUNCTION GET_AES_KEY(p_key_name IN VARCHAR2) RETURN RAW;
    FUNCTION CREATE_AND_STORE_KEY(
        p_key_name IN VARCHAR2,
        p_description IN VARCHAR2 DEFAULT NULL,
        p_created_by IN VARCHAR2 DEFAULT USER
    ) RETURN RAW;
    PROCEDURE DEACTIVATE_KEY(p_key_name IN VARCHAR2);
    
    -- Hàm tiện ích
    FUNCTION ENCRYPT_WITH_SYSTEM_KEY(
        p_plaintext IN VARCHAR2,
        p_key_name IN VARCHAR2 DEFAULT 'AES_SYSTEM_KEY'
    ) RETURN RAW;
    FUNCTION DECRYPT_WITH_SYSTEM_KEY(
        p_ciphertext IN RAW,
        p_key_name IN VARCHAR2 DEFAULT 'AES_SYSTEM_KEY'
    ) RETURN VARCHAR2;
    
END PKG_AES_CRYPTO;
/

-- ============================================================================
-- BƯỚC 2: Tạo Package Body
-- ============================================================================

CREATE OR REPLACE PACKAGE BODY PKG_AES_CRYPTO AS

    FUNCTION GENERATE_AES_KEY RETURN RAW IS
    BEGIN
        RETURN DBMS_CRYPTO.RANDOMBYTES(32);
    END GENERATE_AES_KEY;
    
    FUNCTION GENERATE_IV RETURN RAW IS
    BEGIN
        RETURN DBMS_CRYPTO.RANDOMBYTES(16);
    END GENERATE_IV;
    
    FUNCTION AES_ENCRYPT(p_plaintext IN VARCHAR2, p_key IN RAW) RETURN RAW IS
        v_iv RAW(16);
        v_plaintext_raw RAW(32767);
        v_ciphertext RAW(32767);
    BEGIN
        IF p_plaintext IS NULL THEN RETURN NULL; END IF;
        v_iv := GENERATE_IV();
        v_plaintext_raw := UTL_I18N.STRING_TO_RAW(p_plaintext, 'AL32UTF8');
        v_ciphertext := DBMS_CRYPTO.ENCRYPT(
            src => v_plaintext_raw,
            typ => G_ENCRYPTION_TYPE,
            key => p_key,
            iv  => v_iv
        );
        RETURN v_iv || v_ciphertext;
    END AES_ENCRYPT;
    
    FUNCTION AES_ENCRYPT_RAW(p_data IN RAW, p_key IN RAW) RETURN RAW IS
        v_iv RAW(16);
        v_ciphertext RAW(32767);
    BEGIN
        IF p_data IS NULL THEN RETURN NULL; END IF;
        v_iv := GENERATE_IV();
        v_ciphertext := DBMS_CRYPTO.ENCRYPT(
            src => p_data,
            typ => G_ENCRYPTION_TYPE,
            key => p_key,
            iv  => v_iv
        );
        RETURN v_iv || v_ciphertext;
    END AES_ENCRYPT_RAW;
    
    FUNCTION AES_DECRYPT(p_ciphertext IN RAW, p_key IN RAW) RETURN VARCHAR2 IS
        v_iv RAW(16);
        v_actual_ciphertext RAW(32767);
        v_plaintext_raw RAW(32767);
    BEGIN
        IF p_ciphertext IS NULL THEN RETURN NULL; END IF;
        v_iv := UTL_RAW.SUBSTR(p_ciphertext, 1, 16);
        v_actual_ciphertext := UTL_RAW.SUBSTR(p_ciphertext, 17);
        v_plaintext_raw := DBMS_CRYPTO.DECRYPT(
            src => v_actual_ciphertext,
            typ => G_ENCRYPTION_TYPE,
            key => p_key,
            iv  => v_iv
        );
        RETURN UTL_I18N.RAW_TO_CHAR(v_plaintext_raw, 'AL32UTF8');
    END AES_DECRYPT;
    
    FUNCTION AES_DECRYPT_RAW(p_ciphertext IN RAW, p_key IN RAW) RETURN RAW IS
        v_iv RAW(16);
        v_actual_ciphertext RAW(32767);
    BEGIN
        IF p_ciphertext IS NULL THEN RETURN NULL; END IF;
        v_iv := UTL_RAW.SUBSTR(p_ciphertext, 1, 16);
        v_actual_ciphertext := UTL_RAW.SUBSTR(p_ciphertext, 17);
        RETURN DBMS_CRYPTO.DECRYPT(
            src => v_actual_ciphertext,
            typ => G_ENCRYPTION_TYPE,
            key => p_key,
            iv  => v_iv
        );
    END AES_DECRYPT_RAW;
    
    PROCEDURE STORE_AES_KEY(
        p_key_name IN VARCHAR2,
        p_key IN RAW,
        p_description IN VARCHAR2 DEFAULT NULL,
        p_created_by IN VARCHAR2 DEFAULT USER
    ) IS
    BEGIN
        INSERT INTO ENCRYPTION_KEYS (
            KEY_NAME,
            PUBLIC_KEY,
            AES_KEY,
            ALGORITHM,
            DESCRIPTION,
            CREATED_BY,
            CREATED_AT,
            IS_ACTIVE
        ) VALUES (
            p_key_name,
            TO_CLOB('N/A - Khóa đối xứng AES'),
            p_key,
            'AES-256',
            NVL(p_description, 'Khóa mã hóa AES-256'),
            p_created_by,
            SYSTIMESTAMP,
            1
        );
        COMMIT;
    END STORE_AES_KEY;
    
    FUNCTION GET_AES_KEY(p_key_name IN VARCHAR2) RETURN RAW IS
        v_key RAW(32);
    BEGIN
        SELECT AES_KEY INTO v_key
        FROM ENCRYPTION_KEYS
        WHERE KEY_NAME = p_key_name
          AND IS_ACTIVE = 1
          AND ALGORITHM = 'AES-256';
        
        RETURN v_key;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(-20001, 'Không tìm thấy khóa AES: ' || p_key_name);
    END GET_AES_KEY;
    
    FUNCTION CREATE_AND_STORE_KEY(
        p_key_name IN VARCHAR2,
        p_description IN VARCHAR2 DEFAULT NULL,
        p_created_by IN VARCHAR2 DEFAULT USER
    ) RETURN RAW IS
        v_key RAW(32);
    BEGIN
        v_key := GENERATE_AES_KEY();
        STORE_AES_KEY(p_key_name, v_key, p_description, p_created_by);
        RETURN v_key;
    END CREATE_AND_STORE_KEY;
    
    PROCEDURE DEACTIVATE_KEY(p_key_name IN VARCHAR2) IS
    BEGIN
        UPDATE ENCRYPTION_KEYS
        SET IS_ACTIVE = 0,
            KEY_NAME = 'DEACTIVATED_' || KEY_NAME || '_' || TO_CHAR(SYSDATE, 'YYYYMMDDHH24MISS')
        WHERE KEY_NAME = p_key_name
          AND ALGORITHM = 'AES-256';
        COMMIT;
    END DEACTIVATE_KEY;
    
    FUNCTION ENCRYPT_WITH_SYSTEM_KEY(
        p_plaintext IN VARCHAR2,
        p_key_name IN VARCHAR2 DEFAULT 'AES_SYSTEM_KEY'
    ) RETURN RAW IS
        v_key RAW(32);
    BEGIN
        v_key := GET_AES_KEY(p_key_name);
        RETURN AES_ENCRYPT(p_plaintext, v_key);
    END ENCRYPT_WITH_SYSTEM_KEY;
    
    FUNCTION DECRYPT_WITH_SYSTEM_KEY(
        p_ciphertext IN RAW,
        p_key_name IN VARCHAR2 DEFAULT 'AES_SYSTEM_KEY'
    ) RETURN VARCHAR2 IS
        v_key RAW(32);
    BEGIN
        v_key := GET_AES_KEY(p_key_name);
        RETURN AES_DECRYPT(p_ciphertext, v_key);
    END DECRYPT_WITH_SYSTEM_KEY;

END PKG_AES_CRYPTO;
/

-- ============================================================================
-- BƯỚC 3: Tạo khóa AES hệ thống ban đầu
-- ============================================================================

DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count
    FROM ENCRYPTION_KEYS
    WHERE KEY_NAME = 'AES_SYSTEM_KEY';
    
    IF v_count = 0 THEN
        DECLARE
            v_key RAW(32);
        BEGIN
            v_key := PKG_AES_CRYPTO.CREATE_AND_STORE_KEY(
                'AES_SYSTEM_KEY',
                'Khóa AES-256 mặc định cho mã hóa chung',
                'SYSTEM'
            );
        END;
    END IF;
END;
/

-- Cấp quyền
GRANT EXECUTE ON PKG_AES_CRYPTO TO ROLE_ADMIN;
GRANT EXECUTE ON PKG_AES_CRYPTO TO ROLE_LECTURER;

SELECT 'PKG_AES_CRYPTO đã cài đặt thành công' AS TRANG_THAI FROM DUAL
WHERE EXISTS (SELECT 1 FROM USER_OBJECTS WHERE OBJECT_NAME = 'PKG_AES_CRYPTO' AND OBJECT_TYPE = 'PACKAGE');

COMMIT;
