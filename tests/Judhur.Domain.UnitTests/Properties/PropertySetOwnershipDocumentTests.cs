using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;
using Judhur.Tests.Common;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Properties;

public sealed class PropertySetOwnershipDocumentTests
{
    private const string NewDocument = "judhur/documents/new-ownership-document";

    [Fact]
    public void SetOwnershipDocument_ValidPublicId_StoresIt()
    {
        var property = PropertyFactory.Create();

        var result = property.SetOwnershipDocument(NewDocument);

        Assert.True(result.IsSuccess);
        Assert.Equal(NewDocument, property.OwnershipDocumentPublicId);
    }

    [Fact]
    public void SetOwnershipDocument_ExistingDocument_ReplacesIt()
    {
        var property = PropertyFactory.CreateReadyForReview();

        property.SetOwnershipDocument(NewDocument);

        Assert.Equal(NewDocument, property.OwnershipDocumentPublicId);
    }

    [Fact]
    public void SetOwnershipDocument_PublicIdAtLimit_ReturnsSuccess()
    {
        var property = PropertyFactory.Create();

        var result = property.SetOwnershipDocument(new string('d', Property.MaxOwnershipDocumentPublicIdLength));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void SetOwnershipDocument_PublicIdOverLimit_ReturnsOwnershipDocumentPublicIdTooLong()
    {
        var property = PropertyFactory.Create();

        var result = property.SetOwnershipDocument(new string('d', Property.MaxOwnershipDocumentPublicIdLength + 1));

        Assert.Equal(PropertyErrors.OwnershipDocumentPublicIdTooLong.Code, result.TopError.Code);
        Assert.Null(property.OwnershipDocumentPublicId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void SetOwnershipDocument_BlankPublicId_ReturnsOwnershipDocumentRequired(string publicId)
    {
        var property = PropertyFactory.Create();

        var result = property.SetOwnershipDocument(publicId);

        Assert.Equal(PropertyErrors.OwnershipDocumentRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void SetOwnershipDocument_NullPublicId_ReturnsOwnershipDocumentRequired()
    {
        var property = PropertyFactory.Create();

        var result = property.SetOwnershipDocument(null!);

        Assert.Equal(PropertyErrors.OwnershipDocumentRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void SetOwnershipDocument_BlankPublicId_KeepsExistingDocument()
    {
        var property = PropertyFactory.CreateReadyForReview();

        property.SetOwnershipDocument(" ");

        Assert.Equal(PropertyFactory.DefaultOwnershipDocumentPublicId, property.OwnershipDocumentPublicId);
    }

    [Fact]
    public void SetOwnershipDocument_Approved_ReturnsToPendingAndClearsReviewData()
    {
        var property = PropertyFactory.CreateApproved();

        property.SetOwnershipDocument(NewDocument);

        Assert.Equal(ModerationStatus.Pending, property.ModerationStatus);
        Assert.Null(property.ReviewedBy);
        Assert.Null(property.ReviewedAtUtc);
    }

    [Fact]
    public void SetOwnershipDocument_Rejected_ReturnsToPendingAndClearsReason()
    {
        var property = PropertyFactory.CreateRejected();

        property.SetOwnershipDocument(NewDocument);

        Assert.Equal(ModerationStatus.Pending, property.ModerationStatus);
        Assert.Null(property.RejectionReason);
        Assert.Null(property.ReviewedBy);
        Assert.Null(property.ReviewedAtUtc);
    }

    [Fact]
    public void SetOwnershipDocument_InvalidOnApproved_StaysApproved()
    {
        var property = PropertyFactory.CreateApproved();

        property.SetOwnershipDocument(new string('d', Property.MaxOwnershipDocumentPublicIdLength + 1));

        Assert.Equal(ModerationStatus.Approved, property.ModerationStatus);
        Assert.Equal(PropertyFactory.DefaultOwnershipDocumentPublicId, property.OwnershipDocumentPublicId);
    }

    [Fact]
    public void SetOwnershipDocument_Approved_CanBeApprovedAgain()
    {
        var property = PropertyFactory.CreateApproved();
        property.SetOwnershipDocument(NewDocument).ThrowIfError();

        var result = property.Approve(PropertyFactory.DefaultReviewerId, TestClock.UtcNow.AddDays(1));

        Assert.True(result.IsSuccess);
    }
}
