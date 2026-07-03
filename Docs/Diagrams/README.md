# PRM System - PlantUML Diagrams Documentation

## Overview
This directory contains 5 comprehensive PlantUML diagrams that document the PRM (Project Resource Management) System architecture, workflows, and data model.

## Diagrams

### 1. User Flow Diagram (`01_UserFlow.puml`)
**Purpose:** Illustrates the end-to-end flow of user interactions with the PRM system.

**Key Elements:**
- Login and authentication process
- Session management
- Role-based dashboard routing (Admin, Manager, Employee)
- Password change workflow
- Action processing and menu navigation
- Logout functionality

**When to Use:**
- Understanding user journeys
- Training new users
- Documentation for support teams
- UI/UX design reference

---

### 2. System Flow Diagram (`02_SystemFlow.puml`)
**Purpose:** Shows how system components communicate and interact at the architectural level.

**Key Flows:**
- **Authentication Flow:** Client → API → Service → Repository → Database
- **Resource Management Flow:** Projects retrieval and listing
- **Project & Team Allocation Flow:** Creating and managing allocations
- **Timesheet Submission Flow:** Submitting hours with validation
- **Configuration & AI Integration:** System configuration and AI provider integration

**Components Shown:**
- PRM Console Client (presentation layer)
- API Controllers (API layer)
- Application Services (business logic layer)
- Repositories (data access layer)
- SQLite Database (persistence layer)
- Infrastructure Services (JWT, BCrypt, AI, Scheduler)

**When to Use:**
- Architecture reviews
- API design documentation
- Integration testing planning
- System debugging and troubleshooting

---

### 3. Class Diagram (`03_ClassDiagram.puml`)
**Purpose:** Provides detailed view of all classes, interfaces, and their relationships in the codebase.

**Layers Shown:**

#### PRM.Domain
- **Entities:** User, Employee, Project, Milestone, Allocation, Skill, EmployeeSkill, Timesheet, SystemConfig
- **Enums:** UserRole, EmployeeStatus, ProjectStatus, ProjectHealthStatus, MilestoneStatus, SkillCategory, SkillProficiency
- **Base Class:** BaseEntity (Id, CreatedAt, UpdatedAt)

#### PRM.Application
- **Interfaces:** IUserRepository, IEmployeeRepository, IProjectRepository, IAllocationRepository, ITimesheetRepository, IAuthService, IProjectService, IAllocationService, ITimesheetService, IEmployeeService
- **Services:** AuthService, ProjectService, AllocationService, TimesheetService
- **Implementations:** All repository implementations

#### PRM.Api
- **Controllers:** AuthController, ProjectsController, AllocationsController, TimesheetsController, EmployeesController

#### PRM.Client
- **Components:** SessionContext, AppServices, Screen hierarchy (LoginScreen, AdminMenuScreen, ManagerMenuScreen, EmployeeMenuScreen)

#### PRM.Infrastructure
- **Database:** PrmDbContext with all DbSets
- **Repositories:** Repository implementations
- **Services:** TokenService (ITokenService), AiService (IAiService)

**Relationships:**
- Inheritance (User → Employee relationship)
- Aggregation (Collections)
- Dependencies (Dependency Injection)
- Interface implementation

**When to Use:**
- Understanding class structure and hierarchy
- Refactoring and code improvements
- Design pattern documentation
- Onboarding new developers
- Code review reference

---

### 4. Entity Relationship Diagram (ERD) (`04_EntityRelationshipDiagram.puml`)
**Purpose:** Visualizes the database schema and entity relationships.

**Entities:**
1. **User** - Authentication and system access
   - Primary Key: Id
   - Attributes: FullName, Email, Username, PasswordHash, Role, IsActive, ForcePasswordChange

2. **Employee** - Employee information
   - Primary Key: Id
   - Foreign Key: UserId (User), ManagerId (self-reference for hierarchy)
   - Attributes: Department, Status

3. **Skill** - Available skills in the system
   - Primary Key: Id
   - Attributes: Name, Category (Technical, Business, Soft)

4. **EmployeeSkill** - Junction table (Many-to-Many)
   - Primary Keys: Id
   - Foreign Keys: EmployeeId (Employee), SkillId (Skill)
   - Attributes: Proficiency (Beginner, Intermediate, Expert)

5. **Project** - Project information
   - Primary Key: Id
   - Foreign Key: ManagerId (Employee)
   - Attributes: Name, Description, StartDate, EndDate, Status, TotalStoryPoints, HealthStatus

6. **Milestone** - Project milestones
   - Primary Key: Id
   - Foreign Key: ProjectId (Project)
   - Attributes: Title, DueDate, StoryPoints, Status

7. **Allocation** - Employee-Project allocation (Many-to-Many)
   - Primary Key: Id
   - Foreign Keys: EmployeeId (Employee), ProjectId (Project)
   - Attributes: UtilisationPercent, FromDate, ToDate, IsActive

8. **Timesheet** - Weekly timesheet entries
   - Primary Key: Id
   - Foreign Keys: EmployeeId (Employee), ProjectId (Project)
   - Attributes: WeekStartDate, HoursWorked, ActivityTags

9. **SystemConfig** - System configuration settings
   - Primary Key: Id
   - Attributes: LlmProvider, LlmApiKey, SchedulerIntervalHours, MaxWeeklyHours

**Key Relationships:**
- User (1:0..1) Employee
- Employee (1:*) EmployeeSkill
- Skill (1:*) EmployeeSkill
- Employee (1:*) Allocation
- Project (1:*) Allocation
- Employee (1:*) Timesheet
- Project (1:*) Timesheet
- Project (1:*) Milestone
- Employee (self-reference) Manager-DirectReports

**When to Use:**
- Database design and optimization
- SQL query understanding
- Migration planning
- Data integrity verification
- Backup and recovery planning

---

### 5. Use Case Diagram (`05_UseCaseDiagram.puml`)
**Purpose:** Describes all possible user interactions and system functionalities from a business perspective.

**Actors:**
- **Admin** - System administrator with full access
- **Manager** - Project manager with team management capabilities
- **Employee** - Regular employee with limited access
- **System** - Automated processes (Scheduler/AI)

**Use Cases (30 total):**

#### Authentication (All Actors)
- UC1: Login
- UC2: Change Password
- UC3: Reset User Password (Admin only)

#### User Management (Admin)
- UC4: Create User
- UC5: Deactivate User
- UC6: View User List
- UC7: Update User Role

#### Employee Management
- UC8: Create Employee (Admin)
- UC9: View Employee Details
- UC10: Update Employee Info (Admin, Manager)
- UC11: View Team Members (Manager)
- UC12: Manage Employee Skills

#### Project Management
- UC13: Create Project (Admin, Manager)
- UC14: View Project Details
- UC15: Update Project Status (Admin, Manager)
- UC16: View My Projects (Manager, Employee)
- UC17: Add Milestone (Admin, Manager)
- UC18: Update Milestone Status (Admin, Manager)

#### Allocation Management
- UC19: Create Allocation (Admin, Manager)
- UC20: View Allocations (All logged-in users)
- UC21: Update Allocation (Admin, Manager)
- UC22: Deactivate Allocation (Admin, Manager)
- UC23: Check Resource Availability (Admin, Manager)

#### Timesheet Management
- UC24: Submit Timesheet (Employee)
- UC25: View Timesheet (Employee, Manager)
- UC26: Analyze Timesheet (System)
- UC27: Validate Hours (System)

#### System Administration
- UC28: View System Config (Admin)
- UC29: Update System Config (Admin)
- UC30: Configure AI Provider (Admin)

#### AI & Automation
- UC31: Generate AI Insights (System)
- UC32: Run Scheduler Job (System)

**Relationships:**
- Include (required functionality)
- Extend (optional enhancements)
- Dependencies between use cases

**When to Use:**
- Business requirements gathering
- Scope definition for development sprints
- User acceptance testing (UAT)
- Documentation for stakeholders
- Feature prioritization

---

## How to Render Diagrams

### Using PlantUML Online
1. Go to [PlantUML Editor](http://www.plantuml.com/plantuml/uml/)
2. Copy the content of any .puml file
3. Paste into the editor
4. Click "Update" to render

### Using PlantUML CLI
```bash
# Install PlantUML
# macOS: brew install plantuml
# Ubuntu: sudo apt-get install plantuml

# Generate PNG from puml file
plantuml 01_UserFlow.puml

# Generate SVG (better for web)
plantuml -tsvg 01_UserFlow.puml

# Generate all diagrams
plantuml *.puml
```

### Using VS Code
Install the PlantUML extension:
1. Open VS Code Extensions
2. Search for "PlantUML"
3. Install the official PlantUML extension
4. Open any .puml file
5. Press Alt+D to preview

### Using JetBrains IDEs (IntelliJ, Rider)
1. Install PlantUML integration plugin
2. Open any .puml file
3. Right-click → PlantUML → Preview to render

---

## Diagram Consistency & Updates

**All diagrams are based on actual source code analysis of:**
- Entities: `src/PRM.Domain/Entities/`
- Services: `src/PRM.Application/Services/`
- Controllers: `src/PRM.Api/Controllers/`
- Repositories: `src/PRM.Infrastructure/Persistence/Repositories/`
- Client: `src/PRM.Client/`

**When updating the codebase, remember to update corresponding diagrams:**
- New entity → Update ERD, Class Diagram, Use Cases
- New service → Update System Flow, Class Diagram
- New API endpoint → Update Use Cases, System Flow
- New user flow → Update User Flow Diagram

---

## File Structure
```
Docs/Diagrams/
├── 01_UserFlow.puml                    # User interaction flows
├── 02_SystemFlow.puml                  # System component interactions
├── 03_ClassDiagram.puml                # Class structure and relationships
├── 04_EntityRelationshipDiagram.puml   # Database schema
├── 05_UseCaseDiagram.puml              # User interactions with system
└── README.md                           # This file
```

---

## PlantUML Features Used

- **Activity Diagrams:** For user flows and process flows
- **Sequence Diagrams:** For system interactions and message flows
- **Class Diagrams:** For object-oriented design representation
- **Entity Relationship Diagrams:** For database design
- **Use Case Diagrams:** For functional requirements

---

## References

- [PlantUML Documentation](https://plantuml.com/guide)
- [PlantUML Activity Diagram Syntax](https://plantuml.com/activity-diagram-beta)
- [PlantUML Sequence Diagram Syntax](https://plantuml.com/sequence-diagram)
- [PlantUML Class Diagram Syntax](https://plantuml.com/class-diagram)
- [PlantUML ER Diagram Syntax](https://plantuml.com/ie-diagram)
- [PlantUML Use Case Diagram Syntax](https://plantuml.com/use-case-diagram)

---

## Notes

- All diagrams follow PlantUML standards and are compatible with all rendering engines
- Diagrams are designed for clarity and include detailed notes explaining key concepts
- Color schemes are consistent and optimized for printing and digital viewing
- All entities, services, and relationships are derived from the actual codebase

For questions or updates, refer to the source code or raise a discussion with the development team.
