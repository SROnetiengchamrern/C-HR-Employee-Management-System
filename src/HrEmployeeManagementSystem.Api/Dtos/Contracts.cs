using System.ComponentModel.DataAnnotations;
using HrEmployeeManagementSystem.Api.Models;

namespace HrEmployeeManagementSystem.Api.Dtos;

public record LoginRequest(string Email, string Password);

public record UserProfileDto(string Id, string Email, string FullName, IReadOnlyList<string> Roles, int? EmployeeId);

public record LoginResponse(string Token, DateTime ExpiresAt, UserProfileDto User);

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public record EmployeeResponse(
    int Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string? Phone,
    DateOnly DateOfBirth,
    DateOnly HireDate,
    string Status,
    int DepartmentId,
    string Department,
    string DepartmentCode,
    int PositionId,
    string Position,
    int? ManagerId,
    string? ManagerName);

public record LeaveBalanceDto(int LeaveTypeId, string LeaveType, int Allowance, int UsedDays);

public record EmployeeDetailResponse(EmployeeResponse Employee, IReadOnlyList<LeaveBalanceDto> LeaveBalances);

public class EmployeeWriteRequest
{
    [Required, MaxLength(80)]
    public string FirstName { get; set; } = "";

    [Required, MaxLength(80)]
    public string LastName { get; set; } = "";

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = "";

    [MaxLength(40)]
    public string? Phone { get; set; }

    public DateOnly DateOfBirth { get; set; }
    public DateOnly HireDate { get; set; }

    [Required]
    public string Status { get; set; } = nameof(EmploymentStatus.Active);

    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }

    [Range(1, int.MaxValue)]
    public int PositionId { get; set; }

    public int? ManagerId { get; set; }
    public bool CreateLogin { get; set; }
    public string? Password { get; set; }
}

public class StatusRequest
{
    [Required]
    public string Status { get; set; } = "";
}

public record DepartmentResponse(int Id, string Name, string Code, bool IsActive, int EmployeeCount);

public class DepartmentWriteRequest
{
    [Required, MaxLength(80)]
    public string Name { get; set; } = "";

    [Required, MaxLength(20)]
    public string Code { get; set; } = "";

    public bool IsActive { get; set; } = true;
}

public record PositionResponse(int Id, string Title, int DepartmentId, string Department);

public class PositionWriteRequest
{
    [Required, MaxLength(80)]
    public string Title { get; set; } = "";

    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }
}

public record AttendanceResponse(
    int Id,
    int EmployeeId,
    string EmployeeName,
    string EmployeeCode,
    string Department,
    DateOnly WorkDate,
    DateTime? CheckIn,
    DateTime? CheckOut,
    double? Hours);

public class AttendanceActionRequest
{
    public int? EmployeeId { get; set; }

    [MaxLength(400)]
    public string? Notes { get; set; }
}

public record LeaveTypeDto(int Id, string Name, int DefaultDays);

public record LeaveResponse(
    int Id,
    int EmployeeId,
    string EmployeeName,
    string Department,
    int LeaveTypeId,
    string LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    int Days,
    string Reason,
    string Status,
    string? ReviewComment,
    DateTime CreatedAt);

public class LeaveWriteRequest
{
    public int? EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    [Required, MaxLength(1000)]
    public string Reason { get; set; } = "";
}

public class ReviewLeaveRequest
{
    [MaxLength(1000)]
    public string? Comment { get; set; }
}

public record DepartmentCountDto(int Id, string Name, string Code, int Count);
public record TrainingItemDto(int Id, string Title, DateTime StartsAt, string? Location, bool IsToday);
public record MonthCountDto(int Month, int Count);

public record DashboardResponse(
    int TotalEmployees,
    double HeadcountChangePercent,
    int PresentToday,
    double AttendancePercent,
    int PendingApprovals,
    int OpenPositions,
    IReadOnlyList<DepartmentCountDto> Departments,
    int BirthdayCount,
    IReadOnlyList<string> BirthdayNames,
    int AnniversaryCount,
    IReadOnlyList<string> AnniversaryNames,
    IReadOnlyList<TrainingItemDto> Training,
    IReadOnlyList<int> BirthdayDistribution,
    IReadOnlyList<LeaveResponse> PendingLeaves,
    IReadOnlyList<MonthCountDto> NewHires,
    int HiresYear,
    IReadOnlyList<int> HiresYears);

public record HeadcountReportDto(
    int Total,
    int Active,
    int Probation,
    int OnLeave,
    int Resigned,
    int Terminated,
    IReadOnlyList<DepartmentCountDto> ByDepartment);

public record AttendanceReportDto(
    DateOnly From,
    DateOnly To,
    int Records,
    double AverageHours,
    IReadOnlyList<AttendanceResponse> Rows);

public record LeaveReportDto(
    DateOnly From,
    DateOnly To,
    int Pending,
    int Approved,
    int Rejected,
    int Cancelled,
    int TotalDays,
    IReadOnlyList<LeaveResponse> Rows);

public record NotificationDto(int Id, string Title, string Message, DateTime CreatedAt, bool IsRead);

public record JobOpeningDto(int Id, string Title, int DepartmentId, string Department, int Openings, string Status, string? Description);

public class JobOpeningWriteRequest
{
    [Required, MaxLength(120)]
    public string Title { get; set; } = "";

    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }

    [Range(1, 100)]
    public int Openings { get; set; } = 1;

    [MaxLength(40)]
    public string Status { get; set; } = "Active";

    [MaxLength(1000)]
    public string? Description { get; set; }
}

public class TrainingWriteRequest
{
    [Required, MaxLength(160)]
    public string Title { get; set; } = "";

    public DateTime StartsAt { get; set; }

    [MaxLength(120)]
    public string? Location { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }
}

public record TrainingDto(int Id, string Title, DateTime StartsAt, string? Location, string? Description);
public record TemplateDto(int Id, string Name, string Category, string Body);
public record BenefitDto(int Id, string Name, string Description, string Eligibility);
public record PerformanceDto(int Id, int EmployeeId, string EmployeeName, string Department, int Year, string Period, int Rating, string Summary, string Status);
public record UserListItemDto(string Id, string FullName, string Email, IReadOnlyList<string> Roles, int? EmployeeId, string? EmployeeCode);

public class CreateUserRequest
{
    [Required, MaxLength(120)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, MinLength(8)]
    public string Password { get; set; } = "";

    [Required]
    public string Role { get; set; } = AppRoles.Employee;

    public int? EmployeeId { get; set; }
}
