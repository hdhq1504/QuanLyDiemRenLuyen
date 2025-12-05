-- =========================================================
-- FEATURE 2: Digital Signatures for Score Approval
-- Migration Script: 002_ADD_DIGITAL_SIGNATURE_SCORES
-- Description: Thêm chữ ký điện tử cho điểm rèn luyện
-- =========================================================

-- =========================================================
-- 3) ORACLE PACKAGE: PKG_SCORE_SIGNATURE
-- =========================================================

CREATE OR REPLACE PACKAGE PKG_SCORE_SIGNATURE AS
    FUNCTION CREATE_SCORE_DATA_HASH(
        p_score_id NUMBER
    ) RETURN VARCHAR2;
    
    PROCEDURE LOG_SIGNATURE_AUDIT(
        p_score_id NUMBER,
        p_action_type VARCHAR2,
        p_performed_by VARCHAR2,
        p_signature VARCHAR2 DEFAULT NULL,
        p_result VARCHAR2 DEFAULT NULL,
        p_notes VARCHAR2 DEFAULT NULL
    );
    
    FUNCTION IS_SCORE_TAMPERED(
        p_score_id NUMBER
    ) RETURN NUMBER;
    
    FUNCTION GET_SCORE_DATA_STRING(
        p_score_id NUMBER
    ) RETURN VARCHAR2;
    
END PKG_SCORE_SIGNATURE;
/

CREATE OR REPLACE PACKAGE BODY PKG_SCORE_SIGNATURE AS
    
    FUNCTION GET_SCORE_DATA_STRING(
        p_score_id NUMBER
    ) RETURN VARCHAR2 IS
        v_data VARCHAR2(4000);
        v_student_id VARCHAR2(50);
        v_term_id VARCHAR2(32);
        v_total_score NUMBER;
        v_classification VARCHAR2(50);
        v_status VARCHAR2(20);
    BEGIN
        SELECT STUDENT_ID, TERM_ID, TOTAL_SCORE, CLASSIFICATION, STATUS
        INTO v_student_id, v_term_id, v_total_score, v_classification, v_status
        FROM SCORES
        WHERE ID = p_score_id;
        
        v_data := 'STUDENT_ID=' || v_student_id ||
                  '|TERM_ID=' || v_term_id ||
                  '|TOTAL_SCORE=' || TO_CHAR(v_total_score) ||
                  '|CLASSIFICATION=' || NVL(v_classification, '') ||
                  '|STATUS=' || NVL(v_status, '');
        
        RETURN v_data;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(-20201, 'Score not found: ' || p_score_id);
        WHEN OTHERS THEN
            RAISE_APPLICATION_ERROR(-20202, 'Error getting score data: ' || SQLERRM);
    END GET_SCORE_DATA_STRING;
    
    FUNCTION CREATE_SCORE_DATA_HASH(
        p_score_id NUMBER
    ) RETURN VARCHAR2 IS
        v_data VARCHAR2(4000);
        v_hash_raw RAW(32);
        v_hash_hex VARCHAR2(128);
    BEGIN
        v_data := GET_SCORE_DATA_STRING(p_score_id);
        
        v_hash_raw := DBMS_CRYPTO.HASH(
            UTL_RAW.CAST_TO_RAW(v_data),
            DBMS_CRYPTO.HASH_SH256
        );
        
        v_hash_hex := RAWTOHEX(v_hash_raw);
        
        RETURN v_hash_hex;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE_APPLICATION_ERROR(-20203, 'Error creating hash: ' || SQLERRM);
    END CREATE_SCORE_DATA_HASH;
    
    PROCEDURE LOG_SIGNATURE_AUDIT(
        p_score_id NUMBER,
        p_action_type VARCHAR2,
        p_performed_by VARCHAR2,
        p_signature VARCHAR2 DEFAULT NULL,
        p_result VARCHAR2 DEFAULT NULL,
        p_notes VARCHAR2 DEFAULT NULL
    ) IS
        v_hash_before VARCHAR2(128);
        v_hash_after VARCHAR2(128);
    BEGIN
        v_hash_after := CREATE_SCORE_DATA_HASH(p_score_id);
        
        BEGIN
            SELECT SIGNED_DATA_HASH
            INTO v_hash_before
            FROM SCORES
            WHERE ID = p_score_id;
        EXCEPTION
            WHEN NO_DATA_FOUND THEN
                v_hash_before := NULL;
        END;
        
        INSERT INTO SCORE_AUDIT_SIGNATURES (
            SCORE_ID,
            ACTION_TYPE,
            PERFORMED_BY,
            SIGNATURE_VALUE,
            VERIFICATION_RESULT,
            DATA_HASH_BEFORE,
            DATA_HASH_AFTER,
            NOTES
        ) VALUES (
            p_score_id,
            p_action_type,
            p_performed_by,
            p_signature,
            p_result,
            v_hash_before,
            v_hash_after,
            p_notes
        );
        
        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            DBMS_OUTPUT.PUT_LINE('Error logging audit: ' || SQLERRM);
    END LOG_SIGNATURE_AUDIT;
    
    FUNCTION IS_SCORE_TAMPERED(
        p_score_id NUMBER
    ) RETURN NUMBER IS
        v_current_hash VARCHAR2(128);
        v_stored_hash VARCHAR2(128);
        v_has_signature NUMBER;
    BEGIN
        SELECT SIGNED_DATA_HASH, 
               CASE WHEN DIGITAL_SIGNATURE IS NOT NULL THEN 1 ELSE 0 END
        INTO v_stored_hash, v_has_signature
        FROM SCORES
        WHERE ID = p_score_id;
        
        IF v_has_signature = 0 OR v_stored_hash IS NULL THEN
            RETURN 0;
        END IF;
        
        v_current_hash := CREATE_SCORE_DATA_HASH(p_score_id);
        
        IF v_current_hash = v_stored_hash THEN
            RETURN 0;
        ELSE
            RETURN 1;
        END IF;
        
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RETURN 0;
        WHEN OTHERS THEN
            DBMS_OUTPUT.PUT_LINE('Error checking tamper: ' || SQLERRM);
            RETURN 0;
    END IS_SCORE_TAMPERED;

END PKG_SCORE_SIGNATURE;
/

GRANT EXECUTE ON PKG_SCORE_SIGNATURE TO PUBLIC;

-- =========================================================
-- 4) Verification
-- =========================================================
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    NULLABLE
FROM USER_TAB_COLUMNS
WHERE TABLE_NAME = 'SCORES'
AND COLUMN_NAME IN ('DIGITAL_SIGNATURE', 'SIGNED_DATA_HASH', 'SIGNATURE_VERIFIED', 'SIGNED_BY', 'SIGNED_AT')
ORDER BY COLUMN_ID;

SELECT COUNT(*) as AUDIT_TABLE_EXISTS 
FROM USER_TABLES 
WHERE TABLE_NAME = 'SCORE_AUDIT_SIGNATURES';

SELECT 
    OBJECT_NAME,
    OBJECT_TYPE,
    STATUS
FROM USER_OBJECTS
WHERE OBJECT_NAME = 'PKG_SCORE_SIGNATURE';

PROMPT 'Migration 002_ADD_DIGITAL_SIGNATURE_SCORES completed successfully!';
