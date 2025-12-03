-- =========================================================
-- FEATURE 1: Student Sensitive Data Encryption
-- Migration Script: 001_ADD_ENCRYPTED_COLUMNS_STUDENTS
-- Description: Thêm các cột mã hóa cho thông tin nhạy cảm của sinh viên
-- Author: Thành viên 1
-- Date: 2025-12-02
-- =========================================================

-- =========================================================
-- 1) Thêm columns mã hóa vào bảng STUDENTS
-- =========================================================
ALTER TABLE STUDENTS ADD (
    PHONE_ENCRYPTED       CLOB,              -- Số điện thoại mã hóa RSA  
    ADDRESS_ENCRYPTED     CLOB,              -- Địa chỉ mã hóa RSA
    ID_CARD_NUMBER        VARCHAR2(20),      -- Số CMND/CCCD (plaintext - sẽ migrate)
    ID_CARD_ENCRYPTED     CLOB,              -- Số CMND/CCCD mã hóa RSA
    ENCRYPTED_AT          TIMESTAMP,         -- Thời điểm mã hóa
    ENCRYPTION_KEY_ID     VARCHAR2(32),      -- FK to ENCRYPTION_KEYS
    CONSTRAINT FK_STUDENTS_ENCKEY FOREIGN KEY (ENCRYPTION_KEY_ID) 
        REFERENCES ENCRYPTION_KEYS(ID)
);

-- Comments
COMMENT ON COLUMN STUDENTS.PHONE_ENCRYPTED IS 'Số điện thoại đã mã hóa RSA';
COMMENT ON COLUMN STUDENTS.ADDRESS_ENCRYPTED IS 'Địa chỉ đã mã hóa RSA';
COMMENT ON COLUMN STUDENTS.ID_CARD_ENCRYPTED IS 'Số CMND/CCCD đã mã hóa RSA';
COMMENT ON COLUMN STUDENTS.ENCRYPTED_AT IS 'Thời điểm data được mã hóa';

-- Index
CREATE INDEX IX_STUDENTS_ENCKEY ON STUDENTS(ENCRYPTION_KEY_ID);
CREATE INDEX IX_STUDENTS_ENCRYPTED ON STUDENTS(ENCRYPTED_AT);

-- =========================================================
-- 2) ORACLE PACKAGE: PKG_STUDENT_ENCRYPTION
-- Package xử lý mã hóa thông tin nhạy cảm sinh viên
-- =========================================================

CREATE OR REPLACE PACKAGE PKG_STUDENT_ENCRYPTION AS
    -- Mã hóa số điện thoại
    FUNCTION ENCRYPT_PHONE(p_phone VARCHAR2) RETURN CLOB;
    
    -- Giải mã số điện thoại
    FUNCTION DECRYPT_PHONE(p_encrypted_phone CLOB) RETURN VARCHAR2;
    
    -- Mã hóa địa chỉ
    FUNCTION ENCRYPT_ADDRESS(p_address VARCHAR2) RETURN CLOB;
    
    -- Giải mã địa chỉ
    FUNCTION DECRYPT_ADDRESS(p_encrypted_address CLOB) RETURN VARCHAR2;
    
    -- Mã hóa số CMND/CCCD
    FUNCTION ENCRYPT_ID_CARD(p_id_card VARCHAR2) RETURN CLOB;
    
    -- Giải mã số CMND/CCCD
    FUNCTION DECRYPT_ID_CARD(p_encrypted_id_card CLOB) RETURN VARCHAR2;
    
    -- Migrate dữ liệu cũ sang format mã hóa
    -- (Được gọi từ C# app, không tự động)
    PROCEDURE MIGRATE_STUDENT_DATA;
    
    -- Helper: Lấy key ID hiện tại
    FUNCTION GET_CURRENT_KEY_ID RETURN VARCHAR2;
    
END PKG_STUDENT_ENCRYPTION;
/

CREATE OR REPLACE PACKAGE BODY PKG_STUDENT_ENCRYPTION AS
    
    -- Helper function: Lấy key ID của system key hiện tại
    FUNCTION GET_CURRENT_KEY_ID 
    RETURN VARCHAR2 IS
        v_key_id VARCHAR2(32);
    BEGIN
        SELECT ID
        INTO v_key_id
        FROM ENCRYPTION_KEYS
        WHERE KEY_NAME = 'SYSTEM_MAIN_KEY'
        AND IS_ACTIVE = 1
        AND ROWNUM = 1;
        
        RETURN v_key_id;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(-20100, 'System encryption key not found');
    END GET_CURRENT_KEY_ID;
    
    -- Mã hóa số điện thoại
    -- NOTE: Actual encryption được thực hiện ở C# app layer
    -- Package này chỉ là wrapper/helper functions
    FUNCTION ENCRYPT_PHONE(p_phone VARCHAR2) 
    RETURN CLOB IS
    BEGIN
        -- Validation
        IF p_phone IS NULL OR LENGTH(TRIM(p_phone)) = 0 THEN
            RETURN NULL;
        END IF;
        
        -- Oracle không hỗ trợ RSA encryption trực tiếp mà cần DBMS_CRYPTO
        -- với key được define từ trước. Vì khó quản lý nên ta để C# handle
        -- Function này chỉ để compatibility
        
        RAISE_APPLICATION_ERROR(-20101, 
            'ENCRYPT_PHONE: Encryption should be handled by C# application layer');
    END ENCRYPT_PHONE;
    
    FUNCTION DECRYPT_PHONE(p_encrypted_phone CLOB) 
    RETURN VARCHAR2 IS
    BEGIN
        IF p_encrypted_phone IS NULL THEN
            RETURN NULL;
        END IF;
        
        RAISE_APPLICATION_ERROR(-20102, 
            'DECRYPT_PHONE: Decryption should be handled by C# application layer');
    END DECRYPT_PHONE;
    
    FUNCTION ENCRYPT_ADDRESS(p_address VARCHAR2) 
    RETURN CLOB IS
    BEGIN
        IF p_address IS NULL OR LENGTH(TRIM(p_address)) = 0 THEN
            RETURN NULL;
        END IF;
        
        RAISE_APPLICATION_ERROR(-20103, 
            'ENCRYPT_ADDRESS: Encryption should be handled by C# application layer');
    END ENCRYPT_ADDRESS;
    
    FUNCTION DECRYPT_ADDRESS(p_encrypted_address CLOB) 
    RETURN VARCHAR2 IS
    BEGIN
        IF p_encrypted_address IS NULL THEN
            RETURN NULL;
        END IF;
        
        RAISE_APPLICATION_ERROR(-20104, 
            'DECRYPT_ADDRESS: Decryption should be handled by C# application layer');
    END DECRYPT_ADDRESS;
    
    FUNCTION ENCRYPT_ID_CARD(p_id_card VARCHAR2) 
    RETURN CLOB IS
    BEGIN
        IF p_id_card IS NULL OR LENGTH(TRIM(p_id_card)) = 0 THEN
            RETURN NULL;
        END IF;
        
        RAISE_APPLICATION_ERROR(-20105, 
            'ENCRYPT_ID_CARD: Encryption should be handled by C# application layer');
    END ENCRYPT_ID_CARD;
    
    FUNCTION DECRYPT_ID_CARD(p_encrypted_id_card CLOB) 
    RETURN VARCHAR2 IS
    BEGIN
        IF p_encrypted_id_card IS NULL THEN
            RETURN NULL;
        END IF;
        
        RAISE_APPLICATION_ERROR(-20106, 
            'DECRYPT_ID_CARD: Decryption should be handled by C# application layer');
    END DECRYPT_ID_CARD;
    
    -- Migrate existing student data to encrypted format
    -- NOTE: Actual encryption done in C#, this just provides structure
    PROCEDURE MIGRATE_STUDENT_DATA IS
        v_count NUMBER := 0;
        v_key_id VARCHAR2(32);
    BEGIN
        -- Get current key ID
        v_key_id := GET_CURRENT_KEY_ID();
        
        -- Mark all existing records as needing encryption
        -- The C# app will do the actual encryption
        UPDATE STUDENTS
        SET ENCRYPTION_KEY_ID = v_key_id,
            ENCRYPTED_AT = NULL  -- NULL means "needs encryption"
        WHERE PHONE_ENCRYPTED IS NULL
        OR ADDRESS_ENCRYPTED IS NULL
        OR ID_CARD_ENCRYPTED IS NULL;
        
        v_count := SQL%ROWCOUNT;
        
        COMMIT;
        
        DBMS_OUTPUT.PUT_LINE('Marked ' || v_count || ' student records for encryption');
        DBMS_OUTPUT.PUT_LINE('Run C# migration tool to encrypt the data');
    END MIGRATE_STUDENT_DATA;

END PKG_STUDENT_ENCRYPTION;
/

-- Grant permissions
GRANT EXECUTE ON PKG_STUDENT_ENCRYPTION TO PUBLIC;

-- =========================================================
-- 3) Verification
-- =========================================================
-- Kiểm tra columns đã được thêm
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    NULLABLE
FROM USER_TAB_COLUMNS
WHERE TABLE_NAME = 'STUDENTS'
AND COLUMN_NAME IN ('PHONE_ENCRYPTED', 'ADDRESS_ENCRYPTED', 'ID_CARD_ENCRYPTED', 'ENCRYPTED_AT', 'ENCRYPTION_KEY_ID')
ORDER BY COLUMN_ID;

-- Kiểm tra package
SELECT 
    OBJECT_NAME,
    OBJECT_TYPE,
    STATUS
FROM USER_OBJECTS
WHERE OBJECT_NAME = 'PKG_STUDENT_ENCRYPTION';

PROMPT 'Migration 001_ADD_ENCRYPTED_COLUMNS_STUDENTS completed successfully!';
PROMPT 'NOTE: Actual RSA encryption/decryption is handled by C# application layer';
