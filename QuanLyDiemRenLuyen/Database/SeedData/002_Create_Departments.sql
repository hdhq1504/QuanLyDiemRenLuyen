-- ============================================================================
-- Tạo dữ liệu Khoa (DEPARTMENTS)
-- Chạy với: QLDiemRenLuyen
-- ============================================================================

SET SERVEROUTPUT ON;

PROMPT '========================================';
PROMPT 'Tạo dữ liệu Khoa';
PROMPT '========================================';

-- Insert các khoa
INSERT INTO DEPARTMENTS (ID, CODE, NAME) VALUES (RAWTOHEX(SYS_GUID()), 'CNTT', 'Công nghệ thông tin');
INSERT INTO DEPARTMENTS (ID, CODE, NAME) VALUES (RAWTOHEX(SYS_GUID()), 'QTKD', 'Quản trị kinh doanh');
INSERT INTO DEPARTMENTS (ID, CODE, NAME) VALUES (RAWTOHEX(SYS_GUID()), 'CNTP', 'Công nghệ thực phẩm');

COMMIT;

PROMPT '✓ Đã tạo 3 Khoa!';

-- Xác minh
PROMPT '';
PROMPT 'Danh sách Khoa:';
SELECT ID, CODE, NAME FROM DEPARTMENTS ORDER BY CODE;
