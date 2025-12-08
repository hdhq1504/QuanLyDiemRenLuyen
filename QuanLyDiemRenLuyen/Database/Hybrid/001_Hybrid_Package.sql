-- ============================================================================
-- PKG_HYBRID_CRYPTO: Gói Mã hóa Lai RSA+AES
-- Kết hợp RSA (mã hóa khóa) và AES (mã hóa dữ liệu)
-- Phù hợp để mã hóa dữ liệu lớn một cách hiệu quả
-- ============================================================================

CREATE OR REPLACE PACKAGE PKG_HYBRID_CRYPTO AS
    
    -- =========================================================================
    -- Constants
    -- =========================================================================
    G_SEPARATOR CONSTANT VARCHAR2(5) := '::';  -- Separates encrypted key from encrypted data
    
    -- =========================================================================
    -- Hybrid Encryption/Decryption (uses system keys)
    -- =========================================================================
    
    -- Encrypt large data using hybrid approach:
    -- 1. Generate random AES key
    -- 2. Encrypt data with AES
    -- 3. Encrypt AES key with RSA public key
    -- Returns: RSA_ENCRYPTED_AES_KEY || '::' || AES_ENCRYPTED_DATA (both as hex)
    FUNCTION HYBRID_ENCRYPT(
        p_plaintext IN CLOB
    ) RETURN CLOB;
    
    -- Decrypt data:
    -- 1. Split encrypted key and encrypted data
    -- 2. Decrypt AES key with RSA private key
    -- 3. Decrypt data with AES
    FUNCTION HYBRID_DECRYPT(
        p_ciphertext IN CLOB
    ) RETURN CLOB;
    
    -- =========================================================================
    -- Advanced Functions (with explicit keys)
    -- =========================================================================
    
    FUNCTION HYBRID_ENCRYPT_WITH_KEY(
        p_plaintext IN CLOB,
        p_rsa_public_key IN CLOB
    ) RETURN CLOB;
    
    FUNCTION HYBRID_DECRYPT_WITH_KEY(
        p_ciphertext IN CLOB,
        p_rsa_private_key IN CLOB
    ) RETURN CLOB;
    
    -- =========================================================================
    -- Convenience Functions for Specific Use Cases
    -- =========================================================================
    
    -- Encrypt/decrypt feedback content (for long feedback text)
    FUNCTION ENCRYPT_FEEDBACK_CONTENT(p_content IN CLOB) RETURN CLOB;
    FUNCTION DECRYPT_FEEDBACK_CONTENT(p_encrypted IN CLOB) RETURN CLOB;
    
    -- Encrypt/decrypt student profile (for full profile export)
    FUNCTION ENCRYPT_STUDENT_PROFILE(p_profile_json IN CLOB) RETURN CLOB;
    FUNCTION DECRYPT_STUDENT_PROFILE(p_encrypted IN CLOB) RETURN CLOB;
    
    -- Encrypt/decrypt activity description
    FUNCTION ENCRYPT_ACTIVITY_DESCRIPTION(p_description IN CLOB) RETURN CLOB;
    FUNCTION DECRYPT_ACTIVITY_DESCRIPTION(p_encrypted IN CLOB) RETURN CLOB;
    
    -- =========================================================================
    -- Utility Functions
    -- =========================================================================
    
    -- Check if data is hybrid encrypted
    FUNCTION IS_HYBRID_ENCRYPTED(p_data IN CLOB) RETURN NUMBER;
    
    -- Get estimated size after encryption
    FUNCTION GET_ENCRYPTED_SIZE_ESTIMATE(p_plaintext_length IN NUMBER) RETURN NUMBER;

END PKG_HYBRID_CRYPTO;
/

CREATE OR REPLACE PACKAGE BODY PKG_HYBRID_CRYPTO AS

    -- =========================================================================
    -- Private Helper Functions
    -- =========================================================================
    
    -- Extract AES key part from hybrid ciphertext
    FUNCTION GET_ENCRYPTED_KEY_PART(p_ciphertext IN CLOB) RETURN CLOB IS
        v_separator_pos NUMBER;
    BEGIN
        v_separator_pos := INSTR(p_ciphertext, G_SEPARATOR);
        IF v_separator_pos > 0 THEN
            RETURN SUBSTR(p_ciphertext, 1, v_separator_pos - 1);
        END IF;
        RETURN NULL;
    END GET_ENCRYPTED_KEY_PART;
    
    -- Extract encrypted data part from hybrid ciphertext
    FUNCTION GET_ENCRYPTED_DATA_PART(p_ciphertext IN CLOB) RETURN CLOB IS
        v_separator_pos NUMBER;
    BEGIN
        v_separator_pos := INSTR(p_ciphertext, G_SEPARATOR);
        IF v_separator_pos > 0 THEN
            RETURN SUBSTR(p_ciphertext, v_separator_pos + LENGTH(G_SEPARATOR));
        END IF;
        RETURN NULL;
    END GET_ENCRYPTED_DATA_PART;

    -- =========================================================================
    -- Hybrid Encryption Implementation
    -- =========================================================================
    
    FUNCTION HYBRID_ENCRYPT(p_plaintext IN CLOB) RETURN CLOB IS
        v_aes_key RAW(32);
        v_encrypted_data RAW(32767);
        v_encrypted_key CLOB;
        v_public_key CLOB;
        v_result CLOB;
        v_chunk_size CONSTANT NUMBER := 32000; -- Max chunk size for AES
        v_plaintext_raw RAW(32767);
        v_offset NUMBER := 1;
        v_length NUMBER;
        v_chunk VARCHAR2(32000);
    BEGIN
        IF p_plaintext IS NULL OR DBMS_LOB.GETLENGTH(p_plaintext) = 0 THEN
            RETURN NULL;
        END IF;
        
        -- Generate random AES key
        v_aes_key := PKG_AES_CRYPTO.GENERATE_AES_KEY();
        
        -- Get RSA private key (crypto4ora uses private key for encrypt)
        v_public_key := PKG_RSA.GET_PRIVATE_KEY('RSA_SYSTEM_KEY');
        
        -- Encrypt the AES key with RSA
        v_encrypted_key := CRYPTO.RSA_ENCRYPT(
            RAWTOHEX(v_aes_key),
            v_public_key
        );
        
        -- Initialize result with encrypted key
        DBMS_LOB.CREATETEMPORARY(v_result, TRUE);
        DBMS_LOB.WRITEAPPEND(v_result, LENGTH(v_encrypted_key), v_encrypted_key);
        DBMS_LOB.WRITEAPPEND(v_result, LENGTH(G_SEPARATOR), G_SEPARATOR);
        
        -- Encrypt data in chunks
        v_length := DBMS_LOB.GETLENGTH(p_plaintext);
        
        WHILE v_offset <= v_length LOOP
            -- Get chunk
            v_chunk := DBMS_LOB.SUBSTR(p_plaintext, v_chunk_size, v_offset);
            
            -- Encrypt chunk with AES
            v_encrypted_data := PKG_AES_CRYPTO.AES_ENCRYPT(v_chunk, v_aes_key);
            
            -- Append encrypted chunk (as hex)
            DBMS_LOB.WRITEAPPEND(v_result, LENGTH(RAWTOHEX(v_encrypted_data)), RAWTOHEX(v_encrypted_data));
            
            -- Add chunk separator if not last chunk
            v_offset := v_offset + v_chunk_size;
            IF v_offset <= v_length THEN
                DBMS_LOB.WRITEAPPEND(v_result, 1, '|');
            END IF;
        END LOOP;
        
        RETURN v_result;
    EXCEPTION
        WHEN OTHERS THEN
            IF v_result IS NOT NULL THEN
                DBMS_LOB.FREETEMPORARY(v_result);
            END IF;
            RAISE;
    END HYBRID_ENCRYPT;

    FUNCTION HYBRID_DECRYPT(p_ciphertext IN CLOB) RETURN CLOB IS
        v_encrypted_key CLOB;
        v_encrypted_data CLOB;
        v_aes_key_hex VARCHAR2(100);
        v_aes_key RAW(32);
        v_private_key CLOB;
        v_result CLOB;
        v_chunks DBMS_UTILITY.LNAME_ARRAY;
        v_chunk_count NUMBER;
        v_decrypted_chunk VARCHAR2(32767);
        v_data_parts VARCHAR2(32767);
    BEGIN
        IF p_ciphertext IS NULL OR DBMS_LOB.GETLENGTH(p_ciphertext) = 0 THEN
            RETURN NULL;
        END IF;
        
        -- Extract encrypted key and data
        v_encrypted_key := GET_ENCRYPTED_KEY_PART(p_ciphertext);
        v_encrypted_data := GET_ENCRYPTED_DATA_PART(p_ciphertext);
        
        IF v_encrypted_key IS NULL OR v_encrypted_data IS NULL THEN
            RAISE_APPLICATION_ERROR(-20001, 'Invalid hybrid encrypted format');
        END IF;
        
        -- Get RSA public key (crypto4ora uses public key for decrypt)
        v_private_key := PKG_RSA.GET_PUBLIC_KEY('RSA_SYSTEM_KEY');
        
        -- Decrypt AES key with RSA
        v_aes_key_hex := CRYPTO.RSA_DECRYPT(
            v_encrypted_key,
            v_private_key
        );
        v_aes_key := HEXTORAW(v_aes_key_hex);
        
        -- Initialize result
        DBMS_LOB.CREATETEMPORARY(v_result, TRUE);
        
        -- Decrypt data chunks
        v_data_parts := DBMS_LOB.SUBSTR(v_encrypted_data, 32767, 1);
        
        -- Split by chunk separator and decrypt each
        DECLARE
            v_pos NUMBER := 1;
            v_next_sep NUMBER;
            v_hex_chunk VARCHAR2(32767);
        BEGIN
            LOOP
                v_next_sep := INSTR(v_data_parts, '|', v_pos);
                
                IF v_next_sep = 0 THEN
                    v_hex_chunk := SUBSTR(v_data_parts, v_pos);
                ELSE
                    v_hex_chunk := SUBSTR(v_data_parts, v_pos, v_next_sep - v_pos);
                    v_pos := v_next_sep + 1;
                END IF;
                
                IF v_hex_chunk IS NOT NULL AND LENGTH(v_hex_chunk) > 0 THEN
                    v_decrypted_chunk := PKG_AES_CRYPTO.AES_DECRYPT(HEXTORAW(v_hex_chunk), v_aes_key);
                    DBMS_LOB.WRITEAPPEND(v_result, LENGTH(v_decrypted_chunk), v_decrypted_chunk);
                END IF;
                
                EXIT WHEN v_next_sep = 0;
            END LOOP;
        END;
        
        RETURN v_result;
    EXCEPTION
        WHEN OTHERS THEN
            IF v_result IS NOT NULL THEN
                DBMS_LOB.FREETEMPORARY(v_result);
            END IF;
            RAISE;
    END HYBRID_DECRYPT;

    -- =========================================================================
    -- Advanced Functions Implementation
    -- =========================================================================
    
    FUNCTION HYBRID_ENCRYPT_WITH_KEY(
        p_plaintext IN CLOB,
        p_rsa_public_key IN CLOB
    ) RETURN CLOB IS
        v_aes_key RAW(32);
        v_encrypted_data RAW(32767);
        v_encrypted_key CLOB;
        v_result CLOB;
        v_plaintext_str VARCHAR2(32767);
    BEGIN
        IF p_plaintext IS NULL THEN
            RETURN NULL;
        END IF;
        
        -- Generate random AES key
        v_aes_key := PKG_AES_CRYPTO.GENERATE_AES_KEY();
        
        -- Encrypt the AES key with provided RSA key
        v_encrypted_key := CRYPTO.RSA_ENCRYPT(
            RAWTOHEX(v_aes_key),
            p_rsa_public_key
        );
        
        -- Get plaintext as string (for simplicity, limit to 32K)
        v_plaintext_str := DBMS_LOB.SUBSTR(p_plaintext, 32000, 1);
        
        -- Encrypt data with AES
        v_encrypted_data := PKG_AES_CRYPTO.AES_ENCRYPT(v_plaintext_str, v_aes_key);
        
        -- Combine: encrypted_key || separator || encrypted_data
        DBMS_LOB.CREATETEMPORARY(v_result, TRUE);
        DBMS_LOB.WRITEAPPEND(v_result, LENGTH(v_encrypted_key), v_encrypted_key);
        DBMS_LOB.WRITEAPPEND(v_result, LENGTH(G_SEPARATOR), G_SEPARATOR);
        DBMS_LOB.WRITEAPPEND(v_result, LENGTH(RAWTOHEX(v_encrypted_data)), RAWTOHEX(v_encrypted_data));
        
        RETURN v_result;
    END HYBRID_ENCRYPT_WITH_KEY;

    FUNCTION HYBRID_DECRYPT_WITH_KEY(
        p_ciphertext IN CLOB,
        p_rsa_private_key IN CLOB
    ) RETURN CLOB IS
        v_encrypted_key CLOB;
        v_encrypted_data CLOB;
        v_aes_key_hex VARCHAR2(100);
        v_aes_key RAW(32);
        v_decrypted VARCHAR2(32767);
        v_result CLOB;
    BEGIN
        IF p_ciphertext IS NULL THEN
            RETURN NULL;
        END IF;
        
        v_encrypted_key := GET_ENCRYPTED_KEY_PART(p_ciphertext);
        v_encrypted_data := GET_ENCRYPTED_DATA_PART(p_ciphertext);
        
        -- Decrypt AES key
        v_aes_key_hex := CRYPTO.RSA_DECRYPT(
            v_encrypted_key,
            p_rsa_private_key
        );
        v_aes_key := HEXTORAW(v_aes_key_hex);
        
        -- Decrypt data
        v_decrypted := PKG_AES_CRYPTO.AES_DECRYPT(
            HEXTORAW(DBMS_LOB.SUBSTR(v_encrypted_data, 32767, 1)), 
            v_aes_key
        );
        
        DBMS_LOB.CREATETEMPORARY(v_result, TRUE);
        DBMS_LOB.WRITEAPPEND(v_result, LENGTH(v_decrypted), v_decrypted);
        
        RETURN v_result;
    END HYBRID_DECRYPT_WITH_KEY;

    -- =========================================================================
    -- Convenience Functions Implementation
    -- =========================================================================
    
    FUNCTION ENCRYPT_FEEDBACK_CONTENT(p_content IN CLOB) RETURN CLOB IS
    BEGIN
        IF p_content IS NULL OR DBMS_LOB.GETLENGTH(p_content) = 0 THEN
            RETURN NULL;
        END IF;
        RETURN HYBRID_ENCRYPT(p_content);
    END ENCRYPT_FEEDBACK_CONTENT;

    FUNCTION DECRYPT_FEEDBACK_CONTENT(p_encrypted IN CLOB) RETURN CLOB IS
    BEGIN
        IF p_encrypted IS NULL THEN
            RETURN NULL;
        END IF;
        RETURN HYBRID_DECRYPT(p_encrypted);
    EXCEPTION
        WHEN OTHERS THEN
            RETURN TO_CLOB('[Decryption Error]');
    END DECRYPT_FEEDBACK_CONTENT;

    FUNCTION ENCRYPT_STUDENT_PROFILE(p_profile_json IN CLOB) RETURN CLOB IS
    BEGIN
        IF p_profile_json IS NULL OR DBMS_LOB.GETLENGTH(p_profile_json) = 0 THEN
            RETURN NULL;
        END IF;
        RETURN HYBRID_ENCRYPT(p_profile_json);
    END ENCRYPT_STUDENT_PROFILE;

    FUNCTION DECRYPT_STUDENT_PROFILE(p_encrypted IN CLOB) RETURN CLOB IS
    BEGIN
        IF p_encrypted IS NULL THEN
            RETURN NULL;
        END IF;
        RETURN HYBRID_DECRYPT(p_encrypted);
    EXCEPTION
        WHEN OTHERS THEN
            RETURN TO_CLOB('[Decryption Error]');
    END DECRYPT_STUDENT_PROFILE;

    FUNCTION ENCRYPT_ACTIVITY_DESCRIPTION(p_description IN CLOB) RETURN CLOB IS
    BEGIN
        IF p_description IS NULL OR DBMS_LOB.GETLENGTH(p_description) = 0 THEN
            RETURN NULL;
        END IF;
        RETURN HYBRID_ENCRYPT(p_description);
    END ENCRYPT_ACTIVITY_DESCRIPTION;

    FUNCTION DECRYPT_ACTIVITY_DESCRIPTION(p_encrypted IN CLOB) RETURN CLOB IS
    BEGIN
        IF p_encrypted IS NULL THEN
            RETURN NULL;
        END IF;
        RETURN HYBRID_DECRYPT(p_encrypted);
    EXCEPTION
        WHEN OTHERS THEN
            RETURN TO_CLOB('[Decryption Error]');
    END DECRYPT_ACTIVITY_DESCRIPTION;

    -- =========================================================================
    -- Utility Functions Implementation
    -- =========================================================================
    
    FUNCTION IS_HYBRID_ENCRYPTED(p_data IN CLOB) RETURN NUMBER IS
    BEGIN
        IF p_data IS NULL THEN
            RETURN 0;
        END IF;
        
        -- Check if contains separator
        IF INSTR(p_data, G_SEPARATOR) > 0 THEN
            RETURN 1;
        END IF;
        
        RETURN 0;
    END IS_HYBRID_ENCRYPTED;

    FUNCTION GET_ENCRYPTED_SIZE_ESTIMATE(p_plaintext_length IN NUMBER) RETURN NUMBER IS
    BEGIN
        -- Rough estimate:
        -- RSA encrypted key: ~344 chars (2048-bit key)
        -- AES overhead: ~20% (IV + padding + hex encoding)
        -- Separator: 2 chars
        RETURN 346 + CEIL(p_plaintext_length * 2.4);
    END GET_ENCRYPTED_SIZE_ESTIMATE;

END PKG_HYBRID_CRYPTO;
/

-- ============================================================================
-- Add necessary columns to tables for hybrid encryption
-- ============================================================================

-- Add hybrid encrypted description column to ACTIVITIES
DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count FROM USER_TAB_COLUMNS
    WHERE TABLE_NAME = 'ACTIVITIES' AND COLUMN_NAME = 'DESCRIPTION_ENCRYPTED';
    IF v_count = 0 THEN
        EXECUTE IMMEDIATE 'ALTER TABLE ACTIVITIES ADD DESCRIPTION_ENCRYPTED CLOB';
    END IF;
END;
/

-- Add flag for encryption type
DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count FROM USER_TAB_COLUMNS
    WHERE TABLE_NAME = 'ACTIVITIES' AND COLUMN_NAME = 'ENCRYPTION_TYPE';
    IF v_count = 0 THEN
        EXECUTE IMMEDIATE 'ALTER TABLE ACTIVITIES ADD ENCRYPTION_TYPE VARCHAR2(20) DEFAULT ''NONE''';
    END IF;
END;
/

-- Grant permissions
GRANT EXECUTE ON PKG_HYBRID_CRYPTO TO ROLE_ADMIN;
GRANT EXECUTE ON PKG_HYBRID_CRYPTO TO ROLE_LECTURER;

COMMIT;
