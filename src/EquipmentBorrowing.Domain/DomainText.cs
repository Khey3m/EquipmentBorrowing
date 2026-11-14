namespace EquipmentBorrowing.Domain;

public static class DomainText
{
    public static string Required(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{field} is required.", field);
        return value.Trim();
    }

    public static string NormalizeIdentifier(string value) => Required(value, "Identifier").ToUpperInvariant();
    public static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
