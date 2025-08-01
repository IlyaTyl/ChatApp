using Microsoft.EntityFrameworkCore;
using SignalRAppChat.Shared.Models;
using System.Security.Cryptography;

namespace SignalRAppChat.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<ChatUser> ChatUsers { get; set; }
        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<Friend> Friends { get; set; }
        public DbSet<MessageRead> MessageReads { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ChatUser>()
                .HasKey(cu => new { cu.ChatId, cu.UserId});

            modelBuilder.Entity<ChatUser>()
                .HasOne(cu => cu.Chat)
                .WithMany(cu => cu.ChatUsers)
                .HasForeignKey(cu => cu.ChatId);

            modelBuilder.Entity<ChatUser>()
                .HasOne(cu => cu.User)
                .WithMany(cu => cu.ChatUsers)
                .HasForeignKey(cu => cu.UserId);

            modelBuilder.Entity<FriendRequest>()
                .HasOne(fr => fr.Sender)
                .WithMany(u => u.SentFriendRequests)
                .HasForeignKey(fr => fr.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FriendRequest>()
                .HasOne(fr => fr.Receiver)
                .WithMany(u => u.ReceivedFriendRequests)
                .HasForeignKey(fr => fr.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Friend>()
                .HasOne(f => f.User)
                .WithMany(u => u.Friends)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Friend>()
                .HasOne(f => f.FriendUser)
                .WithMany(u => u.FriendOf)
                .HasForeignKey(f => f.FriendUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MessageRead>()
                .HasOne(mr => mr.User)
                .WithMany(u => u.MessageReads)
                .HasForeignKey(mr => mr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MessageRead>()
                .HasOne(mr => mr.Message)
                .WithMany(m => m.MessageReads)
                .HasForeignKey(m => m.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
