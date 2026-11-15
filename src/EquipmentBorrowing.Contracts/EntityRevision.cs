using System.Security.Cryptography;
using System.Text.Json;

namespace EquipmentBorrowing.Contracts;

// Optional optimistic-concurrency extension; the published editable JSON fields remain unchanged.
public static class EntityRevision
{
    public const string Header = "X-Expected-Revision";
    public static string For(StudentResponse student) => Hash(student);
    public static string For(EquipmentResponse equipment) => Hash(equipment);
    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(
        JsonSerializer.SerializeToUtf8Bytes(value, JsonSerializerOptions.Web)));
}
