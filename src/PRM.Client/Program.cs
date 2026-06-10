using PRM.Client.HttpClients;
using PRM.Client.Session;
using PRM.Client.UI;
using PRM.Client.UI.Screens;

namespace PRM.Client;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Title = "PRM - Project Resource Management";
        
        var http = new HttpClient();
        var session = new SessionContext();

        var services = new AppServices(
            session,
            new AuthHttpClient(http, session),
            new UserHttpClient(http, session),
            new EmployeeHttpClient(http, session),
            new ProjectHttpClient(http, session),
            new AllocationHttpClient(http, session),
            new TimesheetHttpClient(http, session),
            new ConfigHttpClient(http, session),
            new AiHttpClient(http, session)
        );

        while (true)
        {
            if (!session.IsLoggedIn)
            {
                var login = new LoginScreen(services);
                await login.RenderAsync();
            }
            else
            {
                // Main Menu Dispatcher
                ConsoleRenderer.Clear();
                ConsoleRenderer.RenderHeader("Main Menu");
                Console.WriteLine($"Logged in as: {session.UserFullName} [{session.Role}]");
                Console.WriteLine(new string('-', 50));
                
                Console.WriteLine("1. Go to Role Dashboard");
                Console.WriteLine("2. Change Password");
                Console.WriteLine("3. Logout");
                Console.WriteLine("0. Exit Application");
                
                var choice = InputHelper.ReadString("Select an option");
                switch (choice)
                {
                    case "1":
                        Screen dashboard = session.Role switch
                        {
                            Domain.Enums.UserRole.Admin => new AdminMenuScreen(services),
                            Domain.Enums.UserRole.Manager => new ManagerMenuScreen(services),
                            Domain.Enums.UserRole.Employee => new EmployeeMenuScreen(services),
                            _ => throw new NotImplementedException()
                        };
                        
                        bool stayOnDashboard;
                        do { stayOnDashboard = await dashboard.RenderAsync(); } while (stayOnDashboard);
                        break;
                    case "2":
                        await new ChangePasswordScreen(services).RenderAsync();
                        break;
                    case "3":
                        session.Clear();
                        break;
                    case "0":
                        return;
                    default:
                        ConsoleRenderer.RenderError("Invalid option.");
                        ConsoleRenderer.Pause();
                        break;
                }
            }
        }
    }
}
