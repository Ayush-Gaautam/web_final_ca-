using System.Security.Claims;

namespace web_ca_3.Security;

public static class RoleResolver
{
    public static string Resolve(ClaimsPrincipal user)
    {
        if (user.IsInRole(RoleNames.SystemAdministrator) || user.IsInRole("Admin"))
            return RoleNames.SystemAdministrator;
        if (user.IsInRole(RoleNames.MessAdministrator))
            return RoleNames.MessAdministrator;
        if (user.IsInRole(RoleNames.FinanceOfficer))
            return RoleNames.FinanceOfficer;
        if (user.IsInRole(RoleNames.MessManager) || user.IsInRole("Manager"))
            return RoleNames.MessManager;
        if (user.IsInRole(RoleNames.KitchenStaff) || user.IsInRole("Staff"))
            return RoleNames.KitchenStaff;
        return RoleNames.Student;
    }
}
