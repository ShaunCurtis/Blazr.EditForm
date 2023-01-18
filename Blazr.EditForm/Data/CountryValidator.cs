using FluentValidation;
using System;

namespace Blazr.EditForm.Data;

public class CountryValidator : AbstractValidator<CountryEditContext>
{
        public CountryValidator()
        {
                RuleFor(p => p.Name)
                .NotEmpty().WithMessage("You must enter a Name")
                .MaximumLength(50).WithMessage("Name cannot be longer than 50 characters");

                RuleFor(p => p.Code)
                .NotEmpty().WithMessage("You must enter a Code for the Country")
                .MaximumLength(4).WithMessage("A country code is 1, 2, 3 or 4 letters");
        }

}
