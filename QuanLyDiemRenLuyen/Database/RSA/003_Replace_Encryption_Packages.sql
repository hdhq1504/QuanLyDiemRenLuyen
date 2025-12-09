-- ============================================================================
-- PKG_STUDENT_ENCRYPTION: Mã hóa dữ liệu nhạy cảm của sinh viên
-- ============================================================================

CREATE OR REPLACE PACKAGE PKG_STUDENT_ENCRYPTION AS
    
    -- Encrypt student phone number
    FUNCTION ENCRYPT_PHONE(p_phone IN VARCHAR2) RETURN CLOB;
    FUNCTION DECRYPT_PHONE(p_encrypted IN CLOB) RETURN VARCHAR2;
    
    -- Encrypt student address
    FUNCTION ENCRYPT_ADDRESS(p_address IN VARCHAR2) RETURN CLOB;
    FUNCTION DECRYPT_ADDRESS(p_encrypted IN CLOB) RETURN VARCHAR2;
    
    -- Encrypt student ID card number
    FUNCTION ENCRYPT_ID_CARD(p_id_card IN VARCHAR2) RETURN CLOB;
    FUNCTION DECRYPT_ID_CARD(p_encrypted IN CLOB) RETURN VARCHAR2;
    
    -- Batch encrypt all fields for a student (user_id is PK)
    PROCEDURE ENCRYPT_STUDENT_DATA(
        p_user_id IN VARCHAR2,
        p_phone IN VARCHAR2,
        p_address IN VARCHAR2,
        p_id_card IN VARCHAR2
    );
    
    -- Get decrypted student sensitive data
    PROCEDURE GET_STUDENT_SENSITIVE_DATA(
        p_user_id IN VARCHAR2,
        p_phone OUT VARCHAR2,
        p_address OUT VARCHAR2,
        p_id_card OUT VARCHAR2
    );

END PKG_STUDENT_ENCRYPTION;
/

CREATE OR REPLACE PACKAGE BODY PKG_STUDENT_ENCRYPTION AS

    FUNCTION ENCRYPT_PHONE(p_phone IN VARCHAR2) RETURN CLOB IS
    BEGIN
        IF p_phone IS NULL OR LENGTH(TRIM(p_phone)) = 0 THEN
            RETURN NULL;
        END IF;
        RETURN TO_CLOB(PKG_RSA_CRYPTO.ENCRYPT_WITH_SYSTEM_KEY(p_phone));
    END ENCRYPT_PHONE;

    FUNCTION DECRYPT_PHONE(p_encrypted IN CLOB) RETURN VARCHAR2 IS
        v_encrypted_str VARCHAR2(32767);
    BEGIN
        IF p_encrypted IS NULL THEN
            RETURN NULL;
        END IF;
        v_encrypted_str := DBMS_LOB.SUBSTR(p_encrypted, 32767, 1);
        RETURN PKG_RSA_CRYPTO.DECRYPT_WITH_SYSTEM_KEY(v_encrypted_str);
    EXCEPTION
        WHEN OTHERS THEN
            RETURN '[Decryption Error]';
    END DECRYPT_PHONE;

    FUNCTION ENCRYPT_ADDRESS(p_address IN VARCHAR2) RETURN CLOB IS
    BEGIN
        IF p_address IS NULL OR LENGTH(TRIM(p_address)) = 0 THEN
            RETURN NULL;
        END IF;
        RETURN TO_CLOB(PKG_RSA_CRYPTO.ENCRYPT_WITH_SYSTEM_KEY(p_address));
    END ENCRYPT_ADDRESS;

    FUNCTION DECRYPT_ADDRESS(p_encrypted IN CLOB) RETURN VARCHAR2 IS
        v_encrypted_str VARCHAR2(32767);
    BEGIN
        IF p_encrypted IS NULL THEN
            RETURN NULL;
        END IF;
        v_encrypted_str := DBMS_LOB.SUBSTR(p_encrypted, 32767, 1);
        RETURN PKG_RSA_CRYPTO.DECRYPT_WITH_SYSTEM_KEY(v_encrypted_str);
    EXCEPTION
        WHEN OTHERS THEN
            RETURN '[Decryption Error]';
    END DECRYPT_ADDRESS;

    FUNCTION ENCRYPT_ID_CARD(p_id_card IN VARCHAR2) RETURN CLOB IS
    BEGIN
        IF p_id_card IS NULL OR LENGTH(TRIM(p_id_card)) = 0 THEN
            RETURN NULL;
        END IF;
        RETURN TO_CLOB(PKG_RSA_CRYPTO.ENCRYPT_WITH_SYSTEM_KEY(p_id_card));
    END ENCRYPT_ID_CARD;

    FUNCTION DECRYPT_ID_CARD(p_encrypted IN CLOB) RETURN VARCHAR2 IS
        v_encrypted_str VARCHAR2(32767);
    BEGIN
        IF p_encrypted IS NULL THEN
            RETURN NULL;
        END IF;
        v_encrypted_str := DBMS_LOB.SUBSTR(p_encrypted, 32767, 1);
        RETURN PKG_RSA_CRYPTO.DECRYPT_WITH_SYSTEM_KEY(v_encrypted_str);
    EXCEPTION
        WHEN OTHERS THEN
            RETURN '[Decryption Error]';
    END DECRYPT_ID_CARD;

    PROCEDURE ENCRYPT_STUDENT_DATA(
        p_user_id IN VARCHAR2,
        p_phone IN VARCHAR2,
        p_address IN VARCHAR2,
        p_id_card IN VARCHAR2
    ) IS
    BEGIN
        UPDATE STUDENTS
        SET PHONE_ENCRYPTED = ENCRYPT_PHONE(p_phone),
            ADDRESS_ENCRYPTED = ENCRYPT_ADDRESS(p_address),
            ID_CARD_ENCRYPTED = ENCRYPT_ID_CARD(p_id_card),
            ENCRYPTED_AT = SYSTIMESTAMP
        WHERE USER_ID = p_user_id;
        
        COMMIT;
    END ENCRYPT_STUDENT_DATA;

    PROCEDURE GET_STUDENT_SENSITIVE_DATA(
        p_user_id IN VARCHAR2,
        p_phone OUT VARCHAR2,
        p_address OUT VARCHAR2,
        p_id_card OUT VARCHAR2
    ) IS
        v_phone_enc CLOB;
        v_address_enc CLOB;
        v_id_card_enc CLOB;
    BEGIN
        SELECT PHONE_ENCRYPTED, ADDRESS_ENCRYPTED, ID_CARD_ENCRYPTED
        INTO v_phone_enc, v_address_enc, v_id_card_enc
        FROM STUDENTS
        WHERE USER_ID = p_user_id;
        
        p_phone := DECRYPT_PHONE(v_phone_enc);
        p_address := DECRYPT_ADDRESS(v_address_enc);
        p_id_card := DECRYPT_ID_CARD(v_id_card_enc);
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            p_phone := NULL;
            p_address := NULL;
            p_id_card := NULL;
    END GET_STUDENT_SENSITIVE_DATA;

END PKG_STUDENT_ENCRYPTION;
/

-- ============================================================================
-- PKG_SCORE_SIGNATURE: Digital Signature for Score Records
-- Note: SCORES table uses ID as PK (NUMBER), STUDENT_ID/TERM_ID are VARCHAR2
-- ============================================================================

CREATE OR REPLACE PACKAGE PKG_SCORE_SIGNATURE AS
    
    -- Sign a score record
    FUNCTION SIGN_SCORE(
        p_student_id IN VARCHAR2,
        p_term_id IN VARCHAR2,
        p_total_score IN NUMBER,
        p_classification IN VARCHAR2
    ) RETURN CLOB;
    
    -- Verify a score signature
    FUNCTION VERIFY_SCORE_SIGNATURE(
        p_student_id IN VARCHAR2,
        p_term_id IN VARCHAR2,
        p_total_score IN NUMBER,
        p_classification IN VARCHAR2,
        p_signature IN CLOB
    ) RETURN NUMBER;
    
    -- Sign and store signature for a score record (by score ID)
    PROCEDURE SIGN_AND_STORE_SCORE(p_id IN NUMBER);
    
    -- Verify stored signature for a score record
    FUNCTION VERIFY_STORED_SIGNATURE(p_id IN NUMBER) RETURN NUMBER;

END PKG_SCORE_SIGNATURE;
/

CREATE OR REPLACE PACKAGE BODY PKG_SCORE_SIGNATURE AS

    FUNCTION CREATE_SCORE_SIGNATURE_DATA(
        p_student_id IN VARCHAR2,
        p_term_id IN VARCHAR2,
        p_total_score IN NUMBER,
        p_classification IN VARCHAR2
    ) RETURN VARCHAR2 IS
    BEGIN
        RETURN 'SCORE|' || p_student_id || '|' || p_term_id || '|' 
               || p_total_score || '|' || NVL(p_classification, '');
    END CREATE_SCORE_SIGNATURE_DATA;

    FUNCTION SIGN_SCORE(
        p_student_id IN VARCHAR2,
        p_term_id IN VARCHAR2,
        p_total_score IN NUMBER,
        p_classification IN VARCHAR2
    ) RETURN CLOB IS
        v_data VARCHAR2(500);
    BEGIN
        v_data := CREATE_SCORE_SIGNATURE_DATA(
            p_student_id, p_term_id, p_total_score, p_classification
        );
        RETURN TO_CLOB(PKG_RSA_CRYPTO.SIGN_WITH_SYSTEM_KEY(v_data));
    END SIGN_SCORE;

    FUNCTION VERIFY_SCORE_SIGNATURE(
        p_student_id IN VARCHAR2,
        p_term_id IN VARCHAR2,
        p_total_score IN NUMBER,
        p_classification IN VARCHAR2,
        p_signature IN CLOB
    ) RETURN NUMBER IS
        v_data VARCHAR2(500);
        v_sig_str VARCHAR2(32767);
    BEGIN
        v_data := CREATE_SCORE_SIGNATURE_DATA(
            p_student_id, p_term_id, p_total_score, p_classification
        );
        v_sig_str := DBMS_LOB.SUBSTR(p_signature, 32767, 1);
        RETURN PKG_RSA_CRYPTO.VERIFY_WITH_SYSTEM_KEY(v_data, v_sig_str);
    END VERIFY_SCORE_SIGNATURE;

    PROCEDURE SIGN_AND_STORE_SCORE(p_id IN NUMBER) IS
        v_student_id VARCHAR2(50);
        v_term_id VARCHAR2(32);
        v_total_score NUMBER;
        v_classification VARCHAR2(50);
        v_signature CLOB;
    BEGIN
        SELECT STUDENT_ID, TERM_ID, TOTAL_SCORE, CLASSIFICATION
        INTO v_student_id, v_term_id, v_total_score, v_classification
        FROM SCORES
        WHERE ID = p_id;
        
        v_signature := SIGN_SCORE(v_student_id, v_term_id, v_total_score, v_classification);
        
        UPDATE SCORES
        SET DIGITAL_SIGNATURE = v_signature,
            SIGNED_AT = SYSTIMESTAMP,
            SIGNED_BY = USER
        WHERE ID = p_id;
        
        COMMIT;
    END SIGN_AND_STORE_SCORE;

    FUNCTION VERIFY_STORED_SIGNATURE(p_id IN NUMBER) RETURN NUMBER IS
        v_student_id VARCHAR2(50);
        v_term_id VARCHAR2(32);
        v_total_score NUMBER;
        v_classification VARCHAR2(50);
        v_signature CLOB;
    BEGIN
        SELECT STUDENT_ID, TERM_ID, TOTAL_SCORE, CLASSIFICATION, DIGITAL_SIGNATURE
        INTO v_student_id, v_term_id, v_total_score, v_classification, v_signature
        FROM SCORES
        WHERE ID = p_id;
        
        IF v_signature IS NULL THEN
            RETURN 0;
        END IF;
        
        RETURN VERIFY_SCORE_SIGNATURE(
            v_student_id, v_term_id, v_total_score, v_classification, v_signature
        );
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RETURN 0;
    END VERIFY_STORED_SIGNATURE;

END PKG_SCORE_SIGNATURE;
/

-- ============================================================================
-- PKG_FEEDBACK_ENCRYPTION: Feedback Content Encryption
-- Note: FEEDBACKS table uses ID as PK (VARCHAR2), STUDENT_ID/TERM_ID are VARCHAR2
-- ============================================================================

CREATE OR REPLACE PACKAGE PKG_FEEDBACK_ENCRYPTION AS
    
    FUNCTION ENCRYPT_CONTENT(p_content IN VARCHAR2) RETURN CLOB;
    FUNCTION DECRYPT_CONTENT(p_encrypted IN CLOB) RETURN VARCHAR2;
    
    -- Store encrypted feedback
    PROCEDURE STORE_ENCRYPTED_FEEDBACK(
        p_student_id IN VARCHAR2,
        p_term_id IN VARCHAR2,
        p_title IN VARCHAR2,
        p_content IN VARCHAR2,
        p_feedback_id OUT VARCHAR2
    );
    
    -- Get decrypted feedback
    FUNCTION GET_FEEDBACK_CONTENT(p_id IN VARCHAR2) RETURN VARCHAR2;

END PKG_FEEDBACK_ENCRYPTION;
/

CREATE OR REPLACE PACKAGE BODY PKG_FEEDBACK_ENCRYPTION AS

    FUNCTION ENCRYPT_CONTENT(p_content IN VARCHAR2) RETURN CLOB IS
    BEGIN
        IF p_content IS NULL OR LENGTH(TRIM(p_content)) = 0 THEN
            RETURN NULL;
        END IF;
        RETURN TO_CLOB(PKG_RSA_CRYPTO.ENCRYPT_WITH_SYSTEM_KEY(p_content));
    END ENCRYPT_CONTENT;

    FUNCTION DECRYPT_CONTENT(p_encrypted IN CLOB) RETURN VARCHAR2 IS
        v_encrypted_str VARCHAR2(32767);
    BEGIN
        IF p_encrypted IS NULL THEN
            RETURN NULL;
        END IF;
        v_encrypted_str := DBMS_LOB.SUBSTR(p_encrypted, 32767, 1);
        RETURN PKG_RSA_CRYPTO.DECRYPT_WITH_SYSTEM_KEY(v_encrypted_str);
    EXCEPTION
        WHEN OTHERS THEN
            RETURN '[Decryption Error]';
    END DECRYPT_CONTENT;

    PROCEDURE STORE_ENCRYPTED_FEEDBACK(
        p_student_id IN VARCHAR2,
        p_term_id IN VARCHAR2,
        p_title IN VARCHAR2,
        p_content IN VARCHAR2,
        p_feedback_id OUT VARCHAR2
    ) IS
        v_encrypted CLOB;
        v_id VARCHAR2(32);
    BEGIN
        v_encrypted := ENCRYPT_CONTENT(p_content);
        v_id := RAWTOHEX(SYS_GUID());
        
        INSERT INTO FEEDBACKS (
            ID,
            STUDENT_ID,
            TERM_ID,
            TITLE,
            CONTENT,
            CONTENT_ENCRYPTED,
            IS_ENCRYPTED,
            ENCRYPTED_AT,
            ENCRYPTED_BY
        ) VALUES (
            v_id,
            p_student_id,
            p_term_id,
            p_title,
            '[ENCRYPTED]',
            v_encrypted,
            1,
            SYSTIMESTAMP,
            USER
        );
        
        p_feedback_id := v_id;
        COMMIT;
    END STORE_ENCRYPTED_FEEDBACK;

    FUNCTION GET_FEEDBACK_CONTENT(p_id IN VARCHAR2) RETURN VARCHAR2 IS
        v_encrypted CLOB;
        v_is_encrypted NUMBER;
        v_content CLOB;
    BEGIN
        SELECT CONTENT_ENCRYPTED, NVL(IS_ENCRYPTED, 0), CONTENT
        INTO v_encrypted, v_is_encrypted, v_content
        FROM FEEDBACKS
        WHERE ID = p_id;
        
        IF v_is_encrypted = 1 AND v_encrypted IS NOT NULL THEN
            RETURN DECRYPT_CONTENT(v_encrypted);
        ELSE
            RETURN DBMS_LOB.SUBSTR(v_content, 4000, 1);
        END IF;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RETURN NULL;
    END GET_FEEDBACK_CONTENT;

END PKG_FEEDBACK_ENCRYPTION;
/

-- Grant permissions
GRANT EXECUTE ON PKG_STUDENT_ENCRYPTION TO ROLE_ADMIN;
GRANT EXECUTE ON PKG_STUDENT_ENCRYPTION TO ROLE_LECTURER;

GRANT EXECUTE ON PKG_SCORE_SIGNATURE TO ROLE_ADMIN;
GRANT EXECUTE ON PKG_SCORE_SIGNATURE TO ROLE_LECTURER;

GRANT EXECUTE ON PKG_FEEDBACK_ENCRYPTION TO ROLE_ADMIN;
GRANT EXECUTE ON PKG_FEEDBACK_ENCRYPTION TO ROLE_LECTURER;
GRANT EXECUTE ON PKG_FEEDBACK_ENCRYPTION TO ROLE_STUDENT;

COMMIT;
