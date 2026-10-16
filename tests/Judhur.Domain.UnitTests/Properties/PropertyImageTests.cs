using Judhur.Domain.Properties.PropertyImages;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Properties;

public sealed class PropertyImageTests
{
    private const string ValidUrl = "https://res.cloudinary.com/judhur/image/upload/properties/a.jpg";
    private const string ValidPublicId = "judhur/properties/a";

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_BlankFileUrl_ReturnsFileUrlRequired(string fileUrl)
    {
        var result = PropertyFactory.Create().AddImage(Guid.NewGuid(), fileUrl, ValidPublicId, false);

        Assert.Equal(PropertyImageErrors.FileUrlRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_NullFileUrl_ReturnsFileUrlRequired()
    {
        var result = PropertyFactory.Create().AddImage(Guid.NewGuid(), null!, ValidPublicId, false);

        Assert.Equal(PropertyImageErrors.FileUrlRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_FileUrlAtLimit_ReturnsSuccess()
    {
        var result = PropertyFactory.Create().AddImage(Guid.NewGuid(), new string('u', PropertyImage.MaxFileUrlLength), ValidPublicId, false);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_FileUrlOverLimit_ReturnsFileUrlTooLong()
    {
        var result = PropertyFactory.Create().AddImage(Guid.NewGuid(), new string('u', PropertyImage.MaxFileUrlLength + 1), ValidPublicId, false);

        Assert.Equal(PropertyImageErrors.FileUrlTooLong.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_BlankPublicId_ReturnsPublicIdRequired(string publicId)
    {
        var result = PropertyFactory.Create().AddImage(Guid.NewGuid(), ValidUrl, publicId, false);

        Assert.Equal(PropertyImageErrors.PublicIdRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_PublicIdAtLimit_ReturnsSuccess()
    {
        var result = PropertyFactory.Create().AddImage(Guid.NewGuid(), ValidUrl, new string('p', PropertyImage.MaxPublicIdLength), false);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_PublicIdOverLimit_ReturnsPublicIdTooLong()
    {
        var result = PropertyFactory.Create().AddImage(Guid.NewGuid(), ValidUrl, new string('p', PropertyImage.MaxPublicIdLength + 1), false);

        Assert.Equal(PropertyImageErrors.PublicIdTooLong.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_BlankFileUrlAndPublicId_ReturnsFileUrlRequiredFirst()
    {
        var result = PropertyFactory.Create().AddImage(Guid.NewGuid(), "", "", false);

        Assert.Equal(PropertyImageErrors.FileUrlRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_ValidImage_BelongsToProperty()
    {
        var property = PropertyFactory.Create();

        PropertyFactory.AddImage(property);

        Assert.Equal(property.Id, Assert.Single(property.PropertyImages).PropertyId);
    }
}
