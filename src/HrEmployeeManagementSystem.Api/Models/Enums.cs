namespace HrEmployeeManagementSystem.Api.Models;

public enum EmploymentStatus
{
    Active,
    Probation,
    OnLeave,
    Resigned,
    Terminated
}

public enum LeaveStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Hr = "HR";
    public const string Manager = "Manager";
    public const string Employee = "Employee";

    public const string HrStaff = $"{Admin},{Hr}";
    public const string Approvers = $"{Admin},{Hr},{Manager}";
    public const string ReportReaders = $"{Admin},{Hr},{Manager}";

    public static readonly string[] All = [Admin, Hr, Manager, Employee];
}
