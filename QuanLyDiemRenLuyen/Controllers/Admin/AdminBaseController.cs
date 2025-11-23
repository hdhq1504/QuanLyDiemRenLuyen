using System.Web.Mvc;

namespace QuanLyDiemRenLuyen.Controllers.Admin
{
    /// <summary>
    /// Base controller cho tất cả Admin controllers
    /// Cung cấp authentication và authorization checks
    /// </summary>
    [Authorize]
    public abstract class AdminBaseController : Controller
    {
        /// <summary>
        /// Kiểm tra user có phải Admin hoặc Lecturer không
        /// </summary>
        protected bool IsAdmin()
        {
            string roleName = Session["RoleName"]?.ToString();
            return roleName == "ADMIN" || roleName == "LECTURER";
        }

        /// <summary>
        /// Kiểm tra authentication và authorization
        /// Trả về RedirectAction nếu không hợp lệ, null nếu OK
        /// </summary>
        protected ActionResult CheckAuth()
        {
            if (!IsAdmin())
            {
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
