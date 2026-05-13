using HappyHouse.Models;
using PagedList;
using System.Web.Mvc;

namespace HappyHouse.Controllers
{
    public class ChuTroGiaDichVuController : ChuTroBaseController
    {
        private readonly GiaDichVuBusiness _bus = new GiaDichVuBusiness();
        private readonly TienIchBusiness _tiBus = new TienIchBusiness();

        private void LoadDropdown(string maChuTro,
                                  string maToaNha = null,
                                  string maTienIch = null)
        {
            ViewBag.DsToaNha = new SelectList(
                _bus.LayToaNhaCuaChuTro(maChuTro),
                "MaToaNha", "TenToaNha", maToaNha);

            // Chỉ lấy tiện ích CoTinhTien = 1
            ViewBag.DsTienIch = new SelectList(
                _tiBus.LayTienIchTinhTien(),
                "MaTienIch", "TenTienIch", maTienIch);

            // Dropdown filter tiện ích (có Tất cả)
            var dsFilter = _tiBus.LayTienIchTinhTien();
            ViewBag.DsTienIchFilter = new SelectList(
                dsFilter, "MaTienIch", "TenTienIch", maTienIch);
        }

        // DANH SÁCH 

        [HttpGet]
        public ActionResult DanhSach(int page = 1,
                                     string maToaNha = null,
                                     string maTienIch = null)
        {
            return HienThiDanhSach(page, maToaNha, maTienIch);
        }

        [HttpPost]
        public ActionResult DanhSach(string maToaNha,
                                     string maTienIch,
                                     int page = 1)
        {
            return HienThiDanhSach(page, maToaNha, maTienIch);
        }

        private ActionResult HienThiDanhSach(int page,
                                              string maToaNha,
                                              string maTienIch)
        {
            var user = GetUserOnline();
            var lst = _bus.LayDanhSachChuTro(
                           user.MaNguoiDung, maToaNha, maTienIch);

            ViewBag.MaToaNha = maToaNha;
            ViewBag.MaTienIch = maTienIch;
            LoadDropdown(user.MaNguoiDung, maToaNha, maTienIch);

            return View(lst.ToPagedList(page, 10));
        }

        // THÊM MỚI

        [HttpGet]
        public ActionResult ThemMoi()
        {
            var user = GetUserOnline();
            var dsToaNha = _bus.LayToaNhaCuaChuTro(user.MaNguoiDung);

            if (dsToaNha.Count == 0)
            {
                TempData["Error"] = "Bạn chưa có tòa nhà nào được duyệt!";
                return RedirectToAction("DanhSach");
            }

            LoadDropdown(user.MaNguoiDung);
            return View(new GiaDichVu
            {
                NgayApDung = System.DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemMoi(GiaDichVu obj)
        {
            var user = GetUserOnline();

            if (string.IsNullOrEmpty(obj.MaToaNha))
                ModelState.AddModelError("MaToaNha",
                    "Vui lòng chọn tòa nhà!");

            if (string.IsNullOrEmpty(obj.MaTienIch))
                ModelState.AddModelError("MaTienIch",
                    "Vui lòng chọn loại dịch vụ!");

            if (obj.DonGia <= 0)
                ModelState.AddModelError("DonGia",
                    "Đơn giá phải lớn hơn 0!");

            if (obj.NgayKetThuc.HasValue
             && obj.NgayKetThuc.Value <= obj.NgayApDung)
                ModelState.AddModelError("NgayKetThuc",
                    "Ngày kết thúc phải sau ngày áp dụng!");

            if (!ModelState.IsValid)
            {
                LoadDropdown(user.MaNguoiDung,
                             obj.MaToaNha, obj.MaTienIch);
                return View(obj);
            }

            bool kq = _bus.ThemMoi(obj);

            TempData[kq ? "Success" : "Error"] = kq
                ? "Thêm giá dịch vụ thành công!"
                : "Thêm thất bại, vui lòng thử lại!";

            return RedirectToAction("DanhSach");
        }

        // SỬA THÔNG TIN

        [HttpGet]
        public ActionResult SuaThongTin(string maGiaDichVu)
        {
            var user = GetUserOnline();
            var obj = _bus.LayChiTiet(maGiaDichVu);

            if (obj == null) return HttpNotFound();
            if (obj.ToaNha.MaChuTro != user.MaNguoiDung)
                return HttpNotFound();

            LoadDropdown(user.MaNguoiDung,
                         obj.MaToaNha, obj.MaTienIch);
            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaThongTin(GiaDichVu obj)
        {
            var user = GetUserOnline();
            var objDb = _bus.LayChiTiet(obj.MaGiaDichVu);

            if (objDb == null) return HttpNotFound();
            if (objDb.ToaNha.MaChuTro != user.MaNguoiDung)
                return HttpNotFound();

            if (obj.DonGia <= 0)
                ModelState.AddModelError("DonGia",
                    "Đơn giá phải lớn hơn 0!");

            if (obj.NgayKetThuc.HasValue
             && obj.NgayKetThuc.Value <= obj.NgayApDung)
                ModelState.AddModelError("NgayKetThuc",
                    "Ngày kết thúc phải sau ngày áp dụng!");

            if (!ModelState.IsValid)
            {
                LoadDropdown(user.MaNguoiDung,
                             objDb.MaToaNha, objDb.MaTienIch);
                return View(obj);
            }

            bool kq = _bus.CapNhat(obj);

            TempData[kq ? "Success" : "Error"] = kq
                ? "Cập nhật thành công!"
                : "Cập nhật thất bại!";

            return RedirectToAction("DanhSach");
        }

        // ĐỔI TRẠNG THÁI

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DoiTrangThai(string maGiaDichVu)
        {
            bool kq = _bus.DoiTrangThai(maGiaDichVu);

            TempData[kq ? "Success" : "Error"] = kq
                ? "Cập nhật trạng thái thành công."
                : "Thao tác thất bại.";

            return RedirectToAction("DanhSach");
        }

        // XÓA

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Xoa(string maGiaDichVu)
        {
            var user = GetUserOnline();
            bool kq = _bus.Xoa(maGiaDichVu, user.MaNguoiDung);

            TempData[kq ? "Success" : "Error"] = kq
                ? "Đã xóa giá dịch vụ."
                : "Xóa thất bại!";

            return RedirectToAction("DanhSach");
        }
    }
}