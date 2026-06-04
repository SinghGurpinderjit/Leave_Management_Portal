namespace Shared.Common.DTOs.Leaves;

public class LeaveTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DefaultAllocation { get; set; }
    public string? Description { get; set; } = string.Empty;
}