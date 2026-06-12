# PRM System Refactoring - Complete Guide

## Overview
This document summarizes the complete refactoring of the PRM system from an Employee-based architecture to a unified User-based architecture with proper RBAC implementation.

---

## 1. Key Architecture Changes

### Before: Employee-Centric Model
```
User (Authentication only)
  └─ UserRole (enum: Admin, Manager, Employee)
  
Employee (Resource Management)
  ├─ Department, Status, ManagerId
  ├─ EmployeeSkill → Skill
  └─ Allocations, Timesheets
```

### After: User-Centric Model with RBAC
```
User (Authentication + Resource Management)
  ├─ RoleId → Role (FK)
  ├─ Department, Status, ManagerId (self-referencing)
  ├─ UserSkill → Skill
  └─ Allocations, Timesheets

RBAC: User → Role → RolePermission → Permission
```

---

## 2. Entity Changes

### Removed Entities
- **Employee** - DELETED (consolidated into User)

### New Entities
- **ActivityTag** - Normalized tag system for timesheets
- **TimesheetEntry** - Individual timesheet day/entry
- **TimesheetEntryTag** - M:N between TimesheetEntry and ActivityTag

### Modified Entities

#### User.cs
```csharp
// BEFORE
public class User
{
    public UserRole Role { get; set; }  // enum
    public Employee? Employee { get; set; }  // one-to-one
}

// AFTER
public class User
{
    public int RoleId { get; set; }  // FK to Role
    public string Department { get; set; }
    public EmployeeStatus Status { get; set; }
    public int? ManagerId { get; set; }  // Self-referencing FK
    
    public Role Role { get; set; } = null!;
    public User? Manager { get; set; }
    public ICollection<User> DirectReports { get; set; }
    public ICollection<UserSkill> Skills { get; set; }
    public ICollection<Allocation> Allocations { get; set; }
    public ICollection<Timesheet> Timesheets { get; set; }
}
```

#### Allocation.cs
```csharp
// BEFORE
public int EmployeeId { get; set; }
public Employee Employee { get; set; } = null!;

// AFTER
public int UserId { get; set; }
public User User { get; set; } = null!;
```

#### Project.cs
```csharp
// BEFORE
public int ManagerId { get; set; }  // FK → Employee
public Employee Manager { get; set; } = null!;

// AFTER
public int ManagerId { get; set; }  // FK → User
public User Manager { get; set; } = null!;
```

#### Timesheet.cs
```csharp
// BEFORE
public int EmployeeId { get; set; }
public decimal HoursWorked { get; set; }
public string ActivityTags { get; set; } = string.Empty;  // CSV
public Employee Employee { get; set; } = null!;

// AFTER
public int UserId { get; set; }
public decimal TotalHoursWorked { get; set; }  // Calculated from entries
public User User { get; set; } = null!;
public ICollection<TimesheetEntry> Entries { get; set; }
```

#### EmployeeSkill.cs → UserSkill.cs
```csharp
// BEFORE
public int EmployeeId { get; set; }
public Employee Employee { get; set; } = null!;

// AFTER
public int UserId { get; set; }
public User User { get; set; } = null!;
```

---

## 3. Configuration Changes

### New Configurations
- `RoleConfiguration.cs` - Role entity FK and navigation setup
- `PermissionConfiguration.cs` - Permission entity
- `RolePermissionConfiguration.cs` - M:N between Role and Permission
- `UserSkillConfiguration.cs` - Replaces EmployeeSkillConfiguration
- `ActivityTagConfiguration.cs` - Activity tag management
- `TimesheetEntryConfiguration.cs` - Individual timesheet entries
- `TimesheetEntryTagConfiguration.cs` - M:N between entries and tags

### Updated Configurations
- `UserConfiguration.cs` - Added RoleId FK and ManagerId self-reference
- `AllocationConfiguration.cs` - Changed EmployeeId → UserId
- `TimesheetConfiguration.cs` - Changed EmployeeId → UserId, added Entries navigation
- `ProjectConfiguration.cs` - Manager FK now points to User

---

## 4. Repository Changes

### Removed
- **IEmployeeRepository** & **EmployeeRepository** - DELETED

### Enhanced
- **IUserRepository** now includes employee-related methods:
  ```csharp
  Task<IEnumerable<User>> GetByManagerIdAsync(int managerId, CancellationToken ct);
  Task<IEnumerable<User>> GetBenchUsersAsync(int managerId, CancellationToken ct);
  Task<User?> GetWithSkillsAsync(int userId, CancellationToken ct);
  Task<User?> GetWithAllocationsAsync(int userId, CancellationToken ct);
  Task<IEnumerable<User>> GetAllWithDetailsAsync(CancellationToken ct);
  Task<User?> GetWithRoleAndPermissionsAsync(int userId, CancellationToken ct);
  ```

### Updated
- **AllocationRepository** - `GetActiveByEmployeeAsync()` → `GetActiveByUserAsync()`
- **TimesheetRepository** - `GetByEmployeeAsync()` → `GetByUserAsync()`

---

## 5. Service Layer Changes

### Removed
- **IEmployeeService** - DELETED (consolidated into IUserService)
- **EmployeeService** - DELETED (methods merged into UserService)

### Enhanced
- **IUserService** now includes:
  - Employee management methods (GetAllEmployees, GetTeamEmployees, etc.)
  - Skill management methods (AddSkill, UpdateSkill, RemoveSkill)
  - RBAC-aware methods (GetWithRoleAndPermissions)

### Updated
- **AllocationService** - Uses `IUserRepository` instead of `IEmployeeRepository`
- **TimesheetService** - Uses `IUserRepository`, handles new TimesheetEntry structure
- **UserService** - Consolidated user + employee operations

---

## 6. DTO Changes

### Modified User DTOs
```csharp
// BEFORE
public record CreateUserDto(
    string FullName,
    string Email,
    string Username,
    string TemporaryPassword,
    UserRole Role);  // enum

// AFTER
public record CreateUserDto(
    string FullName,
    string Email,
    string Username,
    string TemporaryPassword,
    int RoleId,  // FK reference
    string? Department = null);

// BEFORE
public record UserSummaryDto(
    int Id,
    string Username,
    string FullName,
    string Email,
    UserRole Role,  // enum
    bool IsActive);

// AFTER
public record UserSummaryDto(
    int Id,
    string Username,
    string FullName,
    string Email,
    string RoleName,  // role name from FK
    bool IsActive);
```

### Modified Allocation DTOs
```csharp
// BEFORE
public record CreateAllocationDto(
    int EmployeeId,
    int ProjectId,
    int UtilisationPercent,
    DateOnly FromDate,
    DateOnly ToDate);

// AFTER
public record CreateAllocationDto(
    int UserId,
    int ProjectId,
    int UtilisationPercent,
    DateOnly FromDate,
    DateOnly ToDate);
```

### Updated Project DTOs
```csharp
// AllocationSummaryDto renamed to ProjectAllocationSummaryDto
// EmployeeId → UserId, EmployeeName → UserName
public record ProjectAllocationSummaryDto(
    int Id,
    int UserId,
    string UserName,
    int ProjectId,
    string ProjectName,
    int UtilisationPercent,
    DateOnly FromDate,
    DateOnly ToDate);
```

### New Timesheet DTOs (Future)
```csharp
// Will be expanded to support new TimesheetEntry structure
public record TimesheetEntryDto(
    int Id,
    DateOnly EntryDate,
    decimal HoursWorked,
    string Description,
    List<string> Tags);
```

---

## 7. Database Migration Strategy

### Migration Steps

1. **Create Migration**
   ```bash
   dotnet ef migrations add ConsolidateEmployeeToUser
   ```

2. **Migration Should Handle:**
   - Migrate Employee data → User (preserving Department, Status, ManagerId)
   - Assign default Role to existing users
   - Convert EmployeeSkill → UserSkill
   - Normalize ActivityTags (CSV) → TimesheetEntry + TimesheetEntryTag
   - Update FK relationships:
     - Allocation.EmployeeId → Allocation.UserId
     - Timesheet.EmployeeId → Timesheet.UserId
     - Project.ManagerId now points to User

3. **Sample Migration Data:**
   ```sql
   -- Migrate Employee data to User
   UPDATE Users SET 
       RoleId = (SELECT RoleId FROM DefaultRole), -- Determine from old UserRole
       Department = e.Department,
       Status = e.Status,
       ManagerId = e.ManagerId
   FROM Employee e
   WHERE Users.Id = e.UserId;

   -- Convert EmployeeSkill to UserSkill
   INSERT INTO UserSkills (UserId, SkillId, Proficiency)
   SELECT UserId, SkillId, Proficiency FROM EmployeeSkills;

   -- Normalize Timesheets
   -- Create TimesheetEntry records from hours
   -- Parse ActivityTags CSV → TimesheetEntryTag entries
   ```

---

## 8. Breaking Changes & Migration Guide

### API Endpoint Changes
| Before | After | Impact |
|--------|-------|--------|
| `/api/employees` | `/api/users` (enhanced) | Update client calls |
| POST `/api/employees` | POST `/api/users` | Update creation logic |
| GET `/api/employees/{id}` | GET `/api/users/{id}` | Update retrieval logic |
| `EmployeeId` param | `UserId` param | Update all calls |

### Authorization Changes
- Replace `[Authorize(Roles = "Admin")]` with RBAC checks
- Use `User.FindFirst("Permission")` instead of role-based checks
- Implement permission-based authorization in handlers

### Data Access Changes
```csharp
// BEFORE
var emp = await _employees.GetByIdAsync(empId);
await _employees.DeleteAsync(emp);

// AFTER
var user = await _users.GetByIdAsync(userId);
user.IsActive = false;  // Soft delete
_users.Update(user);
await _users.SaveChangesAsync();
```

---

## 9. Remaining Tasks

### ✅ Completed
1. Entity definitions updated
2. Configurations created
3. Repository interfaces & implementations updated
4. Service implementations consolidated
5. DTO updates
6. DbContext updated

### ⏳ Remaining Tasks
1. **Update Controllers** - Rename/update to use new DTOs and services
2. **Authentication Service** - Update to use Role FK instead of enum
3. **Authorization Middleware** - Implement RBAC permission checks
4. **Mappers** - Update AutoMapper profiles (if used)
5. **Create Database Migration** - Generate and apply migration
6. **HTTP Clients** - Update Client project references
7. **Dependency Injection** - Register new services and remove old ones
8. **Unit Tests** - Update test fixtures and mocks
9. **Integration Tests** - Update test data and assertions
10. **API Documentation** - Update OpenAPI/Swagger specs
11. **Database Seed Data** - Create seed roles, permissions, users

---

## 10. RBAC Implementation Guide

### Authorization Flow
```
1. User authenticates → Gets JWT with UserId
2. JWT claim includes UserId
3. On API call:
   - Get User with Role and Permissions
   - Check if User.Role has required Permission
   - Allow/Deny access
```

### Example Implementation
```csharp
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IUserRepository _users;
    
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst("sub");
        if (userIdClaim == null) return;
        
        var user = await _users.GetWithRoleAndPermissionsAsync(
            int.Parse(userIdClaim.Value), 
            default);
        
        var hasPermission = user?.Role.RolePermissions
            .Any(rp => rp.Permission.Name == requirement.PermissionName) 
            ?? false;
        
        if (hasPermission)
            context.Succeed(requirement);
    }
}
```

---

## 11. Testing Considerations

### Unit Tests to Update
- UserService methods (consolidation)
- AllocationService (UserId refs)
- TimesheetService (UserId refs, entry handling)
- Repository queries

### Integration Tests to Update
- User creation with RBAC
- Manager assignment (self-reference)
- Allocation across users
- Timesheet entries with tags

### Seed Data for Testing
```csharp
// Create default roles
new Role { Name = "Admin", Description = "Administrator" },
new Role { Name = "Manager", Description = "Project Manager" },
new Role { Name = "Employee", Description = "Team Member" }

// Create permissions
new Permission { Name = "ManageUsers" },
new Permission { Name = "ManageAllocations" },
new Permission { Name = "SubmitTimesheet" }

// Assign permissions to roles (RolePermission join)
```

---

## 12. Performance Considerations

### Index Updates Needed
```sql
-- New indexes for performance
CREATE INDEX IX_User_RoleId ON Users(RoleId);
CREATE INDEX IX_User_ManagerId ON Users(ManagerId);
CREATE INDEX IX_Allocation_UserId ON Allocations(UserId);
CREATE INDEX IX_Timesheet_UserId ON Timesheets(UserId);
CREATE INDEX IX_TimesheetEntry_TimesheetId ON TimesheetEntries(TimesheetId);
CREATE INDEX IX_ActivityTag_Name ON ActivityTags(Name) UNIQUE;
```

### Query Optimization
- Eager load related data (Role, Manager, Skills)
- Consider pagination for list endpoints
- Cache frequently accessed roles/permissions

---

## 13. Deployment Checklist

- [ ] Code review of all entity/DTO changes
- [ ] Verify all repositories are updated
- [ ] Test all service methods in dev environment
- [ ] Update all controller endpoints
- [ ] Add new DI registrations (roles, permissions repos)
- [ ] Create and review migration
- [ ] Backup production database
- [ ] Run migration in staging
- [ ] Update client applications
- [ ] Update API documentation
- [ ] Test RBAC in production-like environment
- [ ] Deploy to production
- [ ] Monitor logs for errors
- [ ] Communicate changes to users

---

## 14. Enum Reference

### EmployeeStatus (unchanged, now on User)
```csharp
public enum EmployeeStatus
{
    Bench,      // Not allocated
    Allocated,  // Currently allocated to project(s)
    OnLeave,    // On leave
    Inactive    // Not available
}
```

### SkillProficiency (unchanged)
```csharp
public enum SkillProficiency
{
    Beginner,
    Intermediate,
    Advanced,
    Expert
}
```

---

## Summary of Files Changed

### Domain Layer
- User.cs (modified)
- Employee.cs (DELETED)
- Allocation.cs (modified)
- Project.cs (modified)
- Timesheet.cs (modified)
- EmployeeSkill.cs → UserSkill.cs (renamed/modified)
- ActivityTag.cs (NEW)
- TimesheetEntry.cs (NEW)
- TimesheetEntryTag.cs (NEW)
- Skill.cs (modified - reference change)

### Infrastructure Layer
- PrmDbContext.cs (modified - DbSet changes)
- Configurations/ (7 files updated, 4 new)
- Repositories/ (2 interfaces updated, 1 deleted)
- Repositories/UserRepository.cs (enhanced)
- Repositories/AllocationRepository.cs (updated)
- Repositories/TimesheetRepository.cs (updated)

### Application Layer
- Services/UserService.cs (consolidated)
- Services/EmployeeService.cs (DELETED/merged)
- Services/AllocationService.cs (updated)
- Services/TimesheetService.cs (updated)
- Interfaces/Repositories/ (3 updated, 1 deleted)
- Interfaces/Services/IUserService.cs (enhanced)
- Interfaces/Services/IEmployeeService.cs (DELETED/merged)
- DTOs/User/ (modified)
- DTOs/Allocation/ (modified)
- DTOs/Project/ (modified)

### Next: Controllers, HTTP Clients, Auth, Tests
