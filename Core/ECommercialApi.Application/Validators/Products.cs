
using ECommercialApi.Application.ViewModels;
using ECommercialApi.Domain.Entities.Common;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommercialApi.Application.Validators
{
    public class ProductInputModelValidator : AbstractValidator<ProductInputModel>
    {
        public ProductInputModelValidator()
        {
            RuleFor(x => x.Name).NotEmpty().NotNull().WithMessage("Product name is required.")
            .MinimumLength(2).WithMessage("Product name must be at least 2 characters long.")
            .MaximumLength(100).WithMessage("Product name cannot exceed 100 characters.");
            RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than zero.");
            RuleFor(x => x.Stock).GreaterThanOrEqualTo(0).WithMessage("Stock cannot be negative.");
        }
    }
}