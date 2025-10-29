using BobCrm.Api.Core.DomainCommon;
using BobCrm.Api.Core.DomainCommon.Validation;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace BobCrm.Api.Tests;

public class ValidatorsTests
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

    [Fact]
    public void BusinessValidator_Errors_When_NoFields_Or_EmptyKey()
    {
        var v = new UpdateCustomerBusinessValidator();
        var r1 = v.Validate(new UpdateCustomerDto(new List<FieldDto>(), null)).ToArray();
        Assert.NotEmpty(r1);
        var r2 = v.Validate(new UpdateCustomerDto(new List<FieldDto>{ new("", "x") }, null)).ToArray();
        Assert.Contains(r2, e => e.Field == "key");
    }

    [Fact]
    public async Task PersistenceValidator_UnknownField()
    {
        var repo = new ListRepo<FieldDefinition>();
        repo.Data.Add(new FieldDefinition{ Id=1, Key="email", DisplayName="邮箱", DataType="email"});
        var v = new UpdateCustomerPersistenceValidator(repo);
        var errs = await v.ValidateAsync(new UpdateCustomerDto(new List<FieldDto>{ new("nope", "x") }, null));
        Assert.Contains(errs, e => e.Code == "UnknownField");
    }

    [Fact]
    public void CommonValidator_Required_And_Regex()
    {
        var repo = new ListRepo<FieldDefinition>();
        repo.Data.Add(new FieldDefinition{ Id=1, Key="email", DisplayName="邮箱", DataType="email", Required = true, Validation = @"^[^@\s]+@[^@\s]+\.[^@\s]+$" });
        var v = new UpdateCustomerCommonValidator(repo);
        var miss = v.Validate(new UpdateCustomerDto(new List<FieldDto>(), null)).ToArray();
        Assert.Contains(miss, e => e.Code == "Required");
        var bad = v.Validate(new UpdateCustomerDto(new List<FieldDto>{ new("email", "not-mail") }, null)).ToArray();
        Assert.Contains(bad, e => e.Code == "InvalidFormat");
        // invalid regex pattern should produce InvalidPattern
        repo.Data[0].Validation = "["; // broken regex
        var invalidPattern = v.Validate(new UpdateCustomerDto(new List<FieldDto>{ new("email", "any") }, null)).ToArray();
        Assert.Contains(invalidPattern, e => e.Code == "InvalidPattern");
    }

    [Fact]
    public async Task ValidationPipeline_Stops_On_First_Failure()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBusinessValidator<UpdateCustomerDto>, UpdateCustomerBusinessValidator>();
        services.AddSingleton<ICommonValidator<UpdateCustomerDto>, UpdateCustomerCommonValidator>(_ =>
        {
            var repo = new ListRepo<FieldDefinition>();
            repo.Data.Add(new FieldDefinition{ Id=1, Key="email", DisplayName="邮箱", DataType="email", Required = true });
            return new UpdateCustomerCommonValidator(repo);
        });
        services.AddSingleton<IValidationPipeline, ValidationPipeline>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        var sp = services.BuildServiceProvider();
        var pipe = sp.GetRequiredService<IValidationPipeline>();
        var res = await pipe.ValidateAsync(new UpdateCustomerDto(new List<FieldDto>(), null), new DefaultHttpContext());
        Assert.NotNull(res); // should fail on business validator (no fields)
    }
}
