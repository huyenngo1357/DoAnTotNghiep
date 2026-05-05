using HappyHouse.Models;
using System.Web.Mvc;
using System.Web.Routing;

namespace HappyHouse.Controllers
{
    public class ChuTroBaseController : Controller
    {
        protected NguoiDung GetUserOnline()
        {
            return Session["UserOnline"] as NguoiDung;
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            NguoiDung user = GetUserOnline();

            if (user == null)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(
                        new { controller = "TaiKhoan", action = "DangNhap" }
                    ));
                return;
            }

            // Cả CHUTRO lẫn CHUTHAU đều vào được
            if (user.MaVaiTro != "CHUTRO" && user.MaVaiTro != "CHUTHAU")
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(
                        new { controller = "TrangChu", action = "Index" }
                    ));
                return;
            }
        }
    }
}