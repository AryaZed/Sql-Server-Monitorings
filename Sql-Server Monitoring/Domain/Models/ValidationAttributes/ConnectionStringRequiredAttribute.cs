using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;

namespace Sql_Server_Monitoring.Domain.Models.ValidationAttributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public class ConnectionStringRequiredAttribute : ValidationAttribute
    {
        public ConnectionStringRequiredAttribute() 
            : base("The {0} field is required and must be a valid SQL Server connection string.")
        {
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return new ValidationResult($"The {validationContext.DisplayName} field is required.");
            }

            if (value is not string connectionString || string.IsNullOrWhiteSpace(connectionString))
            {
                return new ValidationResult($"The {validationContext.DisplayName} field must be a non-empty string.");
            }

            try
            {
                // Attempt to parse the connection string to verify it's valid
                var builder = new SqlConnectionStringBuilder(connectionString);
                
                // Check for required properties
                if (string.IsNullOrWhiteSpace(builder.DataSource))
                {
                    return new ValidationResult($"The {validationContext.DisplayName} must contain a valid Data Source or Server.");
                }

                return ValidationResult.Success;
            }
            catch (Exception)
            {
                return new ValidationResult($"The {validationContext.DisplayName} is not a valid SQL Server connection string.");
            }
        }
    }
} 