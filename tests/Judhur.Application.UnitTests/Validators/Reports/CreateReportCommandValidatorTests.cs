using FluentValidation.TestHelper;

using Judhur.Application.Features.Reports.Commands.CreateReport;
using Judhur.Domain.Reports;
using Judhur.Domain.Reports.Enums;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Reports;

public sealed class CreateReportCommandValidatorTests
{
    private static readonly CreateReportCommand Valid = new(Guid.NewGuid(), ReportReason.Fraud, "السعر غير حقيقي");

    private readonly CreateReportCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(Valid).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(ReportReason.Fraud)]
    [InlineData(ReportReason.IncorrectInformation)]
    [InlineData(ReportReason.Duplicate)]
    [InlineData(ReportReason.InappropriateContent)]
    public void Validate_NonOtherReasonWithoutDetails_HasNoErrors(ReportReason reason)
    {
        _validator.TestValidate(Valid with { Reason = reason, Details = null }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyPropertyId_ReturnsError()
    {
        _validator.TestValidate(Valid with { PropertyId = Guid.Empty })
            .ShouldHaveValidationErrorFor(c => c.PropertyId)
            .WithErrorMessage("معرّف العقار مطلوب.");
    }

    [Fact]
    public void Validate_UndefinedReason_ReturnsError()
    {
        _validator.TestValidate(Valid with { Reason = (ReportReason)99 })
            .ShouldHaveValidationErrorFor(c => c.Reason)
            .WithErrorMessage("سبب البلاغ غير صالح.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_OtherReasonWithoutDetails_ReturnsError(string? details)
    {
        _validator.TestValidate(Valid with { Reason = ReportReason.Other, Details = details })
            .ShouldHaveValidationErrorFor(c => c.Details)
            .WithErrorMessage("يجب توضيح سبب البلاغ عند اختيار (أخرى).");
    }

    [Fact]
    public void Validate_OtherReasonWithDetails_HasNoErrors()
    {
        _validator.TestValidate(Valid with { Reason = ReportReason.Other, Details = "صور مسروقة" }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_DetailsAtMaxLengthAfterTrim_HasNoError()
    {
        _validator.TestValidate(Valid with { Details = $"  {Text(Report.MaxDetailsLength)}  " })
            .ShouldNotHaveValidationErrorFor(c => c.Details);
    }

    [Fact]
    public void Validate_DetailsOverMaxLength_ReturnsError()
    {
        _validator.TestValidate(Valid with { Details = Text(Report.MaxDetailsLength + 1) })
            .ShouldHaveValidationErrorFor(c => c.Details)
            .WithErrorMessage($"لا يمكن أن تتجاوز تفاصيل البلاغ {Report.MaxDetailsLength} حرف.");
    }
}
