-- =========================================================
-- RSA ENCRYPTION INFRASTRUCTURE
-- Migration Script: 000_CREATE_ENCRYPTION_INFRASTRUCTURE
-- Description: Tạo bảng ENCRYPTION_KEYS và Oracle packages cho RSA encryption
-- Author: Team RSA
-- Date: 2025-12-02
-- =========================================================

-- =========================================================
-- 1) BẢNG ENCRYPTION_KEYS - Lưu trữ RSA key pairs
-- =========================================================
CREATE TABLE ENCRYPTION_KEYS (
    ID              VARCHAR2(32) DEFAULT RAWTOHEX(SYS_GUID()) PRIMARY KEY,
    KEY_NAME        VARCHAR2(100) NOT NULL UNIQUE,
    PUBLIC_KEY      CLOB NOT NULL,           -- RSA Public Key (XML format)
    PRIVATE_KEY     CLOB,                    -- RSA Private Key (XML format) - MÃ HÓA
    KEY_SIZE        NUMBER DEFAULT 2048,      -- Key size in bits
    ALGORITHM       VARCHAR2(20) DEFAULT 'RSA',
    CREATED_AT      TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
    CREATED_BY      VARCHAR2(50),            -- Username who created the key (nullable)
    IS_ACTIVE       NUMBER(1) DEFAULT 1 NOT NULL,
    EXPIRES_AT      TIMESTAMP,
    LAST_USED_AT    TIMESTAMP,
    DESCRIPTION     VARCHAR2(500),
    CONSTRAINT CK_ENCKEY_ACTIVE CHECK (IS_ACTIVE IN (0,1))
    -- Note: Không dùng FK vì system key được tạo trước khi có users
);

-- Indexes
CREATE INDEX IX_ENCKEY_NAME ON ENCRYPTION_KEYS(KEY_NAME);
CREATE INDEX IX_ENCKEY_ACTIVE ON ENCRYPTION_KEYS(IS_ACTIVE);
CREATE INDEX IX_ENCKEY_CREATED ON ENCRYPTION_KEYS(CREATED_AT);

-- Comments
COMMENT ON TABLE ENCRYPTION_KEYS IS 'Lưu trữ các cặp khóa RSA của hệ thống';
COMMENT ON COLUMN ENCRYPTION_KEYS.PUBLIC_KEY IS 'Public key ở định dạng XML - dùng để mã hóa';
COMMENT ON COLUMN ENCRYPTION_KEYS.PRIVATE_KEY IS 'Private key ở định dạng XML - dùng để giải mã (PHẢI MÃ HÓA)';
COMMENT ON COLUMN ENCRYPTION_KEYS.KEY_NAME IS 'Tên định danh của key (ví dụ: SYSTEM_MAIN_KEY)';

-- =========================================================
-- 2) ORACLE PACKAGE: PKG_RSA_CRYPTO
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
-- 3) Grant permissions
-- =========================================================
GRANT EXECUTE ON PKG_RSA_CRYPTO TO PUBLIC;

-- =========================================================
-- 4) Test data - Tạo system key (sẽ được replace bởi app)
-- =========================================================
-- Note: Key này chỉ là placeholder, app sẽ generate key thật khi khởi động
INSERT INTO ENCRYPTION_KEYS (
    ID,
    KEY_NAME,
    PUBLIC_KEY,
    PRIVATE_KEY,
    CREATED_BY,
    DESCRIPTION
) VALUES (
    RAWTOHEX(SYS_GUID()),
    'SYSTEM_MAIN_KEY',
    '<RSAKeyValue><Modulus>PLACEHOLDER</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>',
    '<RSAKeyValue><Modulus>PLACEHOLDER</Modulus><Exponent>AQAB</Exponent><P>PLACEHOLDER</P></RSAKeyValue>',
    NULL,  -- CREATED_BY = NULL vì system key được tạo trước khi có users
    'Main system RSA key pair - will be replaced by application on first run'
);

COMMIT;

-- =========================================================
-- 5) Verification queries
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
