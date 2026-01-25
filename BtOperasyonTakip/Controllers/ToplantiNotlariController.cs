using BtOperasyonTakip.Data;
using BtOperasyonTakip.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System.Text;

namespace BtOperasyonTakip.Controllers
{
    public class ToplantiNotlariController : Controller
    {
        private readonly AppDbContext _context;

        public ToplantiNotlariController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string q)
        {
            q = (q ?? string.Empty).Trim();
            ViewBag.Q = q;

            var notlarQuery = _context.ToplantiNotlari.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                notlarQuery = notlarQuery.Where(x =>
                    (x.MusteriAdi ?? "").Contains(q) ||
                    (x.EkleyenKisi ?? "").Contains(q) ||
                    (x.NotIcerigi ?? "").Contains(q));
            }

            var notlar = notlarQuery
                .OrderByDescending(x => x.Tarih)
                .ToList();

            return View(notlar);
        }

        [HttpPost]
        public IActionResult Create(ToplantiNotu model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Index");

            model.Tarih = DateTime.Now;
            _context.ToplantiNotlari.Add(model);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateContent(int id, string notIcerigi)
        {
            var not = _context.ToplantiNotlari.Find(id);
            if (not == null) return Json(new { success = false, message = "Kayıt bulunamadı" });

            not.NotIcerigi = notIcerigi;
            _context.SaveChanges();

            return Json(new { success = true, message = "Not içeriği güncellendi" });
        }

        public IActionResult Download(int id, string format)
        {
            var note = _context.ToplantiNotlari.Find(id);
            if (note == null) return NotFound();

            string fileName = $"ToplantiNotu_{note.Id}_{note.MusteriAdi}.{format}";
            string content = $"Müşteri: {note.MusteriAdi}\nEkleyen: {note.EkleyenKisi}\nTarih: {note.Tarih:dd.MM.yyyy HH:mm}\n\n{note.NotIcerigi}";

            if (format == "txt")
                return File(Encoding.UTF8.GetBytes(content), "text/plain", fileName);

            if (format == "pdf")
            {
                using var stream = new MemoryStream();
                var doc = new Document(PageSize.A4, 50, 50, 50, 50);
                PdfWriter.GetInstance(doc, stream);
                doc.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);

                doc.Add(new Paragraph($"Toplantı Notu\n\n", titleFont));
                doc.Add(new Paragraph($"Müşteri: {note.MusteriAdi}\nEkleyen: {note.EkleyenKisi}\nTarih: {note.Tarih:dd.MM.yyyy HH:mm}\n\n", bodyFont));
                doc.Add(new Paragraph(note.NotIcerigi, bodyFont));
                doc.Close();

                return File(stream.ToArray(), "application/pdf", fileName);
            }

            if (format == "docx")
            {
                byte[] bytes = Encoding.UTF8.GetBytes(content);
                return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
            }

            return BadRequest();
        }
    }
}