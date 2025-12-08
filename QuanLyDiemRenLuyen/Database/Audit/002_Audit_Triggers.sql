-- =========================================================
-- KIỂM TOÁN - PHẦN TRIGGERS (Chạy với QLDiemRenLuyen)
-- =========================================================
-- Kết nối: QLDiemRenLuyen
-- Mục đích: Tạo triggers ghi lại giá trị OLD/NEW
-- Điều kiện: Chạy các phần trước đó trước!
-- =========================================================

SET SERVEROUTPUT ON;
SET LINESIZE 200;

PROMPT '========================================';
PROMPT 'KIỂM TOÁN - Audit Triggers';
PROMPT 'Đang thực thi với: QLDiemRenLuyen';
PROMPT '========================================';

-- =========================================================
-- BƯỚC 1: TẠO TRIGGER CHO BẢNG SCORES (ĐIỂM)
-- =========================================================

PROMPT '';
PROMPT 'Đang tạo trigger audit cho SCORES...';

CREATE OR REPLACE TRIGGER TRG_AUDIT_SCORES
AFTER INSERT OR UPDATE OR DELETE ON SCORES
FOR EACH ROW
DECLARE
    v_operation VARCHAR2(10);
    v_old_json CLOB;
    v_new_json CLOB;
    v_changed_cols VARCHAR2(1000);
    v_justification VARCHAR2(1000);
    v_record_id VARCHAR2(100);
BEGIN
    -- Lấy lý do từ context (đặt bởi ứng dụng)
    BEGIN
        v_justification := SYS_CONTEXT('AUDIT_CTX', 'JUSTIFICATION');
    EXCEPTION
        WHEN OTHERS THEN v_justification := NULL;
    END;
    
    IF INSERTING THEN
        v_operation := 'INSERT';
        v_record_id := TO_CHAR(:NEW.ID);
        v_new_json := '{"id":' || :NEW.ID || 
                      ',"student_id":"' || :NEW.STUDENT_ID || '"' ||
                      ',"total_score":' || NVL(TO_CHAR(:NEW.TOTAL_SCORE), 'null') ||
                      ',"classification":"' || NVL(:NEW.CLASSIFICATION, '') || '"' ||
                      ',"status":"' || :NEW.STATUS || '"' ||
                      ',"approved_by":"' || NVL(:NEW.APPROVED_BY, '') || '"' ||
                      '}';
    ELSIF UPDATING THEN
        v_operation := 'UPDATE';
        v_record_id := TO_CHAR(:NEW.ID);
        
        -- Xây dựng danh sách cột đã thay đổi
        v_changed_cols := '';
        IF NVL(:OLD.TOTAL_SCORE, -1) != NVL(:NEW.TOTAL_SCORE, -1) THEN
            v_changed_cols := v_changed_cols || 'TOTAL_SCORE,';
        END IF;
        IF NVL(:OLD.CLASSIFICATION, ' ') != NVL(:NEW.CLASSIFICATION, ' ') THEN
            v_changed_cols := v_changed_cols || 'CLASSIFICATION,';
        END IF;
        IF NVL(:OLD.STATUS, ' ') != NVL(:NEW.STATUS, ' ') THEN
            v_changed_cols := v_changed_cols || 'STATUS,';
        END IF;
        IF NVL(:OLD.APPROVED_BY, ' ') != NVL(:NEW.APPROVED_BY, ' ') THEN
            v_changed_cols := v_changed_cols || 'APPROVED_BY,';
        END IF;
        v_changed_cols := RTRIM(v_changed_cols, ',');
        
        v_old_json := '{"id":' || :OLD.ID || 
                      ',"total_score":' || NVL(TO_CHAR(:OLD.TOTAL_SCORE), 'null') ||
                      ',"classification":"' || NVL(:OLD.CLASSIFICATION, '') || '"' ||
                      ',"status":"' || :OLD.STATUS || '"' ||
                      ',"approved_by":"' || NVL(:OLD.APPROVED_BY, '') || '"' ||
                      '}';
        v_new_json := '{"id":' || :NEW.ID || 
                      ',"total_score":' || NVL(TO_CHAR(:NEW.TOTAL_SCORE), 'null') ||
                      ',"classification":"' || NVL(:NEW.CLASSIFICATION, '') || '"' ||
                      ',"status":"' || :NEW.STATUS || '"' ||
                      ',"approved_by":"' || NVL(:NEW.APPROVED_BY, '') || '"' ||
                      '}';
    ELSIF DELETING THEN
        v_operation := 'DELETE';
        v_record_id := TO_CHAR(:OLD.ID);
        v_old_json := '{"id":' || :OLD.ID || 
                      ',"student_id":"' || :OLD.STUDENT_ID || '"' ||
                      ',"total_score":' || NVL(TO_CHAR(:OLD.TOTAL_SCORE), 'null') ||
                      ',"status":"' || :OLD.STATUS || '"' ||
                      '}';
    END IF;
    
    -- Ghi log audit
    INSERT INTO AUDIT_CHANGE_LOGS(
        TABLE_NAME, RECORD_ID, OPERATION,
        OLD_VALUES, NEW_VALUES, CHANGED_COLUMNS,
        PERFORMED_BY, SESSION_USER, OS_USER, CLIENT_IP, CLIENT_HOST,
        JUSTIFICATION
    ) VALUES (
        'SCORES', v_record_id, v_operation,
        v_old_json, v_new_json, v_changed_cols,
        SYS_CONTEXT('AUDIT_CTX', 'USER_ID'),
        SYS_CONTEXT('USERENV', 'SESSION_USER'),
        SYS_CONTEXT('USERENV', 'OS_USER'),
        SYS_CONTEXT('USERENV', 'IP_ADDRESS'),
        SYS_CONTEXT('USERENV', 'HOST'),
        v_justification
    );
END;
/

PROMPT '✓ Đã tạo TRG_AUDIT_SCORES';

-- =========================================================
-- BƯỚC 2: TẠO TRIGGER CHO BẢNG USERS (NGƯỜI DÙNG)
-- =========================================================

PROMPT '';
PROMPT 'Đang tạo trigger audit cho USERS...';

CREATE OR REPLACE TRIGGER TRG_AUDIT_USERS
AFTER INSERT OR UPDATE OR DELETE ON USERS
FOR EACH ROW
DECLARE
    v_operation VARCHAR2(10);
    v_old_json CLOB;
    v_new_json CLOB;
    v_changed_cols VARCHAR2(1000);
    v_justification VARCHAR2(1000);
    v_record_id VARCHAR2(100);
BEGIN
    BEGIN
        v_justification := SYS_CONTEXT('AUDIT_CTX', 'JUSTIFICATION');
    EXCEPTION
        WHEN OTHERS THEN v_justification := NULL;
    END;
    
    IF INSERTING THEN
        v_operation := 'INSERT';
        v_record_id := :NEW.MAND;
        v_new_json := '{"mand":"' || :NEW.MAND || '"' ||
                      ',"email":"' || :NEW.EMAIL || '"' ||
                      ',"full_name":"' || :NEW.FULL_NAME || '"' ||
                      ',"role_name":"' || :NEW.ROLE_NAME || '"' ||
                      ',"is_active":' || :NEW.IS_ACTIVE ||
                      '}';
    ELSIF UPDATING THEN
        v_operation := 'UPDATE';
        v_record_id := :NEW.MAND;
        
        v_changed_cols := '';
        IF NVL(:OLD.ROLE_NAME, ' ') != NVL(:NEW.ROLE_NAME, ' ') THEN
            v_changed_cols := v_changed_cols || 'ROLE_NAME,';
        END IF;
        IF NVL(:OLD.IS_ACTIVE, -1) != NVL(:NEW.IS_ACTIVE, -1) THEN
            v_changed_cols := v_changed_cols || 'IS_ACTIVE,';
        END IF;
        IF NVL(:OLD.EMAIL, ' ') != NVL(:NEW.EMAIL, ' ') THEN
            v_changed_cols := v_changed_cols || 'EMAIL,';
        END IF;
        IF NVL(:OLD.FULL_NAME, ' ') != NVL(:NEW.FULL_NAME, ' ') THEN
            v_changed_cols := v_changed_cols || 'FULL_NAME,';
        END IF;
        v_changed_cols := RTRIM(v_changed_cols, ',');
        
        v_old_json := '{"mand":"' || :OLD.MAND || '"' ||
                      ',"email":"' || :OLD.EMAIL || '"' ||
                      ',"role_name":"' || :OLD.ROLE_NAME || '"' ||
                      ',"is_active":' || :OLD.IS_ACTIVE ||
                      '}';
        v_new_json := '{"mand":"' || :NEW.MAND || '"' ||
                      ',"email":"' || :NEW.EMAIL || '"' ||
                      ',"role_name":"' || :NEW.ROLE_NAME || '"' ||
                      ',"is_active":' || :NEW.IS_ACTIVE ||
                      '}';
    ELSIF DELETING THEN
        v_operation := 'DELETE';
        v_record_id := :OLD.MAND;
        v_old_json := '{"mand":"' || :OLD.MAND || '"' ||
                      ',"email":"' || :OLD.EMAIL || '"' ||
                      ',"role_name":"' || :OLD.ROLE_NAME || '"' ||
                      '}';
    END IF;
    
    INSERT INTO AUDIT_CHANGE_LOGS(
        TABLE_NAME, RECORD_ID, OPERATION,
        OLD_VALUES, NEW_VALUES, CHANGED_COLUMNS,
        PERFORMED_BY, SESSION_USER, OS_USER, CLIENT_IP, CLIENT_HOST,
        JUSTIFICATION
    ) VALUES (
        'USERS', v_record_id, v_operation,
        v_old_json, v_new_json, v_changed_cols,
        SYS_CONTEXT('AUDIT_CTX', 'USER_ID'),
        SYS_CONTEXT('USERENV', 'SESSION_USER'),
        SYS_CONTEXT('USERENV', 'OS_USER'),
        SYS_CONTEXT('USERENV', 'IP_ADDRESS'),
        SYS_CONTEXT('USERENV', 'HOST'),
        v_justification
    );
END;
/

PROMPT '✓ Đã tạo TRG_AUDIT_USERS';

-- =========================================================
-- BƯỚC 3: TẠO TRIGGER CHO BẢNG FEEDBACKS (PHẢN HỒI)
-- =========================================================

PROMPT '';
PROMPT 'Đang tạo trigger audit cho FEEDBACKS...';

CREATE OR REPLACE TRIGGER TRG_AUDIT_FEEDBACKS
AFTER INSERT OR UPDATE ON FEEDBACKS
FOR EACH ROW
DECLARE
    v_operation VARCHAR2(10);
    v_old_json CLOB;
    v_new_json CLOB;
    v_changed_cols VARCHAR2(1000);
    v_justification VARCHAR2(1000);
BEGIN
    BEGIN
        v_justification := SYS_CONTEXT('AUDIT_CTX', 'JUSTIFICATION');
    EXCEPTION
        WHEN OTHERS THEN v_justification := NULL;
    END;
    
    IF INSERTING THEN
        v_operation := 'INSERT';
        v_new_json := '{"id":"' || :NEW.ID || '"' ||
                      ',"student_id":"' || :NEW.STUDENT_ID || '"' ||
                      ',"title":"' || REPLACE(:NEW.TITLE, '"', '\"') || '"' ||
                      ',"status":"' || :NEW.STATUS || '"' ||
                      '}';
    ELSIF UPDATING THEN
        v_operation := 'UPDATE';
        
        v_changed_cols := '';
        IF NVL(:OLD.STATUS, ' ') != NVL(:NEW.STATUS, ' ') THEN
            v_changed_cols := v_changed_cols || 'STATUS,';
        END IF;
        IF NVL(DBMS_LOB.GETLENGTH(:OLD.RESPONSE), 0) != NVL(DBMS_LOB.GETLENGTH(:NEW.RESPONSE), 0) THEN
            v_changed_cols := v_changed_cols || 'RESPONSE,';
        END IF;
        v_changed_cols := RTRIM(v_changed_cols, ',');
        
        v_old_json := '{"id":"' || :OLD.ID || '"' ||
                      ',"status":"' || :OLD.STATUS || '"' ||
                      ',"has_response":' || CASE WHEN :OLD.RESPONSE IS NOT NULL THEN 'true' ELSE 'false' END ||
                      '}';
        v_new_json := '{"id":"' || :NEW.ID || '"' ||
                      ',"status":"' || :NEW.STATUS || '"' ||
                      ',"has_response":' || CASE WHEN :NEW.RESPONSE IS NOT NULL THEN 'true' ELSE 'false' END ||
                      '}';
    END IF;
    
    INSERT INTO AUDIT_CHANGE_LOGS(
        TABLE_NAME, RECORD_ID, OPERATION,
        OLD_VALUES, NEW_VALUES, CHANGED_COLUMNS,
        PERFORMED_BY, SESSION_USER, CLIENT_IP,
        JUSTIFICATION
    ) VALUES (
        'FEEDBACKS', :NEW.ID, v_operation,
        v_old_json, v_new_json, v_changed_cols,
        SYS_CONTEXT('AUDIT_CTX', 'USER_ID'),
        SYS_CONTEXT('USERENV', 'SESSION_USER'),
        SYS_CONTEXT('USERENV', 'IP_ADDRESS'),
        v_justification
    );
END;
/

PROMPT '✓ Đã tạo TRG_AUDIT_FEEDBACKS';

-- =========================================================
-- BƯỚC 4: TẠO TRIGGER CHO BẢNG PROOFS (MINH CHỨNG)
-- =========================================================

PROMPT '';
PROMPT 'Đang tạo trigger audit cho PROOFS...';

CREATE OR REPLACE TRIGGER TRG_AUDIT_PROOFS
AFTER UPDATE ON PROOFS
FOR EACH ROW
DECLARE
    v_old_json CLOB;
    v_new_json CLOB;
    v_justification VARCHAR2(1000);
BEGIN
    -- Chỉ audit khi STATUS thay đổi
    IF NVL(:OLD.STATUS, ' ') = NVL(:NEW.STATUS, ' ') THEN
        RETURN;
    END IF;
    
    BEGIN
        v_justification := SYS_CONTEXT('AUDIT_CTX', 'JUSTIFICATION');
    EXCEPTION
        WHEN OTHERS THEN v_justification := NULL;
    END;
    
    v_old_json := '{"id":"' || :OLD.ID || '"' ||
                  ',"status":"' || :OLD.STATUS || '"' ||
                  '}';
    v_new_json := '{"id":"' || :NEW.ID || '"' ||
                  ',"status":"' || :NEW.STATUS || '"' ||
                  '}';
    
    INSERT INTO AUDIT_CHANGE_LOGS(
        TABLE_NAME, RECORD_ID, OPERATION,
        OLD_VALUES, NEW_VALUES, CHANGED_COLUMNS,
        PERFORMED_BY, SESSION_USER, CLIENT_IP,
        JUSTIFICATION
    ) VALUES (
        'PROOFS', :NEW.ID, 'UPDATE',
        v_old_json, v_new_json, 'STATUS',
        SYS_CONTEXT('AUDIT_CTX', 'USER_ID'),
        SYS_CONTEXT('USERENV', 'SESSION_USER'),
        SYS_CONTEXT('USERENV', 'IP_ADDRESS'),
        v_justification
    );
END;
/

PROMPT '✓ Đã tạo TRG_AUDIT_PROOFS';

-- =========================================================
-- BƯỚC 5: TẠO TRIGGER CHO BẢNG ACTIVITIES (HOẠT ĐỘNG)
-- =========================================================

PROMPT '';
PROMPT 'Đang tạo trigger audit cho ACTIVITIES...';

CREATE OR REPLACE TRIGGER TRG_AUDIT_ACTIVITIES
AFTER UPDATE ON ACTIVITIES
FOR EACH ROW
DECLARE
    v_old_json CLOB;
    v_new_json CLOB;
    v_changed_cols VARCHAR2(1000);
    v_justification VARCHAR2(1000);
BEGIN
    -- Chỉ audit khi APPROVAL_STATUS hoặc STATUS thay đổi
    IF NVL(:OLD.APPROVAL_STATUS, ' ') = NVL(:NEW.APPROVAL_STATUS, ' ') 
       AND NVL(:OLD.STATUS, ' ') = NVL(:NEW.STATUS, ' ') THEN
        RETURN;
    END IF;
    
    BEGIN
        v_justification := SYS_CONTEXT('AUDIT_CTX', 'JUSTIFICATION');
    EXCEPTION
        WHEN OTHERS THEN v_justification := NULL;
    END;
    
    v_changed_cols := '';
    IF NVL(:OLD.APPROVAL_STATUS, ' ') != NVL(:NEW.APPROVAL_STATUS, ' ') THEN
        v_changed_cols := v_changed_cols || 'APPROVAL_STATUS,';
    END IF;
    IF NVL(:OLD.STATUS, ' ') != NVL(:NEW.STATUS, ' ') THEN
        v_changed_cols := v_changed_cols || 'STATUS,';
    END IF;
    v_changed_cols := RTRIM(v_changed_cols, ',');
    
    v_old_json := '{"id":"' || :OLD.ID || '"' ||
                  ',"title":"' || REPLACE(SUBSTR(:OLD.TITLE, 1, 100), '"', '\"') || '"' ||
                  ',"approval_status":"' || :OLD.APPROVAL_STATUS || '"' ||
                  ',"status":"' || :OLD.STATUS || '"' ||
                  '}';
    v_new_json := '{"id":"' || :NEW.ID || '"' ||
                  ',"title":"' || REPLACE(SUBSTR(:NEW.TITLE, 1, 100), '"', '\"') || '"' ||
                  ',"approval_status":"' || :NEW.APPROVAL_STATUS || '"' ||
                  ',"status":"' || :NEW.STATUS || '"' ||
                  ',"approved_by":"' || NVL(:NEW.APPROVED_BY, '') || '"' ||
                  '}';
    
    INSERT INTO AUDIT_CHANGE_LOGS(
        TABLE_NAME, RECORD_ID, OPERATION,
        OLD_VALUES, NEW_VALUES, CHANGED_COLUMNS,
        PERFORMED_BY, SESSION_USER, CLIENT_IP,
        JUSTIFICATION
    ) VALUES (
        'ACTIVITIES', :NEW.ID, 'UPDATE',
        v_old_json, v_new_json, v_changed_cols,
        SYS_CONTEXT('AUDIT_CTX', 'USER_ID'),
        SYS_CONTEXT('USERENV', 'SESSION_USER'),
        SYS_CONTEXT('USERENV', 'IP_ADDRESS'),
        v_justification
    );
END;
/

PROMPT '✓ Đã tạo TRG_AUDIT_ACTIVITIES';

-- =========================================================
-- BƯỚC 6: TẠO TRIGGER CHO BẢNG CLASS_LECTURER_ASSIGNMENTS
-- =========================================================

PROMPT '';
PROMPT 'Đang tạo trigger audit cho CLASS_LECTURER_ASSIGNMENTS...';

CREATE OR REPLACE TRIGGER TRG_AUDIT_CLASS_ASSIGNMENTS
AFTER INSERT OR UPDATE ON CLASS_LECTURER_ASSIGNMENTS
FOR EACH ROW
DECLARE
    v_operation VARCHAR2(10);
    v_old_json CLOB;
    v_new_json CLOB;
    v_justification VARCHAR2(1000);
BEGIN
    BEGIN
        v_justification := SYS_CONTEXT('AUDIT_CTX', 'JUSTIFICATION');
    EXCEPTION
        WHEN OTHERS THEN v_justification := NULL;
    END;
    
    IF INSERTING THEN
        v_operation := 'INSERT';
        v_new_json := '{"id":"' || :NEW.ID || '"' ||
                      ',"class_id":"' || :NEW.CLASS_ID || '"' ||
                      ',"lecturer_id":"' || :NEW.LECTURER_ID || '"' ||
                      ',"assigned_by":"' || :NEW.ASSIGNED_BY || '"' ||
                      ',"is_active":' || :NEW.IS_ACTIVE ||
                      '}';
    ELSIF UPDATING THEN
        v_operation := 'UPDATE';
        v_old_json := '{"id":"' || :OLD.ID || '"' ||
                      ',"lecturer_id":"' || :OLD.LECTURER_ID || '"' ||
                      ',"is_active":' || :OLD.IS_ACTIVE ||
                      '}';
        v_new_json := '{"id":"' || :NEW.ID || '"' ||
                      ',"lecturer_id":"' || :NEW.LECTURER_ID || '"' ||
                      ',"is_active":' || :NEW.IS_ACTIVE ||
                      ',"removed_by":"' || NVL(:NEW.REMOVED_BY, '') || '"' ||
                      '}';
    END IF;
    
    INSERT INTO AUDIT_CHANGE_LOGS(
        TABLE_NAME, RECORD_ID, OPERATION,
        OLD_VALUES, NEW_VALUES,
        PERFORMED_BY, SESSION_USER, CLIENT_IP,
        JUSTIFICATION
    ) VALUES (
        'CLASS_LECTURER_ASSIGNMENTS', :NEW.ID, v_operation,
        v_old_json, v_new_json,
        SYS_CONTEXT('AUDIT_CTX', 'USER_ID'),
        SYS_CONTEXT('USERENV', 'SESSION_USER'),
        SYS_CONTEXT('USERENV', 'IP_ADDRESS'),
        v_justification
    );
END;
/

PROMPT '✓ Đã tạo TRG_AUDIT_CLASS_ASSIGNMENTS';

-- =========================================================
-- XÁC MINH
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'XÁC MINH - Các Audit Trigger';
PROMPT '========================================';

PROMPT '';
PROMPT 'Các trigger đã tạo:';
SELECT TRIGGER_NAME, TABLE_NAME, STATUS
FROM USER_TRIGGERS
WHERE TRIGGER_NAME LIKE 'TRG_AUDIT%'
ORDER BY TRIGGER_NAME;

PROMPT '';
PROMPT '========================================';
PROMPT '✓ HOÀN THÀNH AUDIT TRIGGERS!';
PROMPT 'Đã tạo trigger cho:';
PROMPT '  - SCORES (Điểm)';
PROMPT '  - USERS (Người dùng)';
PROMPT '  - FEEDBACKS (Phản hồi)';
PROMPT '  - PROOFS (Minh chứng)';
PROMPT '  - ACTIVITIES (Hoạt động)';
PROMPT '  - CLASS_LECTURER_ASSIGNMENTS (Phân công CVHT)';
PROMPT '========================================';
