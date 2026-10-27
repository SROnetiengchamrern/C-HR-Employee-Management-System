using System.ComponentModel.DataAnnotations;

namespace HrEmployeeManagementSystem.Api.Models;

public class DocumentTemplate
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = "";

    [MaxLength(60)]
    public string Category { get; set; } = "";

    public string Body { get; set; } = "";
}

public class BenefitPlan
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = "";

    [MaxLength(1000)]
    public string Description { get; set; } = "";

    [MaxLength(200)]
    public string Eligibility { get; set; } = "";
}

public class PerformanceReview
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public int Year { get; set; }

    [MaxLength(40)]
    public string Period { get; set; } = "";

    public int Rating { get; set; }

    [MaxLength(1000)]
    public string Summary { get; set; } = "";

    [MaxLength(40)]
    public string Status { get; set; } = "Completed";
}
