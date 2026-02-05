using BtOperasyonTakip.Models;
using Microsoft.EntityFrameworkCore;
using static BtOperasyonTakip.Models.DashboardViewModel;

namespace BtOperasyonTakip.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Musteri> Musteriler => Set<Musteri>();
        public DbSet<IletisimBilgileri> IletisimBilgileri => Set<IletisimBilgileri>();
        public DbSet<Detay> Detaylar => Set<Detay>();
        public DbSet<Parametre> Parametreler => Set<Parametre>();
        public DbSet<ToplantiNotu> ToplantiNotlari => Set<ToplantiNotu>();
        public DbSet<JiraTask> JiraTasks => Set<JiraTask>();
        public DbSet<JiraYorum> JiraYorumlar { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Hata> Hatalar { get; set; } // YENİ
        public DbSet<Issue> Issues { get; set; }

        public DbSet<TicketAtamaLog> TicketAtamaLoglari { get; set; }
    }
}