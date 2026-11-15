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
