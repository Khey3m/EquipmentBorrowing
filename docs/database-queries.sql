-- 1. Basic Retrieval: Retrieve all equipment
SELECT * FROM Equipment;

-- 2. Filtering: Retrieve only currently available equipment
SELECT * FROM Equipment 
WHERE IsAvailable = 1;

-- 3. Join: Retrieve active borrowings with student and equipment details
SELECT 
    s.FullName AS Student,
    e.Name AS Equipment,
    b.BorrowedAt AS Borrowed,
    b.ExpectedReturnAt AS Due
FROM Borrowings b
INNER JOIN Students s ON b.StudentId = s.Id
INNER JOIN Equipment e ON b.EquipmentId = e.Id
WHERE b.Status = 'Active';

-- 4. Aggregate: Number of active borrowings per student
SELECT 
    s.FullName, 
    COUNT(b.Id) AS ActiveBorrowingsCount
FROM Borrowings b
INNER JOIN Students s ON b.StudentId = s.Id
WHERE b.Status = 'Active'
GROUP BY s.Id, s.FullName;

-- 5. Update: Mark an equipment item as available
UPDATE Equipment 
SET IsAvailable = 1 
WHERE Id = 101;