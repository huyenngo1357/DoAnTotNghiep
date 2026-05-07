using HappyHouse.Models;
using PagedList;
using System.Linq;
using System.Web.Mvc;

namespace HappyHouse.Controllers
{
    public class AdminNguoiDungController : AdminBaseController
    {
        NguoiDungBusiness nguoiDungBusiness = new NguoiDungBusiness();

        private void LoadDropdown(string maVaiTro = null, bool? trangThai = null)
        {
            ViewBag.DsVaiTro = new SelectList(
                nguoiDungBusiness.LayDanhSachVaiTro(),
                "MaVaiTro", "TenVaiTro", maVaiTro);

            var dsTrangThai = new[]
            {
                new { Value = "",      Text = "--- Tất cả ---" },
                new { Value = "true",  Text = "Hoạt động"      },
                new { Value = "false", Text = "Đã khóa"        },
            };
            ViewBag.DsTrangThai = new SelectList(
                dsTrangThai, "Value", "Text",
                trangThai.HasValue
                    ? trangThai.Value.ToString().ToLower()
                    : "");
        }

        [HttpGet]
        public ActionResult DanhSach(int page = 1,
                                     string tuKhoa = null,
                                     string maVaiTro = null,
                                     string trangThai = null)
        {
            return HienThiDanhSach(page, tuKhoa, maVaiTro, trangThai);
        }

        [HttpPost]
        public ActionResult DanhSach(string tuKhoa,
                                     string maVaiTro,
                                     string trangThai,
                                     int page = 1)
        {
            return HienThiDanhSach(page, tuKhoa, maVaiTro, trangThai);
        }

        private ActionResult HienThiDanhSach(int page,
                                              string tuKhoa,
                                              string maVaiTro,
                                              string trangThai)
        {
            int pageSize = 10;

            bool? trangThaiFilter = null;
            if (trangThai == "true") trangThaiFilter = true;
            if (trangThai == "false") trangThaiFilter = false;

            var lst = nguoiDungBusiness.LayDanhSach(
                          tuKhoa, maVaiTro, trangThaiFilter);

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.MaVaiTro = maVaiTro;
            ViewBag.TrangThai = trangThai;
            LoadDropdown(maVaiTro, trangThaiFilter);

            return View(lst.ToPagedList(page, pageSize));
        }

        // ── THÊM MỚI ─────────────────────────────────────────────────

        [HttpGet]
        public ActionResult ThemMoi()
        {
            LoadDropdown();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemMoi(NguoiDung obj,
                                    System.Web.HttpPostedFileBase anhDaiDien,
                                    string matKhauMoi)
        {
            if (string.IsNullOrWhiteSpace(obj.HoTen))
                ModelState.AddModelError("HoTen",
                    "Họ tên không được để trống!");

            if (string.IsNullOrWhiteSpace(obj.Email))
                ModelState.AddModelError("Email",
                    "Email không được để trống!");

            if (string.IsNullOrWhiteSpace(obj.SoDienThoai))
                ModelState.AddModelError("SoDienThoai",
                    "Số điện thoại không được để trống!");

            if (string.IsNullOrWhiteSpace(matKhauMoi) || matKhauMoi.Length < 6)
                ModelState.AddModelError("",
                    "Mật khẩu phải có ít nhất 6 ký tự!");

            if (!ModelState.IsValid)
            {
                LoadDropdown(obj.MaVaiTro);
                return View(obj);
            }

            obj.MatKhau = matKhauMoi;
            bool kq = nguoiDungBusiness.ThemMoi(obj, anhDaiDien);

            if (kq)
            {
                TempData["Success"] = "Thêm người dùng thành công!";
                return RedirectToAction("DanhSach");
            }

            bool emailTrung = DataProvider.Entities.NguoiDungs
                                  .Any(x => x.Email == obj.Email);
            ModelState.AddModelError(
                emailTrung ? "Email" : "SoDienThoai",
                emailTrung
                    ? "Email này đã được sử dụng!"
                    : "Số điện thoại này đã được sử dụng!");

            LoadDropdown(obj.MaVaiTro);
            return View(obj);
        }

        // ── SỬA THÔNG TIN ─────────────────────────────────────────────

        [HttpGet]
        public ActionResult SuaThongTin(string maNguoiDung)
        {
            var obj = nguoiDungBusiness.LayChiTiet(maNguoiDung);
            if (obj == null) return HttpNotFound();

            LoadDropdown(obj.MaVaiTro);
            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaThongTin(NguoiDung obj,
                                        System.Web.HttpPostedFileBase anhDaiDien,
                                        string matKhauMoi)
        {
            if (string.IsNullOrWhiteSpace(obj.HoTen))
                ModelState.AddModelError("HoTen",
                    "Họ tên không được để trống!");

            if (string.IsNullOrWhiteSpace(obj.Email))
                ModelState.AddModelError("Email",
                    "Email không được để trống!");

            if (string.IsNullOrWhiteSpace(obj.SoDienThoai))
                ModelState.AddModelError("SoDienThoai",
                    "Số điện thoại không được để trống!");

            if (!string.IsNullOrWhiteSpace(matKhauMoi) && matKhauMoi.Length < 6)
                ModelState.AddModelError("MatKhau",
                    "Mật khẩu mới phải có ít nhất 6 ký tự!");

            if (!ModelState.IsValid)
            {
                LoadDropdown(obj.MaVaiTro);
                return View(obj);
            }

            obj.MatKhau = matKhauMoi;
            bool kq = nguoiDungBusiness.CapNhat(obj, anhDaiDien);

            if (kq)
            {
                // FIX: nếu admin đang sửa chính tài khoản mình
                // thì cập nhật Session để navbar hiển thị đúng ngay lập tức
                NguoiDung admin = GetUserOnline();
                if (admin.MaNguoiDung == obj.MaNguoiDung)
                {
                    var userMoi = nguoiDungBusiness.LayChiTiet(obj.MaNguoiDung);
                    if (userMoi != null)
                        Session["UserOnline"] = userMoi;
                }

                TempData["Success"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("DanhSach");
            }

            bool emailTrung = DataProvider.Entities.NguoiDungs
                                  .Any(x => x.Email == obj.Email
                                         && x.MaNguoiDung != obj.MaNguoiDung);
            if (emailTrung)
                ModelState.AddModelError("Email",
                    "Email này đã được sử dụng!");
            else
                ModelState.AddModelError("SoDienThoai",
                    "Số điện thoại này đã được sử dụng!");

            LoadDropdown(obj.MaVaiTro);
            return View(obj);
        }

        // ── KHÓA / MỞ KHÓA ───────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DoiTrangThai(string maNguoiDung)
        {
            NguoiDung admin = GetUserOnline();
            if (admin.MaNguoiDung == maNguoiDung)
            {
                TempData["Error"] = "Không thể khóa tài khoản đang đăng nhập!";
                return RedirectToAction("DanhSach");
            }

            bool kq = nguoiDungBusiness.DoiTrangThai(maNguoiDung);

            TempData[kq ? "Success" : "Error"] = kq
                ? "Cập nhật trạng thái thành công."
                : "Thao tác thất bại.";

            return RedirectToAction("DanhSach");
        }
    }
}