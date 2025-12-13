using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.Responses.Entity;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace BobCrm.Api.Tests;

public class FieldMetadataCacheTests
{
    [Fact]
    public async Task GetFieldsAsync_CachesResults_ByFullTypeNameAndLang()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"FieldMetadataCacheTests_{Guid.NewGuid():N}")
            .Options;

        await using var db = new AppDbContext(options);

        var fullTypeName = "BobCrm.Dynamic.TestEntity";
        var definition = new EntityDefinition
        {
            FullTypeName = fullTypeName,
            Namespace = "BobCrm.Dynamic",
            EntityName = "TestEntity",
            EntityRoute = "test-entities",
            ApiEndpoint = "/api/test-entities",
            Status = EntityStatus.Published,
            Source = EntitySource.Custom,
            IsEnabled = true
        };

        definition.Fields.Add(new FieldMetadata
        {
            EntityDefinitionId = definition.Id,
            PropertyName = "Name",
            DisplayNameKey = "LBL_FIELD_NAME",
            DisplayName = null,
            DataType = FieldDataType.String,
            IsRequired = true,
            SortOrder = 1,
            Source = FieldSource.Interface
        });

        definition.Fields.Add(new FieldMetadata
        {
            EntityDefinitionId = definition.Id,
            PropertyName = "CustomField",
            DisplayNameKey = null,
            DisplayName = new Dictionary<string, string?>
            {
                ["zh"] = "自定义字段",
                ["en"] = "Custom Field"
            },
            DataType = FieldDataType.String,
            IsRequired = false,
            SortOrder = 2,
            Source = FieldSource.Custom
        });

        db.EntityDefinitions.Add(definition);
        await db.SaveChangesAsync();

        var cache = new CountingMemoryCache();
        var sut = new FieldMetadataCache(db, cache, NullLogger<FieldMetadataCache>.Instance);
        var loc = new TestLocalization();

        var multi1 = await sut.GetFieldsAsync(fullTypeName, loc, lang: null);
        Assert.Equal(2, cache.CreateEntryCount); // key set + data cache entry
        Assert.Contains(multi1, f => f.PropertyName == "Name" && f.DisplayNameKey == "LBL_FIELD_NAME");
        Assert.Contains(multi1, f => f.PropertyName == "CustomField" && f.DisplayNameTranslations != null);

        var multi2 = await sut.GetFieldsAsync(fullTypeName, loc, lang: null);
        Assert.Equal(2, cache.CreateEntryCount);
        Assert.Equal(multi1.Count, multi2.Count);

        var single1 = await sut.GetFieldsAsync(fullTypeName, loc, lang: "zh");
        Assert.Equal(3, cache.CreateEntryCount); // adds per-lang view entry
        Assert.Contains(single1, f => f.PropertyName == "Name" && !string.IsNullOrWhiteSpace(f.DisplayName));
        Assert.Contains(single1, f => f.PropertyName == "CustomField" && f.DisplayName == "自定义字段");

        var single2 = await sut.GetFieldsAsync(fullTypeName, loc, lang: "zh");
        Assert.Equal(3, cache.CreateEntryCount);
        Assert.Equal(single1.Count, single2.Count);

        sut.Invalidate(fullTypeName);

        var multi3 = await sut.GetFieldsAsync(fullTypeName, loc, lang: null);
        Assert.Equal(5, cache.CreateEntryCount); // recreates key set + data entry
        Assert.Equal(multi1.Count, multi3.Count);
    }

    private sealed class TestLocalization : ILocalization
    {
        public string T(string key, string lang)
        {
            if (key == "LBL_FIELD_NAME" && lang == "zh")
            {
                return "名称";
            }

            return key;
        }

        public Dictionary<string, string> GetDictionary(string lang) => new();

        public void InvalidateCache()
        {
        }

        public long GetCacheVersion() => 1;
    }

    private sealed class CountingMemoryCache : IMemoryCache
    {
        private readonly Dictionary<object, object?> _values = new();

        public int CreateEntryCount { get; private set; }

        public ICacheEntry CreateEntry(object key)
        {
            CreateEntryCount++;
            return new CountingCacheEntry(key, value => _values[key] = value);
        }

        public void Dispose()
        {
        }

        public void Remove(object key)
        {
            _values.Remove(key);
        }

        public bool TryGetValue(object key, out object? value)
        {
            return _values.TryGetValue(key, out value);
        }

        private sealed class CountingCacheEntry : ICacheEntry
        {
            private readonly Action<object?> _onDispose;

            public CountingCacheEntry(object key, Action<object?> onDispose)
            {
                Key = key;
                _onDispose = onDispose;
            }

            public object Key { get; }

            public object? Value { get; set; }

            public DateTimeOffset? AbsoluteExpiration { get; set; }

            public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

            public TimeSpan? SlidingExpiration { get; set; }

            public IList<IChangeToken> ExpirationTokens { get; } = new List<IChangeToken>();

            public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; } = new List<PostEvictionCallbackRegistration>();

            public CacheItemPriority Priority { get; set; }

            public long? Size { get; set; }

            public void Dispose()
            {
                _onDispose(Value);
            }
        }
    }
}
