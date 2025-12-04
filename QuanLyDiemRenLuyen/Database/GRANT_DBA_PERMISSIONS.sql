-- ============================================
-- GRANT PERMISSIONS FOR ORACLE DBA FEATURES
-- Run this script as SYSDBA user
-- ============================================

-- User: QLDiemRenLuyen
-- Purpose: Grant necessary permissions for Database monitoring and management features

-- ==================== TABLESPACE MONITORING ====================

-- Grant SELECT on DBA views for tablespace information
GRANT SELECT ON DBA_DATA_FILES TO QLDiemRenLuyen;
GRANT SELECT ON DBA_FREE_SPACE TO QLDiemRenLuyen;
GRANT SELECT ON DBA_TABLESPACES TO QLDiemRenLuyen;

-- ==================== PROFILE MANAGEMENT ====================

-- Grant SELECT on DBA_PROFILES to view profile information
GRANT SELECT ON DBA_PROFILES TO QLDiemRenLuyen;

-- Grant CREATE PROFILE privilege (optional - only if you want to create new profiles)
GRANT CREATE PROFILE TO QLDiemRenLuyen;

-- ==================== SESSION MANAGEMENT ====================

-- Grant SELECT on V$SESSION to view active sessions
-- Note: V$SESSION is a synonym for V_$SESSION
GRANT SELECT ON V_$SESSION TO QLDiemRenLuyen;

-- Grant ALTER SYSTEM to kill sessions
-- WARNING: This is a powerful privilege. Use with caution!
GRANT ALTER SYSTEM TO QLDiemRenLuyen;

-- ==================== STORED PROCEDURES ====================

-- Grant EXECUTE on stored procedures created in DBA_Features.sql
-- These procedures should be owned by QLDiemRenLuyen, so no additional grants needed
-- But if they were created by SYSDBA, you would need:
-- GRANT EXECUTE ON SP_CREATE_USER_PROFILE TO QLDiemRenLuyen;
-- GRANT EXECUTE ON SP_KILL_USER_SESSION TO QLDiemRenLuyen;

-- ==================== ADDITIONAL USEFUL GRANTS (OPTIONAL) ====================

-- For more detailed session information
GRANT SELECT ON V_$PROCESS TO QLDiemRenLuyen;
GRANT SELECT ON V_$SQL TO QLDiemRenLuyen;

-- For database statistics
GRANT SELECT ON V_$DATABASE TO QLDiemRenLuyen;
GRANT SELECT ON V_$INSTANCE TO QLDiemRenLuyen;

-- ==================== VERIFICATION ====================

-- Verify granted privileges
SELECT * FROM DBA_SYS_PRIVS WHERE GRANTEE = 'QLDIEMDRENLUYEN';
SELECT * FROM DBA_TAB_PRIVS WHERE GRANTEE = 'QLDIEMDRENLUYEN';

-- ==================== SUCCESS MESSAGE ====================

BEGIN
    DBMS_OUTPUT.PUT_LINE('Successfully granted all required privileges to QLDiemRenLuyen');
    DBMS_OUTPUT.PUT_LINE('User can now:');
    DBMS_OUTPUT.PUT_LINE('  - View tablespace usage');
    DBMS_OUTPUT.PUT_LINE('  - View and manage user profiles');
    DBMS_OUTPUT.PUT_LINE('  - View and kill sessions');
END;
/

COMMIT;
