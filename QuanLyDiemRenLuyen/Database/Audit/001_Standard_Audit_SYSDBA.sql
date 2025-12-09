-- =========================================================
-- AUDIT - PHẦN 1: STANDARD AUDIT (Chạy với SYSDBA)
-- =========================================================
-- Kết nối: SYSDBA (sys as sysdba)
-- Mục đích: Bật AUDIT_TRAIL và tạo các câu lệnh AUDIT
-- =========================================================
--
-- QUAN TRỌNG: Sau khi chạy script này, BẮT BUỘC phải khởi động
-- lại database để thay đổi AUDIT_TRAIL có hiệu lực!
--
-- =========================================================

SET SERVEROUTPUT ON;
SET LINESIZE 200;

PROMPT '========================================';
PROMPT 'KIỂM TOÁN PHẦN 1 - Standard Audit Setup';
PROMPT 'Đang thực thi với quyền: SYSDBA';
PROMPT '========================================';

-- =========================================================
-- BƯỚC 1: KIỂM TRA CÀI ĐẶT AUDIT HIỆN TẠI
-- =========================================================

PROMPT '';
PROMPT 'Cài đặt audit hiện tại:';

SHOW PARAMETER AUDIT_TRAIL;

-- =========================================================
-- BƯỚC 2: BẬT AUDIT_TRAIL=DB,EXTENDED
-- =========================================================

PROMPT '';
PROMPT 'Đang bật AUDIT_TRAIL=DB,EXTENDED...';

-- DB = Lưu bản ghi audit trong bảng SYS.AUD$
-- EXTENDED = Cũng ghi lại SQL text và bind variables
ALTER SYSTEM SET AUDIT_TRAIL=DB,EXTENDED SCOPE=SPFILE;

PROMPT '✓ Đã đặt AUDIT_TRAIL=DB,EXTENDED (cần khởi động lại)';

-- =========================================================
-- BƯỚC 3: CẤP QUYỀN CẦN THIẾT
-- =========================================================

PROMPT '';
PROMPT 'Đang cấp quyền audit...';

-- Cấp quyền SELECT trên các view audit cho schema owner
GRANT SELECT ON SYS.AUD$ TO QLDIEMRENLUYEN;
GRANT SELECT ON SYS.DBA_AUDIT_TRAIL TO QLDIEMRENLUYEN;
GRANT SELECT ON SYS.DBA_FGA_AUDIT_TRAIL TO QLDIEMRENLUYEN;

PROMPT '✓ Đã cấp quyền truy cập view audit cho QLDiemRenLuyen';

-- =========================================================
-- BƯỚC 4: TẠO CHÍNH SÁCH AUDIT CHO CÁC BẢNG
-- =========================================================

PROMPT '';
PROMPT 'Đang tạo chính sách audit cho các bảng...';

-- Audit bảng SCORES (độ quan trọng cao)
AUDIT INSERT ON QLDIEMRENLUYEN.SCORES BY ACCESS;
AUDIT UPDATE ON QLDIEMRENLUYEN.SCORES BY ACCESS;
AUDIT DELETE ON QLDIEMRENLUYEN.SCORES BY ACCESS;
PROMPT '✓ Đã bật audit cho SCORES';

-- Audit bảng USERS (độ quan trọng cao)
AUDIT INSERT ON QLDIEMRENLUYEN.USERS BY ACCESS;
AUDIT UPDATE ON QLDIEMRENLUYEN.USERS BY ACCESS;
AUDIT DELETE ON QLDIEMRENLUYEN.USERS BY ACCESS;
PROMPT '✓ Đã bật audit cho USERS';

-- Audit bảng FEEDBACKS
AUDIT INSERT ON QLDIEMRENLUYEN.FEEDBACKS BY ACCESS;
AUDIT UPDATE ON QLDIEMRENLUYEN.FEEDBACKS BY ACCESS;
AUDIT DELETE ON QLDIEMRENLUYEN.FEEDBACKS BY ACCESS;
PROMPT '✓ Đã bật audit cho FEEDBACKS';

-- Audit bảng ACTIVITIES (chỉ update cho phê duyệt)
AUDIT UPDATE ON QLDIEMRENLUYEN.ACTIVITIES BY ACCESS;
PROMPT '✓ Đã bật audit cho ACTIVITIES';

-- Audit bảng PROOFS (chỉ update cho thay đổi trạng thái)
AUDIT UPDATE ON QLDIEMRENLUYEN.PROOFS BY ACCESS;
PROMPT '✓ Đã bật audit cho PROOFS';

-- Audit bảng CLASS_LECTURER_ASSIGNMENTS
AUDIT INSERT ON QLDIEMRENLUYEN.CLASS_LECTURER_ASSIGNMENTS BY ACCESS;
AUDIT UPDATE ON QLDIEMRENLUYEN.CLASS_LECTURER_ASSIGNMENTS BY ACCESS;
PROMPT '✓ Đã bật audit cho CLASS_LECTURER_ASSIGNMENTS';

-- Audit bảng REGISTRATIONS
AUDIT UPDATE ON QLDIEMRENLUYEN.REGISTRATIONS BY ACCESS;
PROMPT '✓ Đã bật audit cho REGISTRATIONS';

-- =========================================================
-- BƯỚC 5: TẠO CHÍNH SÁCH AUDIT CHO SỰ KIỆN HỆ THỐNG
-- =========================================================

PROMPT '';
PROMPT 'Đang tạo chính sách audit cho sự kiện hệ thống...';

-- Audit sự kiện session
AUDIT CREATE SESSION;
AUDIT ALTER USER;
AUDIT DROP USER;
PROMPT '✓ Đã bật audit cho sự kiện session/user';

-- Audit sự kiện phân quyền
AUDIT GRANT ANY PRIVILEGE;
AUDIT GRANT ANY ROLE;
AUDIT ROLE;
PROMPT '✓ Đã bật audit cho sự kiện phân quyền';

-- Audit DDL trên các đối tượng nhạy cảm
AUDIT ALTER ANY TABLE;
AUDIT DROP ANY TABLE;
AUDIT CREATE ANY TABLE;
PROMPT '✓ Đã bật audit cho sự kiện DDL';

-- =========================================================
-- BƯỚC 6: CẤU HÌNH DỌN DẸP AUDIT TRAIL (TÙY CHỌN)
-- =========================================================

PROMPT '';
PROMPT 'Thông tin chính sách dọn dẹp audit:';
PROMPT 'Dùng DBMS_AUDIT_MGMT để quản lý kích thước audit trail';
PROMPT 'Ví dụ: DBMS_AUDIT_MGMT.CLEAN_AUDIT_TRAIL(...)';

-- =========================================================
-- XÁC MINH
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'XÁC MINH';
PROMPT '========================================';

PROMPT '';
PROMPT 'Các tùy chọn audit đã đặt:';
SELECT OBJECT_NAME, OBJECT_TYPE, INS, UPD, DEL
FROM DBA_OBJ_AUDIT_OPTS
WHERE OWNER = 'QLDIEMRENLUYEN'
ORDER BY OBJECT_NAME;

PROMPT '';
PROMPT 'Các tùy chọn audit câu lệnh:';
SELECT AUDIT_OPTION, SUCCESS, FAILURE
FROM DBA_STMT_AUDIT_OPTS
ORDER BY AUDIT_OPTION;

PROMPT '';
PROMPT '========================================';
PROMPT '⚠️  QUAN TRỌNG: KHỞI ĐỘNG LẠI DATABASE NGAY!';
PROMPT '========================================';
PROMPT 'Chạy các lệnh sau để khởi động lại:';
PROMPT '  SHUTDOWN IMMEDIATE;';
PROMPT '  STARTUP;';
PROMPT '';
PROMPT 'Sau khi khởi động lại, chạy script FGA.';
PROMPT '========================================';
