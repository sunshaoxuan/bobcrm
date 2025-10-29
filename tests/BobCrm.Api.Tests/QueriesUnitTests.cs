using System.Security.Claims;
using System.Text.Json;
using BobCrm.Api.Application.Queries;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Domain;
using BobCrm.Api.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace BobCrm.Api.Tests;

public class QueriesUnitTests
{
    private class ListRepo<T> : IRepository<T> where T : class
    {
        public List<T> Data = new();
        public Task AddAsync(T entity, CancellationToken ct = default) { Data.Add(entity); return Task.CompletedTask; }
        public Task<T?> GetByIdAsync(object id, CancellationToken ct = default) => Task.FromResult<T?>(null);
        public IQueryable<T> Query(System.Linq.Expressions.Expression<Func<T, bool>>? predicate = null)
            => predicate is null ? Data.AsQueryable() : Data.AsQueryable().Where(predicate);
        public void Remove(T entity) => Data.Remove(entity);
        public void Update(T entity) { }
    }

    private class FakeLoc : ILocalization
    {
        public string T(string key, string lang) => key;
    }

    private static IHttpContextAccessor HttpWithUser(string id)
    {
        var http = new HttpContextAccessor();
        var ctx = new DefaultHttpContext();
        var idClaim = new Claim(ClaimTypes.NameIdentifier, id);
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { idClaim }, "test"));
        http.HttpContext = ctx;
        return http;
    }

    [Fact]
    public void CustomerQueries_Respects_Access_Control()
    {
        var repoCustomer = new ListRepo<Customer>();
        repoCustomer.Data.AddRange(new[] { new Customer{ Id=1, Code="C001", Name="A", Version=1 }, new Customer{ Id=2, Code="C002", Name="B", Version=1 } });
        var repoDef = new ListRepo<FieldDefinition>();
        repoDef.Data.Add(new FieldDefinition{ Id=1, Key="email", DisplayName="LBL_EMAIL", DataType="email", DefaultValue = "" });
        var repoVal = new ListRepo<FieldValue>();
        repoVal.Data.Add(new FieldValue{ Id=1, CustomerId=1, FieldDefinitionId=1, Value="\"a@b.com\"", Version=2 });
        var repoAccess = new ListRepo<CustomerAccess>();
        repoAccess.Data.Add(new CustomerAccess{ Id=1, CustomerId=1, UserId="u1", CanEdit=true });

        var q = new CustomerQueries(repoCustomer, repoDef, repoVal, repoAccess, HttpWithUser("u1"), new FakeLoc());
        var list = q.GetList();
        Assert.True(list.Count == 1);

        var detailAllowed = q.GetDetail(1);
        Assert.NotNull(detailAllowed);
        var detailDenied = new CustomerQueries(repoCustomer, repoDef, repoVal, repoAccess, HttpWithUser("uX"), new FakeLoc()).GetDetail(1);
        Assert.Null(detailDenied);
    }

    [Fact]
    public void FieldQueries_Parses_Tags_And_Actions()
    {
        var repo = new ListRepo<FieldDefinition>();
        repo.Data.Add(new FieldDefinition{ Key="email", DisplayName="LBL_EMAIL", DataType="email", Tags = "[\"tag1\"]", Actions = "[]" });
        repo.Data.Add(new FieldDefinition{ Key="link", DisplayName="LBL_LINK", DataType="link", Tags = null, Actions = null });
        var q = new FieldQueries(repo, new FakeLoc(), HttpWithUser("u1"));
        var defs = q.GetDefinitions();
        var json = JsonSerializer.Serialize(defs);
        var arr = JsonDocument.Parse(json).RootElement;
        Assert.True(arr.GetArrayLength() == 2);
        Assert.True(arr[0].GetProperty("tags").GetArrayLength() == 1);
        Assert.True(arr[1].GetProperty("tags").GetArrayLength() == 0);
    }

    [Fact]
    public void LayoutQueries_User_Default_Effective()
    {
        var repo = new ListRepo<UserLayout>();
        repo.Data.Add(new UserLayout{ Id=1, UserId="__default__", CustomerId=1, LayoutJson = "{\"mode\":\"flow\"}" });
        repo.Data.Add(new UserLayout{ Id=2, UserId="u1", CustomerId=1, LayoutJson = "{\"mode\":\"free\"}" });
        var q = new LayoutQueries(repo);
        var user = q.GetLayout("u1", 1, "user");
        var def = q.GetLayout("u1", 1, "default");
        var eff = q.GetLayout("u1", 1, "effective");
        var uj = JsonSerializer.Serialize(user);
        var dj = JsonSerializer.Serialize(def);
        var ej = JsonSerializer.Serialize(eff);
        Assert.Contains("free", uj);
        Assert.Contains("flow", dj);
        Assert.Contains("free", ej);
    }
}

