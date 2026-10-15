using library.management.domain.Models.Entities;
using library.management.domain.Models.ViewModels.LoanVM;

namespace library.management.application.Features.LoanFeatures;

internal static class LoanMapping
{
    internal static LoanVM ToViewModel(this BookLoan loan) => new()
    {
        Id = loan.Id,
        BookId = loan.BookId,
        MemberId = loan.MemberId,
        CheckoutOfficerUserId = loan.CheckoutOfficerUserId,
        ReturnOfficerUserId = loan.ReturnOfficerUserId,
        BorrowedAtUtc = loan.BorrowedAtUtc,
        ReturnedAtUtc = loan.ReturnedAtUtc
    };
}
