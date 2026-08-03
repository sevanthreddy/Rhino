using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Likes>()
    .HasOne(l => l.Post)
    .WithMany(p => p.Likes)
    .HasForeignKey(l => l.Postid);

        modelBuilder.Entity<Likes>()
            .HasOne(l => l.Reply)
            .WithMany(r => r.Likes)
            .HasForeignKey(l => l.RepliesId);

        modelBuilder.Entity<Replies>()
        .HasOne(r => r.User)
        .WithMany()
        .HasForeignKey(r => r.UserId)
        .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Follow>()
        .HasOne(f => f.Follower).WithMany(u => u.Following).HasForeignKey(f => f.FollowerId).OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Follow>()
        .HasOne(f => f.Following).WithMany(u => u.Followers).HasForeignKey(f => f.FollowingId).OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Message>()
        .HasOne(m => m.Sender).WithMany(x => x.SentMessages).HasForeignKey(y => y.SenderId).OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Message>()
        .HasOne(m => m.Receiver).WithMany(x => x.ReceivedMessages).HasForeignKey(y => y.ReceiverId).OnDelete(DeleteBehavior.NoAction);
    }

    public DbSet<User> Users { get; set; }

    public DbSet<Post> Posts { get; set; }

    public DbSet<Likes> Likes { get; set; }

    public DbSet<Images> Images { get; set; }

    public DbSet<Replies> Replies { get; set; }

    public DbSet<Follow> Follow { get; set; }

    public DbSet<Message> Message { get; set; }
}