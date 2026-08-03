using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;

namespace QUIZAPP.Areas.Admin
{
    public class RequiredIfAttribute : ValidationAttribute
    {
        private readonly string _conditionalPropertyName;

        public RequiredIfAttribute(string conditionalPropertyName)
        {
            _conditionalPropertyName = conditionalPropertyName;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var conditionalProperty = validationContext.ObjectType.GetProperty(_conditionalPropertyName);
            if (conditionalProperty == null)
            {
                throw new ArgumentException("Property with this name not found");
            }

            var conditionalPropertyValue = conditionalProperty.GetValue(validationContext.ObjectInstance);

            if (conditionalPropertyValue != null && (bool)conditionalPropertyValue)
            {
                if (value == null || string.IsNullOrEmpty(value.ToString()))
                {
                    return new ValidationResult(ErrorMessage ?? "Field is required.");
                }
            }

            return ValidationResult.Success;
        }
    }
}





