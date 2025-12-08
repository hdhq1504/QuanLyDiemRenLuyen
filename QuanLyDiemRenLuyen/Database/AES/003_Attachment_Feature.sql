-- ============================================================================
-- Tính năng AES 2: Mã hóa Đường dẫn File cho PROOFS và FEEDBACK_ATTACHMENTS
-- Mã hóa đường dẫn file lưu trữ để bảo mật
-- ============================================================================

-- ============================================================================
-- Gói Mã hóa Đường dẫn File
-- ============================================================================

CREATE OR REPLACE PACKAGE PKG_FILE_PATH_CRYPTO AS
    
    -- =========================================================================
    -- Thao tác với bảng PROOFS (Minh chứng)
    -- =========================================================================
    
    -- Mã hóa và lưu đường dẫn minh chứng
    PROCEDURE ENCRYPT_PROOF_PATH(
        p_proof_id IN VARCHAR2,
        p_stored_path IN VARCHAR2
    );
    
    -- Lấy đường dẫn minh chứng đã giải mã
    FUNCTION GET_PROOF_PATH(p_proof_id IN VARCHAR2) RETURN VARCHAR2;
    
    -- =========================================================================
    -- Thao tác với bảng FEEDBACK_ATTACHMENTS (Tệp đính kèm phản hồi)
    -- =========================================================================
    
    -- Mã hóa và lưu đường dẫn tệp đính kèm
    PROCEDURE ENCRYPT_ATTACHMENT_PATH(
        p_attachment_id IN VARCHAR2,
        p_stored_path IN VARCHAR2
    );
    
    -- Lấy đường dẫn tệp đính kèm đã giải mã
    FUNCTION GET_ATTACHMENT_PATH(p_attachment_id IN VARCHAR2) RETURN VARCHAR2;
    
    -- =========================================================================
    -- Hỗ trợ di chuyển hàng loạt
    -- =========================================================================
    
    -- Mã hóa tất cả đường dẫn minh chứng chưa mã hóa
    PROCEDURE ENCRYPT_ALL_PROOF_PATHS;
    
    -- Mã hóa tất cả đường dẫn tệp đính kèm chưa mã hóa
    PROCEDURE ENCRYPT_ALL_ATTACHMENT_PATHS;

END PKG_FILE_PATH_CRYPTO;
/

CREATE OR REPLACE PACKAGE BODY PKG_FILE_PATH_CRYPTO AS

    -- =========================================================================
    -- Thao tác PROOFS
    -- =========================================================================
    
    PROCEDURE ENCRYPT_PROOF_PATH(
        p_proof_id IN VARCHAR2,
        p_stored_path IN VARCHAR2
    ) IS
        v_encrypted RAW(2000);
    BEGIN
        IF p_stored_path IS NULL THEN
            RETURN;
        END IF;
        
        v_encrypted := PKG_AES_CRYPTO.ENCRYPT_WITH_SYSTEM_KEY(p_stored_path);
        
        UPDATE PROOFS
        SET STORED_PATH_ENCRYPTED = v_encrypted,
            IS_PATH_ENCRYPTED = 1
        WHERE ID = p_proof_id;
        
        COMMIT;
    END ENCRYPT_PROOF_PATH;

    FUNCTION GET_PROOF_PATH(p_proof_id IN VARCHAR2) RETURN VARCHAR2 IS
        v_path VARCHAR2(500);
        v_encrypted RAW(2000);
        v_is_encrypted NUMBER;
    BEGIN
        SELECT STORED_PATH, STORED_PATH_ENCRYPTED, NVL(IS_PATH_ENCRYPTED, 0)
        INTO v_path, v_encrypted, v_is_encrypted
        FROM PROOFS
        WHERE ID = p_proof_id;
        
        IF v_is_encrypted = 1 AND v_encrypted IS NOT NULL THEN
            RETURN PKG_AES_CRYPTO.DECRYPT_WITH_SYSTEM_KEY(v_encrypted);
        ELSE
            RETURN v_path;
        END IF;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN RETURN NULL;
    END GET_PROOF_PATH;

    -- =========================================================================
    -- Thao tác FEEDBACK_ATTACHMENTS
    -- =========================================================================
    
    PROCEDURE ENCRYPT_ATTACHMENT_PATH(
        p_attachment_id IN VARCHAR2,
        p_stored_path IN VARCHAR2
    ) IS
        v_encrypted RAW(2000);
    BEGIN
        IF p_stored_path IS NULL THEN
            RETURN;
        END IF;
        
        v_encrypted := PKG_AES_CRYPTO.ENCRYPT_WITH_SYSTEM_KEY(p_stored_path);
        
        UPDATE FEEDBACK_ATTACHMENTS
        SET STORED_PATH_ENCRYPTED = v_encrypted,
            IS_PATH_ENCRYPTED = 1
        WHERE ID = p_attachment_id;
        
        COMMIT;
    END ENCRYPT_ATTACHMENT_PATH;

    FUNCTION GET_ATTACHMENT_PATH(p_attachment_id IN VARCHAR2) RETURN VARCHAR2 IS
        v_path VARCHAR2(500);
        v_encrypted RAW(2000);
        v_is_encrypted NUMBER;
    BEGIN
        SELECT STORED_PATH, STORED_PATH_ENCRYPTED, NVL(IS_PATH_ENCRYPTED, 0)
        INTO v_path, v_encrypted, v_is_encrypted
        FROM FEEDBACK_ATTACHMENTS
        WHERE ID = p_attachment_id;
        
        IF v_is_encrypted = 1 AND v_encrypted IS NOT NULL THEN
            RETURN PKG_AES_CRYPTO.DECRYPT_WITH_SYSTEM_KEY(v_encrypted);
        ELSE
            RETURN v_path;
        END IF;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN RETURN NULL;
    END GET_ATTACHMENT_PATH;

    -- =========================================================================
    -- Di chuyển hàng loạt
    -- =========================================================================
    
    PROCEDURE ENCRYPT_ALL_PROOF_PATHS IS
    BEGIN
        FOR rec IN (
            SELECT ID, STORED_PATH 
            FROM PROOFS 
            WHERE (IS_PATH_ENCRYPTED = 0 OR IS_PATH_ENCRYPTED IS NULL)
              AND STORED_PATH IS NOT NULL
        ) LOOP
            ENCRYPT_PROOF_PATH(rec.ID, rec.STORED_PATH);
        END LOOP;
    END ENCRYPT_ALL_PROOF_PATHS;

    PROCEDURE ENCRYPT_ALL_ATTACHMENT_PATHS IS
    BEGIN
        FOR rec IN (
            SELECT ID, STORED_PATH 
            FROM FEEDBACK_ATTACHMENTS 
            WHERE (IS_PATH_ENCRYPTED = 0 OR IS_PATH_ENCRYPTED IS NULL)
              AND STORED_PATH IS NOT NULL
        ) LOOP
            ENCRYPT_ATTACHMENT_PATH(rec.ID, rec.STORED_PATH);
        END LOOP;
    END ENCRYPT_ALL_ATTACHMENT_PATHS;

END PKG_FILE_PATH_CRYPTO;
/

-- Cấp quyền
GRANT EXECUTE ON PKG_FILE_PATH_CRYPTO TO ROLE_ADMIN;
GRANT EXECUTE ON PKG_FILE_PATH_CRYPTO TO ROLE_LECTURER;
GRANT EXECUTE ON PKG_FILE_PATH_CRYPTO TO ROLE_STUDENT;

COMMIT;
