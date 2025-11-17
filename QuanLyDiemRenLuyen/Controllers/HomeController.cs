using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QuanLyDiemRenLuyen.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Home()
        {
            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult HienThi(int id)
        {
            ViewBag.k = id;
            return View();
        }

        public ActionResult Index3(int id, string name)
        {
            ViewBag.k = id;
            ViewBag.name = name;
            return View();
        }
    }
}