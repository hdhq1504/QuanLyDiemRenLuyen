-- =========================================================
-- KIỂM TOÁN - KIỂM THỬ (Chạy với QLDiemRenLuyen)
-- =========================================================
-- Kết nối: QLDiemRenLuyen
-- Mục đích: Kiểm thử tất cả chức năng auditing
-- Điều kiện: Chạy các phần trước đó trước!
-- =========================================================

SET SERVEROUTPUT ON;
SET LINESIZE 200;

PROMPT '========================================';
PROMPT 'KIỂM TOÁN - Kiểm thử';
PROMPT 'Đang thực thi với: QLDiemRenLuyen';
PROMPT '========================================';

-- =========================================================
-- KIỂM THỬ 1: XÁC MINH CÀI ĐẶT AUDIT_TRAIL
-- =========================================================

PROMPT '';
PROMPT '=== KIỂM THỬ 1: Xác minh cài đặt AUDIT_TRAIL ===';

DECLARE
    v_audit_trail VARCHAR2(100);
BEGIN
    SELECT VALUE INTO v_audit_trail
    FROM V$PARAMETER
    WHERE NAME = 'audit_trail';
    
    DBMS_OUTPUT.PUT_LINE('AUDIT_TRAIL = ' || v_audit_trail);
    
    IF v_audit_trail LIKE '%DB%' THEN
        DBMS_OUTPUT.PUT_LINE('✓ Standard Auditing đã bật');
    ELSE
        DBMS_OUTPUT.PUT_LINE('⚠ Standard Auditing có thể chưa được bật đầy đủ');
    END IF;
END;
/

-- =========================================================
-- KIỂM THỬ 2: XÁC MINH CHÍNH SÁCH FGA
-- =========================================================

PROMPT '';
PROMPT '=== KIỂM THỬ 2: Xác minh chính sách FGA ===';

SELECT OBJECT_NAME, POLICY_NAME, ENABLED
FROM DBA_AUDIT_POLICIES
WHERE OBJECT_SCHEMA = 'QLDIEMRENLUYEN'
ORDER BY OBJECT_NAME;

-- =========================================================
-- KIỂM THỬ 3: KIỂM THỬ TRIGGER AUDIT VỚI LÝ DO
-- =========================================================

PROMPT '';
PROMPT '=== KIỂM THỬ 3: Kiểm thử Trigger Audit ===';

DECLARE
    v_test_score_id NUMBER;
    v_audit_count NUMBER;
BEGIN
    -- Đặt audit context với lý do
    PKG_AUDIT_CONTEXT.SET_CONTEXT(
        p_user_id       => 'TEST_ADMIN',
        p_justification => 'Kiểm thử chức năng audit'
    );
    DBMS_OUTPUT.PUT_LINE('✓ Đã đặt audit context');
    
    -- Lấy ID điểm để kiểm thử (nếu có)
    BEGIN
        SELECT ID INTO v_test_score_id
        FROM SCORES
        WHERE ROWNUM = 1;
        
        -- Cập nhật điểm
        UPDATE SCORES
        SET TOTAL_SCORE = TOTAL_SCORE
        WHERE ID = v_test_score_id;
        
        DBMS_OUTPUT.PUT_LINE('✓ Đã cập nhật score ID: ' || v_test_score_id);
        
        -- Kiểm tra audit log
        SELECT COUNT(*)
        INTO v_audit_count
        FROM AUDIT_CHANGE_LOGS
        WHERE TABLE_NAME = 'SCORES'
          AND RECORD_ID = TO_CHAR(v_test_score_id);
        
        DBMS_OUTPUT.PUT_LINE('✓ Số bản ghi audit cho score này: ' || v_audit_count);
        
        ROLLBACK;
        DBMS_OUTPUT.PUT_LINE('✓ Đã rollback thay đổi kiểm thử');
        
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            DBMS_OUTPUT.PUT_LINE('Không tìm thấy scores để kiểm thử');
    END;
    
    -- Xóa context
    PKG_AUDIT_CONTEXT.CLEAR_CONTEXT;
    DBMS_OUTPUT.PUT_LINE('✓ Đã xóa audit context');
END;
/

-- =========================================================
-- KIỂM THỬ 4: KIỂM THỬ FGA (AUDIT SELECT)
-- =========================================================

PROMPT '';
PROMPT '=== KIỂM THỬ 4: Kiểm thử FGA (SELECT Audit) ===';

DECLARE
    v_count NUMBER;
    v_phone VARCHAR2(50);
BEGIN
    -- Select dữ liệu nhạy cảm để kích hoạt FGA
    BEGIN
        SELECT PHONE INTO v_phone
        FROM STUDENTS
        WHERE ROWNUM = 1;
        
        DBMS_OUTPUT.PUT_LINE('✓ Đã select dữ liệu nhạy cảm (PHONE)');
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            DBMS_OUTPUT.PUT_LINE('Không tìm thấy sinh viên');
    END;
    
    -- Lưu ý: FGA audit có thể có độ trễ
    DBMS_OUTPUT.PUT_LINE('Lưu ý: Kiểm tra DBA_FGA_AUDIT_TRAIL để xem các bản ghi FGA');
END;
/

-- =========================================================
-- KIỂM THỬ 5: KIỂM THỬ GHI LOG HÀNH ĐỘNG NGHIỆP VỤ
-- =========================================================

PROMPT '';
PROMPT '=== KIỂM THỬ 5: Kiểm thử ghi log hành động nghiệp vụ ===';

BEGIN
    SP_LOG_BUSINESS_ACTION(
        p_action_type   => 'TEST_ACTION',
        p_action_desc   => 'Kiểm thử ghi log hành động nghiệp vụ',
        p_entity_type   => 'SYSTEM',
        p_entity_id     => 'TEST_001',
        p_performed_by  => 'TEST_ADMIN',
        p_details       => '{"test": true, "purpose": "verification"}',
        p_status        => 'SUCCESS'
    );
    DBMS_OUTPUT.PUT_LINE('✓ Đã ghi log hành động nghiệp vụ');
END;
/

-- Xác minh
SELECT ACTION_TYPE, ACTION_DESC, PERFORMED_BY, STATUS
FROM AUDIT_BUSINESS_ACTIONS
WHERE ACTION_TYPE = 'TEST_ACTION'
ORDER BY PERFORMED_AT DESC
FETCH FIRST 1 ROW ONLY;

-- =========================================================
-- KIỂM THỬ 6: KIỂM THỬ CÁC VIEWS
-- =========================================================

PROMPT '';
PROMPT '=== KIỂM THỬ 6: Kiểm thử các Audit Views ===';

PROMPT '';
PROMPT 'Thay đổi gần đây (7 ngày):';
SELECT TABLE_NAME, OPERATION, COUNT(*) AS CNT
FROM V_AUDIT_RECENT_CHANGES
GROUP BY TABLE_NAME, OPERATION
ORDER BY TABLE_NAME;

PROMPT '';
PROMPT 'Tổng hợp theo ngày:';
SELECT AUDIT_DATE, TABLE_NAME, CHANGE_COUNT
FROM V_AUDIT_DAILY_SUMMARY
WHERE AUDIT_DATE >= SYSDATE - 1
ORDER BY AUDIT_DATE DESC, TABLE_NAME;

-- =========================================================
-- KIỂM THỬ 7: KIỂM THỬ LỊCH SỬ BẢN GHI
-- =========================================================

PROMPT '';
PROMPT '=== KIỂM THỬ 7: Kiểm thử lịch sử bản ghi ===';

DECLARE
    v_cursor SYS_REFCURSOR;
    v_id VARCHAR2(32);
    v_operation VARCHAR2(10);
    v_performed_at TIMESTAMP;
    v_justification VARCHAR2(1000);
BEGIN
    -- Lấy lịch sử cho một score (nếu có)
    BEGIN
        SP_GET_RECORD_HISTORY(
            p_table_name => 'SCORES',
            p_record_id  => '1',
            p_result     => v_cursor
        );
        
        DBMS_OUTPUT.PUT_LINE('Lịch sử cho bản ghi SCORES 1:');
        
        LOOP
            FETCH v_cursor INTO v_id, v_operation, v_performed_at, v_justification,
                                v_id, v_id, v_id, v_id;
            EXIT WHEN v_cursor%NOTFOUND;
            DBMS_OUTPUT.PUT_LINE('  ' || v_operation || ' lúc ' || v_performed_at);
        END LOOP;
        
        CLOSE v_cursor;
    EXCEPTION
        WHEN OTHERS THEN
            DBMS_OUTPUT.PUT_LINE('Không thể lấy lịch sử: ' || SQLERRM);
    END;
END;
/

-- =========================================================
-- TÓM TẮT
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'TÓM TẮT KIỂM THỬ AUDIT';
PROMPT '========================================';

PROMPT '';
PROMPT 'Cấu hình Audit:';

SELECT 'Bảng Audit' AS COMPONENT, COUNT(*) AS COUNT
FROM USER_TABLES WHERE TABLE_NAME LIKE 'AUDIT%'
UNION ALL
SELECT 'Trigger Audit', COUNT(*)
FROM USER_TRIGGERS WHERE TRIGGER_NAME LIKE 'TRG_AUDIT%'
UNION ALL
SELECT 'View Audit', COUNT(*)
FROM USER_VIEWS WHERE VIEW_NAME LIKE 'V_AUDIT%'
UNION ALL
SELECT 'Change Logs', COUNT(*)
FROM AUDIT_CHANGE_LOGS
UNION ALL
SELECT 'Business Actions', COUNT(*)
FROM AUDIT_BUSINESS_ACTIONS;

PROMPT '';
PROMPT 'Trạng thái Trigger:';
SELECT TRIGGER_NAME, STATUS
FROM USER_TRIGGERS
WHERE TRIGGER_NAME LIKE 'TRG_AUDIT%'
ORDER BY TRIGGER_NAME;

PROMPT '';
PROMPT '========================================';
PROMPT '✓ HOÀN THÀNH KIỂM THỬ AUDITING!';
PROMPT '========================================';
PROMPT '';
PROMPT 'Tóm tắt:';
PROMPT '1. Standard Audit: AUDIT_TRAIL=DB,EXTENDED';
PROMPT '2. Chính sách FGA: STUDENTS, SCORES, FEEDBACKS, USERS';
PROMPT '3. Triggers: SCORES, USERS, FEEDBACKS, v.v.';
PROMPT '4. Views: V_AUDIT_* để báo cáo';
PROMPT '';
PROMPT 'Sử dụng trong ứng dụng:';
PROMPT '  // Trước khi thay đổi:';
PROMPT '  EXEC SP_SET_AUDIT_JUSTIFICATION(user_id, reason);';
PROMPT '  // Sau khi thay đổi:';
PROMPT '  EXEC SP_CLEAR_AUDIT_CONTEXT;';
PROMPT '';
PROMPT '  // Ghi log hành động nghiệp vụ:';
PROMPT '  EXEC SP_LOG_BUSINESS_ACTION(type, desc, ...);';
PROMPT '';
PROMPT '  // Lấy lịch sử bản ghi:';
PROMPT '  EXEC SP_GET_RECORD_HISTORY(table, id, cursor);';
PROMPT '========================================';
