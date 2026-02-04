using BtOperasyonTakip.Data;
using BtOperasyonTakip.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BtOperasyonTakip.Controllers
{
    public class TakipController : Controller
    {
        private readonly AppDbContext _context;

        private const string TalepEdenTur = "TalepEden";
        private const int DefaultPageSize = 10;
        private const int MaxPageSize = 100;

        public TakipController(AppDbContext context) => _context = context;

        [HttpGet]
        public IActionResult Index(string? q, int page = 1, int pageSize = DefaultPageSize)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? DefaultPageSize : pageSize;
            pageSize = pageSize > MaxPageSize ? MaxPageSize : pageSize;

            var talepEdenParamList = _context.Parametreler
                .AsNoTracking()
                .Where(p => (p.Tur ?? "").Trim() == TalepEdenTur && p.ParAdi != null && p.ParAdi != "")
                .OrderBy(p => p.ParAdi)
                .Select(p => p.ParAdi!)
                .ToList();

            var query = _context.JiraTasks
                .AsNoTracking()
                .Include(x => x.Yorumlar)
                .AsQueryable();

            q = (q ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var qq = q.ToLower();

                query = query.Where(t =>
                    (t.JiraId ?? "").ToLower().Contains(qq) ||
                    (t.TalepKonusu ?? "").ToLower().Contains(qq) ||
                    (t.TalepAcan ?? "").ToLower().Contains(qq) ||
                    (t.TakipEden ?? "").ToLower().Contains(qq) ||
                    (t.Durum ?? "").ToLower().Contains(qq));
            }

            query = query.OrderByDescending(x => x.OlusturmaTarihi);

            var totalCount = query.Count();

            var paged = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var model = new JiraBoardViewModel
            {
                Q = q,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,

                Beklemede = paged.Where(t => (t.Durum ?? "").Trim().Equals("Beklemede", StringComparison.OrdinalIgnoreCase)).ToList(),
                Aktif = paged.Where(t => (t.Durum ?? "").Trim().Equals("Aktif", StringComparison.OrdinalIgnoreCase)).ToList(),
                Tamamlandi = paged.Where(t => (t.Durum ?? "").Trim().Equals("Tamamlandı", StringComparison.OrdinalIgnoreCase)).ToList(),

                TalepAcanSecenekleri = talepEdenParamList,
                TakipEdenSecenekleri = talepEdenParamList
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(JiraTask model)
        {
            if (model == null) return RedirectToAction(nameof(Index));

            model.JiraId = (model.JiraId ?? "").Trim();
            model.TalepKonusu = (model.TalepKonusu ?? "").Trim();
            model.TalepAcan = (model.TalepAcan ?? "").Trim();
            model.Durum = string.IsNullOrWhiteSpace(model.Durum) ? "Beklemede" : model.Durum.Trim();
            model.TakipEden = (model.TakipEden ?? "").Trim();

            if (string.IsNullOrWhiteSpace(model.JiraId) || string.IsNullOrWhiteSpace(model.TalepKonusu))
            {
                TempData["JiraError"] = "Jira ID ve Talep Konusu zorunludur.";
                return RedirectToAction(nameof(Index));
            }

            model.OlusturmaTarihi = DateTime.Now;

            _context.JiraTasks.Add(model);
            _context.SaveChanges();

            TempData["JiraOk"] = "Görev eklendi.";
            return RedirectToAction(nameof(Index));
        }

        // Eski form post hâlâ dursun
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddYorum(int jiraTaskId, string yorum, string ekleyen)
        {
            yorum = (yorum ?? "").Trim();
            ekleyen = string.IsNullOrWhiteSpace(ekleyen) ? "Sistem" : ekleyen.Trim();

            if (string.IsNullOrWhiteSpace(yorum))
            {
                TempData["JiraError"] = "Yorum boş olamaz.";
                return RedirectToAction(nameof(Index));
            }

            var taskExists = _context.JiraTasks.Any(t => t.Id == jiraTaskId);
            if (!taskExists)
            {
                TempData["JiraError"] = "Görev bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            _context.JiraYorumlar.Add(new JiraYorum
            {
                JiraTaskId = jiraTaskId,
                YorumMetni = yorum,
                Ekleyen = ekleyen,
                Tarih = DateTime.Now
            });

            _context.SaveChanges();
            TempData["JiraOk"] = "Yorum eklendi.";
            return RedirectToAction(nameof(Index));
        }

        // ✅ NEW: Detaydan fetch(JSON) ile yorum eklemek için
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult AddYorumJson([FromBody] AddYorumJsonModel model)
        {
            if (model == null || model.JiraTaskId <= 0)
                return Json(new { success = false, message = "Geçersiz model" });

            var yorum = (model.Yorum ?? "").Trim();
            var ekleyen = string.IsNullOrWhiteSpace(model.Ekleyen) ? "Sistem" : model.Ekleyen.Trim();

            if (string.IsNullOrWhiteSpace(yorum))
                return Json(new { success = false, message = "Yorum boş olamaz." });

            var taskExists = _context.JiraTasks.Any(t => t.Id == model.JiraTaskId);
            if (!taskExists)
                return Json(new { success = false, message = "Görev bulunamadı." });

            _context.JiraYorumlar.Add(new JiraYorum
            {
                JiraTaskId = model.JiraTaskId,
                YorumMetni = yorum,
                Ekleyen = ekleyen,
                Tarih = DateTime.Now
            });

            _context.SaveChanges();
            return Json(new { success = true });
        }

        public class AddYorumJsonModel
        {
            public int JiraTaskId { get; set; }
            public string? Yorum { get; set; }
            public string? Ekleyen { get; set; }
        }

        [HttpPost]
        public JsonResult UpdateDurum([FromBody] UpdateDurumModel model)
        {
            try
            {
                Console.WriteLine($"🟢 UPDATE DURUM BAŞLADI (Takip): ID={model?.Id}, Durum='{model?.YeniDurum}'");

                if (model == null)
                    return Json(new { success = false, message = "Model is null" });

                if (model.Id <= 0)
                    return Json(new { success = false, message = "Invalid ID" });

                if (string.IsNullOrWhiteSpace(model.YeniDurum))
                    return Json(new { success = false, message = "Empty status" });

                var task = _context.JiraTasks.Find(model.Id);
                if (task == null)
                    return Json(new { success = false, message = "Task not found" });

                var oldStatus = task.Durum;
                task.Durum = model.YeniDurum.Trim();
                _context.SaveChanges();

                Console.WriteLine($"🟢 SUCCESS: {task.JiraId} {oldStatus} -> {task.Durum}");
                return Json(new { success = true, message = "Status updated" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔴 EXCEPTION: {ex.Message}");
                Console.WriteLine($"🔴 STACK TRACE: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public JsonResult Delete([FromBody] DeleteModel model)
        {
            var task = _context.JiraTasks
                               .Include(t => t.Yorumlar)
                               .FirstOrDefault(t => t.Id == model.Id);

            if (task == null)
                return Json(new { success = false, message = "Kayıt bulunamadı." });

            if (task.Yorumlar != null && task.Yorumlar.Any())
                _context.JiraYorumlar.RemoveRange(task.Yorumlar);

            _context.JiraTasks.Remove(task);
            _context.SaveChanges();

            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult Assign([FromBody] AssignModel model)
        {
            if (model == null || model.Id <= 0)
                return Json(new { success = false, message = "Geçersiz model" });

            var task = _context.JiraTasks.Find(model.Id);
            if (task == null)
                return Json(new { success = false, message = "Kayıt bulunamadı" });

            task.TakipEden = (model.TakipEden ?? "").Trim();
            _context.SaveChanges();

            return Json(new { success = true });
        }

        public class AssignModel
        {
            public int Id { get; set; }
            public string? TakipEden { get; set; }
        }

        [HttpGet]
        public IActionResult DetailCard(int id)
        {
            var task = _context.JiraTasks
                               .Include(t => t.Yorumlar)
                               .FirstOrDefault(t => t.Id == id);

            if (task == null)
                return Content("<div class='text-danger small'>Kayıt bulunamadı.</div>", "text/html");

            return PartialView("~/Views/Jira/_JiraDetailCard.cshtml", task);
        }

        public class UpdateDurumModel
        {
            public int Id { get; set; }
            public string YeniDurum { get; set; }
        }

        public class DeleteModel
        {
            public int Id { get; set; }
        }
    }
}