using Microsoft.EntityFrameworkCore;
using WebAPI.Models;

namespace WebAPI.Data
{

    public partial class ApplicationDbContext : DbContext
    {

        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }
        public virtual DbSet<User> User { get; set; }
        public virtual DbSet<Exam> Exam { get; set; }
        public virtual DbSet<ExamAttempt> ExamAttempt { get; set; }
        public virtual DbSet<Reading> Reading { get; set; }
        public virtual DbSet<Listening> Listening { get; set; }
        public virtual DbSet<Writing> Writing { get; set; }
        public virtual DbSet<Speaking> Speaking { get; set; }
        public virtual DbSet<VocabGroup> VocabGroup { get; set; }
        public virtual DbSet<Word> Word { get; set; }
        public virtual DbSet<Report> Report { get; set; }
        public virtual DbSet<Transaction> Transactions { get; set; }
        public virtual DbSet<Notification> Notification { get; set; }
        public virtual DbSet<Post> Post { get; set; }
        public virtual DbSet<PostAttachment> PostAttachment { get; set; }
        public virtual DbSet<Comment> Comment { get; set; }
        public virtual DbSet<Tag> Tag { get; set; }
        public virtual DbSet<PostLike> PostLike { get; set; }
        public virtual DbSet<CommentLike> CommentLike { get; set; }
        public virtual DbSet<UserPostHide> UserPostHide { get; set; }
        public virtual DbSet<UserSignInHistory> UserSignInHistory { get; set; }
        public virtual DbSet<WritingFeedback> WritingFeedback { get; set; }
        public virtual DbSet<VipPlan> VipPlans { get; set; }
        public virtual DbSet<Speaking> Speakings { get; set; }

        public virtual DbSet<SpeakingFeedback> SpeakingFeedbacks { get; set; }
        public virtual DbSet<SpeakingAttempt> SpeakingAttempts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(e => e.CommentId).HasName("PK__Comment__E7957687C296B01F");

                entity.ToTable("Comment");

                entity.Property(e => e.CommentId).HasColumnName("comment_id");
                entity.Property(e => e.Content).HasColumnName("content");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.LikeNumber).HasColumnName("like_number");
                entity.Property(e => e.ParentCommentId).HasColumnName("parent_comment_id");
                entity.Property(e => e.PostId).HasColumnName("post_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasOne(d => d.ParentComment).WithMany(p => p.InverseParentComment)
                    .HasForeignKey(d => d.ParentCommentId)
                    .HasConstraintName("FK__Comment__parent___778AC167");

                entity.HasOne(d => d.Post).WithMany(p => p.Comments)
                    .HasForeignKey(d => d.PostId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Comment__post_id__75A278F5");

                entity.HasOne(d => d.User).WithMany(p => p.Comments)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Comment__user_id__76969D2E");
            });

            modelBuilder.Entity<Exam>(entity =>
            {
                entity.HasKey(e => e.ExamId).HasName("PK__Exam__9C8C7BE95200C68D");

                entity.ToTable("Exam");

                entity.Property(e => e.ExamId).HasColumnName("exam_id");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.ExamName)
                    .HasMaxLength(100)
                    .HasColumnName("exam_name");
                entity.Property(e => e.ExamType)
                    .HasMaxLength(20)
                    .HasColumnName("exam_type");
            });

            modelBuilder.Entity<ExamAttempt>(entity =>
            {
                entity.HasKey(e => e.AttemptId).HasName("PK__ExamAtte__5621F94951E1EFD0");

                entity.ToTable("ExamAttempt");

                entity.Property(e => e.AttemptId).HasColumnName("attempt_id");
                entity.Property(e => e.AnswerText).HasColumnName("answer_text");
                entity.Property(e => e.ExamId).HasColumnName("exam_id");
                entity.Property(e => e.Score)
                    .HasColumnType("decimal(5, 2)")
                    .HasColumnName("score");
                entity.Property(e => e.StartedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())")
                    .HasColumnName("started_at");
                entity.Property(e => e.SubmittedAt)
                    .HasPrecision(0)
                    .HasColumnName("submitted_at");
                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasOne(d => d.Exam).WithMany(p => p.ExamAttempts)
                    .HasForeignKey(d => d.ExamId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__ExamAttem__exam___534D60F1");

                entity.HasOne(d => d.User).WithMany(p => p.ExamAttempts)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__ExamAttem__user___5441852A");
            });

            modelBuilder.Entity<Listening>(entity =>
            {
                entity.HasKey(e => e.ListeningId).HasName("PK__Listenin__91F7AA8D2027FE10");

                entity.ToTable("Listening");

                entity.Property(e => e.ListeningId).HasColumnName("listening_id");
                entity.Property(e => e.CorrectAnswer).HasColumnName("correct_answer");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
                entity.Property(e => e.ExamId).HasColumnName("exam_id");
                entity.Property(e => e.ListeningContent).HasColumnName("listening_content");
                entity.Property(e => e.ListeningQuestion).HasColumnName("listening_question");
                entity.Property(e => e.ListeningType)
                    .HasMaxLength(50)
                    .HasColumnName("listening_type");
                entity.Property(e => e.QuestionHtml).HasColumnName("question_html");

                entity.HasOne(d => d.Exam).WithMany(p => p.Listenings)
                    .HasForeignKey(d => d.ExamId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Listening__exam___5BE2A6F2");
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__E059842F0D1EAED9");

                entity.ToTable("Notification");

                entity.Property(e => e.NotificationId).HasColumnName("notification_id");
                entity.Property(e => e.Content).HasColumnName("content");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.IsRead).HasColumnName("is_read");
                entity.Property(e => e.Type)
                    .HasMaxLength(30)
                    .HasColumnName("type");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.PostId).HasColumnName("post_id");

                entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Notificat__user___6D0D32F4");
            });

            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasKey(e => e.PostId).HasName("PK__Post__3ED787666EAF0D81");

                entity.ToTable("Post");

                entity.Property(e => e.PostId).HasColumnName("post_id");
                entity.Property(e => e.Content).HasColumnName("content");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Title).HasColumnName("Title");
                entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");
                entity.Property(e => e.ViewCount).HasColumnName("ViewCount");
                entity.Property(e => e.IsPinned).HasColumnName("IsPinned");
                entity.Property(e => e.IsHidden).HasColumnName("IsHidden");
                entity.Property(e => e.Status).HasColumnName("Status");
                entity.Property(e => e.RejectionReason).HasColumnName("RejectionReason");

                entity.HasOne(d => d.User).WithMany(p => p.Posts)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Post__user_id__71D1E811");

                entity.HasMany(d => d.Tags).WithMany(p => p.Posts)
                    .UsingEntity<Dictionary<string, object>>(
                        "PostTag",
                        r => r.HasOne<Tag>().WithMany()
                            .HasForeignKey("tag_id")
                            .OnDelete(DeleteBehavior.ClientSetNull)
                            .HasConstraintName("FK__Post_Tag__tag_id__00200768"),
                        l => l.HasOne<Post>().WithMany()
                            .HasForeignKey("post_id")
                            .OnDelete(DeleteBehavior.ClientSetNull)
                            .HasConstraintName("FK__Post_Tag__post_i__7F2BE32F"),
                        j =>
                        {
                            j.HasKey("post_id", "tag_id").HasName("PK__Post_Tag__4AFEED4D40095113");
                            j.ToTable("Post_Tag");
                            j.IndexerProperty<int>("post_id").HasColumnName("post_id");
                            j.IndexerProperty<int>("tag_id").HasColumnName("tag_id");
                        });
            });

            modelBuilder.Entity<PostAttachment>(entity =>
            {
                entity.HasKey(e => e.AttachmentId).HasName("PK__PostAttachment__AttachmentId");

                entity.ToTable("PostAttachment");

                entity.Property(e => e.AttachmentId).HasColumnName("attachment_id");
                entity.Property(e => e.PostId).HasColumnName("post_id");
                entity.Property(e => e.FileName)
                    .HasMaxLength(255)
                    .HasColumnName("file_name");
                entity.Property(e => e.FileUrl)
                    .HasMaxLength(500)
                    .HasColumnName("file_url");
                entity.Property(e => e.FileType)
                    .HasMaxLength(50)
                    .HasColumnName("file_type");
                entity.Property(e => e.FileExtension)
                    .HasMaxLength(10)
                    .HasColumnName("file_extension");
                entity.Property(e => e.FileSize).HasColumnName("file_size");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())")
                    .HasColumnName("created_at");

                entity.HasOne(d => d.Post).WithMany(p => p.Attachments)
                    .HasForeignKey(d => d.PostId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK__PostAttachment__post_id");
            });

            modelBuilder.Entity<PostLike>(entity =>
            {
                entity.HasKey(e => new { e.PostId, e.UserId }).HasName("PK__PostLike__D54C641668B8C345");

                entity.ToTable("PostLike");

                entity.Property(e => e.PostId).HasColumnName("post_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())")
                    .HasColumnName("created_at");

                entity.HasOne(d => d.Post).WithMany(p => p.PostLikes)
                    .HasForeignKey(d => d.PostId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__PostLike__post_i__02FC7413");

                entity.HasOne(d => d.User).WithMany(p => p.PostLikes)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__PostLike__user_i__03F0984C");
            });

            modelBuilder.Entity<Reading>(entity =>
            {
                entity.HasKey(e => e.ReadingId).HasName("PK__Reading__8091F95AEE19033B");

                entity.ToTable("Reading");

                entity.Property(e => e.ReadingId).HasColumnName("reading_id");
                entity.Property(e => e.CorrectAnswer).HasColumnName("correct_answer");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
                entity.Property(e => e.ExamId).HasColumnName("exam_id");
                entity.Property(e => e.QuestionHtml).HasColumnName("question_html");
                entity.Property(e => e.ReadingContent).HasColumnName("reading_content");
                entity.Property(e => e.ReadingQuestion).HasColumnName("reading_question");
                entity.Property(e => e.ReadingType)
                    .HasMaxLength(50)
                    .HasColumnName("reading_type");

                entity.HasOne(d => d.Exam).WithMany(p => p.Readings)
                    .HasForeignKey(d => d.ExamId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Reading__exam_id__5812160E");
            });

            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasKey(e => e.ReportId).HasName("PK__Report__779B7C589EC9B5E3");

                entity.ToTable("Report");

                entity.Property(e => e.ReportId).HasColumnName("report_id");
                entity.Property(e => e.Content).HasColumnName("content");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .HasDefaultValue("Pending")
                    .HasColumnName("status");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.PostId).HasColumnName("post_id");

                entity.HasOne(d => d.User).WithMany(p => p.Reports)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Report__user_id__60A75C0F");

                entity.HasOne(d => d.Post).WithMany(p => p.Reports)
                    .HasForeignKey(d => d.PostId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Report_Post");
            });

            modelBuilder.Entity<Speaking>(entity =>
            {
                entity.HasKey(e => e.SpeakingId).HasName("PK__Speaking__598C697379CF1670");

                entity.ToTable("Speaking");

                entity.Property(e => e.SpeakingId).HasColumnName("speaking_id");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
                entity.Property(e => e.ExamId).HasColumnName("exam_id");
                entity.Property(e => e.SpeakingQuestion).HasColumnName("speaking_question");
                entity.Property(e => e.SpeakingType)
                    .HasMaxLength(50)
                    .HasColumnName("speaking_type");

                entity.HasOne(d => d.Exam).WithMany(p => p.Speakings)
                    .HasForeignKey(d => d.ExamId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Speaking__exam_i__6383C8BA");
            });

            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(e => e.TagId).HasName("PK__Tag__4296A2B65970D7BC");

                entity.ToTable("Tag");

                entity.HasIndex(e => e.TagName, "UQ__Tag__E298655CA5EF38BD").IsUnique();

                entity.Property(e => e.TagId).HasColumnName("tag_id");
                entity.Property(e => e.TagName)
                    .HasMaxLength(50)
                    .HasColumnName("tag_name");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())")
                    .HasColumnName("created_at");
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.TransactionId).HasName("PK__Transact__85C600AF72F38544");

                entity.ToTable("Transactions");

                entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
                entity.Property(e => e.Amount)
                    .HasColumnType("decimal(18, 2)")
                    .HasColumnName("amount");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.Currency)
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValue("VND")
                    .IsFixedLength()
                    .HasColumnName("currency");
                entity.Property(e => e.PaymentMethod)
                    .HasMaxLength(30)
                    .HasColumnName("payment_method");
                entity.Property(e => e.ProviderTxnId)
                    .HasMaxLength(100)
                    .HasColumnName("provider_txn_id");
                entity.Property(e => e.Purpose)
                    .HasMaxLength(50)
                    .HasDefaultValue("VIP")
                    .HasColumnName("purpose");
                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("PENDING")
                    .HasColumnName("status");
                entity.Property(e => e.UserId).HasColumnName("user_id");

                // Indexes for performance and idempotency
                entity.HasIndex(e => new { e.UserId, e.CreatedAt }).HasDatabaseName("IX_Transaction_User_CreatedAt");
                entity.HasIndex(e => new { e.Status, e.CreatedAt }).HasDatabaseName("IX_Transaction_Status_CreatedAt");
                entity.HasIndex(e => e.ProviderTxnId).IsUnique();
                entity.HasIndex(e => e.CreatedAt);

                entity.HasOne(d => d.User).WithMany(p => p.Transactions)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Transacti__user___656C112C");
                entity.HasOne(d => d.Plan)
                    .WithMany()
                    .HasForeignKey(d => d.PlanId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Transactions_VipPlans");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId).HasName("PK__Users__B9BE370F8208A11F");

                entity.ToTable("Users");

                entity.HasIndex(e => e.Email, "UQ__Users__AB6E616418A9BBE3").IsUnique();

                entity.HasIndex(e => e.Username, "UQ__Users__F3DBC572977AB95F").IsUnique();

                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.Email)
                    .HasMaxLength(150)
                    .HasColumnName("email");
                entity.Property(e => e.Firstname)
                    .HasMaxLength(100)
                    .HasColumnName("firstname");
                entity.Property(e => e.Lastname)
                    .HasMaxLength(100)
                    .HasColumnName("lastname");
                entity.Property(e => e.PasswordHash)
                    .HasMaxLength(256)
                    .HasColumnName("password_hash");
                entity.Property(e => e.PasswordSalt)
                    .HasMaxLength(128)
                    .HasColumnName("password_salt");
                entity.Property(e => e.Role)
                    .HasMaxLength(50)
                    .HasDefaultValue("user")
                    .HasColumnName("role");
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(0)
                    .HasColumnName("updated_at");
                entity.Property(e => e.Username)
                    .HasMaxLength(100)
                    .HasColumnName("username");
                entity.Property(e => e.VipExpireAt)
                .HasColumnName("vip_expire_at");
            });

            modelBuilder.Entity<UserSignInHistory>(entity =>
            {
                entity.HasKey(e => e.SigninId).HasName("PK__UserSignInHistory__SignInId");
                entity.ToTable("UserSignInHistory");

                entity.Property(e => e.SigninId).HasColumnName("signin_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.IpAddress)
                    .HasMaxLength(50)
                    .HasColumnName("ip_address");
                entity.Property(e => e.DeviceInfo)
                    .HasMaxLength(200)
                    .HasColumnName("device_info");
                entity.Property(e => e.Location)
                    .HasMaxLength(200)
                    .HasColumnName("location");
                entity.Property(e => e.SignedInAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())")
                    .HasColumnName("signed_in_at");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.SignInHistories)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_UserSignInHistory_User");
            });


            modelBuilder.Entity<VocabGroup>(entity =>
            {
                entity.HasKey(e => e.GroupId).HasName("PK__VocabGro__D57795A0AB20D47E");

                entity.ToTable("VocabGroup");

                entity.HasIndex(e => new { e.UserId, e.Groupname }, "UX_group_user").IsUnique();

                entity.Property(e => e.GroupId).HasColumnName("group_id");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.Groupname)
                    .HasMaxLength(100)
                    .HasColumnName("groupname");
                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasMany(d => d.Words).WithMany(p => p.Groups)
                    .UsingEntity<Dictionary<string, object>>(
                        "VocabGroup_Word",
                        r => r.HasOne<Word>().WithMany()
                            .HasForeignKey("word_id")
                            .OnDelete(DeleteBehavior.Cascade)
                            .HasConstraintName("FK__VocabGrou__word___5DCAEF64"),
                        l => l.HasOne<VocabGroup>().WithMany()
                            .HasForeignKey("group_id")
                            .OnDelete(DeleteBehavior.Cascade)
                            .HasConstraintName("FK__VocabGrou__group__5CD6CB2B"),
                        j =>
                        {
                            j.HasKey("group_id", "word_id")
                             .HasName("PK__VocabGro__C2883474CEDF8177");

                            j.ToTable("VocabGroup_Word");

                            j.IndexerProperty<int>("group_id").HasColumnName("group_id");
                            j.IndexerProperty<int>("word_id").HasColumnName("word_id");
                        });
            });

            modelBuilder.Entity<Word>(entity =>
            {
                entity.HasKey(e => e.WordId).HasName("PK__Word__7FFA1D4039C4B1A6");

                entity.ToTable("Word");

                entity.Property(e => e.WordId).HasColumnName("word_id").ValueGeneratedOnAdd();
                entity.Property(e => e.Audio)
                    .HasMaxLength(512)
                    .HasColumnName("audio");
                entity.Property(e => e.Example).HasColumnName("example");
                entity.Property(e => e.Meaning)
                    .HasMaxLength(255)
                    .HasColumnName("meaning");
                entity.Property(e => e.Term)
                    .HasMaxLength(100)
                    .HasColumnName("word");
            });

            modelBuilder.Entity<CommentLike>(entity =>
            {
                entity.HasKey(e => e.CommentLikeId).HasName("PK__CommentLike__CommentLikeId");

                entity.ToTable("CommentLike");

                entity.Property(e => e.CommentLikeId).HasColumnName("comment_like_id");
                entity.Property(e => e.CommentId).HasColumnName("comment_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())")
                    .HasColumnName("created_at");

                entity.HasOne(d => d.Comment).WithMany(c => c.CommentLikes)
                    .HasForeignKey(d => d.CommentId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__CommentLike__comment_id");

                entity.HasOne(d => d.User).WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__CommentLike__user_id");
            });

            modelBuilder.Entity<UserPostHide>(entity =>
            {
                entity.HasKey(e => e.UserPostHideId).HasName("PK__UserPostH__user_post_hide_id");

                entity.ToTable("UserPostHide");

                entity.Property(e => e.UserPostHideId).HasColumnName("user_post_hide_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.PostId).HasColumnName("post_id");
                entity.Property(e => e.HiddenAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())")
                    .HasColumnName("hidden_at");

                entity.HasOne(d => d.User).WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK__UserPostHide__user_id");

                entity.HasOne(d => d.Post).WithMany()
                    .HasForeignKey(d => d.PostId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK__UserPostHide__post_id");
            });

            modelBuilder.Entity<Writing>(entity =>
            {
                entity.HasKey(e => e.WritingId).HasName("PK__Writing__4AE2633E2FDA5C2C");

                entity.ToTable("Writing");

                entity.Property(e => e.WritingId).HasColumnName("writing_id");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
                entity.Property(e => e.ExamId).HasColumnName("exam_id");
                entity.Property(e => e.WritingQuestion).HasColumnName("writing_question");

                entity.Property(e => e.ImageUrl)
                    .HasColumnName("ImageUrl")
                    .HasMaxLength(500);

                entity.HasOne(d => d.Exam).WithMany(p => p.Writings)
                    .HasForeignKey(d => d.ExamId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Writing__exam_id__5FB337D6");
            });

            modelBuilder.Entity<WritingFeedback>(entity =>
            {
                entity.HasKey(e => e.FeedbackId).HasName("PK__WritingF__7A6B2B8C940C0650");

                entity.ToTable("WritingFeedback");

                entity.Property(e => e.FeedbackId).HasColumnName("feedback_id");
                entity.Property(e => e.AttemptId).HasColumnName("attempt_id");
                entity.Property(e => e.CoherenceCohesion)
                    .HasColumnType("decimal(3, 1)")
                    .HasColumnName("coherence_cohesion");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.FeedbackSections).HasColumnName("feedback_sections");
                entity.Property(e => e.GrammarAccuracy)
                    .HasColumnType("decimal(3, 1)")
                    .HasColumnName("grammar_accuracy");
                entity.Property(e => e.GrammarVocabJson).HasColumnName("grammar_vocab_json");
                entity.Property(e => e.LexicalResource)
                    .HasColumnType("decimal(3, 1)")
                    .HasColumnName("lexical_resource");
                entity.Property(e => e.Overall)
                    .HasColumnType("decimal(3, 1)")
                    .HasColumnName("overall");
                entity.Property(e => e.TaskAchievement)
                    .HasColumnType("decimal(3, 1)")
                    .HasColumnName("task_achievement");
                entity.Property(e => e.WritingId).HasColumnName("writing_id");
            });
            modelBuilder.Entity<VipPlan>(entity =>
            {
                entity.ToTable("VipPlans");

                entity.HasKey(p => p.VipPlanId)
                      .HasName("PK_VipPlans");

                entity.Property(p => p.VipPlanId)
                      .HasColumnName("plan_id");

                entity.Property(p => p.PlanName)
                      .HasColumnName("plan_name")
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(p => p.DurationDays)
                      .HasColumnName("duration_days")
                      .IsRequired();

                entity.Property(p => p.Price)
                      .HasColumnName("price")
                      .HasColumnType("decimal(10,2)")
                      .IsRequired();

                entity.Property(p => p.Description)
                      .HasColumnName("description")
                      .HasMaxLength(255);

                entity.Property(p => p.CreatedAt)
                      .HasColumnName("created_at")
                      .HasDefaultValueSql("SYSDATETIME()");
            });
            modelBuilder.Entity<Speaking>(entity =>
            {
                entity.HasKey(e => e.SpeakingId).HasName("PK__Speaking__598C697384A5D6E1");

                entity.ToTable("Speaking");

                entity.Property(e => e.SpeakingId).HasColumnName("speaking_id");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
                entity.Property(e => e.ExamId).HasColumnName("exam_id");
                entity.Property(e => e.SpeakingQuestion).HasColumnName("speaking_question");
                entity.Property(e => e.SpeakingType)
                    .HasMaxLength(50)
                    .HasColumnName("speaking_type");
            });
            modelBuilder.Entity<SpeakingAttempt>(entity =>
            {
                entity.HasKey(e => e.SpeakingAttemptId)
                    .HasName("PK__SpeakingAttempt");

                entity.ToTable("SpeakingAttempt");

                entity.Property(e => e.SpeakingAttemptId)
                    .HasColumnName("speaking_attempt_id");

                entity.Property(e => e.AttemptId)
                    .HasColumnName("attempt_id");

                entity.Property(e => e.SpeakingId)
                    .HasColumnName("speaking_id");

                entity.Property(e => e.AudioUrl)
                    .HasColumnName("audio_url")
                    .HasMaxLength(500);

                entity.Property(e => e.Transcript)
                    .HasColumnName("transcript");

                entity.Property(e => e.StartedAt)
                    .HasColumnName("started_at")
                    .HasPrecision(0)
                    .HasDefaultValueSql("SYSDATETIME()");

                entity.Property(e => e.SubmittedAt)
                    .HasColumnName("submitted_at")
                    .HasPrecision(0);

                // Foreign Keys
                entity.HasOne(e => e.ExamAttempt)
                    .WithMany()
                    .HasForeignKey(e => e.AttemptId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_SpeakingAttempt_ExamAttempt");

                entity.HasOne(e => e.Speaking)
                    .WithMany()
                    .HasForeignKey(e => e.SpeakingId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_SpeakingAttempt_Speaking");
            });

            modelBuilder.Entity<SpeakingFeedback>(entity =>
            {
                entity.HasKey(e => e.FeedbackId).HasName("PK__Speaking__7A6B2B8CED144F63");

                entity.ToTable("SpeakingFeedback");

                entity.Property(e => e.FeedbackId).HasColumnName("feedback_id");
                entity.Property(e => e.AiAnalysisJson).HasColumnName("ai_analysis_json");
                entity.Property(e => e.Coherence)
                    .HasColumnType("decimal(3, 1)")
                    .HasColumnName("coherence");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.Fluency)
                    .HasColumnType("decimal(3, 1)")
                    .HasColumnName("fluency");
                entity.Property(e => e.GrammarAccuracy)
                    .HasColumnType("decimal(3, 1)")
                    .HasColumnName("grammar_accuracy");
                entity.Property(e => e.LexicalResource)
                    .HasColumnType("decimal(3, 1)")
                    .HasColumnName("lexical_resource");
                entity.Property(e => e.Overall)
                    .HasColumnType("decimal(3, 1)")
                    .HasColumnName("overall");
                entity.Property(e => e.Pronunciation)
                    .HasColumnType("decimal(3, 1)")
                    .HasColumnName("pronunciation");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}