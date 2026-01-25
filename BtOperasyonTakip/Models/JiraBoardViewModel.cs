namespace BtOperasyonTakip.Models
{
    public class JiraBoardViewModel
    {
        public List<JiraTask> Beklemede { get; set; } = new();
        public List<JiraTask> Aktif { get; set; } = new();
        public List<JiraTask> Tamamlandi { get; set; } = new();
    }
}
