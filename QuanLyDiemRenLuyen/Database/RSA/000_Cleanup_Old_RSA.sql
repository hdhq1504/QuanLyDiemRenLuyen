-- ============================================================================
-- Dọn dẹp RSA cũ
-- Chạy script này TRƯỚC KHI sử dụng crypto4ora packages
-- ============================================================================

-- ============================================================================
-- Bước 1: Đánh dấu RSA keys cũ là legacy (không xóa, giữ để tham khảo)
-- ============================================================================

UPDATE ENCRYPTION_KEYS
SET KEY_NAME = 'LEGACY_' || KEY_NAME,
    IS_ACTIVE = 0,
    DESCRIPTION = '[LEGACY] ' || NVL(DESCRIPTION, 'Old RSA key - replaced by crypto4ora')
WHERE ALGORITHM = 'RSA'
  AND KEY_NAME NOT LIKE 'LEGACY_%';

COMMIT;

-- ============================================================================
-- Step 2: Drop old RSA packages (these are just wrappers that don't do real crypto)
-- ============================================================================

-- Drop package bodies first, then specifications
BEGIN
    EXECUTE IMMEDIATE 'DROP PACKAGE BODY PKG_RSA_CRYPTO';
EXCEPTION
    WHEN OTHERS THEN NULL;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP PACKAGE PKG_RSA_CRYPTO';
EXCEPTION
    WHEN OTHERS THEN NULL;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP PACKAGE BODY PKG_STUDENT_ENCRYPTION';
EXCEPTION
    WHEN OTHERS THEN NULL;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP PACKAGE PKG_STUDENT_ENCRYPTION';
EXCEPTION
    WHEN OTHERS THEN NULL;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP PACKAGE BODY PKG_SCORE_SIGNATURE';
EXCEPTION
    WHEN OTHERS THEN NULL;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP PACKAGE PKG_SCORE_SIGNATURE';
EXCEPTION
    WHEN OTHERS THEN NULL;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP PACKAGE BODY PKG_FEEDBACK_ENCRYPTION';
EXCEPTION
    WHEN OTHERS THEN NULL;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP PACKAGE PKG_FEEDBACK_ENCRYPTION';
EXCEPTION
    WHEN OTHERS THEN NULL;
END;
/

-- ============================================================================
-- Step 3: Log cleanup action
-- ============================================================================

INSERT INTO AUDIT_TRAIL (
    WHO,
    ACTION,
    EVENT_AT_UTC
) VALUES (
    USER,
    'CLEANUP: Dropped old RSA packages (PKG_RSA_CRYPTO, PKG_STUDENT_ENCRYPTION, PKG_SCORE_SIGNATURE, PKG_FEEDBACK_ENCRYPTION) for crypto4ora migration',
    SYS_EXTRACT_UTC(SYSTIMESTAMP)
);

COMMIT;

-- ============================================================================
-- Verification: List remaining packages
-- ============================================================================

SELECT 'Remaining encryption packages:' AS INFO FROM DUAL;

SELECT OBJECT_NAME, OBJECT_TYPE, STATUS
FROM USER_OBJECTS
WHERE OBJECT_NAME LIKE 'PKG_%'
  AND OBJECT_TYPE IN ('PACKAGE', 'PACKAGE BODY')
ORDER BY OBJECT_NAME, OBJECT_TYPE;

SELECT 'Legacy RSA keys:' AS INFO FROM DUAL;

SELECT KEY_NAME, ALGORITHM, IS_ACTIVE, DESCRIPTION
FROM ENCRYPTION_KEYS
WHERE KEY_NAME LIKE 'LEGACY_%'
ORDER BY KEY_NAME;
