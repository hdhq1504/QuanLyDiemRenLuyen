-- ============================================================================
-- Tạo Admin Users: AD001
-- Mật khẩu: 123456
-- Chạy với: QLDiemRenLuyen
-- ============================================================================

SET SERVEROUTPUT ON;

PROMPT '========================================';
PROMPT 'Tạo Admin Users';
PROMPT '========================================';

-- Tạo salt và hash password
-- Sử dụng DBMS_CRYPTO để hash SHA512 (password + salt)
DECLARE
    v_password VARCHAR2(20) := '123456';
    v_salt_1 RAW(32);
    v_password_hash_1 VARCHAR2(512);
    v_salt_hex_1 VARCHAR2(256);
BEGIN
    -- Tạo random salt cho mỗi user
    v_salt_1 := DBMS_CRYPTO.RANDOMBYTES(32);
    
    v_salt_hex_1 := RAWTOHEX(v_salt_1);
    
    -- Hash password: SHA512(password + salt)
    v_password_hash_1 := RAWTOHEX(
        DBMS_CRYPTO.HASH(
            UTL_RAW.CAST_TO_RAW(v_password || v_salt_hex_1),
            DBMS_CRYPTO.HASH_SH512
        )
    );
    
    -- Insert AD001
    INSERT INTO USERS (
        MAND, EMAIL, FULL_NAME, ROLE_NAME, 
        PASSWORD_HASH, PASSWORD_SALT, IS_ACTIVE
    ) VALUES (
        'AD001', 
        'quanhd@huit.edu.vn', 
        'Hồ Đức Quân',
        'ADMIN',
        v_password_hash_1,
        v_salt_hex_1,
        1
    );
    
    DBMS_OUTPUT.PUT_LINE('✓ Đã tạo user AD001');
    
    COMMIT;
    
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('========================================');
    DBMS_OUTPUT.PUT_LINE('✓ Hoàn thành tạo Admin Users!');
    DBMS_OUTPUT.PUT_LINE('  - AD001: quanhd@huit.edu.vn');
    DBMS_OUTPUT.PUT_LINE('  - Mật khẩu: 123456');
    DBMS_OUTPUT.PUT_LINE('========================================');
    
EXCEPTION
    WHEN DUP_VAL_ON_INDEX THEN
        DBMS_OUTPUT.PUT_LINE('⚠ User đã tồn tại, bỏ qua...');
        ROLLBACK;
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('❌ Lỗi: ' || SQLERRM);
        ROLLBACK;
        RAISE;
END;
/

-- Xác minh
PROMPT '';
PROMPT 'Danh sách Admin Users:';
SELECT MAND, EMAIL, FULL_NAME, ROLE_NAME, IS_ACTIVE
FROM USERS
WHERE ROLE_NAME = 'ADMIN';
