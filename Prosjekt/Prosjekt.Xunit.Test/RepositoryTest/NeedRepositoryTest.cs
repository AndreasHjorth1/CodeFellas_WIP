using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Prosjekt.DataAccess;
using Prosjekt.DataAccess.Repositories;
using Prosjekt.Models.Entities;
using Prosjekt.Models.Enums;
using Xunit;
using Microsoft.AspNetCore.Http;
using Prosjekt.Services;

namespace Prosjekt.Xunit
{
    public class NeedRepositoryTest
    {
        [Fact]
        public async Task GetAllAsync_ReturnsAllNeeds()
        {
            using var context = CreateContext();
            AddNeed(context, "Vann");
            AddNeed(context, "Mat");
            var auditLogService = new AuditLogService(
    context,
    new HttpContextAccessor());

            var repository = new NeedRepository(context, auditLogService);

            var result = await repository.GetAllAsync();

            Assert.Equal(2, result.Count());
            Assert.Contains(result, n => n.Name == "Vann");
            Assert.Contains(result, n => n.Name == "Mat");
        }

        [Fact]
        public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
        {
            using var context = CreateContext();
            var auditLogService = new AuditLogService(
    context,
    new HttpContextAccessor());

            var repository = new NeedRepository(context, auditLogService);

            var result = await repository.GetAllAsync();

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByIdAsync_WhenNeedExists_ReturnsNeed()
        {
            using var context = CreateContext();
            var need = AddNeed(context);
            var auditLogService = new AuditLogService(
    context,
    new HttpContextAccessor());

            var repository = new NeedRepository(context, auditLogService);

            var result = await repository.GetByIdAsync(need.Id);

            Assert.NotNull(result);
            Assert.Equal(need.Id, result.Id);
            Assert.Equal(need.Name, result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WhenNeedDoesNotExist_ReturnsNull()
        {
            using var context = CreateContext();
            var auditLogService = new AuditLogService(
    context,
    new HttpContextAccessor());

            var repository = new NeedRepository(context, auditLogService);

            var result = await repository.GetByIdAsync(404);

            Assert.Null(result);
        }

        [Fact]
        public async Task AddAsync_SavesNeed()
        {
            using var context = CreateContext();
            var auditLogService = new AuditLogService(
    context,
    new HttpContextAccessor());

            var repository = new NeedRepository(context, auditLogService); ;
            var need = CreateNeed("Strøm");

            await repository.AddAsync(need);

            var saved = Assert.Single(context.Needs);
            Assert.NotEqual(0, saved.Id);
            Assert.Equal("Strøm", saved.Name);
            Assert.Equal(58.1467, saved.Coords.Latitude);
            Assert.Equal(7.9956, saved.Coords.Longitude);
        }

        [Fact]
        public async Task UpdateAsync_SavesChanges()
        {
            using var context = CreateContext();
            var need = AddNeed(context);
            var auditLogService = new AuditLogService(
    context,
    new HttpContextAccessor());

            var repository = new NeedRepository(context, auditLogService);

            need.Name = "Oppdatert navn";
            need.Status = EntryStatus.Assigned;
            await repository.UpdateAsync(need);

            var updated = context.Needs.Find(need.Id);
            Assert.NotNull(updated);
            Assert.Equal("Oppdatert navn", updated.Name);
            Assert.Equal(EntryStatus.Assigned, updated.Status);
            var log = Assert.Single(context.AuditLogs);
            Assert.Equal("Update", log.Action);
            Assert.Equal("Need", log.EntityType);
            Assert.Equal(need.Id, log.EntityId);
            Assert.Equal("Status", log.FieldName);
            Assert.Equal(EntryStatus.New.ToString(), log.OldValue);
            Assert.Equal(EntryStatus.Assigned.ToString(), log.NewValue);
        }

        [Fact]
        public async Task DeleteAsync_WhenNeedExists_RemovesNeed()
        {
            using var context = CreateContext();
            var need = AddNeed(context);
            var auditLogService = new AuditLogService(
    context,
    new HttpContextAccessor());

            var repository = new NeedRepository(context, auditLogService); ;

            await repository.DeleteAsync(need.Id);

            Assert.Empty(context.Needs);
        }

        [Fact]
        public async Task DeleteAsync_WhenNeedDoesNotExist_DoesNothing()
        {
            using var context = CreateContext();
            AddNeed(context);
            var auditLogService = new AuditLogService(
    context,
    new HttpContextAccessor());

            var repository = new NeedRepository(context, auditLogService);

            await repository.DeleteAsync(404);

            Assert.Single(context.Needs);
        }

        private static ApplicationDbContext CreateContext()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private static Need CreateNeed(string name = "Vann")
        {
            return new Need
            {
                Name = name,
                Description = "Trenger vann til 20 personer",
                Category = "Water",
                PhoneNumber = "12345678",
                Priority = "Critical",
                Coords = new Coordinates(58.1467, 7.9956)
            };
        }

        private static Need AddNeed(ApplicationDbContext context, string name = "Vann")
        {
            var need = CreateNeed(name);
            context.Needs.Add(need);
            context.SaveChanges();
            return need;
        }
    }
}