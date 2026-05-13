using HappyHouse.Models;
using PagedList;
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HappyHouse.Controllers
{
    public class ChuTroChiSoController : ChuTroBaseController
    {
        private readonly ChiSoDienNuocBusiness _bus =
            new ChiSoDienNuocBusiness();

        private void LoadDropdown(string maChuTro,
                                   string maHopDong = null,
                                   string loaiDichVu = null)
        {
            var dsHopDong =
                _bus.LayHopDongDangThue(maChuTro);

            // NguoiDung1 = KhachHang
            var dsHienThi = dsHopDong.Select(h => new
            {
                MaHopDong = h.MaHopDong,
                TenHienThi =
                    (h.PhongTro != null
                        ? h.PhongTro.SoPhong
                          + " - " + h.PhongTro.ToaNha.TenToaNha
                        : h.MaHopDong)
                    + (h.NguoiDung1 != null
                        ? " (" + h.NguoiDung1.HoTen + ")"
                        : "")
            }).ToList();

            ViewBag.DsHopDong = new SelectList(
                dsHienThi,
                "MaHopDong",
                "TenHienThi",
                maHopDong);

            var dsLoai = new[]
            {
                new { Value = "",     Text = "--- Tất cả ---" },
                new { Value = "Dien", Text = "Điện"           },
                new { Value = "Nuoc", Text = "Nước"           },
            };
            ViewBag.DsLoaiDichVu = new SelectList(
                dsLoai, "Value", "Text", loaiDichVu);
        }

        // DANH SÁCH

        [HttpGet]
        public ActionResult DanhSach(int page = 1,
                                      string maHopDong = null,
                                      string loaiDichVu = null)
        {
            return HienThiDanhSach(
                page, maHopDong, loaiDichVu);
        }

        [HttpPost]
        public ActionResult DanhSach(string maHopDong,
                                      string loaiDichVu,
                                      int page = 1)
        {
            return HienThiDanhSach(
                page, maHopDong, loaiDichVu);
        }

        private ActionResult HienThiDanhSach(
            int page,
            string maHopDong,
            string loaiDichVu)
        {
            var user = GetUserOnline();
            var lst = _bus.LayDanhSachChuTro(
                user.MaNguoiDung, maHopDong, loaiDichVu);

            ViewBag.MaHopDong = maHopDong;
            ViewBag.LoaiDichVu = loaiDichVu;
            LoadDropdown(
                user.MaNguoiDung, maHopDong, loaiDichVu);

            return View(lst.ToPagedList(page, 10));
        }

        // GHI CHỈ SỐ

        [HttpGet]
        public ActionResult GhiChiSo()
        {
            var user = GetUserOnline();
            var dsHopDong =
                _bus.LayHopDongDangThue(user.MaNguoiDung);

            if (dsHopDong.Count == 0)
            {
                TempData["Error"] =
                    "Không có hợp đồng đang thuê nào!";
                return RedirectToAction("DanhSach");
            }

            LoadDropdown(user.MaNguoiDung);
            ViewBag.ThangGhi =
                DateTime.Today.ToString("yyyy-MM");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GhiChiSo(
            string maHopDong,
            string thangGhi,
            decimal chiSoDauDien,
            decimal chiSoCuoiDien,
            decimal donGiaDien,
            HttpPostedFileBase anhDien,
            decimal chiSoDauNuoc,
            decimal chiSoCuoiNuoc,
            decimal donGiaNuoc,
            HttpPostedFileBase anhNuoc)
        {
            var user = GetUserOnline();

            DateTime thang;
            if (!DateTime.TryParse(
                    thangGhi + "-01", out thang))
            {
                TempData["Error"] =
                    "Tháng ghi không hợp lệ!";
                LoadDropdown(user.MaNguoiDung, maHopDong);
                ViewBag.ThangGhi = thangGhi;
                return View();
            }

            if (string.IsNullOrEmpty(maHopDong))
            {
                TempData["Error"] =
                    "Vui lòng chọn hợp đồng!";
                LoadDropdown(user.MaNguoiDung);
                ViewBag.ThangGhi = thangGhi;
                return View();
            }

            if (chiSoCuoiDien < chiSoDauDien)
            {
                TempData["Error"] =
                    "Chỉ số cuối điện phải >= chỉ số đầu!";
                LoadDropdown(user.MaNguoiDung, maHopDong);
                ViewBag.ThangGhi = thangGhi;
                return View();
            }

            if (chiSoCuoiNuoc < chiSoDauNuoc)
            {
                TempData["Error"] =
                    "Chỉ số cuối nước phải >= chỉ số đầu!";
                LoadDropdown(user.MaNguoiDung, maHopDong);
                ViewBag.ThangGhi = thangGhi;
                return View();
            }

            var objDien = new ChiSoDienNuoc
            {
                MaHopDong = maHopDong,
                ThangGhi = thang,
                ChiSoDau = chiSoDauDien,
                ChiSoCuoi = chiSoCuoiDien,
                DonGia = donGiaDien,
                NguoiGhiId = user.MaNguoiDung
            };

            var objNuoc = new ChiSoDienNuoc
            {
                MaHopDong = maHopDong,
                ThangGhi = thang,
                ChiSoDau = chiSoDauNuoc,
                ChiSoCuoi = chiSoCuoiNuoc,
                DonGia = donGiaNuoc,
                NguoiGhiId = user.MaNguoiDung
            };

            bool kq = _bus.ThemMoiCaDienVaNuoc(
                objDien, objNuoc, anhDien, anhNuoc);

            if (kq)
            {
                TempData["Success"] =
                    "Ghi chỉ số thành công! "
                  + "Điện: " + (chiSoCuoiDien - chiSoDauDien)
                  + " kWh - Nước: "
                  + (chiSoCuoiNuoc - chiSoDauNuoc) + " m3";
                return RedirectToAction("DanhSach");
            }

            TempData["Error"] =
                "Tháng này đã ghi chỉ số rồi!";
            LoadDropdown(user.MaNguoiDung, maHopDong);
            ViewBag.ThangGhi = thangGhi;
            return View();
        }

        // SỬA CHỈ SỐ

        [HttpGet]
        public ActionResult SuaThongTin(string maChiSo)
        {
            var user = GetUserOnline();
            var obj = _bus.LayChiTiet(maChiSo);

            if (obj == null) return HttpNotFound();
            if (obj.HopDong.MaChuTro != user.MaNguoiDung)
                return HttpNotFound();

            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaThongTin(
            ChiSoDienNuoc obj,
            HttpPostedFileBase anhMoi)
        {
            var user = GetUserOnline();
            var objDb = _bus.LayChiTiet(obj.MaChiSo);

            if (objDb == null) return HttpNotFound();
            if (objDb.HopDong.MaChuTro != user.MaNguoiDung)
                return HttpNotFound();

            if (obj.ChiSoCuoi < obj.ChiSoDau)
            {
                TempData["Error"] =
                    "Chỉ số cuối phải >= chỉ số đầu!";
                return View(_bus.LayChiTiet(obj.MaChiSo));
            }

            bool kq = _bus.CapNhat(obj, anhMoi);
            TempData[kq ? "Success" : "Error"] = kq
                ? "Cập nhật chỉ số thành công!"
                : "Cập nhật thất bại!";

            return RedirectToAction("DanhSach");
        }

        //  XÓA 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Xoa(string maChiSo)
        {
            bool kq = _bus.Xoa(maChiSo);
            TempData[kq ? "Success" : "Error"] = kq
                ? "Đã xóa chỉ số."
                : "Xóa thất bại!";

            return RedirectToAction("DanhSach");
        }

        // AJAX

        public ActionResult LayThongTinGhiChiSo(
            string maHopDong)
        {
            var user = GetUserOnline();
            var chiSoDien =
                _bus.LayChiSoGanNhat(maHopDong, "Dien");
            var chiSoNuoc =
                _bus.LayChiSoGanNhat(maHopDong, "Nuoc");

            var hopDong = DataProvider.Entities.HopDongs
                              .Include("PhongTro")
                              .FirstOrDefault(x =>
                                  x.MaHopDong == maHopDong);

            decimal donGiaDien = chiSoDien?.DonGia ?? 0;
            decimal donGiaNuoc = chiSoNuoc?.DonGia ?? 0;

            if (hopDong != null)
            {
                if (donGiaDien == 0)
                {
                    var cs = DataProvider.Entities
                                 .ChiSoDienNuocs
                                 .Where(x =>
                                     x.HopDong.PhongTro.MaToaNha
                                         == hopDong.PhongTro.MaToaNha
                                  && x.LoaiDichVu == "Dien"
                                  && x.TrangThai == true)
                                 .OrderByDescending(
                                     x => x.ThangGhi)
                                 .FirstOrDefault();
                    donGiaDien = cs?.DonGia ?? 0;
                }
                if (donGiaNuoc == 0)
                {
                    var cs = DataProvider.Entities
                                 .ChiSoDienNuocs
                                 .Where(x =>
                                     x.HopDong.PhongTro.MaToaNha
                                         == hopDong.PhongTro.MaToaNha
                                  && x.LoaiDichVu == "Nuoc"
                                  && x.TrangThai == true)
                                 .OrderByDescending(
                                     x => x.ThangGhi)
                                 .FirstOrDefault();
                    donGiaNuoc = cs?.DonGia ?? 0;
                }
            }

            return Json(new
            {
                success = true,
                chiSoCuoiDien = chiSoDien?.ChiSoCuoi ?? 0,
                donGiaDien = donGiaDien,
                chiSoCuoiNuoc = chiSoNuoc?.ChiSoCuoi ?? 0,
                donGiaNuoc = donGiaNuoc
            }, JsonRequestBehavior.AllowGet);
        }
    }
}