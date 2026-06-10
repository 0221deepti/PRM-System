using PRM.Client.HttpClients;
using PRM.Client.Session;

namespace PRM.Client;

public record AppServices(
    SessionContext Session,
    AuthHttpClient Auth,
    UserHttpClient Users,
    EmployeeHttpClient Employees,
    ProjectHttpClient Projects,
    AllocationHttpClient Allocations,
    TimesheetHttpClient Timesheets,
    ConfigHttpClient Config,
    AiHttpClient Ai
);
