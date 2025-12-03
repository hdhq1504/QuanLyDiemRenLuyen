-- =========================================================
-- MIGRATION: Change Score Types from Decimal to Integer
-- File: 004_CHANGE_SCORES_TO_INTEGER.sql
-- Description: Thay đổi tất cả các cột điểm từ NUMBER(x,2) sang NUMBER(x) 
--              để lưu trữ điểm dưới dạng số nguyên
-- Author: System Migration
-- Date: 2025-12-03
-- =========================================================

-- =========================================================
-- BACKUP RECOMMENDATION
-- =========================================================
-- Trước khi chạy script này, nên backup dữ liệu:
-- - EXPDP SCORES table
-- - EXPDP ACTIVITIES table  

-- =========================================================

-- =========================================================
-- 1) SCORES table - TOTAL_SCORE
-- =========================================================
-- Thay đổi từ NUMBER(5,2) sang NUMBER(3)
-- Cho phép giá trị 0-999, đủ cho điểm rèn luyện 0-100

-- Kiểm tra dữ liệu hiện tại có phần thập phân không
SELECT COUNT(*) as RECORDS_WITH_DECIMAL,
       MIN(TOTAL_SCORE) as MIN_SCORE,
       MAX(TOTAL_SCORE) as MAX_SCORE
FROM SCORES
WHERE TOTAL_SCORE != TRUNC(TOTAL_SCORE);

-- Nếu có dữ liệu thập phân, làm tròn trước khi alter
UPDATE SCORES 
SET TOTAL_SCORE = ROUND(TOTAL_SCORE)
WHERE TOTAL_SCORE != TRUNC(TOTAL_SCORE);

COMMIT;

-- Alter column type
ALTER TABLE SCORES MODIFY (TOTAL_SCORE NUMBER(3));

-- Verify
SELECT COLUMN_NAME, DATA_TYPE, DATA_PRECISION, DATA_SCALE
FROM USER_TAB_COLUMNS
WHERE TABLE_NAME = 'SCORES' AND COLUMN_NAME = 'TOTAL_SCORE';

-- =========================================================
-- 2) ACTIVITIES table - POINTS
-- =========================================================
-- Thay đổi từ NUMBER(5,2) sang NUMBER(3)
-- Điểm hoạt động cũng là số nguyên

-- Kiểm tra dữ liệu
SELECT COUNT(*) as RECORDS_WITH_DECIMAL,
       MIN(POINTS) as MIN_POINTS,
       MAX(POINTS) as MAX_POINTS
FROM ACTIVITIES
WHERE POINTS IS NOT NULL AND POINTS != TRUNC(POINTS);

-- Làm tròn nếu cần
UPDATE ACTIVITIES
SET POINTS = ROUND(POINTS)
WHERE POINTS IS NOT NULL AND POINTS != TRUNC(POINTS);

COMMIT;

-- Alter column type
ALTER TABLE ACTIVITIES MODIFY (POINTS NUMBER(3));

-- Verify
SELECT COLUMN_NAME, DATA_TYPE, DATA_PRECISION, DATA_SCALE
FROM USER_TAB_COLUMNS
WHERE TABLE_NAME = 'ACTIVITIES' AND COLUMN_NAME = 'POINTS';


-- =========================================================
-- 3) Verification Queries
-- =========================================================

-- Kiểm tra tất cả các cột đã thay đổi
SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    DATA_PRECISION,
    DATA_SCALE
FROM USER_TAB_COLUMNS
WHERE (TABLE_NAME = 'SCORES' AND COLUMN_NAME = 'TOTAL_SCORE')
   OR (TABLE_NAME = 'ACTIVITIES' AND COLUMN_NAME = 'POINTS')
ORDER BY TABLE_NAME, COLUMN_NAME;

-- Kiểm tra sample data
SELECT 'SCORES' as TABLE_NAME, 
       TO_CHAR(TOTAL_SCORE) as SCORE_VALUE,
       TYPEOF(TOTAL_SCORE) as DATA_TYPE_CHECK
FROM SCORES
WHERE ROWNUM <= 5;

SELECT 'ACTIVITIES' as TABLE_NAME,
       TO_CHAR(POINTS) as SCORE_VALUE
FROM ACTIVITIES
WHERE POINTS IS NOT NULL AND ROWNUM <= 5;


-- =========================================================
-- 4) Rollback Script (if needed)
-- =========================================================
-- Nếu cần rollback, uncomment và chạy:
/*
ALTER TABLE SCORES MODIFY (TOTAL_SCORE NUMBER(5,2) DEFAULT 70 NOT NULL);
ALTER TABLE ACTIVITIES MODIFY (POINTS NUMBER(5,2));
COMMIT;
*/

PROMPT '========================================';
PROMPT 'Migration 004 completed successfully!';
PROMPT 'All score columns changed to INTEGER';
PROMPT '========================================';
