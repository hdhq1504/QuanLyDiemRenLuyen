-- =========================================================
-- Chạy với: QLDiemRenLuyen
-- Mục đích: Chèn dữ liệu mẫu cho các bảng chính để test UI
-- =========================================================

SET SERVEROUTPUT ON;

PROMPT '========================================';
PROMPT 'Cleaning up existing sample data...';
PROMPT '========================================';

-- Xóa dữ liệu cũ (nếu có)
BEGIN
    DELETE FROM NOTIFICATION_READS;
    DELETE FROM FEEDBACK_ATTACHMENTS;
    DELETE FROM FEEDBACKS;
    DELETE FROM PROOFS;
    DELETE FROM REGISTRATIONS;
    DELETE FROM ACTIVITIES;
    DELETE FROM SCORES;
    DELETE FROM NOTIFICATIONS;
    DELETE FROM CLASS_LECTURER_ASSIGNMENTS;
    DELETE FROM STUDENTS;
    DELETE FROM USERS;
    DELETE FROM TERMS;
    DELETE FROM CLASSES;
    DELETE FROM DEPARTMENTS;
    COMMIT;
    DBMS_OUTPUT.PUT_LINE('✓ Cleanup completed');
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Cleanup warning: ' || SQLERRM);
END;
/

PROMPT '========================================';
PROMPT 'Inserting Sample Data';
PROMPT '========================================';

-- =========================================================
-- DECLARE VARIABLES FOR IDs
-- =========================================================
DECLARE
    -- Department IDs
    v_dept_cntt VARCHAR2(32);
    v_dept_qtkd VARCHAR2(32);
    v_dept_cntp VARCHAR2(32);
    
    -- Class IDs
    v_class_1 VARCHAR2(32);
    v_class_2 VARCHAR2(32);
    v_class_3 VARCHAR2(32);
    v_class_4 VARCHAR2(32);
    v_class_5 VARCHAR2(32);
    
    -- Term IDs
    v_term_hk1 VARCHAR2(32);
    v_term_hk2 VARCHAR2(32);
    v_term_hk3 VARCHAR2(32);
    
    -- Activity IDs
    v_act_1 VARCHAR2(32);
    v_act_2 VARCHAR2(32);
    v_act_3 VARCHAR2(32);
    v_act_4 VARCHAR2(32);
    v_act_5 VARCHAR2(32);
    
    -- Registration IDs
    v_reg_1 VARCHAR2(32);
    v_reg_2 VARCHAR2(32);
    v_reg_3 VARCHAR2(32);
    v_reg_4 VARCHAR2(32);
    v_reg_5 VARCHAR2(32);
    v_reg_6 VARCHAR2(32);
    v_reg_7 VARCHAR2(32);
    v_reg_8 VARCHAR2(32);
    v_reg_9 VARCHAR2(32);
    v_reg_10 VARCHAR2(32);
    
    -- Notification IDs
    v_noti_1 VARCHAR2(32);
    v_noti_2 VARCHAR2(32);
    v_noti_3 VARCHAR2(32);
    
    -- Feedback IDs
    v_fb_1 VARCHAR2(32);
    v_fb_2 VARCHAR2(32);
    v_fb_3 VARCHAR2(32);
    
    -- Assignment IDs
    v_assign_1 VARCHAR2(32);
    v_assign_2 VARCHAR2(32);
    v_assign_3 VARCHAR2(32);
    
BEGIN
    -- Generate IDs
    v_dept_cntt := RAWTOHEX(SYS_GUID());
    v_dept_qtkd := RAWTOHEX(SYS_GUID());
    v_dept_cntp := RAWTOHEX(SYS_GUID());
    
    v_class_1 := RAWTOHEX(SYS_GUID());
    v_class_2 := RAWTOHEX(SYS_GUID());
    v_class_3 := RAWTOHEX(SYS_GUID());
    v_class_4 := RAWTOHEX(SYS_GUID());
    v_class_5 := RAWTOHEX(SYS_GUID());
    
    v_term_hk1 := RAWTOHEX(SYS_GUID());
    v_term_hk2 := RAWTOHEX(SYS_GUID());
    v_term_hk3 := RAWTOHEX(SYS_GUID());
    
    v_act_1 := RAWTOHEX(SYS_GUID());
    v_act_2 := RAWTOHEX(SYS_GUID());
    v_act_3 := RAWTOHEX(SYS_GUID());
    v_act_4 := RAWTOHEX(SYS_GUID());
    v_act_5 := RAWTOHEX(SYS_GUID());
    
    v_reg_1 := RAWTOHEX(SYS_GUID());
    v_reg_2 := RAWTOHEX(SYS_GUID());
    v_reg_3 := RAWTOHEX(SYS_GUID());
    v_reg_4 := RAWTOHEX(SYS_GUID());
    v_reg_5 := RAWTOHEX(SYS_GUID());
    v_reg_6 := RAWTOHEX(SYS_GUID());
    v_reg_7 := RAWTOHEX(SYS_GUID());
    v_reg_8 := RAWTOHEX(SYS_GUID());
    v_reg_9 := RAWTOHEX(SYS_GUID());
    v_reg_10 := RAWTOHEX(SYS_GUID());
    
    v_noti_1 := RAWTOHEX(SYS_GUID());
    v_noti_2 := RAWTOHEX(SYS_GUID());
    v_noti_3 := RAWTOHEX(SYS_GUID());
    
    v_fb_1 := RAWTOHEX(SYS_GUID());
    v_fb_2 := RAWTOHEX(SYS_GUID());
    v_fb_3 := RAWTOHEX(SYS_GUID());
    
    v_assign_1 := RAWTOHEX(SYS_GUID());
    v_assign_2 := RAWTOHEX(SYS_GUID());
    v_assign_3 := RAWTOHEX(SYS_GUID());
    
    -- =========================================================
    -- 1. DEPARTMENTS
    -- =========================================================
    DBMS_OUTPUT.PUT_LINE('Inserting DEPARTMENTS...');
    
    INSERT INTO DEPARTMENTS (ID, CODE, NAME) VALUES (v_dept_cntt, 'CNTT', N'Công nghệ thông tin');
    INSERT INTO DEPARTMENTS (ID, CODE, NAME) VALUES (v_dept_qtkd, 'QTKD', N'Quản trị kinh doanh');
    INSERT INTO DEPARTMENTS (ID, CODE, NAME) VALUES (v_dept_cntp, 'CNTP', N'Công nghệ thực phẩm');
    
    DBMS_OUTPUT.PUT_LINE('✓ Inserted DEPARTMENTS');
    
    -- =========================================================
    -- 2. CLASSES
    -- =========================================================
    DBMS_OUTPUT.PUT_LINE('Inserting CLASSES...');
    
    INSERT INTO CLASSES (ID, CODE, NAME, DEPARTMENT_ID) VALUES (v_class_1, '13DHTH01', '13DHTH01', v_dept_cntt);
    INSERT INTO CLASSES (ID, CODE, NAME, DEPARTMENT_ID) VALUES (v_class_2, '13DHTH02', '13DHTH02', v_dept_cntt);
    INSERT INTO CLASSES (ID, CODE, NAME, DEPARTMENT_ID) VALUES (v_class_3, '13DHTH03', '13DHTH03', v_dept_cntt);
    INSERT INTO CLASSES (ID, CODE, NAME, DEPARTMENT_ID) VALUES (v_class_4, '13DHQTKD01', '13DHQTKD01', v_dept_qtkd);
    INSERT INTO CLASSES (ID, CODE, NAME, DEPARTMENT_ID) VALUES (v_class_5, '13DHTP01', '13DHTP01', v_dept_cntp);
    
    DBMS_OUTPUT.PUT_LINE('✓ Inserted CLASSES');
    
    -- =========================================================
    -- 3. TERMS
    -- =========================================================
    DBMS_OUTPUT.PUT_LINE('Inserting TERMS...');
    
    INSERT INTO TERMS (ID, NAME, YEAR, TERM_NUMBER, START_DATE, END_DATE, IS_CURRENT, SCORE_STATUS)
    VALUES (v_term_hk1, N'Học kỳ 1 - 2024-2025', 2024, 1, 
            TO_DATE('2024-09-01', 'YYYY-MM-DD'), TO_DATE('2025-01-31', 'YYYY-MM-DD'), 
            1, 'PROVISIONAL');
    
    INSERT INTO TERMS (ID, NAME, YEAR, TERM_NUMBER, START_DATE, END_DATE, IS_CURRENT, SCORE_STATUS)
    VALUES (v_term_hk2, N'Học kỳ 2 - 2023-2024', 2023, 2, 
            TO_DATE('2024-02-01', 'YYYY-MM-DD'), TO_DATE('2024-05-31', 'YYYY-MM-DD'), 
            0, 'OFFICIAL');
    
    INSERT INTO TERMS (ID, NAME, YEAR, TERM_NUMBER, START_DATE, END_DATE, IS_CURRENT, SCORE_STATUS)
    VALUES (v_term_hk3, N'Học kỳ 3 - 2023-2024', 2023, 3, 
            TO_DATE('2024-06-01', 'YYYY-MM-DD'), TO_DATE('2024-08-31', 'YYYY-MM-DD'), 
            0, 'OFFICIAL');
    
    DBMS_OUTPUT.PUT_LINE('✓ Inserted TERMS');
    
    -- =========================================================
    -- 4. USERS
    -- =========================================================
    DBMS_OUTPUT.PUT_LINE('Inserting USERS...');
    
    -- Admin
    INSERT INTO USERS (MAND, EMAIL, FULL_NAME, ROLE_NAME, PASSWORD_HASH, PASSWORD_SALT, IS_ACTIVE)
    VALUES ('AD001', 'quanhd@huit.edu.vn', N'Hồ Đức Quân', 'ADMIN', '123456', 'salt123', 1);
    INSERT INTO USERS (MAND, EMAIL, FULL_NAME, ROLE_NAME, PASSWORD_HASH, PASSWORD_SALT, IS_ACTIVE)
    VALUES ('AD002', 'binhtt@huit.edu.vn', N'Trương Thanh Bình', 'ADMIN', '123456', 'salt123', 1);
    
    -- Lecturers
    INSERT INTO USERS (MAND, EMAIL, FULL_NAME, ROLE_NAME, PASSWORD_HASH, PASSWORD_SALT, IS_ACTIVE)
    VALUES ('GV001', 'giangtv@huit.edu.vn', N'Trần Văn Giang', 'LECTURER', '123456', 'salt123', 1);
    INSERT INTO USERS (MAND, EMAIL, FULL_NAME, ROLE_NAME, PASSWORD_HASH, PASSWORD_SALT, IS_ACTIVE)
    VALUES ('GV002', 'danhbc@huit.edu.vn', N'Bùi Công Danh', 'LECTURER', '123456', 'salt123', 1);
    INSERT INTO USERS (MAND, EMAIL, FULL_NAME, ROLE_NAME, PASSWORD_HASH, PASSWORD_SALT, IS_ACTIVE)
    VALUES ('GV003', 'hunghv@huit.edu.vn', N'Huỳnh Văn Hùng', 'LECTURER', '123456', 'salt123', 1);
    INSERT INTO USERS (MAND, EMAIL, FULL_NAME, ROLE_NAME, PASSWORD_HASH, PASSWORD_SALT, IS_ACTIVE)
    VALUES ('GV004', 'maitt@huit.edu.vn', N'Trần Thị Mai', 'LECTURER', '123456', 'salt123', 1);
    
    -- Students
    INSERT INTO USERS (MAND, EMAIL, FULL_NAME, ROLE_NAME, PASSWORD_HASH, PASSWORD_SALT, IS_ACTIVE)
    VALUES ('2001221234', '2001221234@huit.edu.vn', N'Nguyễn Văn An', 'STUDENT', '123456', 'salt123', 1);
    INSERT INTO USERS (MAND, EMAIL, FULL_NAME, ROLE_NAME, PASSWORD_HASH, PASSWORD_SALT, IS_ACTIVE)
    VALUES ('2001222345', '2001222345@huit.edu.vn', N'Trần Thị Bình', 'STUDENT', '123456', 'salt123', 1);
    INSERT INTO USERS (MAND, EMAIL, FULL_NAME, ROLE_NAME, PASSWORD_HASH, PASSWORD_SALT, IS_ACTIVE)
    VALUES ('2001223456', '2001223456@huit.edu.vn', N'Lê Văn Cường', 'STUDENT', '123456', 'salt123', 1);
    INSERT INTO USERS (MAND, EMAIL, FULL_NAME, ROLE_NAME, PASSWORD_HASH, PASSWORD_SALT, IS_ACTIVE)
    VALUES ('2001224567', '2001224567@huit.edu.vn', N'Phạm Thị Dung', 'STUDENT', '123456', 'salt123', 1);
    INSERT INTO USERS (MAND, EMAIL, FULL_NAME, ROLE_NAME, PASSWORD_HASH, PASSWORD_SALT, IS_ACTIVE)
    VALUES ('2001225678', '2001225678@huit.edu.vn', N'Hoàng Văn Em', 'STUDENT', '123456', 'salt123', 1);
    INSERT INTO USERS (MAND, EMAIL, FULL_NAME, ROLE_NAME, PASSWORD_HASH, PASSWORD_SALT, IS_ACTIVE)
    VALUES ('2001226789', '2001226789@huit.edu.vn', N'Vũ Thị Phương', 'STUDENT', '123456', 'salt123', 1);
    INSERT INTO USERS (MAND, EMAIL, FULL_NAME, ROLE_NAME, PASSWORD_HASH, PASSWORD_SALT, IS_ACTIVE)
    VALUES ('2001227890', '2001227890@huit.edu.vn', N'Đặng Văn Quang', 'STUDENT', '123456', 'salt123', 1);
    INSERT INTO USERS (MAND, EMAIL, FULL_NAME, ROLE_NAME, PASSWORD_HASH, PASSWORD_SALT, IS_ACTIVE)
    VALUES ('2001228901', '2001228901@huit.edu.vn', N'Ngô Thị Hương', 'STUDENT', '123456', 'salt123', 1);
    INSERT INTO USERS (MAND, EMAIL, FULL_NAME, ROLE_NAME, PASSWORD_HASH, PASSWORD_SALT, IS_ACTIVE)
    VALUES ('2001229012', '2001229012@huit.edu.vn', N'Bùi Văn Khải', 'STUDENT', '123456', 'salt123', 1);
    INSERT INTO USERS (MAND, EMAIL, FULL_NAME, ROLE_NAME, PASSWORD_HASH, PASSWORD_SALT, IS_ACTIVE)
    VALUES ('2001220123', '2001220123@huit.edu.vn', N'Đinh Thị Lan', 'STUDENT', '123456', 'salt123', 1);
    
    DBMS_OUTPUT.PUT_LINE('✓ Inserted USERS');
    
    -- =========================================================
    -- 5. STUDENTS
    -- =========================================================
    DBMS_OUTPUT.PUT_LINE('Inserting STUDENTS...');
    
    INSERT INTO STUDENTS (USER_ID, STUDENT_CODE, CLASS_ID, DEPARTMENT_ID, DATE_OF_BIRTH, GENDER)
    VALUES ('2001221234', '2001221234', v_class_1, v_dept_cntt, TO_DATE('2004-01-15', 'YYYY-MM-DD'), N'Nam');
    INSERT INTO STUDENTS (USER_ID, STUDENT_CODE, CLASS_ID, DEPARTMENT_ID, DATE_OF_BIRTH, GENDER)
    VALUES ('2001222345', '2001222345', v_class_1, v_dept_cntt, TO_DATE('2004-03-22', 'YYYY-MM-DD'), N'Nữ');
    INSERT INTO STUDENTS (USER_ID, STUDENT_CODE, CLASS_ID, DEPARTMENT_ID, DATE_OF_BIRTH, GENDER)
    VALUES ('2001223456', '2001223456', v_class_1, v_dept_cntt, TO_DATE('2004-05-10', 'YYYY-MM-DD'), N'Nam');
    INSERT INTO STUDENTS (USER_ID, STUDENT_CODE, CLASS_ID, DEPARTMENT_ID, DATE_OF_BIRTH, GENDER)
    VALUES ('2001224567', '2001224567', v_class_1, v_dept_cntt, TO_DATE('2004-07-08', 'YYYY-MM-DD'), N'Nữ');
    INSERT INTO STUDENTS (USER_ID, STUDENT_CODE, CLASS_ID, DEPARTMENT_ID, DATE_OF_BIRTH, GENDER)
    VALUES ('2001225678', '2001225678', v_class_1, v_dept_cntt, TO_DATE('2004-09-25', 'YYYY-MM-DD'), N'Nam');
    INSERT INTO STUDENTS (USER_ID, STUDENT_CODE, CLASS_ID, DEPARTMENT_ID, DATE_OF_BIRTH, GENDER)
    VALUES ('2001226789', '2001226789', v_class_2, v_dept_cntt, TO_DATE('2004-02-14', 'YYYY-MM-DD'), N'Nữ');
    INSERT INTO STUDENTS (USER_ID, STUDENT_CODE, CLASS_ID, DEPARTMENT_ID, DATE_OF_BIRTH, GENDER)
    VALUES ('2001227890', '2001227890', v_class_2, v_dept_cntt, TO_DATE('2004-04-30', 'YYYY-MM-DD'), N'Nam');
    INSERT INTO STUDENTS (USER_ID, STUDENT_CODE, CLASS_ID, DEPARTMENT_ID, DATE_OF_BIRTH, GENDER)
    VALUES ('2001228901', '2001228901', v_class_2, v_dept_cntt, TO_DATE('2004-06-18', 'YYYY-MM-DD'), N'Nữ');
    INSERT INTO STUDENTS (USER_ID, STUDENT_CODE, CLASS_ID, DEPARTMENT_ID, DATE_OF_BIRTH, GENDER)
    VALUES ('2001229012', '2001229012', v_class_3, v_dept_cntt, TO_DATE('2004-08-12', 'YYYY-MM-DD'), N'Nam');
    INSERT INTO STUDENTS (USER_ID, STUDENT_CODE, CLASS_ID, DEPARTMENT_ID, DATE_OF_BIRTH, GENDER)
    VALUES ('2001220123', '2001220123', v_class_3, v_dept_cntt, TO_DATE('2004-11-05', 'YYYY-MM-DD'), N'Nữ');
    
    DBMS_OUTPUT.PUT_LINE('✓ Inserted STUDENTS');
    
    -- =========================================================
    -- 6. CLASS_LECTURER_ASSIGNMENTS (Phân công CVHT)
    -- =========================================================
    DBMS_OUTPUT.PUT_LINE('Inserting CLASS_LECTURER_ASSIGNMENTS...');
    
    INSERT INTO CLASS_LECTURER_ASSIGNMENTS (ID, CLASS_ID, LECTURER_ID, ASSIGNED_BY, IS_ACTIVE)
    VALUES (v_assign_1, v_class_1, 'GV002', 'AD002', 1);
    INSERT INTO CLASS_LECTURER_ASSIGNMENTS (ID, CLASS_ID, LECTURER_ID, ASSIGNED_BY, IS_ACTIVE)
    VALUES (v_assign_2, v_class_2, 'GV003', 'AD002', 1);
    INSERT INTO CLASS_LECTURER_ASSIGNMENTS (ID, CLASS_ID, LECTURER_ID, ASSIGNED_BY, IS_ACTIVE)
    VALUES (v_assign_3, v_class_3, 'GV004', 'AD002', 1);
    
    DBMS_OUTPUT.PUT_LINE('✓ Inserted CLASS_LECTURER_ASSIGNMENTS');
    
    -- =========================================================
    -- 7. SCORES
    -- =========================================================
    DBMS_OUTPUT.PUT_LINE('Inserting SCORES...');
    
    INSERT INTO SCORES (STUDENT_ID, TERM_ID, TOTAL_SCORE, CLASSIFICATION, STATUS)
    VALUES ('2001221234', v_term_hk1, 92, N'Xuất sắc', 'PROVISIONAL');
    INSERT INTO SCORES (STUDENT_ID, TERM_ID, TOTAL_SCORE, CLASSIFICATION, STATUS)
    VALUES ('2001222345', v_term_hk1, 85, N'Giỏi', 'PROVISIONAL');
    INSERT INTO SCORES (STUDENT_ID, TERM_ID, TOTAL_SCORE, CLASSIFICATION, STATUS)
    VALUES ('2001223456', v_term_hk1, 78, N'Khá', 'PROVISIONAL');
    INSERT INTO SCORES (STUDENT_ID, TERM_ID, TOTAL_SCORE, CLASSIFICATION, STATUS)
    VALUES ('2001224567', v_term_hk1, 65, N'Khá', 'PROVISIONAL');
    INSERT INTO SCORES (STUDENT_ID, TERM_ID, TOTAL_SCORE, CLASSIFICATION, STATUS)
    VALUES ('2001225678', v_term_hk1, 55, N'Trung bình', 'PROVISIONAL');
    INSERT INTO SCORES (STUDENT_ID, TERM_ID, TOTAL_SCORE, CLASSIFICATION, STATUS)
    VALUES ('2001226789', v_term_hk1, 88, N'Giỏi', 'PROVISIONAL');
    INSERT INTO SCORES (STUDENT_ID, TERM_ID, TOTAL_SCORE, CLASSIFICATION, STATUS)
    VALUES ('2001227890', v_term_hk1, 70, N'Khá', 'PROVISIONAL');
    INSERT INTO SCORES (STUDENT_ID, TERM_ID, TOTAL_SCORE, CLASSIFICATION, STATUS)
    VALUES ('2001228901', v_term_hk1, 45, N'Yếu', 'PROVISIONAL');
    INSERT INTO SCORES (STUDENT_ID, TERM_ID, TOTAL_SCORE, CLASSIFICATION, STATUS)
    VALUES ('2001229012', v_term_hk1, 95, N'Xuất sắc', 'PROVISIONAL');
    INSERT INTO SCORES (STUDENT_ID, TERM_ID, TOTAL_SCORE, CLASSIFICATION, STATUS)
    VALUES ('2001220123', v_term_hk1, 72, N'Khá', 'PROVISIONAL');
    
    DBMS_OUTPUT.PUT_LINE('✓ Inserted SCORES');
    
    -- =========================================================
    -- 8. ACTIVITIES
    -- =========================================================
    DBMS_OUTPUT.PUT_LINE('Inserting ACTIVITIES...');
    
    -- Hoạt động đã kết thúc, đã điểm danh
    INSERT INTO ACTIVITIES (ID, TITLE, DESCRIPTION, TERM_ID, START_AT, END_AT, STATUS, 
                           MAX_SEATS, LOCATION, POINTS, APPROVAL_STATUS, ORGANIZER_ID,
                           ABSENCE_PENALTY, ATTENDANCE_STATUS)
    VALUES (v_act_1, N'Hiến máu nhân đạo', N'Chương trình hiến máu tình nguyện', v_term_hk1,
            TO_TIMESTAMP('2024-10-15 08:00:00', 'YYYY-MM-DD HH24:MI:SS'),
            TO_TIMESTAMP('2024-10-15 17:00:00', 'YYYY-MM-DD HH24:MI:SS'),
            'CLOSED', 100, N'Hội trường A', 5, 'APPROVED', 'GV002', 5, 'CONFIRMED');
    
    -- Hoạt động đã kết thúc, chưa điểm danh
    INSERT INTO ACTIVITIES (ID, TITLE, DESCRIPTION, TERM_ID, START_AT, END_AT, STATUS, 
                           MAX_SEATS, LOCATION, POINTS, APPROVAL_STATUS, ORGANIZER_ID,
                           ABSENCE_PENALTY, ATTENDANCE_STATUS)
    VALUES (v_act_2, N'Seminar AI', N'Hội thảo về trí tuệ nhân tạo', v_term_hk1,
            TO_TIMESTAMP('2024-11-20 14:00:00', 'YYYY-MM-DD HH24:MI:SS'),
            TO_TIMESTAMP('2024-11-20 17:00:00', 'YYYY-MM-DD HH24:MI:SS'),
            'CLOSED', 200, N'Phòng hội nghị B', 3, 'APPROVED', 'GV003', 5, 'PENDING');
    
    -- Hoạt động đang mở đăng ký
    INSERT INTO ACTIVITIES (ID, TITLE, DESCRIPTION, TERM_ID, START_AT, END_AT, STATUS, 
                           MAX_SEATS, LOCATION, POINTS, APPROVAL_STATUS, ORGANIZER_ID,
                           ABSENCE_PENALTY, ATTENDANCE_STATUS)
    VALUES (v_act_3, N'Tình nguyện mùa hè xanh', N'Hoạt động tình nguyện tại vùng cao', v_term_hk1,
            TO_TIMESTAMP('2024-12-15 06:00:00', 'YYYY-MM-DD HH24:MI:SS'),
            TO_TIMESTAMP('2024-12-17 18:00:00', 'YYYY-MM-DD HH24:MI:SS'),
            'OPEN', 50, N'Tỉnh Hà Giang', 10, 'APPROVED', 'GV002', 5, 'PENDING');
    
    -- Hoạt động mới, đang chờ duyệt
    INSERT INTO ACTIVITIES (ID, TITLE, DESCRIPTION, TERM_ID, START_AT, END_AT, STATUS, 
                           MAX_SEATS, LOCATION, POINTS, APPROVAL_STATUS, ORGANIZER_ID,
                           ABSENCE_PENALTY, ATTENDANCE_STATUS)
    VALUES (v_act_4, N'Workshop Kỹ năng mềm', N'Hội thảo kỹ năng giao tiếp và thuyết trình', v_term_hk1,
            TO_TIMESTAMP('2024-12-20 08:00:00', 'YYYY-MM-DD HH24:MI:SS'),
            TO_TIMESTAMP('2024-12-20 12:00:00', 'YYYY-MM-DD HH24:MI:SS'),
            'OPEN', 80, N'Phòng hội nghị C', 4, 'PENDING', 'GV004', 5, 'PENDING');
    
    -- Hoạt động đang điểm danh
    INSERT INTO ACTIVITIES (ID, TITLE, DESCRIPTION, TERM_ID, START_AT, END_AT, STATUS, 
                           MAX_SEATS, LOCATION, POINTS, APPROVAL_STATUS, ORGANIZER_ID,
                           ABSENCE_PENALTY, ATTENDANCE_STATUS)
    VALUES (v_act_5, N'Ngày hội việc làm', N'Kết nối sinh viên với doanh nghiệp', v_term_hk1,
            TO_TIMESTAMP('2024-12-10 08:00:00', 'YYYY-MM-DD HH24:MI:SS'),
            TO_TIMESTAMP('2024-12-10 17:00:00', 'YYYY-MM-DD HH24:MI:SS'),
            'CLOSED', 300, N'Sân trường', 5, 'APPROVED', 'GV002', 5, 'IN_PROGRESS');
    
    DBMS_OUTPUT.PUT_LINE('✓ Inserted ACTIVITIES');
    
    -- =========================================================
    -- 9. REGISTRATIONS (với attendance_status mới)
    -- =========================================================
    DBMS_OUTPUT.PUT_LINE('Inserting REGISTRATIONS...');
    
    -- Hoạt động 1: Đã xác nhận điểm danh
    INSERT INTO REGISTRATIONS (ID, ACTIVITY_ID, STUDENT_ID, STATUS, CHECKED_IN_AT, CHECKED_IN_BY, ATTENDANCE_STATUS, SCORE_APPLIED)
    VALUES (v_reg_1, v_act_1, '2001221234', 'CHECKED_IN', TO_TIMESTAMP('2024-10-15 08:30:00', 'YYYY-MM-DD HH24:MI:SS'), 'GV002', 'PRESENT', 1);
    INSERT INTO REGISTRATIONS (ID, ACTIVITY_ID, STUDENT_ID, STATUS, CHECKED_IN_AT, CHECKED_IN_BY, ATTENDANCE_STATUS, SCORE_APPLIED)
    VALUES (v_reg_2, v_act_1, '2001222345', 'CHECKED_IN', TO_TIMESTAMP('2024-10-15 09:00:00', 'YYYY-MM-DD HH24:MI:SS'), 'GV002', 'PRESENT', 1);
    INSERT INTO REGISTRATIONS (ID, ACTIVITY_ID, STUDENT_ID, STATUS, ATTENDANCE_STATUS, SCORE_APPLIED)
    VALUES (v_reg_3, v_act_1, '2001223456', 'REGISTERED', 'ABSENT', 1);
    
    -- Hoạt động 2: Chưa điểm danh
    INSERT INTO REGISTRATIONS (ID, ACTIVITY_ID, STUDENT_ID, STATUS, ATTENDANCE_STATUS)
    VALUES (v_reg_4, v_act_2, '2001221234', 'REGISTERED', 'PENDING');
    INSERT INTO REGISTRATIONS (ID, ACTIVITY_ID, STUDENT_ID, STATUS, ATTENDANCE_STATUS)
    VALUES (v_reg_5, v_act_2, '2001226789', 'REGISTERED', 'PENDING');
    INSERT INTO REGISTRATIONS (ID, ACTIVITY_ID, STUDENT_ID, STATUS, ATTENDANCE_STATUS)
    VALUES (v_reg_6, v_act_2, '2001227890', 'REGISTERED', 'PENDING');
    
    -- Hoạt động 5: Đang điểm danh (một số đã có mặt, một số chưa)
    INSERT INTO REGISTRATIONS (ID, ACTIVITY_ID, STUDENT_ID, STATUS, CHECKED_IN_AT, CHECKED_IN_BY, ATTENDANCE_STATUS)
    VALUES (v_reg_7, v_act_5, '2001221234', 'CHECKED_IN', TO_TIMESTAMP('2024-12-10 08:15:00', 'YYYY-MM-DD HH24:MI:SS'), 'GV002', 'PRESENT');
    INSERT INTO REGISTRATIONS (ID, ACTIVITY_ID, STUDENT_ID, STATUS, ATTENDANCE_STATUS)
    VALUES (v_reg_8, v_act_5, '2001224567', 'REGISTERED', 'ABSENT');
    INSERT INTO REGISTRATIONS (ID, ACTIVITY_ID, STUDENT_ID, STATUS, ATTENDANCE_STATUS)
    VALUES (v_reg_9, v_act_5, '2001225678', 'REGISTERED', 'PENDING');
    INSERT INTO REGISTRATIONS (ID, ACTIVITY_ID, STUDENT_ID, STATUS, ATTENDANCE_STATUS)
    VALUES (v_reg_10, v_act_5, '2001228901', 'REGISTERED', 'PENDING');
    
    DBMS_OUTPUT.PUT_LINE('✓ Inserted REGISTRATIONS');
    
    -- =========================================================
    -- 10. NOTIFICATIONS
    -- =========================================================
    DBMS_OUTPUT.PUT_LINE('Inserting NOTIFICATIONS...');
    
    INSERT INTO NOTIFICATIONS (ID, TITLE, CONTENT, TARGET_ROLE)
    VALUES (v_noti_1, N'Thông báo điểm rèn luyện HK1', 
            N'Điểm rèn luyện học kỳ 1 năm 2024-2025 đã được công bố. Sinh viên vui lòng kiểm tra và phản hồi nếu có thắc mắc.',
            'STUDENT');
    
    INSERT INTO NOTIFICATIONS (ID, TITLE, CONTENT, TARGET_ROLE)
    VALUES (v_noti_2, N'Hoạt động tình nguyện mới',
            N'Đã mở đăng ký cho hoạt động Tình nguyện mùa hè xanh. Số lượng có hạn, đăng ký ngay!',
            'STUDENT');
    
    INSERT INTO NOTIFICATIONS (ID, TITLE, CONTENT, TO_USER_ID)
    VALUES (v_noti_3, N'Yêu cầu phê duyệt hoạt động',
            N'Có hoạt động mới cần phê duyệt: Workshop Kỹ năng mềm. Vui lòng xem xét và duyệt.',
            'AD002');
    
    DBMS_OUTPUT.PUT_LINE('✓ Inserted NOTIFICATIONS');
    
    -- =========================================================
    -- 11. FEEDBACKS
    -- =========================================================
    DBMS_OUTPUT.PUT_LINE('Inserting FEEDBACKS...');
    
    INSERT INTO FEEDBACKS (ID, STUDENT_ID, TERM_ID, ACTIVITY_ID, TITLE, CONTENT, STATUS)
    VALUES (v_fb_1, '2001221234', v_term_hk1, v_act_1, 
            N'Xin xác nhận tham gia hoạt động',
            N'Em đã tham gia hoạt động Hiến máu nhân đạo nhưng chưa được điểm danh do đến trễ. Xin thầy/cô xác nhận giúp em.',
            'SUBMITTED');
    
    INSERT INTO FEEDBACKS (ID, STUDENT_ID, TERM_ID, TITLE, CONTENT, STATUS, RESPONSE, RESPONDED_AT)
    VALUES (v_fb_2, '2001222345', v_term_hk1, 
            N'Thắc mắc về điểm rèn luyện',
            N'Em thắc mắc về điểm rèn luyện học kỳ này. Em đã tham gia 5 hoạt động nhưng chỉ được tính 3.',
            'RESPONDED',
            N'Điểm của em đã được kiểm tra và cập nhật. 2 hoạt động còn lại chưa được tính do em chưa nộp minh chứng. Vui lòng liên hệ phòng công tác sinh viên.',
            SYSTIMESTAMP - INTERVAL '2' DAY);
    
    INSERT INTO FEEDBACKS (ID, STUDENT_ID, TERM_ID, TITLE, CONTENT, STATUS)
    VALUES (v_fb_3, '2001225678', v_term_hk1,
            N'Xin cấp giấy xác nhận',
            N'Em cần giấy xác nhận điểm rèn luyện để nộp hồ sơ xin học bổng. Xin thầy/cô hỗ trợ.',
            'SUBMITTED');
    
    DBMS_OUTPUT.PUT_LINE('✓ Inserted FEEDBACKS');
    
    -- =========================================================
    -- 12. PROOFS (Minh chứng)
    -- =========================================================
    DBMS_OUTPUT.PUT_LINE('Inserting PROOFS...');
    
    -- Minh chứng cho registration 1 (đã duyệt)
    INSERT INTO PROOFS (ID, REGISTRATION_ID, STUDENT_ID, ACTIVITY_ID, FILE_NAME, STORED_PATH, CONTENT_TYPE, FILE_SIZE, STATUS, NOTE)
    VALUES (RAWTOHEX(SYS_GUID()), v_reg_1, '2001221234', v_act_1, 
            N'chung_nhan_hien_mau.pdf', '/Uploads/Proofs/2001221234_hienmau_20241015.pdf',
            'application/pdf', 256000, 'APPROVED', N'Giấy chứng nhận hiến máu nhân đạo');
    
    -- Minh chứng cho registration 2 (đang chờ duyệt)
    INSERT INTO PROOFS (ID, REGISTRATION_ID, STUDENT_ID, ACTIVITY_ID, FILE_NAME, STORED_PATH, CONTENT_TYPE, FILE_SIZE, STATUS)
    VALUES (RAWTOHEX(SYS_GUID()), v_reg_2, '2001222345', v_act_1, 
            N'anh_tham_gia.jpg', '/Uploads/Proofs/2001222345_hienmau_20241015.jpg',
            'image/jpeg', 512000, 'SUBMITTED');
    
    -- Minh chứng bị từ chối
    INSERT INTO PROOFS (ID, REGISTRATION_ID, STUDENT_ID, ACTIVITY_ID, FILE_NAME, STORED_PATH, CONTENT_TYPE, FILE_SIZE, STATUS, NOTE, REVIEWED_AT_UTC)
    VALUES (RAWTOHEX(SYS_GUID()), v_reg_4, '2001221234', v_act_2, 
            N'anh_seminar.png', '/Uploads/Proofs/2001221234_seminar_20241120.png',
            'image/png', 1024000, 'REJECTED', N'Ảnh không rõ mặt, vui lòng nộp lại ảnh có chứng thực',
            SYS_EXTRACT_UTC(SYSTIMESTAMP) - INTERVAL '3' DAY);
    
    -- Minh chứng đang chờ
    INSERT INTO PROOFS (ID, REGISTRATION_ID, STUDENT_ID, ACTIVITY_ID, FILE_NAME, STORED_PATH, CONTENT_TYPE, FILE_SIZE, STATUS)
    VALUES (RAWTOHEX(SYS_GUID()), v_reg_7, '2001221234', v_act_5, 
            N'giay_xac_nhan_viec_lam.docx', '/Uploads/Proofs/2001221234_vieclam_20241210.docx',
            'application/vnd.openxmlformats-officedocument.wordprocessingml.document', 45000, 'SUBMITTED');
    
    DBMS_OUTPUT.PUT_LINE('✓ Inserted PROOFS');
    
    -- =========================================================
    -- 13. FEEDBACK_ATTACHMENTS (Tệp đính kèm phản hồi)
    -- =========================================================
    DBMS_OUTPUT.PUT_LINE('Inserting FEEDBACK_ATTACHMENTS...');
    
    -- Đính kèm cho feedback 1
    INSERT INTO FEEDBACK_ATTACHMENTS (ID, FEEDBACK_ID, FILE_NAME, STORED_PATH, CONTENT_TYPE, FILE_SIZE)
    VALUES (RAWTOHEX(SYS_GUID()), v_fb_1, 
            N'bang_chung_tham_gia.jpg', '/Uploads/Feedbacks/2001221234_fb1_bangchung.jpg',
            'image/jpeg', 350000);
    
    INSERT INTO FEEDBACK_ATTACHMENTS (ID, FEEDBACK_ID, FILE_NAME, STORED_PATH, CONTENT_TYPE, FILE_SIZE)
    VALUES (RAWTOHEX(SYS_GUID()), v_fb_1, 
            N'giay_xac_nhan.pdf', '/Uploads/Feedbacks/2001221234_fb1_xacnhan.pdf',
            'application/pdf', 125000);
    
    -- Đính kèm cho feedback 3
    INSERT INTO FEEDBACK_ATTACHMENTS (ID, FEEDBACK_ID, FILE_NAME, STORED_PATH, CONTENT_TYPE, FILE_SIZE)
    VALUES (RAWTOHEX(SYS_GUID()), v_fb_3, 
            N'don_xin_cap_giay.pdf', '/Uploads/Feedbacks/2001225678_fb3_donxin.pdf',
            'application/pdf', 89000);
    
    DBMS_OUTPUT.PUT_LINE('✓ Inserted FEEDBACK_ATTACHMENTS');
    
    -- =========================================================
    -- 14. NOTIFICATION_READS (Đã đọc thông báo)
    -- =========================================================
    DBMS_OUTPUT.PUT_LINE('Inserting NOTIFICATION_READS...');
    
    -- Một số sinh viên đã đọc thông báo 1
    INSERT INTO NOTIFICATION_READS (NOTIFICATION_ID, STUDENT_ID) VALUES (v_noti_1, '2001221234');
    INSERT INTO NOTIFICATION_READS (NOTIFICATION_ID, STUDENT_ID) VALUES (v_noti_1, '2001222345');
    INSERT INTO NOTIFICATION_READS (NOTIFICATION_ID, STUDENT_ID) VALUES (v_noti_1, '2001226789');
    
    DBMS_OUTPUT.PUT_LINE('✓ Inserted NOTIFICATION_READS');
    
    COMMIT;
    
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('========================================');
    DBMS_OUTPUT.PUT_LINE('✓ Sample data inserted successfully!');
    DBMS_OUTPUT.PUT_LINE('========================================');
    
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        DBMS_OUTPUT.PUT_LINE('ERROR: ' || SQLERRM);
        RAISE;
END;
/

-- =========================================================
-- VERIFICATION
-- =========================================================
PROMPT '';
PROMPT 'Data Summary:';

SELECT 'DEPARTMENTS' as TABLE_NAME, COUNT(*) as ROW_COUNT FROM DEPARTMENTS
UNION ALL SELECT 'CLASSES', COUNT(*) FROM CLASSES
UNION ALL SELECT 'TERMS', COUNT(*) FROM TERMS
UNION ALL SELECT 'USERS', COUNT(*) FROM USERS
UNION ALL SELECT 'STUDENTS', COUNT(*) FROM STUDENTS
UNION ALL SELECT 'CLASS_LECTURER_ASSIGNMENTS', COUNT(*) FROM CLASS_LECTURER_ASSIGNMENTS
UNION ALL SELECT 'SCORES', COUNT(*) FROM SCORES
UNION ALL SELECT 'ACTIVITIES', COUNT(*) FROM ACTIVITIES
UNION ALL SELECT 'REGISTRATIONS', COUNT(*) FROM REGISTRATIONS
UNION ALL SELECT 'NOTIFICATIONS', COUNT(*) FROM NOTIFICATIONS
UNION ALL SELECT 'NOTIFICATION_READS', COUNT(*) FROM NOTIFICATION_READS
UNION ALL SELECT 'FEEDBACKS', COUNT(*) FROM FEEDBACKS
UNION ALL SELECT 'FEEDBACK_ATTACHMENTS', COUNT(*) FROM FEEDBACK_ATTACHMENTS
UNION ALL SELECT 'PROOFS', COUNT(*) FROM PROOFS;
