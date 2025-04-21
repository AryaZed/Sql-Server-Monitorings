namespace Sql_Server_Monitoring.Domain.Models
{
    public enum SensitivityType
    {
        Other,
        Password,
        SSN,
        CreditCard,
        Email,
        PhoneNumber,
        Address,
        DateOfBirth,
        PassportNumber,
        DriversLicense,
        SecurityToken
    }
}
