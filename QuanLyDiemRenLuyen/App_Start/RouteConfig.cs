using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace QuanLyDiemRenLuyen
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Route cho Account
            routes.MapRoute(
                name: "Account",
                url: "Account/{action}",
                defaults: new { controller = "Account", action = "Login" }
            );

            // Routes cho Student nested controllers
            routes.MapRoute(
                name: "StudentDashboard",
                url: "Student/Dashboard/{action}/{id}",
                defaults: new { controller = "Dashboard", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QuanLyDiemRenLuyen.Controllers.Student" }
            );

            routes.MapRoute(
                name: "StudentProfile",
                url: "Student/Profile/{action}/{id}",
                defaults: new { controller = "Profile", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QuanLyDiemRenLuyen.Controllers.Student" }
            );

            routes.MapRoute(
                name: "StudentActivities",
                url: "Student/Activities/{action}/{id}",
                defaults: new { controller = "Activities", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QuanLyDiemRenLuyen.Controllers.Student" }
            );

            routes.MapRoute(
                name: "StudentScores",
                url: "Student/Scores/{action}/{id}",
                defaults: new { controller = "Scores", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QuanLyDiemRenLuyen.Controllers.Student" }
            );

            routes.MapRoute(
                name: "StudentNotifications",
                url: "Student/Notifications/{action}/{id}",
                defaults: new { controller = "Notifications", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QuanLyDiemRenLuyen.Controllers.Student" }
            );

            routes.MapRoute(
                name: "StudentFeedbacks",
                url: "Student/Feedbacks/{action}/{id}",
                defaults: new { controller = "Feedbacks", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QuanLyDiemRenLuyen.Controllers.Student" }
            );

            // Routes cho Admin nested controllers
            routes.MapRoute(
                name: "AdminDashboard",
                url: "Admin/Dashboard/{action}/{id}",
                defaults: new { controller = "Dashboard", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QuanLyDiemRenLuyen.Controllers.Admin" }
            );

            routes.MapRoute(
                name: "AdminActivities",
                url: "Admin/Activities/{action}/{id}",
                defaults: new { controller = "Activities", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QuanLyDiemRenLuyen.Controllers.Admin" }
            );

            routes.MapRoute(
                name: "AdminScores",
                url: "Admin/Scores/{action}/{id}",
                defaults: new { controller = "Scores", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QuanLyDiemRenLuyen.Controllers.Admin" }
            );

            routes.MapRoute(
                name: "AdminReviewRequests",
                url: "Admin/ReviewRequests/{action}/{id}",
                defaults: new { controller = "ReviewRequests", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QuanLyDiemRenLuyen.Controllers.Admin" }
            );

            routes.MapRoute(
                name: "AdminNotifications",
                url: "Admin/Notifications/{action}/{id}",
                defaults: new { controller = "Notifications", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QuanLyDiemRenLuyen.Controllers.Admin" }
            );

            routes.MapRoute(
                name: "AdminFeedbacks",
                url: "Admin/Feedbacks/{action}/{id}",
                defaults: new { controller = "Feedbacks", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QuanLyDiemRenLuyen.Controllers.Admin" }
            );

            routes.MapRoute(
                name: "AdminClasses",
                url: "Admin/Classes/{action}/{id}",
                defaults: new { controller = "Classes", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QuanLyDiemRenLuyen.Controllers.Admin" }
            );
            
            routes.MapRoute(
                name: "AdminUsers",
                url: "Admin/Users/{action}/{id}",
                defaults: new { controller = "Users", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QuanLyDiemRenLuyen.Controllers.Admin" }
            );

            // Route cho Student (redirect to Dashboard)
            routes.MapRoute(
                name: "Student",
                url: "Student/{action}/{id}",
                defaults: new { controller = "Dashboard", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QuanLyDiemRenLuyen.Controllers.Student" }
            );

            // Route mặc định - redirect đến trang đăng nhập
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Account", action = "Login", id = UrlParameter.Optional }
            );
        }
    }
}