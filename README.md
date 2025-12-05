# 🎓 Hệ Thống Quản Lý Điểm Rèn Luyện

Hệ thống quản lý điểm rèn luyện sinh viên với đầy đủ tính năng bảo mật Oracle: **RBAC**, **DAC**, **MAC (OLS)**, và **Auditing**.

## 📋 Mục Lục

- [Tổng Quan](#tổng-quan)
- [Công Nghệ Sử Dụng](#công-nghệ-sử-dụng)
- [Cấu Trúc Dự Án](#cấu-trúc-dự-án)
- [Cài Đặt](#cài-đặt)
- [Kiến Trúc Bảo Mật](#kiến-trúc-bảo-mật)
- [Hướng Dẫn Sử Dụng](#hướng-dẫn-sử-dụng)

---

## 🎯 Tổng Quan

### Mô Tả

Hệ thống quản lý điểm rèn luyện sinh viên, hỗ trợ:

- Sinh viên tự đánh giá điểm rèn luyện
- Cố vấn học tập (CVHT) xét duyệt điểm
- Quản trị viên quản lý toàn bộ hệ thống
- Đăng ký và theo dõi hoạt động ngoại khóa

### Vai Trò Người Dùng

| Vai trò      | Mô tả                                                       |
| ------------ | ----------------------------------------------------------- |
| **STUDENT**  | Sinh viên - Xem điểm, tự đánh giá, đăng ký hoạt động        |
| **LECTURER** | Giảng viên/CVHT - Quản lý điểm lớp phụ trách, tạo hoạt động |
| **ADMIN**    | Quản trị viên - Toàn quyền quản lý hệ thống                 |

---

## 🛠 Công Nghệ Sử Dụng

### Backend

- **ASP.NET MVC 5** (.NET Framework 4.7.2)
- **Oracle Database 19c**
- **Oracle Data Provider for .NET (ODP.NET)**

### Frontend

- **Razor Views** với custom CSS
- **Bootstrap 5.3** - UI framework
- **Font Awesome 6** - Icons
- **Inter Font** - Typography
- **jQuery** - DOM manipulation

### Bảo Mật Oracle

- **RBAC** - Role-Based Access Control
- **DAC** - Discretionary Access Control
- **MAC/OLS** - Oracle Label Security (Mandatory Access Control)
- **VPD** - Virtual Private Database
- **Auditing** - Standard, FGA, Custom Triggers

---

## 📁 Cấu Trúc Dự Án

```
QuanLyDiemRenLuyen/
├── Controllers/
│   ├── AccountController.cs      # Đăng nhập, đăng ký
│   ├── StudentController.cs      # Chức năng sinh viên
│   ├── LecturerController.cs     # Chức năng giảng viên
│   └── Admin/
│       ├── AdminController.cs    # Dashboard admin
│       ├── UsersController.cs    # Quản lý người dùng
│       ├── ClassesController.cs  # Quản lý lớp
│       ├── DatabaseController.cs # Quản trị database
│       ├── SecurityController.cs # RBAC management
│       └── AuditLogsController.cs # Nhật ký hệ thống
├── Models/
│   ├── User.cs, Student.cs, ...  # Domain models
│   ├── ViewModels/               # View models
│   └── AuditLogsViewModel.cs     # Audit UI models
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml        # Student layout
│   │   ├── _AdminLayout.cshtml   # Admin layout
│   │   └── _LecturerLayout.cshtml # Lecturer layout
│   ├── Student/                  # Student views
│   ├── Lecturer/                 # Lecturer views
│   └── Admin/                    # Admin views
└── Database/
    ├── QLDiemRenLuyen.sql        # Schema chính
    ├── sysDBA.sql                # Setup SYSDBA
    ├── RBAC/                     # Role-Based Access Control
    │   ├── 001_RBAC_Roles_SYSDBA.sql
    │   └── 002_RBAC_Permissions_QLDiemRenLuyen.sql
    ├── DAC/                      # Discretionary Access Control
    │   └── 001_Score_Sharing.sql
    ├── VPD/                      # Virtual Private Database
    │   └── 001_VPD_Scores_SYSDBA.sql
    ├── MAC_OLS/                  # Oracle Label Security
    │   ├── 000_OLS_Cleanup_SYSDBA.sql
    │   ├── 001_OLS_Setup_SYSDBA.sql
    │   ├── 002_OLS_Labels_SYSDBA.sql
    │   ├── 003_OLS_UserLabels_SYSDBA.sql
    │   └── 004_OLS_Testing.sql
    └── Audit/                    # Auditing
        ├── 001_Standard_Audit_SYSDBA.sql
        ├── 002_FGA_Policies_SYSDBA.sql
        ├── 003_Audit_Tables_QLDiemRenLuyen.sql
        ├── 004_Audit_Triggers_QLDiemRenLuyen.sql
        ├── 005_Audit_Helpers_QLDiemRenLuyen.sql
        ├── 006_Audit_Views_QLDiemRenLuyen.sql
        └── 007_Audit_Testing.sql
```

---

## ⚙️ Cài Đặt

### Yêu Cầu

- **Visual Studio 2019+** với .NET Framework 4.7.2
- **Oracle Database 19c**
- **SQL\*Plus** hoặc SQL Developer

### Bước 1: Clone Repository

```bash
git clone <repository-url>
cd QuanLyDiemRenLuyen
```

### Bước 2: Cấu Hình Database

1. **Tạo schema và user:**

```sql
-- Chạy với SYSDBA
@Database/sysDBA.sql
```

2. **Tạo schema tables:**

```sql
-- Chạy với QLDiemRenLuyen
@Database/QLDiemRenLuyen.sql
```

3. **Cài đặt RBAC:**

```sql
-- SYSDBA
@Database/RBAC/001_RBAC_Roles_SYSDBA.sql
-- QLDiemRenLuyen
@Database/RBAC/002_RBAC_Permissions_QLDiemRenLuyen.sql
```

4. **Cài đặt Auditing:**

```sql
-- SYSDBA
@Database/Audit/001_Standard_Audit_SYSDBA.sql
@Database/Audit/002_FGA_Policies_SYSDBA.sql
-- QLDiemRenLuyen
@Database/Audit/003_Audit_Tables_QLDiemRenLuyen.sql
@Database/Audit/004_Audit_Triggers_QLDiemRenLuyen.sql
@Database/Audit/005_Audit_Helpers_QLDiemRenLuyen.sql
@Database/Audit/006_Audit_Views_QLDiemRenLuyen.sql
```

### Bước 3: Cấu Hình Connection String

Chỉnh sửa `Web.config`:

```xml
<connectionStrings>
  <add name="OracleDbContext"
       connectionString="User Id=QLDIEMRENLUYEN;Password=your_password;Data Source=localhost:1521/XEPDB1"
       providerName="Oracle.ManagedDataAccess.Client" />
</connectionStrings>
```

### Bước 4: Chạy Ứng Dụng

```bash
# Mở solution trong Visual Studio
QuanLyDiemRenLuyen.sln
# Nhấn F5 để chạy
```

---

## 🔐 Kiến Trúc Bảo Mật

### 1. RBAC - Role-Based Access Control

**Database Roles:**

- `ROLE_STUDENT` - Quyền cơ bản cho sinh viên
- `ROLE_LECTURER` - Kế thừa ROLE_STUDENT + quyền quản lý
- `ROLE_ADMIN` - Toàn quyền

```sql
-- Ví dụ grant
GRANT SELECT ON SCORES TO ROLE_STUDENT;
GRANT UPDATE ON SCORES TO ROLE_LECTURER;
GRANT ALL ON SCORES TO ROLE_ADMIN;
```

### 2. DAC - Discretionary Access Control

**Score Sharing:** CVHT có thể chia sẻ quyền xem điểm tạm thời.

```sql
-- Cấp quyền xem điểm
EXEC SP_GRANT_SCORE_PERMISSION(
    p_class_id => 'CNTT01',
    p_grantee_id => 'GV002',
    p_permission_level => 'VIEW',
    p_expires_at => SYSDATE + 30
);
```

### 3. VPD - Virtual Private Database

**Row-Level Security:** Sinh viên chỉ thấy điểm của mình.

```sql
-- Policy function
CREATE FUNCTION fn_scores_policy(...)
RETURN VARCHAR2 AS
BEGIN
    IF v_role = 'STUDENT' THEN
        RETURN 'STUDENT_ID = ''' || v_user_id || '''';
    END IF;
    RETURN '1=1'; -- Admin/Lecturer thấy tất cả
END;
```

### 4. MAC/OLS - Oracle Label Security

**Sensitivity Levels:**
| Level | Short | Mô tả |
|-------|-------|-------|
| CONFIDENTIAL | CONF | Dữ liệu nhạy cảm (Admin only) |
| INTERNAL | INT | Dữ liệu nội bộ (Lecturer+) |
| PUBLIC | PUB | Dữ liệu công khai (All) |

**Compartments:** UNI (University), DEPT (Department), CLS (Class)

### 5. Auditing

**Ba loại audit:**

1. **Standard Auditing** (`AUDIT_TRAIL=DB,EXTENDED`)

   - DDL, DML operations
   - Login/Logout events

2. **Fine-Grained Auditing (FGA)**

   - SELECT trên dữ liệu nhạy cảm
   - PHONE, ID_CARD_NUMBER, PASSWORD_HASH

3. **Custom Triggers**
   - Capture OLD/NEW values (JSON)
   - Justification cho thay đổi quan trọng
   - Lưu vào `AUDIT_CHANGE_LOGS`

**UI Admin:**

- `/Admin/AuditLogs` - Xem nhật ký
- Filters: Table, Operation, User, Date range
- Chi tiết với diff OLD/NEW values

---

## 📖 Hướng Dẫn Sử Dụng

### Đăng Nhập

- URL: `/Account/Login`
- Sử dụng MAND (mã người dùng) và mật khẩu

### Sinh Viên (`/Student/*`)

- **Dashboard:** Tổng quan điểm rèn luyện
- **Điểm rèn luyện:** Xem chi tiết điểm theo học kỳ
- **Phản hồi điểm:** Gửi yêu cầu phúc khảo
- **Hoạt động:** Đăng ký hoạt động ngoại khóa

### Giảng Viên (`/Lecturer/*`)

- **Dashboard:** Tổng quan lớp phụ trách
- **Quản lý hoạt động:** Tạo/sửa/xóa hoạt động
- **Quản lý phân quyền:** Chia sẻ quyền xem điểm

### Quản Trị Viên (`/Admin/*`)

- **Dashboard:** Thống kê toàn trường
- **Quản lý hoạt động:** Phê duyệt hoạt động
- **Quản lý người dùng:** CRUD users
- **Quản lý lớp:** Phân công CVHT
- **Xét duyệt điểm:** Approve/Reject điểm
- **Security (RBAC):** Quản lý database users/roles
- **Nhật ký hệ thống:** Xem audit logs
