using FluentValidation;

public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(u => u.FirstName).NotEmpty().MaximumLength(128);
        RuleFor(u => u.LastName).MaximumLength(128);
        RuleFor(u => u.Email).NotEmpty().EmailAddress();
        RuleFor(u => u.DateOfBirth)
            .NotEmpty()
            .Must(BeAtLeast18YearsOld).WithMessage("User must be 18 years or older.");
        RuleFor(u => u.PhoneNumber).Matches(@"^\d{10}$").WithMessage("Phone number must be 10 digits.");
    }

    private bool BeAtLeast18YearsOld(DateTime dateOfBirth)
    {
        return DateTime.Now.Year - dateOfBirth.Year >= 18;
    }
}
