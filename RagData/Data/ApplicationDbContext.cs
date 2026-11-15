using Microsoft.EntityFrameworkCore;
using Rag.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Data.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        DbSet<ServiceAgent> ServiceAgents {  get; set; }
        DbSet<LlmProvider> LlmProviders { get; set; }
        DbSet<KnowledgeBaseCategory> KnowledgeBaseCategories { get; set; }
        DbSet<KnowledgeBase> KnowledgeBases { get; set; }
        DbSet<FileItem> FileItems { get; set; }
        DbSet<KnowledgeBaseDocument> KnowledgeBaseDocuments { get; set; }
        DbSet<Conversation> Conversations { get; set; }
        DbSet<Turn> Turns { get; set; }
        DbSet<TurnFeedback> Feedbacks { get; set; }
        DbSet<TelegramConversation> TelegramConversations { get; set; }
        DbSet<MenuItem> MenuItems { get; set; }
        DbSet<RestaurantTable> RestaurantTables { get; set; }
        DbSet<Booking> Bookings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LlmProvider>().HasData(
                  new LlmProvider { Id = 1, Name = "Open AI" },
                  new LlmProvider { Id = 2, Name = "Antropic" },
                  new LlmProvider { Id = 3, Name = "GemenAI" },
                  new LlmProvider { Id = 4, Name = "Deepseek" }
             );

            modelBuilder.Entity<Turn>()
                .Property(x => x.RetrievedChunkIds)
                .HasColumnType("text[]");

            modelBuilder.Entity<Turn>()
                .HasOne(t => t.Feedback)
                .WithOne(f => f.Turn)
                .HasForeignKey<TurnFeedback>(f => f.TurnId);

            #region Restaurant
            modelBuilder.Entity<MenuItem>().Property(x => x.Price).HasPrecision(10, 2);
            modelBuilder.Entity<MenuItem>().HasIndex(x => x.Category);
            modelBuilder.Entity<RestaurantTable>().HasIndex(x => x.Number).IsUnique();
            modelBuilder.Entity<Booking>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            modelBuilder.Entity<Booking>().HasIndex(x => new { x.TableId, x.StartTime });
            modelBuilder.Entity<Booking>().HasIndex(x => x.Phone);
            modelBuilder.Entity<Booking>()
                .HasOne(x => x.Table)
                .WithMany(t => t.Bookings)
                .HasForeignKey(x => x.TableId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MenuItem>().HasData(
                new MenuItem { Id = 1, Category = "Salads", Name = "Armenian seasonal salad", Description = "Tomatoes, cucumbers, sweet peppers, fresh greens and sunflower oil", Price = 2200, IsAvailable = true },
                new MenuItem { Id = 2, Category = "Salads", Name = "Caesar salad with chicken", Description = "Romaine, grilled chicken, parmesan, croutons, Caesar dressing", Price = 3200, IsAvailable = true },
                new MenuItem { Id = 3, Category = "Salads", Name = "Eetch", Description = "Bulgur with tomato paste, peppers and herbs", Price = 1800, IsAvailable = true },
                new MenuItem { Id = 4, Category = "Soups", Name = "Spas", Description = "Warm yogurt soup with wheat and herbs", Price = 1600, IsAvailable = true },
                new MenuItem { Id = 5, Category = "Soups", Name = "Khash", Description = "Traditional beef soup served with garlic, lavash and radish (served until noon)", Price = 3800, IsAvailable = true },
                new MenuItem { Id = 6, Category = "Soups", Name = "Okroshka", Description = "Cold summer soup with kefir and vegetables", Price = 1700, IsAvailable = false },
                new MenuItem { Id = 7, Category = "Grill", Name = "Pork khorovats", Description = "Charcoal-grilled pork with onions and lavash", Price = 4500, IsAvailable = true },
                new MenuItem { Id = 8, Category = "Grill", Name = "Lamb khorovats", Description = "Charcoal-grilled lamb ribs", Price = 5200, IsAvailable = true },
                new MenuItem { Id = 9, Category = "Grill", Name = "Chicken lula kebab", Description = "Minced chicken kebab with grilled vegetables", Price = 3600, IsAvailable = true },
                new MenuItem { Id = 10, Category = "Main courses", Name = "Tolma", Description = "Minced meat and rice wrapped in grape leaves, with matsun", Price = 3400, IsAvailable = true },
                new MenuItem { Id = 11, Category = "Main courses", Name = "Harissa", Description = "Slow-cooked wheat and chicken porridge", Price = 3000, IsAvailable = true },
                new MenuItem { Id = 12, Category = "Main courses", Name = "Grilled Sevan trout", Description = "Whole trout with lemon and tarragon", Price = 5600, IsAvailable = true },
                new MenuItem { Id = 13, Category = "Desserts", Name = "Gata", Description = "Sweet Armenian pastry", Price = 1200, IsAvailable = true },
                new MenuItem { Id = 14, Category = "Desserts", Name = "Pakhlava", Description = "Layered pastry with walnuts and honey", Price = 1500, IsAvailable = true },
                new MenuItem { Id = 15, Category = "Drinks", Name = "Armenian coffee", Description = "Traditional coffee brewed in a jazzve", Price = 800, IsAvailable = true }
            );

            modelBuilder.Entity<RestaurantTable>().HasData(
                new RestaurantTable { Id = 1, Number = 1, Seats = 2, Location = "hall" },
                new RestaurantTable { Id = 2, Number = 2, Seats = 2, Location = "hall" },
                new RestaurantTable { Id = 3, Number = 3, Seats = 4, Location = "hall" },
                new RestaurantTable { Id = 4, Number = 4, Seats = 4, Location = "hall" },
                new RestaurantTable { Id = 5, Number = 5, Seats = 6, Location = "hall" },
                new RestaurantTable { Id = 6, Number = 6, Seats = 2, Location = "terrace" },
                new RestaurantTable { Id = 7, Number = 7, Seats = 4, Location = "terrace" },
                new RestaurantTable { Id = 8, Number = 8, Seats = 4, Location = "terrace" },
                new RestaurantTable { Id = 9, Number = 9, Seats = 8, Location = "VIP" },
                new RestaurantTable { Id = 10, Number = 10, Seats = 12, Location = "VIP" }
            );

            // Booking times are stored as given (no time zone logic).
            var seedCreated = new DateTime(2026, 11, 15, 8, 0, 0, DateTimeKind.Unspecified);
            modelBuilder.Entity<Booking>().HasData(
                new Booking { Id = 1, TableId = 3, CustomerName = "Anna Petrosyan", Phone = "+37491123456", Guests = 4, StartTime = new DateTime(2026, 11, 20, 19, 0, 0), EndTime = new DateTime(2026, 11, 20, 21, 0, 0), Status = BookingStatus.Confirmed, CreatedAt = seedCreated },
                new Booking { Id = 2, TableId = 9, CustomerName = "David Sargsyan", Phone = "+37477234567", Guests = 7, StartTime = new DateTime(2026, 11, 20, 20, 0, 0), EndTime = new DateTime(2026, 11, 20, 22, 0, 0), Notes = "Birthday, needs a cake plate", Status = BookingStatus.Confirmed, CreatedAt = seedCreated },
                new Booking { Id = 3, TableId = 6, CustomerName = "Elena Ivanova", Phone = "+37455345678", Guests = 2, StartTime = new DateTime(2026, 11, 21, 13, 0, 0), EndTime = new DateTime(2026, 11, 21, 15, 0, 0), Notes = "Terrace if possible", Status = BookingStatus.Confirmed, CreatedAt = seedCreated },
                new Booking { Id = 4, TableId = 5, CustomerName = "John Smith", Phone = "+37493456789", Guests = 5, StartTime = new DateTime(2026, 11, 19, 18, 30, 0), EndTime = new DateTime(2026, 11, 19, 20, 30, 0), Status = BookingStatus.Cancelled, CreatedAt = seedCreated }
            );
            #endregion
        }
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<DateTime>()
                .HaveColumnType("timestamp without time zone");

            configurationBuilder.Properties<DateTime?>()
           .HaveColumnType("timestamp without time zone");
        }
    }
}
