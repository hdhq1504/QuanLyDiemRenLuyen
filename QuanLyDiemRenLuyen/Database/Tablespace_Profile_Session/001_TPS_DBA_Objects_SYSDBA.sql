-- ============================================
-- TẠO VIEWS VÀ PROCEDURES DBA
-- Chạy TOÀN BỘ script này với quyền SYSDBA
-- ============================================

-- ==================== CREATE VIEWS IN SYS SCHEMA ====================

-- 1. Tablespace Usage View
CREATE OR REPLACE VIEW SYS.V_TABLESPACE_USAGE AS
SELECT 
    df.tablespace_name AS TABLESPACE_NAME,
    df.total_space_mb AS TOTAL_SPACE_MB,
    df.total_space_mb - NVL(fs.free_space_mb, 0) AS USED_SPACE_MB,
    NVL(fs.free_space_mb, 0) AS FREE_SPACE_MB,
    ROUND((df.total_space_mb - NVL(fs.free_space_mb, 0)) / df.total_space_mb * 100, 2) AS USAGE_PERCENT
FROM 
    (SELECT tablespace_name, ROUND(SUM(bytes)/1024/1024, 2) AS total_space_mb
     FROM dba_data_files
     GROUP BY tablespace_name) df
LEFT JOIN 
    (SELECT tablespace_name, ROUND(SUM(bytes)/1024/1024, 2) AS free_space_mb
     FROM dba_free_space
     GROUP BY tablespace_name) fs
ON df.tablespace_name = fs.tablespace_name;

-- 2. User Profiles View
CREATE OR REPLACE VIEW SYS.V_USER_PROFILES AS
SELECT 
    profile AS PROFILE_NAME,
    resource_name AS RESOURCE_NAME,
    resource_type AS RESOURCE_TYPE,
    limit AS LIMIT_VALUE
FROM dba_profiles
ORDER BY profile, resource_name;

-- 3. Active Sessions View
CREATE OR REPLACE VIEW SYS.V_ACTIVE_SESSIONS AS
SELECT 
    s.sid AS SID,
    s.serial# AS SERIAL,
    s.username AS USERNAME,
    s.status AS STATUS,
    s.schemaname AS SCHEMA_NAME,
    s.osuser AS OS_USER,
    s.machine AS MACHINE,
    s.program AS PROGRAM,
    s.logon_time AS LOGON_TIME,
    ROUND((SYSDATE - s.logon_time) * 24 * 60) AS MINUTES_CONNECTED,
    s.last_call_et AS SECONDS_SINCE_LAST_CALL
FROM v$session s
WHERE s.username IS NOT NULL
  AND s.type = 'USER'
ORDER BY s.logon_time DESC;

-- ==================== GRANT SELECT ON VIEWS ====================

GRANT SELECT ON SYS.V_TABLESPACE_USAGE TO QLDiemRenLuyen;
GRANT SELECT ON SYS.V_USER_PROFILES TO QLDiemRenLuyen;
GRANT SELECT ON SYS.V_ACTIVE_SESSIONS TO QLDiemRenLuyen;

-- ==================== CREATE SYNONYMS FOR CONVENIENCE ====================
-- This allows QLDiemRenLuyen to use "V_TABLESPACE_USAGE" instead of "SYS.V_TABLESPACE_USAGE"

CREATE OR REPLACE PUBLIC SYNONYM V_TABLESPACE_USAGE FOR SYS.V_TABLESPACE_USAGE;
CREATE OR REPLACE PUBLIC SYNONYM V_USER_PROFILES FOR SYS.V_USER_PROFILES;
CREATE OR REPLACE PUBLIC SYNONYM V_ACTIVE_SESSIONS FOR SYS.V_ACTIVE_SESSIONS;

-- ==================== CREATE STORED PROCEDURES IN QLDiemRenLuyen SCHEMA ====================

-- Switch to QLDiemRenLuyen schema for procedures
-- Note: These will be created in QLDiemRenLuyen's schema

-- 1. Create Profile Procedure
CREATE OR REPLACE PROCEDURE QLDiemRenLuyen.SP_CREATE_USER_PROFILE(
    p_profile_name IN VARCHAR2,
    p_sessions_per_user IN NUMBER DEFAULT NULL,
    p_cpu_per_session IN NUMBER DEFAULT NULL,
    p_idle_time IN NUMBER DEFAULT NULL,
    p_connect_time IN NUMBER DEFAULT NULL
) AS
    v_sql VARCHAR2(1000);
BEGIN
    v_sql := 'CREATE PROFILE ' || p_profile_name || ' LIMIT ';
    v_sql := v_sql || 'SESSIONS_PER_USER ' || NVL(TO_CHAR(p_sessions_per_user), 'UNLIMITED') || ' ';
    v_sql := v_sql || 'CPU_PER_SESSION ' || NVL(TO_CHAR(p_cpu_per_session), 'UNLIMITED') || ' ';
    v_sql := v_sql || 'IDLE_TIME ' || NVL(TO_CHAR(p_idle_time), 'UNLIMITED') || ' ';
    v_sql := v_sql || 'CONNECT_TIME ' || NVL(TO_CHAR(p_connect_time), 'UNLIMITED');
    
    EXECUTE IMMEDIATE v_sql;
    
    COMMIT;
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        RAISE;
END;
/

-- 2. Kill Session Procedure
CREATE OR REPLACE PROCEDURE QLDiemRenLuyen.SP_KILL_USER_SESSION(
    p_sid IN NUMBER,
    p_serial IN NUMBER
) AS
    v_sql VARCHAR2(200);
BEGIN
    v_sql := 'ALTER SYSTEM KILL SESSION ''' || p_sid || ',' || p_serial || ''' IMMEDIATE';
    EXECUTE IMMEDIATE v_sql;
    
    COMMIT;
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        RAISE;
END;
/

-- ==================== VERIFICATION ====================

-- Test views
SELECT COUNT(*) as TABLESPACE_COUNT FROM V_TABLESPACE_USAGE;
SELECT COUNT(*) as PROFILE_COUNT FROM V_USER_PROFILES;
SELECT COUNT(*) as SESSION_COUNT FROM V_ACTIVE_SESSIONS;

-- Verify objects created
SELECT object_name, object_type, owner, status 
FROM dba_objects 
WHERE object_name IN ('V_TABLESPACE_USAGE', 'V_USER_PROFILES', 'V_ACTIVE_SESSIONS',
                      'SP_CREATE_USER_PROFILE', 'SP_KILL_USER_SESSION')
ORDER BY owner, object_type, object_name;

-- ==================== SUCCESS MESSAGE ====================

BEGIN
    DBMS_OUTPUT.PUT_LINE('==============================================');
    DBMS_OUTPUT.PUT_LINE('SUCCESS! All objects created successfully');
    DBMS_OUTPUT.PUT_LINE('==============================================');
    DBMS_OUTPUT.PUT_LINE('Created Views (in SYS schema):');
    DBMS_OUTPUT.PUT_LINE('  - V_TABLESPACE_USAGE');
    DBMS_OUTPUT.PUT_LINE('  - V_USER_PROFILES');
    DBMS_OUTPUT.PUT_LINE('  - V_ACTIVE_SESSIONS');
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('Created Procedures (in QLDiemRenLuyen schema):');
    DBMS_OUTPUT.PUT_LINE('  - SP_CREATE_USER_PROFILE');
    DBMS_OUTPUT.PUT_LINE('  - SP_KILL_USER_SESSION');
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('Granted SELECT to QLDiemRenLuyen on all views');
    DBMS_OUTPUT.PUT_LINE('Created public synonyms for easy access');
    DBMS_OUTPUT.PUT_LINE('==============================================');
END;
/

COMMIT;
