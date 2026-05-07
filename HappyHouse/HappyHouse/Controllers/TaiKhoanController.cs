using HappyHouse.Models;
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HappyHouse.Controllers
{
    public class TaiKhoanController : Controller
    {
        // ── ĐĂNG NHẬP 

        [HttpGet]
        public ActionResult DangNhap(string returnUrl = null)
        {
            if (Session["UserOnline"] != null)
                return RedirectToAction("Index", "TrangChu");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangNhap(string email,
                                     string matKhau,
                                     string returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(matKhau))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin!";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            var user = DataProvider.Entities.NguoiDungs
                           .FirstOrDefault(x => x.Email == email);

            // Dùng chung 1 message để tránh lộ thông tin
            if (user == null ||
                !BCrypt.Net.BCrypt.Verify(matKhau, user.MatKhau))
            {
                ViewBag.Error = "Email hoặc mật khẩu không đúng!";
                ViewBag.Email = email;
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            if (!user.TrangThai)
            {
                ViewBag.Error = "Tài khoản của bạn đã bị khóa. " +
                                   "Vui lòng liên hệ quản trị viên!";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            Session["UserOnline"] = user;

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            switch (user.MaVaiTro)
            {
                case "CHUTHAU":
                    return RedirectToAction("Index", "AdminDashboard");
                case "CHUTRO":
                    return RedirectToAction("Index", "ChuTroDashboard");
                default:
                    return RedirectToAction("Index", "TrangChu");
            }
        }

        // ── ĐĂNG KÝ 

        [HttpGet]
        public ActionResult DangKy()
        {
            if (Session["UserOnline"] != null)
                return RedirectToAction("Index", "TrangChu");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangKy(NguoiDung obj,
                                   string xacNhanMatKhau)
        {
            // ── Validate thủ công 
            if (string.IsNullOrWhiteSpace(obj.HoTen))
                ModelState.AddModelError("HoTen",
                    "Họ tên không được để trống!");

            if (string.IsNullOrWhiteSpace(obj.Email))
                ModelState.AddModelError("Email",
                    "Email không được để trống!");

            if (string.IsNullOrWhiteSpace(obj.SoDienThoai))
                ModelState.AddModelError("SoDienThoai",
                    "Số điện thoại không được để trống!");

            if (string.IsNullOrWhiteSpace(obj.MatKhau))
                ModelState.AddModelError("MatKhau",
                    "Mật khẩu không được để trống!");
            else if (obj.MatKhau.Length < 6)
                ModelState.AddModelError("MatKhau",
                    "Mật khẩu phải có ít nhất 6 ký tự!");
            else if (obj.MatKhau != xacNhanMatKhau)
                ModelState.AddModelError("MatKhau",
                    "Mật khẩu xác nhận không khớp!");

            // ── Kiểm tra trùng 
            if (!string.IsNullOrWhiteSpace(obj.Email))
            {
                bool emailTrung = DataProvider.Entities.NguoiDungs
                                      .Any(x => x.Email == obj.Email);
                if (emailTrung)
                    ModelState.AddModelError("Email",
                        "Email này đã được sử dụng!");
            }

            if (!string.IsNullOrWhiteSpace(obj.SoDienThoai))
            {
                bool sdtTrung = DataProvider.Entities.NguoiDungs
                                    .Any(x => x.SoDienThoai == obj.SoDienThoai);
                if (sdtTrung)
                    ModelState.AddModelError("SoDienThoai",
                        "Số điện thoại này đã được sử dụng!");
            }

            if (!ModelState.IsValid)
                return View(obj);

            // ── Lưu DB 
            // MaVaiTro từ radio button, mặc định KHACHHANG nếu không có
            if (string.IsNullOrEmpty(obj.MaVaiTro))
                obj.MaVaiTro = "KHACHHANG";

            // Chỉ cho phép KHACHHANG và CHUTRO tự đăng ký
            if (obj.MaVaiTro != "KHACHHANG" && obj.MaVaiTro != "CHUTRO")
                obj.MaVaiTro = "KHACHHANG";

            obj.MaNguoiDung = "ND" + DateTime.Now.ToString("yyyyMMddHHmmss");
            obj.TrangThai = true;
            obj.NgayTao = DateTime.Now;
            obj.MatKhau = BCrypt.Net.BCrypt.HashPassword(obj.MatKhau);

            var db = DataProvider.Entities;
            db.NguoiDungs.Add(obj);
            db.Configuration.ValidateOnSaveEnabled = false;
            try
            {
                db.SaveChanges();
            }
            finally
            {
                db.Configuration.ValidateOnSaveEnabled = true;
            }

            TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("DangNhap");
        }

        // ── ĐĂNG XUẤT 

        public ActionResult DangXuat()
        {
            Session.Clear();
            return RedirectToAction("Index", "TrangChu");
        }

        // ── THÔNG TIN TÀI KHOẢN 

        [HttpGet]
        public ActionResult ThongTin()
        {
            var user = Session["UserOnline"] as NguoiDung;
            if (user == null)
                return RedirectToAction("DangNhap",
                    new { returnUrl = "/TaiKhoan/ThongTin" });

            var userDb = DataProvider.Entities.NguoiDungs
                             .FirstOrDefault(x => x.MaNguoiDung
                                               == user.MaNguoiDung);

            if (userDb == null) return HttpNotFound();
            return View(userDb);
        }

        // Hiện đang dùng DataProvider.Entities trực tiếp ở cuối
        // nhưng đã get userDb từ DataProvider.Entities ở đầu
        // → Có thể bị EntityFramework "detached" context
        // Sửa lại dùng cùng 1 context:

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThongTin(NguoiDung obj,
                                      HttpPostedFileBase anhDaiDien,
                                      string matKhauCu = null,
                                      string matKhauMoi = null,
                                      string xacNhanMatKhau = null)
        {
            var user = Session["UserOnline"] as NguoiDung;
            if (user == null)
                return RedirectToAction("DangNhap");

            var db = DataProvider.Entities;   // ← dùng 1 context duy nhất
            var userDb = db.NguoiDungs
                            .FirstOrDefault(x => x.MaNguoiDung
                                                == user.MaNguoiDung);
            if (userDb == null) return HttpNotFound();

            if (string.IsNullOrWhiteSpace(obj.HoTen))
                ModelState.AddModelError("HoTen",
                    "Họ tên không được để trống!");

            if (string.IsNullOrWhiteSpace(obj.SoDienThoai))
                ModelState.AddModelError("SoDienThoai",
                    "Số điện thoại không được để trống!");
            else
            {
                bool sdtTrung = db.NguoiDungs
                                   .Any(x => x.SoDienThoai == obj.SoDienThoai
                                          && x.MaNguoiDung != user.MaNguoiDung);
                if (sdtTrung)
                    ModelState.AddModelError("SoDienThoai",
                        "Số điện thoại này đã được người khác sử dụng!");
            }

            bool doiMatKhau = !string.IsNullOrWhiteSpace(matKhauMoi);
            if (doiMatKhau)
            {
                if (string.IsNullOrWhiteSpace(matKhauCu))
                    ModelState.AddModelError("MatKhauCu",
                        "Vui lòng nhập mật khẩu hiện tại!");
                else if (!BCrypt.Net.BCrypt.Verify(matKhauCu, userDb.MatKhau))
                    ModelState.AddModelError("MatKhauCu",
                        "Mật khẩu hiện tại không đúng!");

                if (!string.IsNullOrWhiteSpace(matKhauMoi))
                {
                    if (matKhauMoi.Length < 6)
                        ModelState.AddModelError("MatKhauMoi",
                            "Mật khẩu mới phải có ít nhất 6 ký tự!");
                    else if (matKhauMoi != xacNhanMatKhau)
                        ModelState.AddModelError("MatKhauMoi",
                            "Mật khẩu xác nhận không khớp!");
                }
            }

            if (!ModelState.IsValid)
                return View(userDb);

            userDb.HoTen = obj.HoTen;
            userDb.SoDienThoai = obj.SoDienThoai;
            userDb.ZaloPhone = obj.ZaloPhone;
            userDb.DiaChi = obj.DiaChi;
            userDb.NgaySinh = obj.NgaySinh;
            userDb.GioiTinh = obj.GioiTinh;
            userDb.NgayCapNhat = DateTime.Now;

            if (doiMatKhau)
                userDb.MatKhau = BCrypt.Net.BCrypt.HashPassword(matKhauMoi);

            if (anhDaiDien != null && anhDaiDien.ContentLength > 0)
            {
                string ext = Path.GetExtension(anhDaiDien.FileName).ToLower();
                if (ext != ".jpg" && ext != ".jpeg"
                 && ext != ".png" && ext != ".gif")
                {
                    TempData["Error"] = "Ảnh chỉ chấp nhận JPG, PNG, GIF!";
                    return View(userDb);
                }

                string folder = Server.MapPath("~/Content/images/avatars/");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                if (!string.IsNullOrEmpty(userDb.AnhDaiDien))
                {
                    string pathCu = Path.Combine(folder, userDb.AnhDaiDien);
                    if (System.IO.File.Exists(pathCu))
                        System.IO.File.Delete(pathCu);
                }

                string tenFile = Guid.NewGuid() + ext;
                anhDaiDien.SaveAs(Path.Combine(folder, tenFile));
                userDb.AnhDaiDien = tenFile;
            }

            db.Configuration.ValidateOnSaveEnabled = false;
            try { db.SaveChanges(); }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }

            Session["UserOnline"] = userDb;
            TempData["Success"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("ThongTin");
        }
    }
}