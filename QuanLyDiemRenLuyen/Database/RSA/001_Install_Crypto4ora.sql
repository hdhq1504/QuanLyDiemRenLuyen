-- ============================================================================
-- Script Cài đặt crypto4ora
-- 
-- ĐIỀU KIỆN TIÊN QUYẾT:
-- 1. Oracle JVM phải được bật (SELECT * FROM V$OPTION WHERE PARAMETER = 'Java')
-- 2. Tải crypto4ora.jar từ: https://github.com/AlessandroVaccarino/crypto4ora
-- 3. Phải chạy với SYSDBA hoặc có quyền DBA
-- ============================================================================

-- ============================================================================
-- Step 1: Verify Oracle JVM is enabled
-- ============================================================================

SELECT 'Oracle JVM Status:' AS INFO FROM DUAL;
SELECT PARAMETER, VALUE 
FROM V$OPTION 
WHERE PARAMETER = 'Java';

-- If Java is not enabled, you need to install it:
-- @?/javavm/install/initjvm.sql (as SYSDBA)

-- ============================================================================
-- Step 2: Load crypto4ora.jar using OS command
-- Run this from command line as DBA:
-- 
-- loadjava -user QLDiemRenLuyen/<password>@<database> -resolve -verbose crypto4ora.jar
-- 
-- Or using SQL*Loader from within SQL*Plus:
-- ============================================================================

/*
-- Example loadjava command (run from OS command line):
loadjava -user QLDiemRenLuyen/YourPassword@orcl -resolve -verbose crypto4ora.jar

-- Alternative using dbms_java (if already loaded but not resolved):
BEGIN
    DBMS_JAVA.SET_OUTPUT(1000000);
    DBMS_JAVA.LOADJAVA('-verbose -resolve crypto4ora.jar');
END;
/
*/

-- ============================================================================
-- Step 3: Run crypto4ora.sql to create the CRYPTO package
-- This file should be provided with crypto4ora.jar
-- ============================================================================

/*
-- Run the SQL script that comes with crypto4ora:
-- @crypto4ora.sql

-- Or if you downloaded from GitHub, the package creation is usually:
-- CREATE OR REPLACE AND COMPILE JAVA SOURCE NAMED "Crypto4ora" AS
-- ... (Java source code)
-- /
*/

-- ============================================================================
-- Step 4: Grant execute permission to roles
-- ============================================================================
-- Note: CRYPTO package is installed in QLDiemRenLuyen schema, NOT in SYS
-- QLDiemRenLuyen is the owner, so no need to grant to itself

-- Grant to roles for access control (run these as QLDiemRenLuyen, not SYSDBA)
-- GRANT EXECUTE ON CRYPTO TO ROLE_ADMIN;
-- If running with SYSDBA, use full qualified name:
GRANT EXECUTE ON QLDiemRenLuyen.CRYPTO TO ROLE_ADMIN;

-- ============================================================================
-- Step 5: Verify installation
-- ============================================================================

SELECT 'Verifying CRYPTO package installation:' AS INFO FROM DUAL;

SELECT OBJECT_NAME, OBJECT_TYPE, STATUS
FROM ALL_OBJECTS
WHERE OBJECT_NAME = 'CRYPTO'
  AND OBJECT_TYPE IN ('PACKAGE', 'PACKAGE BODY', 'JAVA CLASS');

-- ============================================================================
-- Step 6: Test RSA key generation (basic test)
-- ============================================================================

-- Test RSA key generation (run as QLDiemRenLuyen user):
/*
DECLARE
    v_key_pair CLOB;
BEGIN
    -- Generate 2048-bit RSA key pair
    v_key_pair := CRYPTO.RSA_GENERATE_KEYS(2048);
    DBMS_OUTPUT.PUT_LINE('Key pair generated successfully');
    DBMS_OUTPUT.PUT_LINE('Length: ' || LENGTH(v_key_pair));
END;
/
*/
