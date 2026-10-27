using HrEmployeeManagementSystem.Api.Dtos;
using HrEmployeeManagementSystem.Api.Models;

namespace HrEmployeeManagementSystem.Api.Services;

public static class Mappers
{
    public static EmployeeResponse ToEmployee(Employee employee) => new(
        employee.Id,
        employee.EmployeeCode,
        employee.FirstName,
        employee.LastName,
        employee.FullName,
        employee.Email,
        employee.Phone,
        employee.DateOfBirth,
        employee.HireDate,
        employee.Status.ToString(),
        employee.DepartmentId,
        employee.Department?.Name ?? "",
        employee.Department?.Code ?? "",
        employee.PositionId,
        employee.Position?.Title ?? "",
        employee.ManagerId,
        employee.Manager?.FullName);

    public static LeaveResponse ToLeave(LeaveRequest leave) => new(
        leave.Id,
        leave.EmployeeId,
        leave.Employee.FullName,
        leave.Employee.Department?.Name ?? "",
        leave.LeaveTypeId,
        leave.LeaveType.Name,
        leave.StartDate,
        leave.EndDate,
        DateRules.InclusiveDays(leave.StartDate, leave.EndDate),
        leave.Reason,
        leave.Status.ToString(),
        leave.ReviewComment,
        leave.CreatedAt);

    public static AttendanceResponse ToAttendance(AttendanceRecord record)
    {
        double? hours = record.CheckIn is DateTime checkIn && record.CheckOut is DateTime checkOut
            ? Math.Round((checkOut - checkIn).TotalHours, 2)
            : null;

        return new AttendanceResponse(
            record.Id,
            record.EmployeeId,
            record.Employee.FullName,
            record.Employee.EmployeeCode,
            record.Employee.Department?.Name ?? "",
            record.WorkDate,
            record.CheckIn,
            record.CheckOut,
            hours);
    }
}
