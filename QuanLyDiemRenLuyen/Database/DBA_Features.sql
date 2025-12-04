-- ============================================
-- Oracle DBA Features - Database Objects
-- ============================================

-- ==================== TABLESPACE MONITORING ====================

-- View để xem thông tin tablespace usage
CREATE OR REPLACE VIEW V_TABLESPACE_USAGE AS
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

-- ==================== PROFILE MANAGEMENT ====================

-- View để xem user profiles và resource limits
CREATE OR REPLACE VIEW V_USER_PROFILES AS
SELECT 
    profile AS PROFILE_NAME,
    resource_name AS RESOURCE_NAME,
    resource_type AS RESOURCE_TYPE,
    limit AS LIMIT_VALUE
FROM dba_profiles
ORDER BY profile, resource_name;

-- Stored Procedure: Tạo profile mới
CREATE OR REPLACE PROCEDURE SP_CREATE_USER_PROFILE(
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

-- ==================== SESSION MANAGEMENT ====================

-- View để xem active sessions
CREATE OR REPLACE VIEW V_ACTIVE_SESSIONS AS
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

-- Stored Procedure: Kill session
CREATE OR REPLACE PROCEDURE SP_KILL_USER_SESSION(
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

-- ==================== PERMISSIONS ====================
-- Grant necessary permissions to application user
-- IMPORTANT: Run these as SYSDBA or user with DBA privileges

-- Example (replace 'YOUR_APP_USER' with actual username):
/*
GRANT SELECT ON DBA_DATA_FILES TO YOUR_APP_USER;
GRANT SELECT ON DBA_FREE_SPACE TO YOUR_APP_USER;
GRANT SELECT ON DBA_PROFILES TO YOUR_APP_USER;
GRANT SELECT ON V_$SESSION TO YOUR_APP_USER;
GRANT ALTER SYSTEM TO YOUR_APP_USER;
GRANT CREATE PROFILE TO YOUR_APP_USER;

-- Grant execute on procedures
GRANT EXECUTE ON SP_CREATE_USER_PROFILE TO YOUR_APP_USER;
GRANT EXECUTE ON SP_KILL_USER_SESSION TO YOUR_APP_USER;
*/

-- ==================== VERIFICATION ====================
-- Test queries to verify views are working

-- Test tablespace view
SELECT * FROM V_TABLESPACE_USAGE;

-- Test profile view
SELECT * FROM V_USER_PROFILES WHERE PROFILE_NAME = 'DEFAULT';

-- Test session view
SELECT * FROM V_ACTIVE_SESSIONS;

-- ==================== CLEANUP (Optional) ====================
-- To drop these objects if needed:
/*
DROP VIEW V_TABLESPACE_USAGE;
DROP VIEW V_USER_PROFILES;
DROP VIEW V_ACTIVE_SESSIONS;
DROP PROCEDURE SP_CREATE_USER_PROFILE;
DROP PROCEDURE SP_KILL_USER_SESSION;
*/
