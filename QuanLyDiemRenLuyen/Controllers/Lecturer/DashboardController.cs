using System.Web.Mvc;

namespace QuanLyDiemRenLuyen.Controllers.Lecturer
{
    public class DashboardController : LecturerBaseController
    {
        // GET: Lecturer/Dashboard
        public ActionResult Index()
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            return View("~/Views/Lecturer/Dashboard/Index.cshtml");
        }
    }
}
