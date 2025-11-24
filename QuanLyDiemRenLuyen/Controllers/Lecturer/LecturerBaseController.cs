using System.Web.Mvc;

namespace QuanLyDiemRenLuyen.Controllers.Lecturer
{
    /// <summary>
    /// Base controller cho tất cả Lecturer controllers
    /// Cung cấp authentication và authorization checks
    /// </summary>
    [Authorize]
    public abstract class LecturerBaseController : Controller
    {
        /// <summary>
        /// Kiểm tra user có phải Lecturer không
        /// </summary>
        protected bool IsLecturer()
        {
            string roleName = Session["RoleName"]?.ToString();
            return roleName == "LECTURER";
        }

        /// <summary>
        /// Kiểm tra authentication và authorization
        /// Trả về RedirectAction nếu không hợp lệ, null nếu OK
        /// </summary>
        protected ActionResult CheckAuth()
        {
            if (!IsLecturer())
            {
                // Nếu là Admin, có thể cho phép truy cập hoặc redirect về Admin Dashboard
                // Ở đây ta redirect về Login nếu không phải Lecturer
                return RedirectToAction("Login", "Account");
            }
            return null;
        }

        /// <summary>
        /// Lấy MAND của user hiện tại
        /// </summary>
        protected string GetCurrentUserId()
        {
            return Session["MAND"]?.ToString();
        }
    }
}
