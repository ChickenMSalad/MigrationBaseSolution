namespace Migration.Domain.Models;

public sealed record ValidationIssue(string Code, string Message, bool IsError = true);
