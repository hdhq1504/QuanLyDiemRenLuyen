-- =========================================================
-- FGA (FINE-GRAINED AUDITING)
-- =========================================================
-- Kết nối: SYSDBA (sys as sysdba)
-- Mục đích: Tạo chính sách FGA sử dụng DBMS_FGA
-- Điều kiện: Chạy script Standard Audit và khởi động lại DB trước!
-- =========================================================
--
-- FGA audit các câu SELECT trên dữ liệu nhạy cảm
-- Lưu trữ trong SYS.FGA_LOG$ / DBA_FGA_AUDIT_TRAIL
--
-- =========================================================

SET SERVEROUTPUT ON;
SET LINESIZE 200;

PROMPT '========================================';
PROMPT 'FGA - Fine-Grained Auditing';
PROMPT 'Đang thực thi với quyền: SYSDBA';
PROMPT '========================================';

-- =========================================================
-- BƯỚC 1: KIỂM TRA AUDIT_TRAIL ĐÃ BẬT CHƯA
-- =========================================================

PROMPT '';
PROMPT 'Đang kiểm tra trạng thái audit trail...';

DECLARE
    v_audit_trail VARCHAR2(100);
BEGIN
    SELECT VALUE INTO v_audit_trail
    FROM V$PARAMETER
    WHERE NAME = 'audit_trail';
    
    IF v_audit_trail LIKE '%DB%' THEN
        DBMS_OUTPUT.PUT_LINE('✓ AUDIT_TRAIL đã bật: ' || v_audit_trail);
    ELSE
        DBMS_OUTPUT.PUT_LINE('⚠ AUDIT_TRAIL hiện tại: ' || v_audit_trail);
        DBMS_OUTPUT.PUT_LINE('Vui lòng bật AUDIT_TRAIL=DB,EXTENDED và khởi động lại database');
    END IF;
END;
/

-- =========================================================
-- BƯỚC 2: XÓA CÁC CHÍNH SÁCH FGA CŨ (nếu chạy lại)
-- =========================================================

PROMPT '';
PROMPT 'Đang dọn dẹp các chính sách FGA cũ...';

BEGIN
    DBMS_FGA.DROP_POLICY(
        object_schema => 'QLDIEMRENLUYEN',
        object_name   => 'STUDENTS',
        policy_name   => 'FGA_STUDENTS_SENSITIVE'
    );
    DBMS_OUTPUT.PUT_LINE('Đã xóa FGA_STUDENTS_SENSITIVE');
EXCEPTION
    WHEN OTHERS THEN NULL;
END;
/

BEGIN
    DBMS_FGA.DROP_POLICY(
        object_schema => 'QLDIEMRENLUYEN',
        object_name   => 'SCORES',
        policy_name   => 'FGA_SCORES_READ'
    );
    DBMS_OUTPUT.PUT_LINE('Đã xóa FGA_SCORES_READ');
EXCEPTION
    WHEN OTHERS THEN NULL;
END;
/

BEGIN
    DBMS_FGA.DROP_POLICY(
        object_schema => 'QLDIEMRENLUYEN',
        object_name   => 'FEEDBACKS',
        policy_name   => 'FGA_FEEDBACKS_CONTENT'
    );
    DBMS_OUTPUT.PUT_LINE('Đã xóa FGA_FEEDBACKS_CONTENT');
EXCEPTION
    WHEN OTHERS THEN NULL;
END;
/

BEGIN
    DBMS_FGA.DROP_POLICY(
        object_schema => 'QLDIEMRENLUYEN',
        object_name   => 'USERS',
        policy_name   => 'FGA_USERS_PASSWORD'
    );
    DBMS_OUTPUT.PUT_LINE('Đã xóa FGA_USERS_PASSWORD');
EXCEPTION
    WHEN OTHERS THEN NULL;
END;
/

-- =========================================================
-- BƯỚC 3: TẠO CHÍNH SÁCH FGA CHO DỮ LIỆU NHẠY CẢM SINH VIÊN
-- =========================================================

PROMPT '';
PROMPT 'Đang tạo các chính sách FGA...';

-- Chính sách: Audit truy cập thông tin nhạy cảm sinh viên
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => 'QLDIEMRENLUYEN',
        object_name     => 'STUDENTS',
        policy_name     => 'FGA_STUDENTS_SENSITIVE',
        audit_column    => 'PHONE,ADDRESS,ID_CARD_NUMBER,PHONE_ENCRYPTED,ADDRESS_ENCRYPTED,ID_CARD_ENCRYPTED',
        audit_condition => NULL,  -- Audit tất cả SELECT
        statement_types => 'SELECT',
        audit_trail     => DBMS_FGA.DB + DBMS_FGA.EXTENDED
    );
    DBMS_OUTPUT.PUT_LINE('✓ Đã tạo FGA_STUDENTS_SENSITIVE');
    DBMS_OUTPUT.PUT_LINE('  - Audit: SĐT, Địa chỉ, CMND (các trường mã hóa)');
END;
/

-- =========================================================
-- BƯỚC 4: TẠO CHÍNH SÁCH FGA CHO ĐIỂM
-- =========================================================

-- Chính sách: Audit xem điểm
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => 'QLDIEMRENLUYEN',
        object_name     => 'SCORES',
        policy_name     => 'FGA_SCORES_READ',
        audit_column    => 'TOTAL_SCORE,CLASSIFICATION,STATUS',
        audit_condition => NULL,  -- Audit tất cả SELECT
        statement_types => 'SELECT',
        audit_trail     => DBMS_FGA.DB + DBMS_FGA.EXTENDED
    );
    DBMS_OUTPUT.PUT_LINE('✓ Đã tạo FGA_SCORES_READ');
    DBMS_OUTPUT.PUT_LINE('  - Audit: TOTAL_SCORE, CLASSIFICATION, STATUS');
END;
/

-- =========================================================
-- BƯỚC 5: TẠO CHÍNH SÁCH FGA CHO PHẢN HỒI
-- =========================================================

-- Chính sách: Audit xem nội dung phản hồi
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => 'QLDIEMRENLUYEN',
        object_name     => 'FEEDBACKS',
        policy_name     => 'FGA_FEEDBACKS_CONTENT',
        audit_column    => 'CONTENT,RESPONSE,CONTENT_ENCRYPTED,RESPONSE_ENCRYPTED',
        audit_condition => NULL,  -- Audit tất cả SELECT
        statement_types => 'SELECT',
        audit_trail     => DBMS_FGA.DB + DBMS_FGA.EXTENDED
    );
    DBMS_OUTPUT.PUT_LINE('✓ Đã tạo FGA_FEEDBACKS_CONTENT');
    DBMS_OUTPUT.PUT_LINE('  - Audit: CONTENT, RESPONSE (các trường mã hóa)');
END;
/

-- =========================================================
-- BƯỚC 6: TẠO CHÍNH SÁCH FGA CHO MẬT KHẨU NGƯỜI DÙNG
-- =========================================================

-- Chính sách: Audit truy cập các trường liên quan mật khẩu
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => 'QLDIEMRENLUYEN',
        object_name     => 'USERS',
        policy_name     => 'FGA_USERS_PASSWORD',
        audit_column    => 'PASSWORD_HASH,PASSWORD_SALT',
        audit_condition => NULL,  -- Audit tất cả SELECT
        statement_types => 'SELECT',
        audit_trail     => DBMS_FGA.DB + DBMS_FGA.EXTENDED
    );
    DBMS_OUTPUT.PUT_LINE('✓ Đã tạo FGA_USERS_PASSWORD');
    DBMS_OUTPUT.PUT_LINE('  - Audit: PASSWORD_HASH, PASSWORD_SALT');
END;
/

-- =========================================================
-- BƯỚC 7: KÍCH HOẠT CÁC CHÍNH SÁCH FGA
-- =========================================================

PROMPT '';
PROMPT 'Đang kích hoạt các chính sách FGA...';

BEGIN
    DBMS_FGA.ENABLE_POLICY('QLDIEMRENLUYEN', 'STUDENTS', 'FGA_STUDENTS_SENSITIVE');
    DBMS_OUTPUT.PUT_LINE('✓ Đã kích hoạt FGA_STUDENTS_SENSITIVE');
END;
/

BEGIN
    DBMS_FGA.ENABLE_POLICY('QLDIEMRENLUYEN', 'SCORES', 'FGA_SCORES_READ');
    DBMS_OUTPUT.PUT_LINE('✓ Đã kích hoạt FGA_SCORES_READ');
END;
/

BEGIN
    DBMS_FGA.ENABLE_POLICY('QLDIEMRENLUYEN', 'FEEDBACKS', 'FGA_FEEDBACKS_CONTENT');
    DBMS_OUTPUT.PUT_LINE('✓ Đã kích hoạt FGA_FEEDBACKS_CONTENT');
END;
/

BEGIN
    DBMS_FGA.ENABLE_POLICY('QLDIEMRENLUYEN', 'USERS', 'FGA_USERS_PASSWORD');
    DBMS_OUTPUT.PUT_LINE('✓ Đã kích hoạt FGA_USERS_PASSWORD');
END;
/

-- =========================================================
-- XÁC MINH
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'XÁC MINH - Các chính sách FGA';
PROMPT '========================================';

PROMPT '';
PROMPT 'Các chính sách FGA đã cấu hình:';
SELECT 
    OBJECT_SCHEMA,
    OBJECT_NAME,
    POLICY_NAME,
    POLICY_COLUMN,
    ENABLED
FROM DBA_AUDIT_POLICIES
WHERE OBJECT_SCHEMA = 'QLDIEMRENLUYEN'
ORDER BY OBJECT_NAME;

PROMPT '';
PROMPT '========================================';
PROMPT '✓ HOÀN THÀNH FGA!';
PROMPT 'Các chính sách FGA đã tạo cho:';
PROMPT '  - STUDENTS (thông tin cá nhân nhạy cảm)';
PROMPT '  - SCORES (thông tin điểm)';
PROMPT '  - FEEDBACKS (nội dung phản hồi)';
PROMPT '  - USERS (các trường mật khẩu)';
PROMPT '========================================';
