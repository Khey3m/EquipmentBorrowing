using System.Text.Json.Serialization;

namespace EquipmentBorrowing.Contracts;

public sealed record ApiError(string Code, string Message);
public sealed record ApiStatus(string Service, string Version, string Phase);
public sealed record StudentResponse([property: JsonRequired] int Id, [property: JsonRequired] string StudentNumber,
    [property: JsonRequired] string FullName, string? Contact, [property: JsonRequired] bool IsEligible);
public sealed record EquipmentResponse([property: JsonRequired] int Id, [property: JsonRequired] string EquipmentCode,
    [property: JsonRequired] string Name, string? Description, [property: JsonRequired] bool IsAvailable);
[JsonConverter(typeof(JsonStringEnumConverter<BorrowingState>))]
public enum BorrowingState { Active, Returned }
public sealed record BorrowingResponse([property: JsonRequired] int Id, [property: JsonRequired] int StudentId,
    [property: JsonRequired] int EquipmentId, [property: JsonRequired] DateOnly BorrowedDate,
    [property: JsonRequired] DateOnly ExpectedReturnDate, [property: JsonRequired] DateOnly? ReturnedDate,
    [property: JsonRequired] BorrowingState Status,
    string? StudentNumber = null, string? StudentName = null,
    string? EquipmentCode = null, string? EquipmentName = null);
public sealed record CheckoutResponse([property: JsonRequired] IReadOnlyList<BorrowingResponse> Borrowings);

// Nullable inputs preserve the distinction between missing fields and supplied values.
// Endpoint/application validation assigns the appropriate 400/404/409 category.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record StudentInput([property: JsonRequired] string? StudentNumber,
    [property: JsonRequired] string? FullName, string? Contact);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record StudentUpdateInput([property: JsonRequired] string? StudentNumber,
    [property: JsonRequired] string? FullName, [property: JsonRequired] string? Contact);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record EquipmentInput([property: JsonRequired] string? EquipmentCode,
    [property: JsonRequired] string? Name, string? Description);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record EquipmentUpdateInput([property: JsonRequired] string? EquipmentCode,
    [property: JsonRequired] string? Name, [property: JsonRequired] string? Description);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record EligibilityInput(bool? IsEligible);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record BorrowInput([property: JsonRequired] int? StudentId,
    [property: JsonRequired] int? EquipmentId, [property: JsonRequired] DateOnly? ExpectedReturnDate);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CheckoutInput([property: JsonRequired] int? StudentId,
    [property: JsonRequired] int[]? EquipmentIds, [property: JsonRequired] DateOnly? ExpectedReturnDate);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReturnInput;
