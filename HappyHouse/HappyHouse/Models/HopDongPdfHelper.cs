using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.IO;
using System.Linq;
using System.Web;

namespace HappyHouse.Models
{
    public class HopDongPdfHelper
    {
        // ── Font ─────────────────────────────────────────────────────
        private static BaseFont _bf;
        private static BaseFont _bfBold;

        private static BaseFont GetBF(bool bold = false)
        {
            if (bold)
            {
                if (_bfBold != null) return _bfBold;
                var boldPaths = new[]
                {
                    @"C:\Windows\Fonts\arialbd.ttf",
                    @"C:\Windows\Fonts\tahomabd.ttf",
                    @"C:\Windows\Fonts\arial.ttf",
                };
                foreach (var p in boldPaths)
                {
                    try
                    {
                        if (!File.Exists(p)) continue;
                        _bfBold = BaseFont.CreateFont(
                            p, BaseFont.IDENTITY_H,
                            BaseFont.EMBEDDED);
                        return _bfBold;
                    }
                    catch { }
                }
                return GetBF(false);
            }

            if (_bf != null) return _bf;
            var paths = new[]
            {
                @"C:\Windows\Fonts\arial.ttf",
                @"C:\Windows\Fonts\tahoma.ttf",
                @"C:\Windows\Fonts\verdana.ttf",
            };
            foreach (var p in paths)
            {
                try
                {
                    if (!File.Exists(p)) continue;
                    _bf = BaseFont.CreateFont(
                        p, BaseFont.IDENTITY_H,
                        BaseFont.EMBEDDED);
                    return _bf;
                }
                catch { }
            }
            _bf = BaseFont.CreateFont(
                BaseFont.HELVETICA,
                BaseFont.CP1252,
                BaseFont.NOT_EMBEDDED);
            return _bf;
        }

        // ── Màu ──────────────────────────────────────────────────────
        private static readonly BaseColor BLACK =
            new BaseColor(0, 0, 0);
        private static readonly BaseColor DARK =
            new BaseColor(30, 30, 30);
        private static readonly BaseColor GRAY =
            new BaseColor(90, 90, 90);
        private static readonly BaseColor LIGHT_GRAY =
            new BaseColor(240, 240, 240);
        private static readonly BaseColor MID_GRAY =
            new BaseColor(180, 180, 180);
        private static readonly BaseColor WHITE =
            BaseColor.WHITE;

        // ── Font helpers ─────────────────────────────────────────────
        private static Font F(float size,
                       bool bold = false,
                       bool italic = false,
                       BaseColor color = null)
        {
            int style = Font.NORMAL;

            // ❌ KHÔNG dùng Font.BOLD nữa vì font file đã là bold
            if (italic) style = Font.ITALIC;

            return new Font(GetBF(bold), size, style,
                            color ?? DARK);
        }

        // ── ENTRY POINT ──────────────────────────────────────────────
        public static byte[] TaoHopDongPdf(HopDong hd)
        {
            _bf = null; _bfBold = null;

            var db = DataProvider.Entities;
            var phong = hd.PhongTro
                         ?? db.PhongTroes
                              .Include("ToaNha")
                              .FirstOrDefault(
                                  x => x.MaPhong == hd.MaPhong);
            var toaNha = phong?.ToaNha;
            var chuTro = db.NguoiDungs.FirstOrDefault(
                x => x.MaNguoiDung == hd.MaChuTro);
            var khach = hd.NguoiDung1
                         ?? db.NguoiDungs.FirstOrDefault(
                             x => x.MaNguoiDung == hd.MaKhachHang);
            var dsDV = db.HopDong_DichVu
                           .Include("GiaDichVu")
                           .Include("GiaDichVu.TienIch")
                           .Where(x => x.MaHopDong == hd.MaHopDong)
                           .ToList();

            int soThang = ((hd.NgayKetThuc.Year
                            - hd.NgayBatDau.Year) * 12)
                         + hd.NgayKetThuc.Month
                         - hd.NgayBatDau.Month;

            string ngayLap = hd.NgayTao.HasValue
                ? hd.NgayTao.Value.ToString("dd/MM/yyyy")
                : DateTime.Now.ToString("dd/MM/yyyy");

            string diaChi = (toaNha?.DiaChi ?? "")
                + (toaNha?.TinhThanh != null
                    ? ", " + toaNha.TinhThanh : "");

            using (var ms = new MemoryStream())
            {
                var doc = new Document(
                    PageSize.A4, 60f, 55f, 70f, 70f);
                var writer = PdfWriter.GetInstance(doc, ms);
                doc.Open();

                // ════════════════════════════════════════════════
                // HEADER
                // ════════════════════════════════════════════════
                var tblHeader = new PdfPTable(2)
                { WidthPercentage = 100 };
                tblHeader.SetWidths(
                    new float[] { 1f, 1f });
                tblHeader.DefaultCell.Border =
                    Rectangle.NO_BORDER;

                // Trái: Nhà nước
                var cLeft = new PdfPCell
                {
                    Border = Rectangle.NO_BORDER,
                    HorizontalAlignment =
                        Element.ALIGN_CENTER,
                    PaddingBottom = 4f
                };
                cLeft.AddElement(new Paragraph(
                    "CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM",
                    F(9f, bold: true))
                { Alignment = Element.ALIGN_CENTER });
                cLeft.AddElement(new Paragraph(
                    "Độc lập  –  Tự do  –  Hạnh phúc",
                    F(9f, bold: true))
                { Alignment = Element.ALIGN_CENTER });

                tblHeader.AddCell(cLeft);

                // Phải: Địa danh + số HĐ
                var cRight = new PdfPCell
                {
                    Border = Rectangle.NO_BORDER,
                    HorizontalAlignment =
                        Element.ALIGN_CENTER,
                    PaddingBottom = 4f,
                    PaddingTop = 4f
                };
                cRight.AddElement(new Paragraph(
                    (toaNha?.TinhThanh ?? "")
                    + ", ngày " + ngayLap,
                    F(9f, italic: true, color: GRAY))
                { Alignment = Element.ALIGN_CENTER });
                cRight.AddElement(new Paragraph(
                    "Số hợp đồng: " + hd.MaHopDong,
                    F(9f, bold: true))
                { Alignment = Element.ALIGN_CENTER });
                tblHeader.AddCell(cRight);
                doc.Add(tblHeader);
                doc.Add(SP(8f));

                // ════════════════════════════════════════════════
                // TIÊU ĐỀ
                // ════════════════════════════════════════════════
                doc.Add(new Paragraph(
                    "HỢP ĐỒNG THUÊ PHÒNG TRỌ",
                    F(16f, bold: true))
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 2f
                });

                doc.Add(SP(12f));

                // ════════════════════════════════════════════════
                // MỞ ĐẦU
                // ════════════════════════════════════════════════
                doc.Add(BodyIndent(
                    "Hôm nay, ngày " + ngayLap
                    + ", tại " + diaChi
                    + ". Chúng tôi gồm có:"));
                doc.Add(SP(6f));

                // ════════════════════════════════════════════════
                // BÊN A
                // ════════════════════════════════════════════════
                doc.Add(SectionTitle(
                    "BÊN A: BÊN CHO THUÊ"));
                doc.Add(InfoLine("Họ và tên",
                    chuTro?.HoTen ?? ""));
                doc.Add(InfoLine("Số điện thoại",
                    chuTro?.SoDienThoai ?? ""));
                doc.Add(InfoLine("Địa chỉ tòa nhà",
                    diaChi));
                doc.Add(SP(6f));

                // ════════════════════════════════════════════════
                // BÊN B
                // ════════════════════════════════════════════════
                doc.Add(SectionTitle(
                    "BÊN B: BÊN THUÊ"));
                doc.Add(InfoLine("Họ và tên",
                    khach?.HoTen ?? ""));
                doc.Add(InfoLine("Ngày sinh",
                    khach?.NgaySinh.HasValue == true
                        ? khach.NgaySinh.Value
                               .ToString("dd/MM/yyyy")
                        : ""));
                doc.Add(InfoLine("Số điện thoại",
                    khach?.SoDienThoai ?? ""));
                doc.Add(InfoLine("Email",
                    khach?.Email ?? ""));
                doc.Add(InfoLine("Thường trú tại",
                    khach?.DiaChi ?? ""));
                doc.Add(SP(8f));

                doc.Add(BodyIndent(
                    "Hai bên cùng thỏa thuận và thống nhất "
                    + "ký kết hợp đồng với các điều khoản "
                    + "sau đây:"));
                doc.Add(SP(8f));

                // ════════════════════════════════════════════════
                // ĐIỀU 1
                // ════════════════════════════════════════════════
                doc.Add(ArticleTitle(
                    "Điều 1. Đối tượng hợp đồng"));
                doc.Add(BodyIndent(
                    "Bên A đồng ý cho Bên B thuê 01 (một) "
                    + "phòng trọ với thông tin cụ thể như sau:"));
                doc.Add(SP(4f));

                // Bảng thông tin phòng
                var tblPhong = BuildTable2Col(92f);
                AddRow2(tblPhong,
                    "Số phòng", phong?.SoPhong ?? "");
                AddRow2(tblPhong,
                    "Tầng",
                    phong?.Tang.ToString() ?? "");
                AddRow2(tblPhong,
                    "Diện tích",
                    phong?.DienTich.HasValue == true
                        ? phong.DienTich + " m²" : "");
                AddRow2(tblPhong,
                    "Tòa nhà",
                    toaNha?.TenToaNha ?? "");
                AddRow2(tblPhong,
                    "Địa chỉ", diaChi);
                doc.Add(tblPhong);
                doc.Add(SP(6f));

                // ════════════════════════════════════════════════
                // ĐIỀU 2
                // ════════════════════════════════════════════════
                doc.Add(ArticleTitle(
                    "Điều 2. Thời hạn thuê"));
                doc.Add(BulletLine(
                    "Thời hạn thuê: "
                    + soThang + " tháng."));
                doc.Add(BulletLine(
                    "Bắt đầu từ ngày: "
                    + hd.NgayBatDau.ToString("dd/MM/yyyy")
                    + "."));
                doc.Add(BulletLine(
                    "Kết thúc vào ngày: "
                    + hd.NgayKetThuc.ToString("dd/MM/yyyy")
                    + "."));
                doc.Add(BulletLine(
                    "Sau khi hết hạn, nếu hai bên không "
                    + "có thông báo chấm dứt, hợp đồng "
                    + "tự động gia hạn thêm 01 tháng."));
                doc.Add(SP(6f));

                // ════════════════════════════════════════════════
                // ĐIỀU 3
                // ════════════════════════════════════════════════
                doc.Add(ArticleTitle(
                    "Điều 3. Giá thuê và phương thức "
                    + "thanh toán"));
                doc.Add(BulletLine(
                    "Giá thuê phòng: "
                    + string.Format("{0:N0}",
                        hd.GiaThueThang)
                    + " đồng/tháng "
                    + "(chưa bao gồm điện, nước)."));
                if (hd.TienCoc.HasValue && hd.TienCoc > 0)
                    doc.Add(BulletLine(
                        "Tiền đặt cọc: "
                        + string.Format("{0:N0}",
                            hd.TienCoc)
                        + " đồng (sẽ hoàn trả khi "
                        + "bàn giao phòng nguyên vẹn)."));
                doc.Add(BulletLine(
                    "Ngày thanh toán hàng tháng: ngày "
                    + (hd.NgayThanhToanHangThang > 0
                        ? hd.NgayThanhToanHangThang
                               .ToString()
                        : "05")
                    + " hàng tháng."));
                doc.Add(BulletLine(
                    "Hình thức: chuyển khoản ngân hàng "
                    + "hoặc tiền mặt."));
                doc.Add(BulletLine(
                    "Thanh toán trễ quá 07 ngày, Bên A "
                    + "có quyền nhắc nhở và thu phí phạt "
                    + "theo thỏa thuận."));
                doc.Add(SP(6f));

                // ════════════════════════════════════════════════
                // ĐIỀU 4: Dịch vụ
                // ════════════════════════════════════════════════
                if (dsDV != null && dsDV.Any())
                {
                    doc.Add(ArticleTitle(
                        "Điều 4. Dịch vụ kèm theo"));
                    doc.Add(BodyIndent(
                        "Các dịch vụ đi kèm và mức phí "
                        + "hàng tháng:"));
                    doc.Add(SP(4f));

                    var tblDV = new PdfPTable(4)
                    {
                        WidthPercentage = 90,
                        SpacingAfter = 4f
                    };
                    tblDV.SetWidths(new float[]
                    { 0.5f, 3f, 1.5f, 1f });

                    // Header
                    foreach (var h in new[]
                    {
                        "STT", "Tên dịch vụ",
                        "Đơn giá (đ)", "Số lượng"
                    })
                    {
                        tblDV.AddCell(new PdfPCell(
                            new Phrase(h,
                                F(9f, bold: true)))
                        {
                            BackgroundColor =
                                LIGHT_GRAY,
                            HorizontalAlignment =
                                Element.ALIGN_CENTER,
                            Padding = 6f,
                            BorderColor = MID_GRAY
                        });
                    }

                    int stt = 1; bool alt = false;
                    foreach (var dv in dsDV)
                    {
                        var bg = alt
                            ? new BaseColor(252, 252, 252)
                            : WHITE;
                        tblDV.AddCell(DvCell(
                            stt.ToString(),
                            Element.ALIGN_CENTER, bg));
                        tblDV.AddCell(DvCell(
                            dv.GiaDichVu?.TienIch
                              ?.TenTienIch ?? "—",
                            Element.ALIGN_LEFT, bg));
                        tblDV.AddCell(DvCell(
                            string.Format("{0:N0}",
                                dv.GiaDichVu?.DonGia ?? 0),
                            Element.ALIGN_RIGHT, bg));
                        tblDV.AddCell(DvCell(
                            dv.SoLuong.ToString(),
                            Element.ALIGN_CENTER, bg));
                        stt++; alt = !alt;
                    }
                    doc.Add(tblDV);
                    doc.Add(SP(6f));
                }

                // ════════════════════════════════════════════════
                // ĐIỀU 5 (hoặc 4 nếu không có DV)
                // ════════════════════════════════════════════════
                int dieuIdx = dsDV != null && dsDV.Any()
                    ? 5 : 4;

                doc.Add(ArticleTitle(
                    "Điều " + dieuIdx
                    + ". Trách nhiệm Bên A"));
                foreach (var d in new[]
                {
                    "Đảm bảo phòng trọ không có tranh chấp, "
                    + "có đầy đủ điện nước sinh hoạt.",
                    "Đăng ký tạm trú với chính quyền địa "
                    + "phương theo quy định.",
                    "Thông báo trước ít nhất 30 ngày nếu "
                    + "muốn chấm dứt hợp đồng.",
                    "Không tự ý vào phòng khi Bên B chưa "
                    + "đồng ý, trừ trường hợp khẩn cấp.",
                    "Hoàn trả tiền cọc trong vòng 07 ngày "
                    + "sau khi Bên B trả phòng.",
                })
                    doc.Add(BulletLine(d));
                doc.Add(SP(6f));

                doc.Add(ArticleTitle(
                    "Điều " + (dieuIdx + 1)
                    + ". Trách nhiệm Bên B"));
                foreach (var d in new[]
                {
                    "Thanh toán tiền thuê đúng hạn hàng tháng.",
                    "Nộp tiền đặt cọc trước khi nhận phòng.",
                    "Sử dụng phòng đúng mục đích; "
                    + "tối đa 04 người (kể cả trẻ em).",
                    "Cung cấp giấy tờ tùy thân để đăng ký "
                    + "tạm trú.",
                    "Giữ trật tự, vệ sinh; không gây ồn ào "
                    + "sau 22:00.",
                    "Không tự ý cải tạo, sửa chữa phòng khi "
                    + "chưa có sự đồng ý của Bên A.",
                    "Không cho thuê lại hoặc chuyển nhượng "
                    + "hợp đồng.",
                    "Bồi thường thiệt hại (nếu có) theo giá "
                    + "thị trường.",
                    "Thông báo trước ít nhất 30 ngày khi "
                    + "muốn chấm dứt hợp đồng.",
                    "Không chứa hàng cấm; không nuôi thú "
                    + "cưng khi chưa được Bên A đồng ý.",
                })
                    doc.Add(BulletLine(d));
                doc.Add(SP(6f));

                doc.Add(ArticleTitle(
                    "Điều " + (dieuIdx + 2)
                    + ". Chấm dứt hợp đồng"));
                doc.Add(BodyIndent(
                    "Hợp đồng chấm dứt khi:"));
                foreach (var d in new[]
                {
                    "Hết thời hạn và các bên không gia hạn.",
                    "Một bên vi phạm nghĩa vụ sau khi đã "
                    + "được nhắc nhở bằng văn bản.",
                    "Hai bên thỏa thuận chấm dứt bằng "
                    + "văn bản.",
                })
                    doc.Add(BulletLine(d));
                doc.Add(SP(6f));

                doc.Add(ArticleTitle(
                    "Điều " + (dieuIdx + 3)
                    + ". Điều khoản chung"));
                doc.Add(BodyIndent(
                    "Hợp đồng có hiệu lực kể từ ngày ký, "
                    + "được lập thành 02 bản có giá trị "
                    + "pháp lý như nhau, mỗi bên giữ 01 bản."));
                doc.Add(BodyIndent(
                    "Mọi tranh chấp ưu tiên giải quyết "
                    + "qua thương lượng. Nếu không thỏa "
                    + "thuận được, sẽ đưa ra Tòa án nhân "
                    + "dân có thẩm quyền giải quyết."));

                if (!string.IsNullOrWhiteSpace(
                    hd.DieuKhoan))
                {
                    doc.Add(SP(4f));
                    doc.Add(new Paragraph(
                        "Điều khoản bổ sung:",
                        F(10f, bold: true,
                          italic: true))
                    { SpacingAfter = 2f });
                    doc.Add(BodyIndent(hd.DieuKhoan));
                }

                doc.Add(SP(28f));

                // ════════════════════════════════════════════════
                // KÝ TÊN
                // ════════════════════════════════════════════════
                var tblKy = new PdfPTable(2)
                {
                    WidthPercentage = 100,
                    SpacingBefore = 10f
                };
                tblKy.SetWidths(new float[] { 1f, 1f });
                tblKy.DefaultCell.Border =
                    Rectangle.NO_BORDER;
                tblKy.AddCell(SignCell(
                    "BÊN B (NGƯỜI THUÊ)",
                    khach?.HoTen ?? ""));
                tblKy.AddCell(SignCell(
                    "BÊN A (CHỦ THUÊ)",
                    chuTro?.HoTen ?? ""));
                doc.Add(tblKy);

                doc.Close();
                return ms.ToArray();
            }
        }

        // ── HELPERS ──────────────────────────────────────────────────

        private static Paragraph SP(float h) =>
            new Paragraph(" ", F(h / 2.8f));

        private static Paragraph BodyIndent(string t) =>
            new Paragraph(t, F(10f))
            {
                FirstLineIndent = 20f,
                SpacingAfter = 3f,
                Leading = 16f
            };

        private static Paragraph BulletLine(string t) =>
            new Paragraph(
                "\u2022  " + t, F(10f))
            {
                IndentationLeft = 20f,
                FirstLineIndent = 0f,
                SpacingAfter = 3f,
                Leading = 16f
            };

        private static Paragraph SectionTitle(string t)
        {
            var p = new Paragraph(t,
                F(10f, bold: true))
            {
                SpacingBefore = 2f,
                SpacingAfter = 4f
            };
            return p;
        }

        private static Paragraph ArticleTitle(string t)
        {
            // Gạch ngang nhỏ đầu điều
            var p = new Paragraph(t,
                F(10.5f, bold: true))
            {
                SpacingBefore = 10f,
                SpacingAfter = 5f
            };
            return p;
        }

        private static Paragraph InfoLine(
            string label, string val)
        {
            var p = new Paragraph
            {
                IndentationLeft = 16f,
                SpacingAfter = 3f
            };
            p.Add(new Chunk(
                label + ": ",
                F(10f, bold: true)));
            p.Add(new Chunk(val, F(10f)));
            return p;
        }

        // ── Bảng thông tin phòng ─────────────────────────────────────
        private static PdfPTable BuildTable2Col(
            float widthPct)
        {
            var t = new PdfPTable(2)
            {
                WidthPercentage = widthPct,
                SpacingBefore = 4f,
                SpacingAfter = 6f
            };
            t.SetWidths(new float[] { 1.2f, 2.8f });
            return t;
        }

        private static void AddRow2(PdfPTable tbl,
            string key, string val)
        {
            tbl.AddCell(new PdfPCell(
                new Phrase(key, F(9.5f, bold: true)))
            {
                BackgroundColor = LIGHT_GRAY,
                Padding = 6f,
                BorderColor = MID_GRAY,
                HorizontalAlignment =
                    Element.ALIGN_LEFT
            });
            tbl.AddCell(new PdfPCell(
                new Phrase(val, F(9.5f)))
            {
                Padding = 6f,
                BorderColor = MID_GRAY
            });
        }

        // ── Bảng dịch vụ ─────────────────────────────────────────────
        private static PdfPCell DvCell(string t,
            int align, BaseColor bg) =>
            new PdfPCell(new Phrase(t, F(9f)))
            {
                HorizontalAlignment = align,
                BackgroundColor = bg,
                Padding = 5f,
                BorderColor = MID_GRAY
            };

        // ── Ký tên ───────────────────────────────────────────────────
        private static PdfPCell SignCell(
            string title, string name)
        {
            var cell = new PdfPCell
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment =
                    Element.ALIGN_CENTER,
                PaddingTop = 6f
            };

            cell.AddElement(new Paragraph(title,
                F(10f, bold: true))
            { Alignment = Element.ALIGN_CENTER });

            cell.AddElement(new Paragraph(
                "(Ký và ghi rõ họ tên)",
                F(9f, italic: true,
                  color: GRAY))
            { Alignment = Element.ALIGN_CENTER });

            // Khoảng trống ký
            cell.AddElement(
                new Paragraph("\n\n\n\n", F(10f)));

            cell.AddElement(new Paragraph(name,
                F(10f, bold: true))
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingBefore = 5f
            });

            return cell;
        }
    }
}