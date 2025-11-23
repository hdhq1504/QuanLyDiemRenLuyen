using System.Web.Mvc;

namespace QuanLyDiemRenLuyen.Controllers.Student
{
    /// <summary>
    /// Base controller cho tất cả Student controllers
    /// Cung cấp authentication checks và helper methods
    /// </summary>
    [Authorize]
    public abstract class StudentBaseController : Controller
    {
        /// <summary>
        /// Lấy MAND của sinh viên hiện tại
        /// </summary>
        protected string GetCurrentStudentId()
        {
            return Session["MAND"]?.ToString();
        }

        /// <summary>
        /// Kiểm tra authentication
        /// Trả về RedirectAction nếu chưa login, null nếu OK
        /// </summary>
        protected ActionResult CheckAuth()
        {
            if (string.IsNullOrEmpty(GetCurrentStudentId()))
            {
                return RedirectToAction("Login", "Account");
            }
            return null;
        }

        /// <summary>
        /// Lấy tên đầy đủ của sinh viên
        /// </summary>
        protected string GetCurrentStudentName()
        {
            return Session["FullName"]?.ToString();
        }

        /// <summary>
        /// Lấy email của sinh viên
        /// </summary>
        protected string GetCurrentStudentEmail()
        {
            return Session["Email"]?.ToString();
        }
    }
}
