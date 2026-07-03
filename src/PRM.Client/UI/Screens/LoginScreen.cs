using PRM.Client.UI;
using PRM.Domain.Enums;

namespace PRM.Client.UI.Screens;

public class LoginScreen : Screen
{
    public LoginScreen(AppServices services) : base(services) { }

    public override async Task<bool> RenderAsync()
    {
        ShowHeader("Login to PRM");

        var username = InputHelper.ReadString("Username");
        var password = InputHelper.ReadPassword("Password");

        try
        {
            var loginResult = await _services.Auth.LoginAsync(username, password);
            ConsoleRenderer.RenderSuccess($"Welcome back, {_services.Session.UserFullName}!");
            ConsoleRenderer.Pause();

            // If the backend indicates the user must change password on first login,
            // force employees and managers to change it now.
            if (loginResult.ForcePasswordChange &&
                (loginResult.RoleName == "Employee" || loginResult.RoleName == "Manager"))
            {
                ConsoleRenderer.RenderWarning("You must change your temporary password before proceeding.");
                // Loop until password successfully changed (ChangePasswordScreen returns false on success)
                var keepShowing = true;
                while (keepShowing)
                {
                    var cont = await new ChangePasswordScreen(_services).RenderAsync();
                    // RenderAsync returns false on success (we want to stop showing), true on error (retry)
                    keepShowing = cont;
                    if (!keepShowing) break;
                }
            }

            return false; // Exit login loop to go to main menu
        }
        catch (Exception ex)
        {
            ConsoleRenderer.RenderError(ex.Message);
            ConsoleRenderer.Pause();
            return true; // Stay on login screen
        }
    }
}

public class ChangePasswordScreen : Screen
{
    public ChangePasswordScreen(AppServices services) : base(services) { }

    public override async Task<bool> RenderAsync()
    {
        ShowHeader("Change Password");

        var newPass = InputHelper.ReadPassword("New Password");
        var confirmPass = InputHelper.ReadPassword("Confirm Password");

        if (newPass != confirmPass)
        {
            ConsoleRenderer.RenderError("Passwords do not match.");
            ConsoleRenderer.Pause();
            return true;
        }

        try
        {
            await _services.Auth.ChangePasswordAsync(newPass, confirmPass);
            ConsoleRenderer.RenderSuccess("Password changed successfully.");
            ConsoleRenderer.Pause();
            return false;
        }
        catch (Exception ex)
        {
            ConsoleRenderer.RenderError(ex.Message);
            ConsoleRenderer.Pause();
            return true;
        }
    }
}
