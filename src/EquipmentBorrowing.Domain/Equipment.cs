namespace EquipmentBorrowing.Domain;

public class Equipment
{
    public int Id { get; private set; }
    public string EquipmentIdentifier { get; private set; } = string.Empty;
    public string NormalizedEquipmentIdentifier { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsAvailable { get; private set; }

    private Equipment() { } // EF materialization

    public Equipment(int id, string equipmentIdentifier, string name, string? description = null)
    {
        Id = id;
        UpdateInformation(equipmentIdentifier, name, description);
        IsAvailable = true;
    }

    public void UpdateInformation(string equipmentIdentifier, string name, string? description)
    {
        var identifier = DomainText.Required(equipmentIdentifier, nameof(EquipmentIdentifier));
        var validName = DomainText.Required(name, nameof(Name));
        EquipmentIdentifier = identifier;
        NormalizedEquipmentIdentifier = DomainText.NormalizeIdentifier(identifier);
        Name = validName;
        Description = DomainText.Optional(description);
    }

    public void MarkAsBorrowed()
    {
        if (!IsAvailable) throw new InvalidOperationException($"'{Name}' is currently unavailable.");
        IsAvailable = false;
    }

    public void MarkAsReturned() => IsAvailable = true;
}
