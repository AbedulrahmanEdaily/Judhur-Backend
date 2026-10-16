using System.Reflection;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties;

namespace Judhur.Domain.UnitTests.Common;

public sealed class ErrorCatalogTests
{
    private static readonly Assembly DomainAssembly = typeof(Property).Assembly;

    private static IEnumerable<FieldInfo> CatalogFields()
        => DomainAssembly.GetTypes()
            .Where(t => t is { IsAbstract: true, IsSealed: true } && t.Name.EndsWith("Errors", StringComparison.Ordinal))
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(f => f.FieldType == typeof(Error));

    public static TheoryData<string, string> CatalogEntries()
    {
        var data = new TheoryData<string, string>();
        foreach (var field in CatalogFields())
        {
            data.Add(field.DeclaringType!.Name, field.Name);
        }

        return data;
    }

    private static Error Read(string catalog, string field)
        => (Error)DomainAssembly.GetTypes()
            .Single(t => t.Name == catalog)
            .GetField(field, BindingFlags.Public | BindingFlags.Static)!
            .GetValue(null)!;

    [Fact]
    public void Catalogs_AreDiscovered()
    {
        var catalogs = CatalogFields().Select(f => f.DeclaringType!.Name).Distinct().ToList();

        Assert.Equal(9, catalogs.Count);
    }

    [Theory]
    [MemberData(nameof(CatalogEntries))]
    public void Error_Code_IsCatalogNameDotFieldName(string catalog, string field)
    {
        var error = Read(catalog, field);

        Assert.Equal($"{catalog}.{field}", error.Code);
    }

    [Theory]
    [MemberData(nameof(CatalogEntries))]
    public void Error_Description_IsNotBlank(string catalog, string field)
    {
        var error = Read(catalog, field);

        Assert.False(string.IsNullOrWhiteSpace(error.Description));
    }

    [Theory]
    [MemberData(nameof(CatalogEntries))]
    public void Error_Field_IsReadOnly(string catalog, string field)
    {
        var info = DomainAssembly.GetTypes().Single(t => t.Name == catalog).GetField(field)!;

        Assert.True(info.IsInitOnly);
    }

    [Fact]
    public void Errors_AllCodesAreUnique()
    {
        var codes = CatalogFields().Select(f => ((Error)f.GetValue(null)!).Code).ToList();

        Assert.Equal(codes.Count, codes.Distinct(StringComparer.Ordinal).Count());
    }
}
