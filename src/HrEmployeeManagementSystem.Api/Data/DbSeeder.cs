using HrEmployeeManagementSystem.Api.Models;
using HrEmployeeManagementSystem.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HrEmployeeManagementSystem.Api.Data;

public static class DbSeeder
{
    private static readonly string[] FirstNames =
    [
        "Amila", "Nimal", "Kasun", "Sanduni", "Dilani", "Ruwan", "Tharindu", "Ishara", "Chamari", "Nuwan",
        "Sahan", "Piyumi", "Hasitha", "Malsha", "Kavindu", "Thisara", "Anjali", "Dinesh", "Fathima", "Gayan",
        "Hiruni", "Lahiru", "Madushi", "Nadeesha", "Osanda", "Praveen", "Rashmi", "Sachini", "Udara", "Yasiru"
    ];

    private static readonly string[] LastNames =
    [
        "Perera", "Fernando", "Silva", "Jayawardena", "Bandara", "Wickramasinghe", "Rathnayake", "Gunasekara",
        "Herath", "Kumara", "Dissanayake", "Alwis", "Senanayake", "Weerasinghe", "Karunaratne", "DeSilva"
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        if (await db.Departments.AnyAsync())
            return;

        var plan = new (string Code, string Name, int Count)[]
        {
            ("Admin", "Administration", 8),
            ("Corporate", "Corporate", 4),
            ("Finance", "Finance", 19),
            ("Health", "Health", 6),
            ("HRM", "Human Resources", 8),
            ("Intern", "Intern", 5),
            ("Legal", "Legal", 7),
            ("Marketing", "Marketing", 11),
            ("Operations", "Operations", 10),
            ("Product", "Product", 9),
            ("Quality", "Quality", 6),
            ("Research", "Research", 13),
            ("Sales", "Sales", 17),
            ("Tech", "Technology", 16),
            ("Warehouse", "Warehouse", 15)
        };

        var departments = plan.Select(item => new Department { Code = item.Code, Name = item.Name, IsActive = true }).ToList();
        db.Departments.AddRange(departments);

        var leaveTypes = new List<LeaveType>
        {
            new() { Name = "Annual", DefaultDays = 14 },
            new() { Name = "Casual", DefaultDays = 7 },
            new() { Name = "Sick", DefaultDays = 7 },
            new() { Name = "Unpaid", DefaultDays = 0 }
        };
        db.LeaveTypes.AddRange(leaveTypes);

        db.DocumentTemplates.AddRange(
            new DocumentTemplate { Name = "Offer letter", Category = "Recruitment", Body = "We are pleased to offer you the position of {{position}} in {{department}}, starting {{startDate}}." },
            new DocumentTemplate { Name = "Leave approval", Category = "Leave", Body = "Your {{leaveType}} request from {{startDate}} to {{endDate}} has been approved." },
            new DocumentTemplate { Name = "Experience certificate", Category = "Separation", Body = "This is to certify that {{employee}} worked with us from {{hireDate}} as {{position}}." },
            new DocumentTemplate { Name = "Warning letter", Category = "Conduct", Body = "This letter records a formal warning regarding {{subject}}. Please speak with your manager if you need clarification." });

        db.BenefitPlans.AddRange(
            new BenefitPlan { Name = "Health insurance", Description = "Inpatient and outpatient cover for the employee and registered dependents.", Eligibility = "Active employees after probation" },
            new BenefitPlan { Name = "EPF and ETF", Description = "Statutory provident and trust fund contributions.", Eligibility = "All employees" },
            new BenefitPlan { Name = "Annual bonus", Description = "Performance bonus reviewed with the yearly appraisal.", Eligibility = "Employees who complete the appraisal cycle" },
            new BenefitPlan { Name = "Transport allowance", Description = "Monthly transport allowance based on work location.", Eligibility = "Employees assigned outside the head office" });

        await db.SaveChangesAsync();

        var positions = new List<Position>();
        foreach (var department in departments)
        {
            foreach (var title in new[] { "Manager", "Specialist", "Associate" })
                positions.Add(new Position { Title = title, DepartmentId = department.Id });
        }
        db.Positions.AddRange(positions);

        db.JobOpenings.AddRange(
            Opening(departments, "Tech", "Software Engineer", 3, "Build and support internal HR products."),
            Opening(departments, "Finance", "Accountant", 2, "Own monthly close and payroll checks."),
            Opening(departments, "Sales", "Sales Executive", 2, "Grow the mid-market pipeline."),
            Opening(departments, "HRM", "HR Officer", 1, "Support recruitment and employee relations."),
            Opening(departments, "Warehouse", "Warehouse Associate", 2, "Inbound, picking, and stock counts."),
            Opening(departments, "Health", "Nurse", 2, "Staff the on-site clinic."));

        var today = DateOnly.FromDateTime(DateTime.Today);
        db.TrainingSessions.AddRange(
            new TrainingSession { Title = "Leadership workshop", StartsAt = DateTime.Today.AddHours(14), Location = "Colombo HQ", Description = "Leadership workshop for people managers." },
            new TrainingSession { Title = "Safety induction", StartsAt = DateTime.Today.AddDays(7).AddHours(10), Location = "Warehouse floor", Description = "Required induction for new warehouse and operations staff." },
            new TrainingSession { Title = "Payroll clinic", StartsAt = DateTime.Today.AddDays(-20).AddHours(11), Location = "Finance room", Description = "Open clinic for payslip questions." });

        await db.SaveChangesAsync();

        var wave = BuildWave();
        var employees = new List<Employee>();
        var number = 0;
        foreach (var spec in plan)
        {
            var department = departments.First(d => d.Code == spec.Code);
            var managerPosition = positions.First(p => p.DepartmentId == department.Id && p.Title == "Manager");
            var specialist = positions.First(p => p.DepartmentId == department.Id && p.Title == "Specialist");
            var associate = positions.First(p => p.DepartmentId == department.Id && p.Title == "Associate");
            Employee? manager = null;
            for (var index = 0; index < spec.Count; index++)
            {
                number++;
                var isAmila = spec.Code == "HRM" && index == 0;
                var first = isAmila ? "Amila" : FirstNames[number % FirstNames.Length];
                var last = isAmila ? "Perera" : LastNames[(number * 3) % LastNames.Length];
                var employee = new Employee
                {
                    EmployeeCode = $"EMP-{number:0000}",
                    FirstName = first,
                    LastName = last,
                    Email = isAmila ? "amila@corehr.lk" : $"{first}.{last}.{number}@corehr.lk".ToLowerInvariant(),
                    Phone = $"+94 77 {(number * 137 % 900) + 100} {(number * 91 % 9000) + 1000}",
                    DateOfBirth = BirthdayFor(number, today),
                    HireDate = HireFor(number, today, wave),
                    Status = number >= 151 ? EmploymentStatus.Probation : number % 47 == 0 ? EmploymentStatus.OnLeave : EmploymentStatus.Active,
                    DepartmentId = department.Id,
                    PositionId = (index == 0 ? managerPosition : index % 2 == 0 ? specialist : associate).Id,
                    Manager = index == 0 ? null : manager
                };
                if (index == 0)
                    manager = employee;
                employees.Add(employee);
            }
        }

        if (number != 154)
            throw new InvalidOperationException($"Seed expected 154 employees and created {number}.");

        db.Employees.AddRange(employees);
        await db.SaveChangesAsync();

        var hrDepartment = departments.First(d => d.Code == "HRM");
        var techDepartment = departments.First(d => d.Code == "Tech");
        var amila = employees.First(e => e.Email == "amila@corehr.lk");
        var hrPerson = employees.First(e => e.DepartmentId == hrDepartment.Id && e.Id != amila.Id);
        hrPerson.FirstName = "Nimali";
        hrPerson.LastName = "Fernando";
        hrPerson.Email = "hr@corehr.lk";
        var techManager = employees.First(e => e.DepartmentId == techDepartment.Id && e.ManagerId == null);
        techManager.FirstName = "Ruwan";
        techManager.LastName = "Bandara";
        techManager.Email = "manager@corehr.lk";
        var staff = employees.First(e => e.DepartmentId == techDepartment.Id && e.ManagerId != null);
        staff.FirstName = "Kasun";
        staff.LastName = "Silva";
        staff.Email = "employee@corehr.lk";
        await db.SaveChangesAsync();

        var admin = await CreateUser(userManager, "Amila Perera", "amila@corehr.lk", "Admin@123456", AppRoles.Admin, amila.Id);
        var hrUser = await CreateUser(userManager, "Nimali Fernando", "hr@corehr.lk", "Hr@123456", AppRoles.Hr, hrPerson.Id);
        var managerUser = await CreateUser(userManager, "Ruwan Bandara", "manager@corehr.lk", "Manager@123456", AppRoles.Manager, techManager.Id);
        var employeeUser = await CreateUser(userManager, "Kasun Silva", "employee@corehr.lk", "Employee@123456", AppRoles.Employee, staff.Id);
        amila.UserId = admin.Id;
        hrPerson.UserId = hrUser.Id;
        techManager.UserId = managerUser.Id;
        staff.UserId = employeeUser.Id;

        var annual = leaveTypes.First(t => t.Name == "Annual");
        var sick = leaveTypes.First(t => t.Name == "Sick");
        var casual = leaveTypes.First(t => t.Name == "Casual");
        db.LeaveRequests.AddRange(
            Leave(employees[20], annual, today.AddDays(7), today.AddDays(9), "Family visit", LeaveStatus.Pending, null),
            Leave(employees[21], casual, today.AddDays(3), today.AddDays(3), "Personal errand", LeaveStatus.Pending, null),
            Leave(employees[30], annual, today.AddDays(-40), today.AddDays(-36), "Annual trip", LeaveStatus.Approved, "Enjoy the break."),
            Leave(employees[31], sick, today.AddDays(-12), today.AddDays(-11), "Fever", LeaveStatus.Approved, "Get well soon."),
            Leave(employees[32], casual, today.AddDays(-6), today.AddDays(-6), "Appointment", LeaveStatus.Approved, null),
            Leave(employees[33], annual, today.AddDays(20), today.AddDays(24), "Travel during peak week", LeaveStatus.Rejected, "Please move this outside the inventory count."));

        var records = new List<AttendanceRecord>();
        foreach (var employee in employees.Where(e => e.Status != EmploymentStatus.Resigned))
        {
            for (var offset = 1; offset <= 16; offset++)
            {
                var day = today.AddDays(-offset);
                if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    continue;
                if ((employee.Id + offset) % 10 >= 7)
                    continue;

                records.Add(new AttendanceRecord
                {
                    EmployeeId = employee.Id,
                    WorkDate = day,
                    CheckIn = day.ToDateTime(new TimeOnly(8, 0)).AddMinutes((employee.Id * 3 + offset) % 75),
                    CheckOut = day.ToDateTime(new TimeOnly(17, 0)).AddMinutes((employee.Id + offset) % 40)
                });
            }
        }
        db.AttendanceRecords.AddRange(records);

        db.Notifications.AddRange(
            new AppNotification { Title = "Welcome to CoreHR", Message = "People, attendance, leave, and reports are ready to use.", CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new AppNotification { UserId = admin.Id, Title = "Pending approvals", Message = "2 leave requests are waiting for a decision.", CreatedAt = DateTime.UtcNow.AddHours(-3) },
            new AppNotification { UserId = admin.Id, Title = "Birthdays this week", Message = "3 employees have birthdays this week.", CreatedAt = DateTime.UtcNow.AddDays(-1), IsRead = true },
            new AppNotification { UserId = employeeUser.Id, Title = "Check in", Message = "You have not checked in today.", CreatedAt = DateTime.UtcNow.AddMinutes(-30) });

        for (var index = 0; index < 12; index++)
        {
            var employee = employees[index + 5];
            db.PerformanceReviews.Add(new PerformanceReview
            {
                EmployeeId = employee.Id,
                Year = today.Year - (index % 2),
                Period = index % 2 == 0 ? "H1" : "H2",
                Rating = 3 + (index % 3),
                Summary = $"{employee.FullName} is meeting the expectations agreed for this review period.",
                Status = index == 11 ? "Draft" : "Completed"
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> CreateUser(
        UserManager<ApplicationUser> userManager,
        string name,
        string email,
        string password,
        string role,
        int employeeId)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = name,
            EmailConfirmed = true,
            EmployeeId = employeeId
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    private static JobOpening Opening(IEnumerable<Department> departments, string code, string title, int openings, string description) =>
        new()
        {
            DepartmentId = departments.First(d => d.Code == code).Id,
            Title = title,
            Openings = openings,
            Status = "Active",
            Description = description
        };

    private static LeaveRequest Leave(Employee employee, LeaveType type, DateOnly start, DateOnly end, string reason, LeaveStatus status, string? comment) =>
        new()
        {
            EmployeeId = employee.Id,
            LeaveTypeId = type.Id,
            StartDate = start,
            EndDate = end,
            Reason = reason,
            Status = status,
            ReviewComment = comment,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            ReviewedAt = status == LeaveStatus.Pending ? null : DateTime.UtcNow.AddDays(-1)
        };

    private static int[] BuildWave()
    {
        int[] weights = [2, 3, 5, 2, 6, 3, 4, 7, 3, 5, 4, 8];
        var months = new List<int>();
        for (var month = 0; month < weights.Length; month++)
        {
            for (var count = 0; count < weights[month]; count++)
                months.Add(month + 1);
        }
        return months.ToArray();
    }

    private static DateOnly BirthdayFor(int number, DateOnly today)
    {
        if (number is 2 or 3 or 4)
        {
            var occurrence = today.AddDays(number - 2);
            return new DateOnly(1991 + (number - 2), occurrence.Month, occurrence.Day);
        }

        var month = (number % 12) + 1;
        var day = Math.Min((number % 27) + 1, DateTime.DaysInMonth(1990, month));
        var birthday = new DateOnly(1978 + (number % 20), month, day);
        var weekStart = DateRules.StartOfWeek(today);
        if (DateRules.MonthDayFallsIn(birthday, weekStart, weekStart.AddDays(6)))
            birthday = birthday.AddMonths(1);
        return birthday;
    }

    private static DateOnly HireFor(int number, DateOnly today, int[] wave)
    {
        if (number == 10)
            return YearsAgo(today, 6);

        if (number >= 151)
        {
            var day = Math.Max(1, today.Day - (number - 151));
            return new DateOnly(today.Year, today.Month, Math.Min(day, today.Day));
        }

        int year;
        int month;
        if (number <= 52)
        {
            year = 2024;
            month = wave[(number - 1) % wave.Length];
        }
        else if (number <= 100)
        {
            year = 2025;
            month = wave[(number + 3) % wave.Length];
        }
        else if (number <= 130)
        {
            year = today.Year;
            month = Math.Min((number % 9) + 1, today.Month);
        }
        else
        {
            year = 2021 + (number % 2);
            month = (number % 12) + 1;
        }

        var dayOfMonth = Math.Min(4 + (number % 20), DateTime.DaysInMonth(year, month));
        var hire = new DateOnly(year, month, dayOfMonth);
        if (hire > today)
            hire = new DateOnly(today.Year, today.Month, Math.Max(1, today.Day - 1));

        var weekStart = DateRules.StartOfWeek(today);
        var weekEnd = weekStart.AddDays(6);
        if (DateRules.MonthDayFallsIn(hire, weekStart, weekEnd) && today.Year - hire.Year >= 5)
        {
            var shifted = hire.AddDays(12);
            hire = shifted > today ? hire.AddMonths(-1) : shifted;
        }

        return hire > today ? today : hire;
    }

    private static DateOnly YearsAgo(DateOnly date, int years)
    {
        var year = date.Year - years;
        var day = Math.Min(date.Day, DateTime.DaysInMonth(year, date.Month));
        return new DateOnly(year, date.Month, day);
    }
}
