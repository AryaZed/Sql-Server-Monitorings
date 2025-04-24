namespace Sql_Server_Monitoring.Domain.Models
{
    /// <summary>
    /// Type of data sensitivity for database columns
    /// </summary>
    public enum SensitivityType
    {
        None,
        PersonalIdentification,
        FinancialInformation,
        HealthInformation,
        Credentials,
        Classification,
        BusinessConfidential,
        Password,
        SSN,
        CreditCard,
        Email,
        PhoneNumber,
        Address,
        DateOfBirth,
        PassportNumber,
        DriversLicense,
        SecurityToken,
        Other
    }
}
