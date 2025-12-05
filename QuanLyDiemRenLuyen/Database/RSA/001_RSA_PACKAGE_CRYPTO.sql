-- =========================================================
-- RSA ENCRYPTION INFRASTRUCTURE
-- Migration Script: 000_CREATE_ENCRYPTION_INFRASTRUCTURE
-- Description: Oracle packages cho RSA encryption
-- NOTE: ENCRYPTION_KEYS table already created in db.sql
-- =========================================================

SET SERVEROUTPUT ON;

-- =========================================================
-- 1) ORACLE PACKAGE: PKG_RSA_CRYPTO
-- Package xử lý RSA encryption/decryption trong Oracle
-- =========================================================

CREATE OR REPLACE PACKAGE PKG_RSA_CRYPTO AS
    -- Constants
    C_DEFAULT_KEY_NAME CONSTANT VARCHAR2(50) := 'SYSTEM_MAIN_KEY';
    
    -- Lấy public key theo tên
    FUNCTION GET_PUBLIC_KEY(p_key_name VARCHAR2 DEFAULT C_DEFAULT_KEY_NAME) RETURN CLOB;
    
    -- Lấy private key theo tên (CHỈ DBO hoặc ADMIN có quyền)
    FUNCTION GET_PRIVATE_KEY(p_key_name VARCHAR2 DEFAULT C_DEFAULT_KEY_NAME) RETURN CLOB;
    
    -- Kiểm tra key có tồn tại và active không
    FUNCTION IS_KEY_ACTIVE(p_key_name VARCHAR2) RETURN NUMBER;
    
    -- Cập nhật thời gian sử dụng key cuối cùng
    PROCEDURE UPDATE_KEY_USAGE(p_key_name VARCHAR2);
    
    -- Tạo key mới (sẽ call từ C# để lưu vào DB)
    PROCEDURE CREATE_KEY(
        p_key_name VARCHAR2,
        p_public_key CLOB,
        p_private_key CLOB,
        p_created_by VARCHAR2,
        p_description VARCHAR2 DEFAULT NULL
    );
    
    -- Vô hiệu hóa key
    PROCEDURE DEACTIVATE_KEY(p_key_name VARCHAR2);
    
    -- Hàm tính SHA-256 hash (để verify data integrity)
    FUNCTION COMPUTE_SHA256(p_data VARCHAR2) RETURN VARCHAR2;
    
END PKG_RSA_CRYPTO;
/

CREATE OR REPLACE PACKAGE BODY PKG_RSA_CRYPTO AS

    -- Lấy public key
    FUNCTION GET_PUBLIC_KEY(p_key_name VARCHAR2 DEFAULT C_DEFAULT_KEY_NAME) 
    RETURN CLOB IS
        v_public_key CLOB;
    BEGIN
        SELECT PUBLIC_KEY
        INTO v_public_key
        FROM ENCRYPTION_KEYS
        WHERE KEY_NAME = p_key_name
        AND IS_ACTIVE = 1;
        
        RETURN v_public_key;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(-20001, 'Key không tồn tại hoặc đã bị vô hiệu hóa: ' || p_key_name);
        WHEN OTHERS THEN
            RAISE_APPLICATION_ERROR(-20002, 'Lỗi khi lấy public key: ' || SQLERRM);
    END GET_PUBLIC_KEY;
    
    -- Lấy private key (cần quyền đặc biệt)
    FUNCTION GET_PRIVATE_KEY(p_key_name VARCHAR2 DEFAULT C_DEFAULT_KEY_NAME) 
    RETURN CLOB IS
        v_private_key CLOB;
        v_current_user VARCHAR2(100);
    BEGIN
        -- Lấy user hiện tại
        SELECT USER INTO v_current_user FROM DUAL;
        
        -- Chỉ cho phép DBO access private key
        -- Trong thực tế nên check role cụ thể
        IF v_current_user NOT IN ('QLDIEM RENLLUYEN', 'SYS', 'SYSTEM') THEN
            RAISE_APPLICATION_ERROR(-20003, 'Không có quyền truy cập private key');
        END IF;
        
        SELECT PRIVATE_KEY
        INTO v_private_key
        FROM ENCRYPTION_KEYS
        WHERE KEY_NAME = p_key_name
        AND IS_ACTIVE = 1;
        
        RETURN v_private_key;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(-20001, 'Key không tồn tại hoặc đã bị vô hiệu hóa: ' || p_key_name);
        WHEN OTHERS THEN
            RAISE_APPLICATION_ERROR(-20002, 'Lỗi khi lấy private key: ' || SQLERRM);
    END GET_PRIVATE_KEY;
    
    -- Kiểm tra key active
    FUNCTION IS_KEY_ACTIVE(p_key_name VARCHAR2) 
    RETURN NUMBER IS
        v_count NUMBER;
    BEGIN
        SELECT COUNT(*)
        INTO v_count
        FROM ENCRYPTION_KEYS
        WHERE KEY_NAME = p_key_name
        AND IS_ACTIVE = 1
        AND (EXPIRES_AT IS NULL OR EXPIRES_AT > SYSTIMESTAMP);
        
        RETURN v_count;
    END IS_KEY_ACTIVE;
    
    -- Update last used time
    PROCEDURE UPDATE_KEY_USAGE(p_key_name VARCHAR2) IS
    BEGIN
        UPDATE ENCRYPTION_KEYS
        SET LAST_USED_AT = SYSTIMESTAMP
        WHERE KEY_NAME = p_key_name;
        
        COMMIT;
    END UPDATE_KEY_USAGE;
    
    -- Tạo key mới
    PROCEDURE CREATE_KEY(
        p_key_name VARCHAR2,
        p_public_key CLOB,
        p_private_key CLOB,
        p_created_by VARCHAR2,
        p_description VARCHAR2 DEFAULT NULL
    ) IS
    BEGIN
        INSERT INTO ENCRYPTION_KEYS (
            KEY_NAME,
            PUBLIC_KEY,
            PRIVATE_KEY,
            CREATED_BY,
            DESCRIPTION,
            CREATED_AT,
            IS_ACTIVE
        ) VALUES (
            p_key_name,
            p_public_key,
            p_private_key,
            p_created_by,
            p_description,
            SYSTIMESTAMP,
            1
        );
        
        COMMIT;
    EXCEPTION
        WHEN DUP_VAL_ON_INDEX THEN
            RAISE_APPLICATION_ERROR(-20004, 'Key name đã tồn tại: ' || p_key_name);
        WHEN OTHERS THEN
            RAISE_APPLICATION_ERROR(-20005, 'Lỗi khi tạo key: ' || SQLERRM);
    END CREATE_KEY;
    
    -- Vô hiệu hóa key
    PROCEDURE DEACTIVATE_KEY(p_key_name VARCHAR2) IS
    BEGIN
        UPDATE ENCRYPTION_KEYS
        SET IS_ACTIVE = 0
        WHERE KEY_NAME = p_key_name;
        
        IF SQL%ROWCOUNT = 0 THEN
            RAISE_APPLICATION_ERROR(-20006, 'Key không tồn tại: ' || p_key_name);
        END IF;
        
        COMMIT;
    END DEACTIVATE_KEY;
    
    -- Tính SHA-256 hash
    FUNCTION COMPUTE_SHA256(p_data VARCHAR2) 
    RETURN VARCHAR2 IS
        v_hash RAW(32);
    BEGIN
        v_hash := DBMS_CRYPTO.HASH(
            src => UTL_RAW.CAST_TO_RAW(p_data),
            typ => DBMS_CRYPTO.HASH_SH256
        );
        
        RETURN LOWER(RAWTOHEX(v_hash));
    END COMPUTE_SHA256;

END PKG_RSA_CRYPTO;
/

-- =========================================================
-- 2) Grant permissions
-- =========================================================
GRANT EXECUTE ON PKG_RSA_CRYPTO TO PUBLIC;

-- =========================================================
-- 3) Verification queries
-- =========================================================
-- Kiểm tra table đã tạo
SELECT 'ENCRYPTION_KEYS table created' AS STATUS FROM DUAL
WHERE EXISTS (SELECT 1 FROM USER_TABLES WHERE TABLE_NAME = 'ENCRYPTION_KEYS');

-- Kiểm tra package đã compile
SELECT 'PKG_RSA_CRYPTO package created' AS STATUS FROM DUAL
WHERE EXISTS (
    SELECT 1 FROM USER_OBJECTS 
    WHERE OBJECT_NAME = 'PKG_RSA_CRYPTO' 
    AND OBJECT_TYPE = 'PACKAGE'
    AND STATUS = 'VALID'
);

-- Hiển thị thông tin keys
SELECT 
    KEY_NAME,
    KEY_SIZE,
    ALGORITHM,
    IS_ACTIVE,
    CREATED_AT,
    DESCRIPTION
FROM ENCRYPTION_KEYS;

PROMPT 'Migration 000_CREATE_ENCRYPTION_INFRASTRUCTURE completed successfully!';
