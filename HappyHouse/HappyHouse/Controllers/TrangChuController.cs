using HappyHouse.Models;
using PagedList;
using System;
using System.Linq;
using System.Web.Mvc;

namespace HappyHouse.Controllers
{
    public class TrangChuController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.DsPhongNoiBat = DataProvider.Entities.PhongTroes
                .Include("ToaNha")
                .Include("HinhAnhPhongs")
                .Include("TienIches")
                .Where(x => x.TrangThaiPhong == "Trong"
                         && x.TrangThai == true
                         && x.ToaNha.TrangThaiDuyet == "DaDuyet"
                         && x.ToaNha.TrangThai == true)
                .OrderByDescending(x => x.NgayTao)
                .Take(6)
                .ToList();

            ViewBag.DsTinTucMoiNhat = DataProvider.Entities.TinTucs
                .Include("NguoiDung")
                .Include("ChuDeTinTuc")
                .Where(x => x.TrangThaiDang == "DaDang"
                         && x.TrangThai == true)
                .OrderByDescending(x => x.NgayDang)
                .Take(3)
                .ToList();

            ViewBag.TongPhongTrong = DataProvider.Entities.PhongTroes
                .Count(x => x.TrangThaiPhong == "Trong"
                         && x.TrangThai == true
                         && x.ToaNha.TrangThaiDuyet == "DaDuyet");

            ViewBag.TongToaNha = DataProvider.Entities.ToaNhas
                .Count(x => x.TrangThaiDuyet == "DaDuyet"
                         && x.TrangThai == true);

            ViewBag.TongKhachHang = DataProvider.Entities.NguoiDungs
                .Count(x => x.MaVaiTro == "KHACHHANG"
                         && x.TrangThai == true);

            return View();
        }

        // Giữ nguyên DanhSachTinTuc, ChiTietTinTuc, LienHe
        public ActionResult DanhSachTinTuc(string tuKhoa = null,
                                            string maChuDe = null,
                                            int page = 1)
        {
            int pageSize = 6;

            var lst = DataProvider.Entities.TinTucs
                          .Include("ChuDeTinTuc")
                          .Include("NguoiDung")
                          .Where(x => x.TrangThaiDang == "DaDang"
                                   && x.TrangThai == true)
                          .AsQueryable();

            if (!string.IsNullOrEmpty(tuKhoa))
                lst = lst.Where(x => x.TieuDe.Contains(tuKhoa)
                                  || x.TomTat.Contains(tuKhoa));

            if (!string.IsNullOrEmpty(maChuDe))
                lst = lst.Where(x => x.MaChuDe == maChuDe);

            var dsChuDe = DataProvider.Entities.ChuDeTinTucs
                              .Where(x => x.TrangThai == true)
                              .OrderBy(x => x.TenChuDe)
                              .ToList();

            var tinTucMoiNhat = DataProvider.Entities.TinTucs
                                    .Where(x => x.TrangThaiDang == "DaDang"
                                             && x.TrangThai == true)
                                    .OrderByDescending(x => x.NgayDang)
                                    .Take(4)
                                    .ToList();

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.MaChuDe = maChuDe;
            ViewBag.DsChuDe = dsChuDe;
            ViewBag.TinTucMoiNhat = tinTucMoiNhat;
            ViewBag.TongKetQua = lst.Count();

            return View(lst.OrderByDescending(x => x.NgayDang)
                           .ToPagedList(page, pageSize));
        }

        public ActionResult ChiTietTinTuc(string maTinTuc)
        {
            var tinTuc = DataProvider.Entities.TinTucs
                             .Include("ChuDeTinTuc")
                             .Include("NguoiDung")
                             .FirstOrDefault(x => x.MaTinTuc == maTinTuc
                                               && x.TrangThaiDang == "DaDang"
                                               && x.TrangThai == true);

            if (tinTuc == null) return HttpNotFound();

            tinTuc.LuotXem++;
            DataProvider.Entities.Configuration.ValidateOnSaveEnabled = false;
            try { DataProvider.Entities.SaveChanges(); }
            finally { DataProvider.Entities.Configuration.ValidateOnSaveEnabled = true; }

            var tinLienQuan = DataProvider.Entities.TinTucs
                                  .Include("ChuDeTinTuc")
                                  .Where(x => x.MaChuDe == tinTuc.MaChuDe
                                           && x.MaTinTuc != maTinTuc
                                           && x.TrangThaiDang == "DaDang"
                                           && x.TrangThai == true)
                                  .OrderByDescending(x => x.NgayDang)
                                  .Take(3)
                                  .ToList();

            var tinTucMoiNhat = DataProvider.Entities.TinTucs
                                    .Where(x => x.TrangThaiDang == "DaDang"
                                             && x.TrangThai == true
                                             && x.MaTinTuc != maTinTuc)
                                    .OrderByDescending(x => x.NgayDang)
                                    .Take(4)
                                    .ToList();

            var dsChuDe = DataProvider.Entities.ChuDeTinTucs
                              .Where(x => x.TrangThai == true)
                              .OrderBy(x => x.TenChuDe)
                              .ToList();

            ViewBag.TinLienQuan = tinLienQuan;
            ViewBag.TinTucMoiNhat = tinTucMoiNhat;
            ViewBag.DsChuDe = dsChuDe;

            return View(tinTuc);
        }

        public ActionResult LienHe()
        {
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ChatBot(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return Json(new
                {
                    success = false,
                    message = "Vui lòng nhập tin nhắn!"
                });
            try
            {
                var db = DataProvider.Entities;

                // Lấy thông tin hệ thống để bot tư vấn
                int tongPhongTrong = db.PhongTroes
                    .Count(x => x.TrangThaiPhong == "Trong"
                              && x.TrangThai == true
                              && x.ToaNha.TrangThaiDuyet == "DaDuyet");

                int tongToaNha = db.ToaNhas
                    .Count(x => x.TrangThaiDuyet == "DaDuyet"
                              && x.TrangThai == true);

                // Lấy danh sách phòng trống giá rẻ nhất
                var dsPhong = db.PhongTroes
                    .Include("ToaNha")
                    .Where(x => x.TrangThaiPhong == "Trong"
                              && x.TrangThai == true
                              && x.ToaNha.TrangThaiDuyet == "DaDuyet")
                    .OrderBy(x => x.GiaThue)
                    .Take(10)
                    .Select(x => new
                    {
                        Ten = "Phòng " + x.SoPhong,
                        Toa = x.ToaNha.TenToaNha,
                        Gia = x.GiaThue,
                        Dia = x.ToaNha.TinhThanh
                    })
                    .ToList();

                string danhSachPhong = string.Join("; ",
                    dsPhong.Select(p =>
                        p.Ten + " tại " + p.Toa
                        + " (" + p.Dia + ") - "
                        + string.Format("{0:N0}", p.Gia) + "đ/tháng"));

                string systemPrompt =
                    "Bạn là trợ lý tư vấn thuê phòng trọ của HappyHouse. "
                  + "Nhiệm vụ: tư vấn khách hàng tìm phòng trọ, "
                  + "giải đáp thắc mắc về giá cả, thủ tục thuê phòng. "
                  + "Trả lời ngắn gọn, thân thiện, dùng tiếng Việt. "
                  + "KHÔNG bịa thông tin, chỉ tư vấn dựa trên dữ liệu có sẵn.\n\n"
                  + "Thông tin hệ thống:\n"
                  + "- Tổng tòa nhà đang hoạt động: " + tongToaNha + " tòa\n"
                  + "- Phòng trống hiện có: " + tongPhongTrong + " phòng\n"
                  + "- Website: HappyHouse.vn\n"
                  + "- Hỗ trợ: 24/7 qua hotline hoặc chat\n\n"
                  + "Chính sách thuê phòng:\n"
                  + "- Đặt cọc 1 tháng tiền thuê\n"
                  + "- Thanh toán tiền thuê hàng tháng\n"
                  + "- Báo trước 30 ngày khi muốn trả phòng\n"
                  + "- Hỗ trợ ký hợp đồng điện tử\n\n"
                  + "Danh sách phòng trống hiện có:\n"
                  + (string.IsNullOrEmpty(danhSachPhong)
                      ? "Hiện chưa có phòng trống."
                      : danhSachPhong);

                string apiKey = System.Configuration
                    .ConfigurationManager
                    .AppSettings["GroqApiKey"];

                if (string.IsNullOrEmpty(apiKey))
                    return Json(new
                    {
                        success = false,
                        message = "Hệ thống chatbot chưa được cấu hình."
                    });

                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout =
                        System.TimeSpan.FromSeconds(30);
                    client.DefaultRequestHeaders.Add(
                        "Authorization", "Bearer " + apiKey);

                    var body = new
                    {
                        model = "llama-3.1-8b-instant",
                        max_tokens = 500,
                        messages = new[]
                        {
                    new { role = "system",
                          content = systemPrompt },
                    new { role = "user",
                          content = message }
                }
                    };

                    var json = Newtonsoft.Json.JsonConvert
                        .SerializeObject(body);
                    var content = new System.Net.Http.StringContent(
                        json,
                        System.Text.Encoding.UTF8,
                        "application/json");

                    var response = client.PostAsync(
                        "https://api.groq.com/openai/v1/chat/completions",
                        content).Result;

                    var responseStr = response.Content
                        .ReadAsStringAsync().Result;

                    if (!response.IsSuccessStatusCode)
                        return Json(new
                        {
                            success = false,
                            message = "Lỗi kết nối AI. Vui lòng thử lại sau."
                        });

                    var result = Newtonsoft.Json
                        .JsonConvert
                        .DeserializeObject<dynamic>(responseStr);

                    string reply = result["choices"][0]["message"]["content"]
                        .ToString();

                    return Json(new { success = true, message = reply });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Xin lỗi, hệ thống đang bận. "
                            + "Vui lòng thử lại sau."
                });
            }
        }
    }
}