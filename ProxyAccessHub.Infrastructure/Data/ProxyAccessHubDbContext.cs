using Microsoft.EntityFrameworkCore;
using ProxyAccessHub.Infrastructure.Data.Entities;

namespace ProxyAccessHub.Infrastructure.Data;

/// <summary>
/// Основной контекст базы данных приложения.
/// </summary>
public class ProxyAccessHubDbContext(DbContextOptions<ProxyAccessHubDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Серверы пользователей.
    /// </summary>
    public DbSet<ProxyServerEntity> Servers => Set<ProxyServerEntity>();

    /// <summary>
    /// Тарифы пользователей.
    /// </summary>
    public DbSet<TariffDefinitionEntity> Tariffs => Set<TariffDefinitionEntity>();

    /// <summary>
    /// Пользователи.
    /// </summary>
    public DbSet<ProxyUserEntity> Users => Set<ProxyUserEntity>();

    /// <summary>
    /// Заявки на оплату.
    /// </summary>
    public DbSet<PaymentRequestEntity> PaymentRequests => Set<PaymentRequestEntity>();

    /// <summary>
    /// Платежи.
    /// </summary>
    public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();

    /// <summary>
    /// Подписки пользователей.
    /// </summary>
    public DbSet<SubscriptionEntity> Subscriptions => Set<SubscriptionEntity>();

    /// <summary>
    /// История назначений тарифов пользователей.
    /// </summary>
    public DbSet<UserTariffAssignmentEntity> UserTariffAssignments => Set<UserTariffAssignmentEntity>();

    /// <summary>
    /// Настраивает схему базы данных приложения.
    /// </summary>
    /// <param name="modelBuilder">Построитель модели.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProxyServerEntity>(builder =>
        {
            builder.ToTable("Servers");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Code).HasMaxLength(64).IsRequired();
            builder.Property(entity => entity.Name).HasMaxLength(256).IsRequired();
            builder.Property(entity => entity.Host).HasMaxLength(256).IsRequired();
            builder.Property(entity => entity.ApiPort).IsRequired();
            builder.Property(entity => entity.ApiBearerToken).HasMaxLength(1024).IsRequired();
            builder.Property(entity => entity.SyncIntervalMinutes).IsRequired();
            builder.Property(entity => entity.LastDailyRenewalProcessedDateUtc);
            builder.HasIndex(entity => entity.Code).IsUnique();
        });

        modelBuilder.Entity<TariffDefinitionEntity>(builder =>
        {
            builder.ToTable("Tariffs");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Name).HasMaxLength(256).IsRequired();
            builder.Property(entity => entity.PeriodPriceRub).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ProxyUserEntity>(builder =>
        {
            builder.ToTable("Users");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.TelemtUserId).HasMaxLength(128).IsRequired();
            builder.Property(entity => entity.ProxyLink).HasMaxLength(2048).IsRequired();
            builder.Property(entity => entity.ProxyLinkLookupKey).HasMaxLength(512).IsRequired();
            builder.Property(entity => entity.CustomPeriodPriceRub).HasPrecision(18, 2);
            builder.Property(entity => entity.DiscountPercent).HasPrecision(5, 2);
            builder.Property(entity => entity.BalanceRub).HasPrecision(18, 2);
            builder.Property(entity => entity.IsTelemtAccessActive).IsRequired();
            builder.Property(entity => entity.ManualHandlingReason).HasMaxLength(1024);
            builder.Property(entity => entity.UserAdTag).HasMaxLength(64);
            builder.Property(entity => entity.TelemtRevision).HasMaxLength(128).IsRequired();
            builder.HasIndex(entity => entity.TelemtUserId).IsUnique();
            builder.HasIndex(entity => entity.ProxyLinkLookupKey).IsUnique();
        });

        modelBuilder.Entity<PaymentRequestEntity>(builder =>
        {
            builder.ToTable("PaymentRequests");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Label).HasMaxLength(128).IsRequired();
            builder.Property(entity => entity.AmountRub).HasPrecision(18, 2);
            builder.HasIndex(entity => entity.Label).IsUnique();
        });

        modelBuilder.Entity<PaymentEntity>(builder =>
        {
            builder.ToTable("Payments");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.ProviderOperationId).HasMaxLength(128).IsRequired();
            builder.Property(entity => entity.AmountRub).HasPrecision(18, 2);
            builder.Property(entity => entity.ActualAmountRub).HasPrecision(18, 2);
            builder.HasIndex(entity => entity.ProviderOperationId).IsUnique();
        });

        modelBuilder.Entity<SubscriptionEntity>(builder =>
        {
            builder.ToTable("Subscriptions");
            builder.HasKey(entity => entity.Id);
            builder.HasIndex(entity => entity.UserId).IsUnique();
        });

        modelBuilder.Entity<UserTariffAssignmentEntity>(builder =>
        {
            builder.ToTable("UserTariffAssignments");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Comment).HasMaxLength(1024);
            builder.Property(entity => entity.AssignedBy).HasMaxLength(256);
            builder.HasIndex(entity => new { entity.UserId, entity.EndedAtUtc })
                .HasFilter("[EndedAtUtc] IS NULL")
                .IsUnique();
            builder.HasIndex(entity => new { entity.IsTrial, entity.EndedAtUtc });
            builder.HasIndex(entity => entity.UserId);
        });
    }
}
