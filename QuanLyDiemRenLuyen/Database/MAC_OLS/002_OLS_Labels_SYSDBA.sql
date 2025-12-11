-- =========================================================
-- MAC + OLS - PHẦN B (Chạy với SYSDBA)
-- =========================================================
-- Kết nối: SYSDBA (sys as sysdba)
-- Mục đích: Áp dụng OLS Policy lên các bảng và cập nhật dữ liệu có sẵn
-- Điều kiện: Chạy 001_OLS_Setup_SYSDBA.sql trước!
-- =========================================================

SET SERVEROUTPUT ON;
SET LINESIZE 200;

PROMPT '========================================';
PROMPT 'MAC + OLS PHẦN B - Áp dụng Policy lên bảng';
PROMPT 'Đang thực thi với: SYSDBA';
PROMPT '========================================';

-- =========================================================
-- BƯỚC 1: SET LABELS CHO QLDIEMRENLUYEN
-- =========================================================

PROMPT '';
PROMPT 'Setting labels for QLDiemRenLuyen...';

BEGIN
    SA_USER_ADMIN.SET_LEVELS(
        policy_name => 'OLS_DRL_POLICY',
        user_name   => 'QLDIEMRENLUYEN',
        max_level   => 'CONF',
        min_level   => 'PUB',
        def_level   => 'CONF',
        row_level   => 'PUB'
    );
    DBMS_OUTPUT.PUT_LINE('✓ Set levels for QLDiemRenLuyen');
END;
/

BEGIN
    SA_USER_ADMIN.SET_COMPARTMENTS(
        policy_name => 'OLS_DRL_POLICY',
        user_name   => 'QLDIEMRENLUYEN',
        read_comps  => 'FB,EV,AU',
        write_comps => 'FB,EV,AU',
        def_comps   => NULL,
        row_comps   => NULL
    );
    DBMS_OUTPUT.PUT_LINE('✓ Set compartments for QLDiemRenLuyen');
END;
/

BEGIN
    SA_USER_ADMIN.SET_GROUPS(
        policy_name  => 'OLS_DRL_POLICY',
        user_name    => 'QLDIEMRENLUYEN',
        read_groups  => 'UNI',
        write_groups => 'UNI',
        def_groups   => 'UNI',
        row_groups   => NULL
    );
    DBMS_OUTPUT.PUT_LINE('✓ Set groups for QLDiemRenLuyen');
END;
/

-- =========================================================
-- BƯỚC 2: ÁP DỤNG POLICY VÀO CÁC BẢNG
-- =========================================================

PROMPT '';
PROMPT 'Áp dụng policy lên các bảng...';

BEGIN
    SA_POLICY_ADMIN.APPLY_TABLE_POLICY(
        policy_name    => 'OLS_DRL_POLICY',
        schema_name    => 'QLDIEMRENLUYEN',
        table_name     => 'FEEDBACKS',
        table_options  => 'READ_CONTROL,WRITE_CONTROL'
    );
    DBMS_OUTPUT.PUT_LINE('✓ Applied to FEEDBACKS');
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('FEEDBACKS: ' || SQLERRM);
END;
/

BEGIN
    SA_POLICY_ADMIN.APPLY_TABLE_POLICY(
        policy_name    => 'OLS_DRL_POLICY',
        schema_name    => 'QLDIEMRENLUYEN',
        table_name     => 'PROOFS',
        table_options  => 'READ_CONTROL,WRITE_CONTROL'
    );
    DBMS_OUTPUT.PUT_LINE('✓ Applied to PROOFS');
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('PROOFS: ' || SQLERRM);
END;
/

BEGIN
    SA_POLICY_ADMIN.APPLY_TABLE_POLICY(
        policy_name    => 'OLS_DRL_POLICY',
        schema_name    => 'QLDIEMRENLUYEN',
        table_name     => 'SCORE_AUDIT_SIGNATURES',
        table_options  => 'READ_CONTROL,WRITE_CONTROL'
    );
    DBMS_OUTPUT.PUT_LINE('✓ Applied to SCORE_AUDIT_SIGNATURES');
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('SCORE_AUDIT_SIGNATURES: ' || SQLERRM);
END;
/

-- =========================================================
-- BƯỚC 3: CẬP NHẬT DỮ LIỆU CÓ SẴN VỚI LABEL
-- =========================================================

PROMPT '';
PROMPT 'Cập nhật dữ liệu có sẵn với label...';

-- Update FEEDBACKS
BEGIN
    UPDATE QLDIEMRENLUYEN.FEEDBACKS
    SET OLS_LABEL = CASE STATUS
        WHEN 'DRAFT' THEN 1000
        WHEN 'SUBMITTED' THEN 2010
        WHEN 'RESPONDED' THEN 2010
        WHEN 'CLOSED' THEN 3010
        ELSE 1000
    END
    WHERE OLS_LABEL IS NULL;
    DBMS_OUTPUT.PUT_LINE('✓ Updated ' || SQL%ROWCOUNT || ' FEEDBACKS');
    COMMIT;
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('FEEDBACKS update: ' || SQLERRM);
END;
/

-- Update PROOFS
BEGIN
    UPDATE QLDIEMRENLUYEN.PROOFS
    SET OLS_LABEL = CASE STATUS
        WHEN 'SUBMITTED' THEN 1000
        WHEN 'APPROVED' THEN 2020
        WHEN 'REJECTED' THEN 2020
        ELSE 1000
    END
    WHERE OLS_LABEL IS NULL;
    DBMS_OUTPUT.PUT_LINE('✓ Updated ' || SQL%ROWCOUNT || ' PROOFS');
    COMMIT;
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('PROOFS update: ' || SQLERRM);
END;
/

-- Update SCORE_AUDIT_SIGNATURES
BEGIN
    UPDATE QLDIEMRENLUYEN.SCORE_AUDIT_SIGNATURES
    SET OLS_LABEL = CASE ACTION_TYPE
        WHEN 'TAMPER_DETECTED' THEN 3030
        ELSE 2030
    END
    WHERE OLS_LABEL IS NULL;
    DBMS_OUTPUT.PUT_LINE('✓ Updated ' || SQL%ROWCOUNT || ' SCORE_AUDIT_SIGNATURES');
    COMMIT;
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('SCORE_AUDIT_SIGNATURES update: ' || SQLERRM);
END;
/

-- =========================================================
-- BƯỚC 4: TẠO TRIGGER TỰ ĐIỀU CHỈNH LABEL (trong schema)
-- =========================================================

PROMPT '';
PROMPT 'Tạo trigger tự điều chỉnh label...';

-- Trigger for FEEDBACKS
BEGIN
    EXECUTE IMMEDIATE '
    CREATE OR REPLACE TRIGGER QLDIEMRENLUYEN.TRG_FEEDBACKS_OLS_LABEL
    BEFORE INSERT OR UPDATE ON QLDIEMRENLUYEN.FEEDBACKS
    FOR EACH ROW
    DECLARE
        v_label_tag NUMBER;
    BEGIN
        CASE :NEW.STATUS
            WHEN ''DRAFT'' THEN v_label_tag := 1000;
            WHEN ''SUBMITTED'' THEN v_label_tag := 2010;
            WHEN ''RESPONDED'' THEN v_label_tag := 2010;
            WHEN ''CLOSED'' THEN v_label_tag := 3010;
            ELSE v_label_tag := 1000;
        END CASE;
        :NEW.OLS_LABEL := v_label_tag;
    END;';
    DBMS_OUTPUT.PUT_LINE('✓ Created TRG_FEEDBACKS_OLS_LABEL');
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('TRG_FEEDBACKS_OLS_LABEL: ' || SQLERRM);
END;
/

-- Trigger for PROOFS
BEGIN
    EXECUTE IMMEDIATE '
    CREATE OR REPLACE TRIGGER QLDIEMRENLUYEN.TRG_PROOFS_OLS_LABEL
    BEFORE INSERT OR UPDATE ON QLDIEMRENLUYEN.PROOFS
    FOR EACH ROW
    DECLARE
        v_label_tag NUMBER;
    BEGIN
        CASE :NEW.STATUS
            WHEN ''SUBMITTED'' THEN v_label_tag := 1000;
            WHEN ''APPROVED'' THEN v_label_tag := 2020;
            WHEN ''REJECTED'' THEN v_label_tag := 2020;
            ELSE v_label_tag := 1000;
        END CASE;
        :NEW.OLS_LABEL := v_label_tag;
    END;';
    DBMS_OUTPUT.PUT_LINE('✓ Created TRG_PROOFS_OLS_LABEL');
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('TRG_PROOFS_OLS_LABEL: ' || SQLERRM);
END;
/

-- Trigger for SCORE_AUDIT_SIGNATURES
BEGIN
    EXECUTE IMMEDIATE '
    CREATE OR REPLACE TRIGGER QLDIEMRENLUYEN.TRG_AUDIT_SIG_OLS_LABEL
    BEFORE INSERT OR UPDATE ON QLDIEMRENLUYEN.SCORE_AUDIT_SIGNATURES
    FOR EACH ROW
    DECLARE
        v_label_tag NUMBER;
    BEGIN
        CASE :NEW.ACTION_TYPE
            WHEN ''SIGN'' THEN v_label_tag := 2030;
            WHEN ''VERIFY'' THEN v_label_tag := 2030;
            WHEN ''TAMPER_DETECTED'' THEN v_label_tag := 3030;
            WHEN ''RE_SIGN'' THEN v_label_tag := 2030;
            ELSE v_label_tag := 2030;
        END CASE;
        :NEW.OLS_LABEL := v_label_tag;
    END;';
    DBMS_OUTPUT.PUT_LINE('✓ Created TRG_AUDIT_SIG_OLS_LABEL');
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('TRG_AUDIT_SIG_OLS_LABEL: ' || SQLERRM);
END;
/

-- =========================================================
-- VERIFICATION
-- =========================================================

PROMPT '';
PROMPT '========================================';
PROMPT 'VERIFICATION';
PROMPT '========================================';

PROMPT '';
PROMPT 'Tables with OLS Policy:';
SELECT SCHEMA_NAME, TABLE_NAME, POLICY_NAME, STATUS
FROM DBA_SA_TABLE_POLICIES
WHERE POLICY_NAME = 'OLS_DRL_POLICY';

PROMPT '';
PROMPT 'OLS_LABEL columns:';
SELECT OWNER, TABLE_NAME, COLUMN_NAME
FROM DBA_TAB_COLUMNS
WHERE COLUMN_NAME = 'OLS_LABEL' AND OWNER = 'QLDIEMRENLUYEN';

PROMPT '';
PROMPT 'Triggers:';
SELECT OWNER, TRIGGER_NAME, TABLE_NAME, STATUS
FROM DBA_TRIGGERS
WHERE OWNER = 'QLDIEMRENLUYEN' AND TRIGGER_NAME LIKE 'TRG_%OLS%';

PROMPT '';
PROMPT 'User Levels:';
SELECT USER_NAME, MAX_LEVEL, MIN_LEVEL, DEF_LEVEL, ROW_LEVEL
FROM DBA_SA_USER_LEVELS
WHERE POLICY_NAME = 'OLS_DRL_POLICY' AND USER_NAME = 'QLDIEMRENLUYEN';

PROMPT '';
PROMPT '========================================';
PROMPT '✓ PART B COMPLETED!';
PROMPT 'Next: Run Part C for additional user labels';
PROMPT '========================================';
