using System.Text.Json.Serialization;

namespace EquipmentBorrowing.Contracts;

public sealed record ApiError(string Code, string Message);
public sealed record ApiStatus(string Service, string Version, string Phase);
public sealed record StudentResponse(int Id, string StudentNumber, string FullName, string? Contact, bool IsEligible);
public sealed record EquipmentResponse(int Id, string EquipmentCode, string Name, string? Description, bool IsAvailable);
public sealed record BorrowingResponse(int Id, int StudentId, int EquipmentId, DateOnly BorrowedDate,
    DateOnly ExpectedReturnDate, DateOnly? ReturnedDate, string Status,
    string? StudentNumber = null, string? StudentName = null,
    string? EquipmentCode = null, string? EquipmentName = null);
public sealed record CheckoutResponse(IReadOnlyList<BorrowingResponse> Borrowings);

// Nullable inputs preserve the distinction between missing fields and supplied values.
// Endpoint/application validation assigns the appropriate 400/404/409 category.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record StudentInput([property: JsonRequired] string? StudentNumber,
    [property: JsonRequired] string? FullName, string? Contact);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record StudentUpdateInput([property: JsonRequired] string? StudentNumber,
    [property: JsonRequired] string? FullName, [property: JsonRequired] string? Contact);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record EquipmentInput(string? EquipmentCode, string? Name, string? Description);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record EligibilityInput(bool? IsEligible);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record BorrowInput(int? StudentId, int? EquipmentId, DateOnly? ExpectedReturnDate);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CheckoutInput(int? StudentId, int[]? EquipmentIds, DateOnly? ExpectedReturnDate);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReturnInput;
