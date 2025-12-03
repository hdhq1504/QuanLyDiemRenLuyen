-- =========================================================
-- FEATURE 3: Encrypted Feedback Content
-- Migration Script: 003_ADD_ENCRYPTED_FEEDBACKS
-- Description: Mã hóa nội dung phúc khảo nhạy cảm
-- Author: Thành viên 3
-- Date: 2025-12-02
-- =========================================================

-- =========================================================
-- 1) Thêm columns encrypted vào bảng FEEDBACKS
-- =========================================================
ALTER TABLE FEEDBACKS ADD (
    CONTENT_ENCRYPTED     CLOB,              -- Nội dung mã hóa RSA
    RESPONSE_ENCRYPTED    CLOB,              -- Phản hồi mã hóa RSA
    IS_ENCRYPTED          NUMBER(1) DEFAULT 0,  -- 1 = encrypted, 0 = plaintext
    ENCRYPTION_KEY_ID     VARCHAR2(32),      -- FK to ENCRYPTION_KEYS
    ALLOWED_READERS       CLOB,              -- JSON array user IDs được phép đọc
    ENCRYPTED_AT          TIMESTAMP,         -- Thời điểm mã hóa
    ENCRYPTED_BY          VARCHAR2(50),      -- User ID người mã hóa
    CONSTRAINT CK_FEEDBACK_ENCRYPTED CHECK (IS_ENCRYPTED IN (0,1)),
    CONSTRAINT FK_FEEDBACK_ENCKEY FOREIGN KEY (ENCRYPTION_KEY_ID) 
        REFERENCES ENCRYPTION_KEYS(ID)
);

-- Comments
COMMENT ON COLUMN FEEDBACKS.CONTENT_ENCRYPTED IS 'Nội dung phúc khảo đã mã hóa RSA';
COMMENT ON COLUMN FEEDBACKS.RESPONSE_ENCRYPTED IS 'Phản hồi đã mã hóa RSA';
COMMENT ON COLUMN FEEDBACKS.IS_ENCRYPTED IS '1 = dữ liệu đã mã hóa';
COMMENT ON COLUMN FEEDBACKS.ALLOWED_READERS IS 'JSON array các user ID được phép đọc';

-- Indexes
CREATE INDEX IX_FEEDBACK_ENCRYPTED ON FEEDBACKS(IS_ENCRYPTED);
CREATE INDEX IX_FEEDBACK_ENCKEY ON FEEDBACKS(ENCRYPTION_KEY_ID);

-- =========================================================
-- 2) Tạo bảng FEEDBACK_ACCESS_LOG
-- Ghi log mọi lần truy cập feedback encrypted
-- =========================================================
CREATE TABLE FEEDBACK_ACCESS_LOG (
    ID                VARCHAR2(32) DEFAULT RAWTOHEX(SYS_GUID()) PRIMARY KEY,
    FEEDBACK_ID       VARCHAR2(32) NOT NULL,
    ACCESSED_BY       VARCHAR2(50) NOT NULL,
    ACCESS_TIME       TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
    ACCESS_TYPE       VARCHAR2(20) NOT NULL,  -- READ, WRITE, DECRYPT, ACCESS_DENIED
    ACCESS_RESULT     VARCHAR2(20),  -- SUCCESS, DENIED
    IP_ADDRESS        VARCHAR2(50),
    USER_AGENT        VARCHAR2(200),
    NOTES             VARCHAR2(500),
    CONSTRAINT FK_ACCESS_LOG_FEEDBACK FOREIGN KEY (FEEDBACK_ID) 
        REFERENCES FEEDBACKS(ID) ON DELETE CASCADE,
    CONSTRAINT CK_ACCESS_TYPE CHECK (ACCESS_TYPE IN ('READ', 'WRITE', 'DECRYPT', 'ACCESS_DENIED'))
);

-- Index
CREATE INDEX IX_ACCESS_FEEDBACK ON FEEDBACK_ACCESS_LOG(FEEDBACK_ID);
CREATE INDEX IX_ACCESS_USER ON FEEDBACK_ACCESS_LOG(ACCESSED_BY);
CREATE INDEX IX_ACCESS_TIME ON FEEDBACK_ACCESS_LOG(ACCESS_TIME);

-- =========================================================
-- 3) ORACLE PACKAGE: PKG_FEEDBACK_ENCRYPTION
-- Package xử lý encryption cho feedbacks
-- =========================================================

CREATE OR REPLACE PACKAGE PKG_FEEDBACK_ENCRYPTION AS
    -- Kiểm tra user có quyền đọc feedback không
    FUNCTION CAN_READ_FEEDBACK(
        p_feedback_id VARCHAR2,
        p_user_id VARCHAR2
    ) RETURN NUMBER; -- 1 = allowed, 0 = denied
    
    -- Thêm user vào allowed readers
    PROCEDURE ADD_ALLOWED_READER(
        p_feedback_id VARCHAR2,
        p_user_id VARCHAR2
    );
    
    -- Xóa user khỏi allowed readers
    PROCEDURE REMOVE_ALLOWED_READER(
        p_feedback_id VARCHAR2,
        p_user_id VARCHAR2
    );
    
    -- Log feedback access
    PROCEDURE LOG_FEEDBACK_ACCESS(
        p_feedback_id VARCHAR2,
        p_accessed_by VARCHAR2,
        p_access_type VARCHAR2,
        p_result VARCHAR2,
        p_notes VARCHAR2 DEFAULT NULL
    );
    
    -- Get allowed readers list
    FUNCTION GET_ALLOWED_READERS(
        p_feedback_id VARCHAR2
    ) RETURN CLOB;
    
END PKG_FEEDBACK_ENCRYPTION;
/

CREATE OR REPLACE PACKAGE BODY PKG_FEEDBACK_ENCRYPTION AS
    
    -- Kiểm tra user có quyền đọc feedback không
    FUNCTION CAN_READ_FEEDBACK(
        p_feedback_id VARCHAR2,
        p_user_id VARCHAR2
    ) RETURN NUMBER IS
        v_is_encrypted NUMBER;
        v_allowed_readers CLOB;
        v_student_id VARCHAR2(50);
        v_is_owner NUMBER := 0;
    BEGIN
        -- Lấy thông tin feedback
        SELECT IS_ENCRYPTED, ALLOWED_READERS, STUDENT_ID
        INTO v_is_encrypted, v_allowed_readers, v_student_id
        FROM FEEDBACKS
        WHERE ID = p_feedback_id;
        
        -- Nếu không mã hóa → cho phép tất cả admin/lecturer
        IF v_is_encrypted = 0 THEN
            RETURN 1;
        END IF;
        
        -- Kiểm tra owner
        IF v_student_id = p_user_id THEN
            v_is_owner := 1;
        END IF;
        
        -- Kiểm tra trong allowed_readers (JSON array)
        -- Format: ["USER1", "USER2", ...]
        IF v_allowed_readers IS NOT NULL AND 
           (INSTR(v_allowed_readers, '"' || p_user_id || '"') > 0 OR v_is_owner = 1) THEN
            RETURN 1;
        END IF;
        
        RETURN 0;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RETURN 0;
        WHEN OTHERS THEN
            DBMS_OUTPUT.PUT_LINE('Error checking access: ' || SQLERRM);
            RETURN 0;
    END CAN_READ_FEEDBACK;
    
    -- Thêm user vào allowed readers
    PROCEDURE ADD_ALLOWED_READER(
        p_feedback_id VARCHAR2,
        p_user_id VARCHAR2
    ) IS
        v_readers CLOB;
        v_new_readers CLOB;
    BEGIN
        -- Lấy danh sách hiện tại
        SELECT NVL(ALLOWED_READERS, '[]')
        INTO v_readers
        FROM FEEDBACKS
        WHERE ID = p_feedback_id;
        
        -- Kiểm tra đã tồn tại chưa
        IF INSTR(v_readers, '"' || p_user_id || '"') > 0 THEN
            RETURN; -- Đã tồn tại
        END IF;
        
        -- Thêm user mới
        IF v_readers = '[]' THEN
            v_new_readers := '["' || p_user_id || '"]';
        ELSE
            -- Remove closing bracket, add user, add bracket
            v_new_readers := SUBSTR(v_readers, 1, LENGTH(v_readers)-1) || 
                           ',"' || p_user_id || '"]';
        END IF;
        
        -- Update
        UPDATE FEEDBACKS
        SET ALLOWED_READERS = v_new_readers
        WHERE ID = p_feedback_id;
        
        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            DBMS_OUTPUT.PUT_LINE('Error adding reader: ' || SQLERRM);
    END ADD_ALLOWED_READER;
    
    -- Xóa user khỏi allowed readers
    PROCEDURE REMOVE_ALLOWED_READER(
        p_feedback_id VARCHAR2,
        p_user_id VARCHAR2
    ) IS
        v_readers CLOB;
        v_new_readers CLOB;
    BEGIN
        SELECT ALLOWED_READERS
        INTO v_readers
        FROM FEEDBACKS
        WHERE ID = p_feedback_id;
        
        IF v_readers IS NULL THEN
            RETURN;
        END IF;
        
        -- Remove user (simple replace - trong production nên dùng JSON parser)
        v_new_readers := REPLACE(v_readers, ',"' || p_user_id || '"', '');
        v_new_readers := REPLACE(v_new_readers, '"' || p_user_id || '",', '');
        v_new_readers := REPLACE(v_new_readers, '"' || p_user_id || '"', '');
        
        UPDATE FEEDBACKS
        SET ALLOWED_READERS = v_new_readers
        WHERE ID = p_feedback_id;
        
        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            DBMS_OUTPUT.PUT_LINE('Error removing reader: ' || SQLERRM);
    END REMOVE_ALLOWED_READER;
    
    -- Log feedback access
    PROCEDURE LOG_FEEDBACK_ACCESS(
        p_feedback_id VARCHAR2,
        p_accessed_by VARCHAR2,
        p_access_type VARCHAR2,
        p_result VARCHAR2,
        p_notes VARCHAR2 DEFAULT NULL
    ) IS
    BEGIN
        INSERT INTO FEEDBACK_ACCESS_LOG (
            FEEDBACK_ID,
            ACCESSED_BY,
            ACCESS_TYPE,
            ACCESS_RESULT,
            NOTES
        ) VALUES (
            p_feedback_id,
            p_accessed_by,
            p_access_type,
            p_result,
            p_notes
        );
        
        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            DBMS_OUTPUT.PUT_LINE('Error logging access: ' || SQLERRM);
    END LOG_FEEDBACK_ACCESS;
    
    -- Get allowed readers list
    FUNCTION GET_ALLOWED_READERS(
        p_feedback_id VARCHAR2
    ) RETURN CLOB IS
        v_readers CLOB;
    BEGIN
        SELECT NVL(ALLOWED_READERS, '[]')
        INTO v_readers
        FROM FEEDBACKS
        WHERE ID = p_feedback_id;
        
        RETURN v_readers;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RETURN '[]';
        WHEN OTHERS THEN
            RETURN '[]';
    END GET_ALLOWED_READERS;

END PKG_FEEDBACK_ENCRYPTION;
/

-- Grant permissions
GRANT EXECUTE ON PKG_FEEDBACK_ENCRYPTION TO PUBLIC;

-- =========================================================
-- 4) Verification
-- =========================================================
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    NULLABLE
FROM USER_TAB_COLUMNS
WHERE TABLE_NAME = 'FEEDBACKS'
AND COLUMN_NAME IN ('CONTENT_ENCRYPTED', 'RESPONSE_ENCRYPTED', 'IS_ENCRYPTED', 'ALLOWED_READERS', 'ENCRYPTED_AT')
ORDER BY COLUMN_ID;

-- Kiểm tra bảng access log
SELECT COUNT(*) as ACCESS_LOG_EXISTS 
FROM USER_TABLES 
WHERE TABLE_NAME = 'FEEDBACK_ACCESS_LOG';

-- Kiểm tra package
SELECT 
    OBJECT_NAME,
    OBJECT_TYPE,
    STATUS
FROM USER_OBJECTS
WHERE OBJECT_NAME = 'PKG_FEEDBACK_ENCRYPTION';

PROMPT 'Migration 003_ADD_ENCRYPTED_FEEDBACKS completed successfully!';
