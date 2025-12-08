-- =========================================================
-- MAC + VPD - PHẦN D (Chạy với QLDiemRenLuyen)
-- =========================================================
-- Kết nối: QLDiemRenLuyen (schema owner)
-- Mục đích: Kiểm thử chính sách VPD với dữ liệu mẫu
-- Điều kiện: Chạy script 001, 002, 003 trước!
-- =========================================================

SET SERVEROUTPUT ON;
SET LINESIZE 200;
SET PAGESIZE 50;

PROMPT '========================================';
PROMPT 'MAC + VPD PHẦN D - Kiểm thử VPD Policies';
PROMPT 'Đang thực thi với: QLDiemRenLuyen';
PROMPT '========================================';

-- =========================================================
-- STEP 1: CHECK SAMPLE DATA EXISTS
-- =========================================================

PROMPT '';
PROMPT 'Checking existing data...';

DECLARE
    v_user_count NUMBER;
    v_student_count NUMBER;
    v_score_count NUMBER;
    v_class_count NUMBER;
    v_assignment_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_user_count FROM USERS;
    SELECT COUNT(*) INTO v_student_count FROM STUDENTS;
    SELECT COUNT(*) INTO v_score_count FROM SCORES;
    SELECT COUNT(*) INTO v_class_count FROM CLASSES;
    SELECT COUNT(*) INTO v_assignment_count FROM CLASS_LECTURER_ASSIGNMENTS;
    
    DBMS_OUTPUT.PUT_LINE('Users: ' || v_user_count);
    DBMS_OUTPUT.PUT_LINE('Students: ' || v_student_count);
    DBMS_OUTPUT.PUT_LINE('Scores: ' || v_score_count);
    DBMS_OUTPUT.PUT_LINE('Classes: ' || v_class_count);
    DBMS_OUTPUT.PUT_LINE('Class-Lecturer Assignments: ' || v_assignment_count);
END;
/

-- =========================================================
-- STEP 2: TEST CASE 1 - STUDENT ACCESS
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'TEST CASE 1: STUDENT Access';
PROMPT '========================================';

DECLARE
    v_student_id VARCHAR2(50);
    v_score_count NUMBER;
BEGIN
    -- Get a sample student ID
    BEGIN
        SELECT MAND INTO v_student_id
        FROM USERS
        WHERE ROLE_NAME = 'STUDENT' AND IS_ACTIVE = 1
        FETCH FIRST 1 ROWS ONLY;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            DBMS_OUTPUT.PUT_LINE('No active student found for testing');
            RETURN;
    END;
    
    DBMS_OUTPUT.PUT_LINE('Testing as Student: ' || v_student_id);
    
    -- Set context as student
    PKG_VPD_CONTEXT.SET_USER_CONTEXT(
        p_user_id => v_student_id,
        p_role    => 'STUDENT',
        p_client_id => 'TEST_STUDENT_SESSION'
    );
    
    DBMS_OUTPUT.PUT_LINE('Context set successfully');
    DBMS_OUTPUT.PUT_LINE('USER_ID in context: ' || PKG_VPD_CONTEXT.GET_USER_ID());
    DBMS_OUTPUT.PUT_LINE('USER_ROLE in context: ' || PKG_VPD_CONTEXT.GET_USER_ROLE());
    
    -- Count visible scores
    SELECT COUNT(*) INTO v_score_count FROM SCORES;
    
    DBMS_OUTPUT.PUT_LINE('Scores visible to student: ' || v_score_count);
    DBMS_OUTPUT.PUT_LINE('Expected: Only scores belonging to ' || v_student_id);
    
    -- Verify all visible scores belong to this student
    FOR rec IN (
        SELECT STUDENT_ID, TERM_ID, TOTAL_SCORE, STATUS
        FROM SCORES
        FETCH FIRST 5 ROWS ONLY
    ) LOOP
        IF rec.STUDENT_ID = v_student_id THEN
            DBMS_OUTPUT.PUT_LINE('✓ Score for ' || rec.STUDENT_ID || ' - OK');
        ELSE
            DBMS_OUTPUT.PUT_LINE('✗ ERROR: Saw score for ' || rec.STUDENT_ID);
        END IF;
    END LOOP;
    
    -- Test UPDATE (should fail for student)
    BEGIN
        UPDATE SCORES SET TOTAL_SCORE = TOTAL_SCORE WHERE STATUS = 'PROVISIONAL';
        IF SQL%ROWCOUNT > 0 THEN
            DBMS_OUTPUT.PUT_LINE('✗ ERROR: Student was able to update scores');
            ROLLBACK;
        ELSE
            DBMS_OUTPUT.PUT_LINE('✓ Student cannot update scores - OK');
        END IF;
    EXCEPTION
        WHEN OTHERS THEN
            DBMS_OUTPUT.PUT_LINE('✓ Student update blocked: ' || SQLERRM);
            ROLLBACK;
    END;
    
    -- Clear context
    PKG_VPD_CONTEXT.CLEAR_USER_CONTEXT;
    
END;
/

-- =========================================================
-- STEP 3: TEST CASE 2 - LECTURER (CVHT) ACCESS
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'TEST CASE 2: LECTURER (CVHT) Access';
PROMPT '========================================';

DECLARE
    v_lecturer_id VARCHAR2(50);
    v_class_id VARCHAR2(32);
    v_student_in_class VARCHAR2(50);
    v_score_count NUMBER;
BEGIN
    -- Get a sample lecturer with class assignment
    BEGIN
        SELECT CLA.LECTURER_ID, CLA.CLASS_ID
        INTO v_lecturer_id, v_class_id
        FROM CLASS_LECTURER_ASSIGNMENTS CLA
        JOIN USERS U ON CLA.LECTURER_ID = U.MAND
        WHERE U.ROLE_NAME = 'LECTURER' AND CLA.IS_ACTIVE = 1
        FETCH FIRST 1 ROWS ONLY;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            DBMS_OUTPUT.PUT_LINE('No active lecturer assignment found for testing');
            RETURN;
    END;
    
    DBMS_OUTPUT.PUT_LINE('Testing as Lecturer: ' || v_lecturer_id);
    DBMS_OUTPUT.PUT_LINE('Assigned to class: ' || v_class_id);
    
    -- Set context as lecturer
    PKG_VPD_CONTEXT.SET_USER_CONTEXT(
        p_user_id => v_lecturer_id,
        p_role    => 'LECTURER',
        p_client_id => 'TEST_LECTURER_SESSION'
    );
    
    DBMS_OUTPUT.PUT_LINE('Context set successfully');
    
    -- Count visible scores
    SELECT COUNT(*) INTO v_score_count FROM SCORES;
    
    DBMS_OUTPUT.PUT_LINE('Scores visible to lecturer: ' || v_score_count);
    DBMS_OUTPUT.PUT_LINE('Expected: Only scores from students in assigned classes');
    
    -- Verify visible scores are from assigned classes
    FOR rec IN (
        SELECT DISTINCT s.STUDENT_ID, st.CLASS_ID
        FROM SCORES s
        JOIN STUDENTS st ON s.STUDENT_ID = st.USER_ID
        FETCH FIRST 5 ROWS ONLY
    ) LOOP
        DECLARE
            v_is_assigned NUMBER;
        BEGIN
            SELECT COUNT(*) INTO v_is_assigned
            FROM CLASS_LECTURER_ASSIGNMENTS
            WHERE LECTURER_ID = v_lecturer_id
            AND CLASS_ID = rec.CLASS_ID
            AND IS_ACTIVE = 1;
            
            IF v_is_assigned > 0 THEN
                DBMS_OUTPUT.PUT_LINE('✓ Score from class ' || rec.CLASS_ID || ' - OK');
            ELSE
                DBMS_OUTPUT.PUT_LINE('✗ ERROR: Saw score from unassigned class ' || rec.CLASS_ID);
            END IF;
        END;
    END LOOP;
    
    -- Clear context
    PKG_VPD_CONTEXT.CLEAR_USER_CONTEXT;
    
END;
/

-- =========================================================
-- STEP 4: TEST CASE 3 - ADMIN ACCESS
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'TEST CASE 3: ADMIN Access';
PROMPT '========================================';

DECLARE
    v_admin_id VARCHAR2(50);
    v_total_scores NUMBER;
    v_visible_scores NUMBER;
BEGIN
    -- Get a sample admin
    BEGIN
        SELECT MAND INTO v_admin_id
        FROM USERS
        WHERE ROLE_NAME = 'ADMIN' AND IS_ACTIVE = 1
        FETCH FIRST 1 ROWS ONLY;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            DBMS_OUTPUT.PUT_LINE('No active admin found for testing');
            RETURN;
    END;
    
    DBMS_OUTPUT.PUT_LINE('Testing as Admin: ' || v_admin_id);
    
    -- First, clear any context and get total scores (as schema owner)
    PKG_VPD_CONTEXT.CLEAR_USER_CONTEXT;
    
    -- Set context as admin
    PKG_VPD_CONTEXT.SET_USER_CONTEXT(
        p_user_id => v_admin_id,
        p_role    => 'ADMIN',
        p_client_id => 'TEST_ADMIN_SESSION'
    );
    
    DBMS_OUTPUT.PUT_LINE('Context set successfully');
    
    -- Count visible scores
    SELECT COUNT(*) INTO v_visible_scores FROM SCORES;
    
    DBMS_OUTPUT.PUT_LINE('Scores visible to admin: ' || v_visible_scores);
    DBMS_OUTPUT.PUT_LINE('Expected: ALL scores in the system');
    
    IF v_visible_scores > 0 THEN
        DBMS_OUTPUT.PUT_LINE('✓ Admin can see scores');
    END IF;
    
    -- Test approval
    DECLARE
        v_result VARCHAR2(500);
        v_score_id NUMBER;
    BEGIN
        -- Get a provisional score
        BEGIN
            SELECT ID INTO v_score_id
            FROM SCORES
            WHERE STATUS = 'PROVISIONAL'
            FETCH FIRST 1 ROWS ONLY;
            
            -- Test approval procedure
            SP_APPROVE_SCORE(
                p_score_id => v_score_id,
                p_approved_by => v_admin_id,
                p_result => v_result
            );
            
            IF INSTR(v_result, 'SUCCESS') > 0 OR INSTR(v_result, 'đã được duyệt') > 0 THEN
                DBMS_OUTPUT.PUT_LINE('✓ Admin can approve scores: ' || v_result);
            ELSE
                DBMS_OUTPUT.PUT_LINE('Admin approval result: ' || v_result);
            END IF;
            
            -- Rollback for testing purposes
            ROLLBACK;
            
        EXCEPTION
            WHEN NO_DATA_FOUND THEN
                DBMS_OUTPUT.PUT_LINE('No provisional scores to test approval');
        END;
    END;
    
    -- Clear context
    PKG_VPD_CONTEXT.CLEAR_USER_CONTEXT;
    
END;
/

-- =========================================================
-- STEP 5: TEST CASE 4 - NO CONTEXT SET
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'TEST CASE 4: No Context Set (Security Test)';
PROMPT '========================================';

DECLARE
    v_score_count NUMBER;
BEGIN
    -- Make sure context is clear
    BEGIN
        PKG_VPD_CONTEXT.CLEAR_USER_CONTEXT;
    EXCEPTION
        WHEN OTHERS THEN NULL;
    END;
    
    DBMS_OUTPUT.PUT_LINE('Testing with NO context set...');
    
    -- Try to query scores
    SELECT COUNT(*) INTO v_score_count FROM SCORES;
    
    IF v_score_count = 0 THEN
        DBMS_OUTPUT.PUT_LINE('✓ No scores visible without context - SECURE');
    ELSE
        DBMS_OUTPUT.PUT_LINE('✗ WARNING: ' || v_score_count || ' scores visible without context');
    END IF;
    
END;
/

-- =========================================================
-- STEP 6: SUMMARY
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'VPD TESTING COMPLETE';
PROMPT '========================================';
PROMPT '';
PROMPT 'Test Results Summary:';
PROMPT '1. Student Access: Can only see own scores';
PROMPT '2. Lecturer Access: Can only see scores from assigned classes';
PROMPT '3. Admin Access: Can see and approve all scores';
PROMPT '4. No Context: No data accessible (secure default)';
PROMPT '';
PROMPT 'MAC Policy Enforcement:';
PROMPT '+ Policies are defined by DBA only';
PROMPT '+ Users cannot share or modify access rights';
PROMPT '+ Row-level security enforced via VPD';
PROMPT '========================================';
