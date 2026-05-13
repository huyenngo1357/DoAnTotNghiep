using HappyHouse.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace HappyHouse.Controllers
{
    public class PhongTroController : Controller
    {
        // ── Helper: lấy danh sách tỉnh/thành ────────────────────────
        private List<string> LayDSTinhThanh()
        {
            return DataProvider.Entities.ToaNhas
                       .Where(x => x.TrangThai == true
                                && x.TinhThanh != null
                                && x.TinhThanh != "")
                       .Select(x => x.TinhThanh)
                       .Distinct()
                       .OrderBy(x => x)
                       .ToList();
        }

        // ── Helper: phòng nổi bật sidebar ────────────────────────────
        private List<PhongTro> LayPhongNoiBat()
        {
            return DataProvider.Entities.PhongTroes
                       .Include("HinhAnhPhongs")
                       .Include("ToaNha")
                       .Where(x => x.TrangThai == true
                                && x.TrangThaiPhong == "Trong"
                                && x.ToaNha.TrangThai == true)
                       .OrderByDescending(x => x.NgayTao)
                       .Take(4)
                       .ToList();
        }

        // ── DANH SÁCH ─────────────────────────────────────────────────
        public ActionResult DanhSach(
            string tuKhoa = null,
            string tinhThanh = null,
            string phuongXa = null,
            decimal? giaMin = null,
            decimal? giaMax = null,
            decimal? dienTichMin = null,
            int? soNguoi = null,
            int page = 1)
        {
            int pageSize = 8;

            tuKhoa = string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim();

            // ✅ Không filter TrangThaiDuyet để tránh lỗi nếu
            //    DB chưa có data DaDuyet
            var lst = DataProvider.Entities.PhongTroes
                          .Include("ToaNha")
                          .Include("HinhAnhPhongs")
                          .Include("TienIches")
                          .Where(x => x.TrangThai == true
                                   && x.TrangThaiPhong == "Trong"
                                   && x.ToaNha.TrangThai == true)
                          .AsQueryable();

            // Nếu có tòa nhà DaDuyet thì ưu tiên lọc
            bool coDaDuyet = DataProvider.Entities.ToaNhas
                                 .Any(x => x.TrangThaiDuyet == "DaDuyet"
                                        && x.TrangThai == true);
            if (coDaDuyet)
                lst = lst.Where(x =>
                    x.ToaNha.TrangThaiDuyet == "DaDuyet");

            if (!string.IsNullOrEmpty(tuKhoa))
                lst = lst.Where(x =>
                    x.SoPhong.Contains(tuKhoa)
                 || x.ToaNha.TenToaNha.Contains(tuKhoa)
                 || x.ToaNha.DiaChi.Contains(tuKhoa));

            if (!string.IsNullOrEmpty(tinhThanh))
                lst = lst.Where(x =>
                    x.ToaNha.TinhThanh == tinhThanh);

            if (!string.IsNullOrEmpty(phuongXa))
                lst = lst.Where(x =>
                    x.ToaNha.PhuongXa == phuongXa);

            if (giaMin.HasValue)
                lst = lst.Where(x => x.GiaThue >= giaMin.Value);

            if (giaMax.HasValue)
                lst = lst.Where(x => x.GiaThue <= giaMax.Value);

            if (dienTichMin.HasValue)
                lst = lst.Where(x => x.DienTich >= dienTichMin.Value);

            if (soNguoi.HasValue)
                lst = lst.Where(x =>
                    x.SoNguoiToiDa >= soNguoi.Value);

            // ── Phường/Xã theo tỉnh đã chọn ─────────────────────────
            var dsPhuongXa = new List<string>();
            if (!string.IsNullOrEmpty(tinhThanh))
            {
                var qPX = DataProvider.Entities.ToaNhas
                              .Where(x => x.TinhThanh == tinhThanh
                                       && x.PhuongXa != null
                                       && x.PhuongXa != ""
                                       && x.TrangThai == true);
                if (coDaDuyet)
                    qPX = qPX.Where(x =>
                        x.TrangThaiDuyet == "DaDuyet");

                dsPhuongXa = qPX
                    .Select(x => x.PhuongXa)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();
            }

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.TinhThanh = tinhThanh;
            ViewBag.PhuongXa = phuongXa;
            ViewBag.GiaMin = giaMin;
            ViewBag.GiaMax = giaMax;
            ViewBag.DienTichMin = dienTichMin;
            ViewBag.SoNguoi = soNguoi;
            ViewBag.TongKetQua = lst.Count();
            ViewBag.DsTinhThanh = LayDSTinhThanh();
            ViewBag.DsPhuongXa = dsPhuongXa;
            ViewBag.DsPhongNoiBat = LayPhongNoiBat();

            return View(
                lst.OrderByDescending(x => x.NgayTao)
                   .ToPagedList(page, pageSize));
        }

        // ── AJAX: phường/xã theo tỉnh ────────────────────────────────
        public ActionResult LayPhuongXa(string tinhThanh)
        {
            if (string.IsNullOrEmpty(tinhThanh))
                return Json(new string[] { },
                    JsonRequestBehavior.AllowGet);

            // ✅ Không lọc TrangThaiDuyet cứng — tự detect
            var query = DataProvider.Entities.ToaNhas
                            .Where(x => x.TinhThanh == tinhThanh
                                     && x.PhuongXa != null
                                     && x.PhuongXa != ""
                                     && x.TrangThai == true);

            bool coDaDuyet = DataProvider.Entities.ToaNhas
                                 .Any(x => x.TrangThaiDuyet == "DaDuyet"
                                        && x.TrangThai == true);
            if (coDaDuyet)
                query = query.Where(x =>
                    x.TrangThaiDuyet == "DaDuyet");

            var lst = query
                .Select(x => x.PhuongXa)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            return Json(lst, JsonRequestBehavior.AllowGet);
        }

        // ── CHI TIẾT ─────────────────────────────────────────────────
        public ActionResult ChiTiet(string maPhong)
        {
            var phong = DataProvider.Entities.PhongTroes
                            .Include("ToaNha")
                            .Include("ToaNha.NguoiDung")
                            .Include("HinhAnhPhongs")
                            .Include("TienIches")
                            .FirstOrDefault(x =>
                                x.MaPhong == maPhong
                             && x.TrangThai == true);

            if (phong == null) return HttpNotFound();

            var dsDanhGia = DataProvider.Entities.DanhGiaPhongs
                                .Include("NguoiDung")
                                .Where(x => x.MaPhong == maPhong
                                         && x.TrangThai == true)
                                .OrderByDescending(x => x.NgayTao)
                                .ToList();

            double diemTB = dsDanhGia.Any()
                ? Math.Round(
                    dsDanhGia.Average(
                        x => (double)x.DiemDanhGia), 1)
                : 0;

            var phongTuongTu = DataProvider.Entities.PhongTroes
                                   .Include("HinhAnhPhongs")
                                   .Include("ToaNha")
                                   .Where(x =>
                                       x.MaToaNha == phong.MaToaNha
                                    && x.MaPhong != maPhong
                                    && x.TrangThaiPhong == "Trong"
                                    && x.TrangThai == true)
                                   .Take(3)
                                   .ToList();

            ViewBag.DsTinhThanh = LayDSTinhThanh();
            ViewBag.DsDanhGia = dsDanhGia;
            ViewBag.DiemTB = diemTB;
            ViewBag.TongDanhGia = dsDanhGia.Count;
            ViewBag.PhongTuongTu = phongTuongTu;

            return View(phong);
        }
    }
}