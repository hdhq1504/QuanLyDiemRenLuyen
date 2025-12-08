-- =========================================================
-- KIỂM TOÁN - CÁC VIEWS (Chạy với QLDiemRenLuyen)
-- =========================================================
-- Kết nối: QLDiemRenLuyen
-- Mục đích: Tạo các views thống nhất cho dữ liệu audit
-- Điều kiện: Chạy các phần trước đó trước!
-- =========================================================

SET SERVEROUTPUT ON;
SET LINESIZE 200;

PROMPT '========================================';
PROMPT 'KIỂM TOÁN - Audit Views';
PROMPT 'Đang thực thi với: QLDiemRenLuyen';
PROMPT '========================================';

-- =========================================================
-- BƯỚC 1: TẠO VIEW LỊCH SỬ ĐIỂM
-- =========================================================

PROMPT '';
PROMPT 'Đang tạo các audit views...';

CREATE OR REPLACE VIEW V_AUDIT_SCORES_HISTORY AS
SELECT 
    acl.ID AS AUDIT_ID,
    acl.RECORD_ID AS SCORE_ID,
    acl.OPERATION,
    acl.OLD_VALUES,
    acl.NEW_VALUES,
    acl.CHANGED_COLUMNS,
    acl.PERFORMED_BY,
    acl.PERFORMED_AT,
    acl.JUSTIFICATION,
    s.STUDENT_ID,
    s.TERM_ID,
    u.FULL_NAME AS PERFORMER_NAME
FROM AUDIT_CHANGE_LOGS acl
LEFT JOIN SCORES s ON TO_CHAR(s.ID) = acl.RECORD_ID
LEFT JOIN USERS u ON u.MAND = acl.PERFORMED_BY
WHERE acl.TABLE_NAME = 'SCORES'
ORDER BY acl.PERFORMED_AT DESC;

COMMENT ON VIEW V_AUDIT_SCORES_HISTORY IS 'Lịch sử tất cả thay đổi của bảng SCORES';

PROMPT '✓ Đã tạo V_AUDIT_SCORES_HISTORY';

-- =========================================================
-- BƯỚC 2: TẠO VIEW HOẠT ĐỘNG NGƯỜI DÙNG
-- =========================================================

CREATE OR REPLACE VIEW V_AUDIT_USER_ACTIVITY AS
SELECT 
    acl.PERFORMED_BY AS USER_ID,
    u.FULL_NAME AS USER_NAME,
    u.ROLE_NAME,
    acl.TABLE_NAME,
    acl.OPERATION,
    COUNT(*) AS ACTION_COUNT,
    MIN(acl.PERFORMED_AT) AS FIRST_ACTION,
    MAX(acl.PERFORMED_AT) AS LAST_ACTION
FROM AUDIT_CHANGE_LOGS acl
LEFT JOIN USERS u ON u.MAND = acl.PERFORMED_BY
WHERE acl.PERFORMED_BY IS NOT NULL
GROUP BY acl.PERFORMED_BY, u.FULL_NAME, u.ROLE_NAME, acl.TABLE_NAME, acl.OPERATION
ORDER BY acl.PERFORMED_BY, acl.TABLE_NAME;

COMMENT ON VIEW V_AUDIT_USER_ACTIVITY IS 'Tổng hợp hoạt động người dùng theo bảng và thao tác';

PROMPT '✓ Đã tạo V_AUDIT_USER_ACTIVITY';

-- =========================================================
-- BƯỚC 3: TẠO VIEW TỔNG HỢP THEO NGÀY
-- =========================================================

CREATE OR REPLACE VIEW V_AUDIT_DAILY_SUMMARY AS
SELECT 
    TRUNC(PERFORMED_AT) AS AUDIT_DATE,
    TABLE_NAME,
    OPERATION,
    COUNT(*) AS CHANGE_COUNT,
    COUNT(DISTINCT PERFORMED_BY) AS UNIQUE_USERS,
    COUNT(CASE WHEN JUSTIFICATION IS NOT NULL THEN 1 END) AS WITH_JUSTIFICATION
FROM AUDIT_CHANGE_LOGS
GROUP BY TRUNC(PERFORMED_AT), TABLE_NAME, OPERATION
ORDER BY AUDIT_DATE DESC, TABLE_NAME, OPERATION;

COMMENT ON VIEW V_AUDIT_DAILY_SUMMARY IS 'Tổng hợp hoạt động audit theo ngày';

PROMPT '✓ Đã tạo V_AUDIT_DAILY_SUMMARY';

-- =========================================================
-- BƯỚC 4: TẠO VIEW THAY ĐỔI GẦN ĐÂY
-- =========================================================

CREATE OR REPLACE VIEW V_AUDIT_RECENT_CHANGES AS
SELECT 
    acl.ID AS AUDIT_ID,
    acl.TABLE_NAME,
    acl.RECORD_ID,
    acl.OPERATION,
    acl.CHANGED_COLUMNS,
    acl.PERFORMED_BY,
    u.FULL_NAME AS PERFORMER_NAME,
    acl.PERFORMED_AT,
    acl.JUSTIFICATION,
    acl.CLIENT_IP
FROM AUDIT_CHANGE_LOGS acl
LEFT JOIN USERS u ON u.MAND = acl.PERFORMED_BY
WHERE acl.PERFORMED_AT >= SYSDATE - 7  -- 7 ngày gần đây
ORDER BY acl.PERFORMED_AT DESC;

COMMENT ON VIEW V_AUDIT_RECENT_CHANGES IS 'Các thay đổi trong 7 ngày gần đây';

PROMPT '✓ Đã tạo V_AUDIT_RECENT_CHANGES';

-- =========================================================
-- BƯỚC 5: TẠO VIEW HÀNH ĐỘNG NGHIỆP VỤ
-- =========================================================

CREATE OR REPLACE VIEW V_AUDIT_BUSINESS_ACTIONS AS
SELECT 
    aba.ID AS ACTION_ID,
    aba.ACTION_TYPE,
    aba.ACTION_DESC,
    aba.ENTITY_TYPE,
    aba.ENTITY_ID,
    aba.PERFORMED_BY,
    u.FULL_NAME AS PERFORMER_NAME,
    aba.PERFORMED_AT,
    aba.STATUS,
    aba.ERROR_MESSAGE
FROM AUDIT_BUSINESS_ACTIONS aba
LEFT JOIN USERS u ON u.MAND = aba.PERFORMED_BY
ORDER BY aba.PERFORMED_AT DESC;

COMMENT ON VIEW V_AUDIT_BUSINESS_ACTIONS IS 'Các hành động nghiệp vụ được ứng dụng ghi log';

PROMPT '✓ Đã tạo V_AUDIT_BUSINESS_ACTIONS';

-- =========================================================
-- BƯỚC 6: TẠO VIEW AUDIT TỔNG HỢP
-- =========================================================

CREATE OR REPLACE VIEW V_AUDIT_ALL AS
-- Logs thay đổi
SELECT 
    'CHANGE_LOG' AS SOURCE,
    ID AS AUDIT_ID,
    TABLE_NAME,
    RECORD_ID,
    OPERATION,
    PERFORMED_BY,
    PERFORMED_AT,
    JUSTIFICATION,
    OLD_VALUES,
    NEW_VALUES
FROM AUDIT_CHANGE_LOGS

UNION ALL

-- Hành động nghiệp vụ
SELECT 
    'BUSINESS_ACTION' AS SOURCE,
    ID AS AUDIT_ID,
    ENTITY_TYPE AS TABLE_NAME,
    ENTITY_ID AS RECORD_ID,
    ACTION_TYPE AS OPERATION,
    PERFORMED_BY,
    PERFORMED_AT,
    ACTION_DESC AS JUSTIFICATION,
    NULL AS OLD_VALUES,
    DETAILS AS NEW_VALUES
FROM AUDIT_BUSINESS_ACTIONS;

COMMENT ON VIEW V_AUDIT_ALL IS 'View kết hợp tất cả nguồn audit';

PROMPT '✓ Đã tạo V_AUDIT_ALL';

-- =========================================================
-- BƯỚC 7: TẠO VIEW THEO DÕI PHÊ DUYỆT
-- =========================================================

CREATE OR REPLACE VIEW V_AUDIT_APPROVALS AS
SELECT 
    acl.ID AS AUDIT_ID,
    acl.TABLE_NAME,
    acl.RECORD_ID,
    acl.PERFORMED_BY AS APPROVER_ID,
    u.FULL_NAME AS APPROVER_NAME,
    acl.PERFORMED_AT AS APPROVED_AT,
    acl.JUSTIFICATION AS APPROVAL_REASON,
    acl.OLD_VALUES,
    acl.NEW_VALUES
FROM AUDIT_CHANGE_LOGS acl
LEFT JOIN USERS u ON u.MAND = acl.PERFORMED_BY
WHERE acl.TABLE_NAME IN ('SCORES', 'ACTIVITIES', 'PROOFS')
  AND acl.OPERATION = 'UPDATE'
  AND (
      acl.CHANGED_COLUMNS LIKE '%STATUS%'
      OR acl.CHANGED_COLUMNS LIKE '%APPROVAL%'
      OR acl.CHANGED_COLUMNS LIKE '%APPROVED%'
  )
ORDER BY acl.PERFORMED_AT DESC;

COMMENT ON VIEW V_AUDIT_APPROVALS IS 'Theo dõi tất cả hành động phê duyệt';

PROMPT '✓ Đã tạo V_AUDIT_APPROVALS';

-- =========================================================
-- BƯỚC 8: TẠO VIEW TRUY CẬP DỮ LIỆU NHẠY CẢM (FGA)
-- =========================================================

-- Lưu ý: View này cần quyền SELECT trên DBA_FGA_AUDIT_TRAIL
-- Chạy với SYSDBA: GRANT SELECT ON DBA_FGA_AUDIT_TRAIL TO QLDIEMRENLUYEN;

CREATE OR REPLACE VIEW V_AUDIT_SENSITIVE_ACCESS AS
SELECT 
    'FGA' AS SOURCE,
    fga.TIMESTAMP AS ACCESS_TIME,
    fga.OBJECT_SCHEMA || '.' || fga.OBJECT_NAME AS OBJECT_NAME,
    fga.POLICY_NAME,
    fga.DB_USER,
    fga.USERHOST AS CLIENT_HOST,
    fga.OS_USER,
    fga.STATEMENT_TYPE,
    fga.SQL_TEXT
FROM DBA_FGA_AUDIT_TRAIL fga
WHERE fga.OBJECT_SCHEMA = 'QLDIEMRENLUYEN'
  AND fga.POLICY_NAME LIKE 'FGA_%'
ORDER BY fga.TIMESTAMP DESC;

COMMENT ON VIEW V_AUDIT_SENSITIVE_ACCESS IS 'FGA audit trail cho truy cập dữ liệu nhạy cảm';

PROMPT '✓ Đã tạo V_AUDIT_SENSITIVE_ACCESS';

-- =========================================================
-- XÁC MINH
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'XÁC MINH - Các Audit View';
PROMPT '========================================';

PROMPT '';
PROMPT 'Các views đã tạo:';
SELECT VIEW_NAME
FROM USER_VIEWS
WHERE VIEW_NAME LIKE 'V_AUDIT%'
ORDER BY VIEW_NAME;

PROMPT '';
PROMPT '========================================';
PROMPT '✓ HOÀN THÀNH AUDIT VIEWS!';
PROMPT 'Đã tạo:';
PROMPT '  - V_AUDIT_SCORES_HISTORY (Lịch sử điểm)';
PROMPT '  - V_AUDIT_USER_ACTIVITY (Hoạt động người dùng)';
PROMPT '  - V_AUDIT_DAILY_SUMMARY (Tổng hợp theo ngày)';
PROMPT '  - V_AUDIT_RECENT_CHANGES (Thay đổi gần đây)';
PROMPT '  - V_AUDIT_BUSINESS_ACTIONS (Hành động nghiệp vụ)';
PROMPT '  - V_AUDIT_ALL (Tổng hợp)';
PROMPT '  - V_AUDIT_APPROVALS (Phê duyệt)';
PROMPT '  - V_AUDIT_SENSITIVE_ACCESS (Truy cập nhạy cảm)';
PROMPT '========================================';
