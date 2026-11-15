namespace web_ca_3.Security;

public static class RoleNames
{
    public const string Student = "Student";
    public const string KitchenStaff = "KitchenStaff";
    public const string MessManager = "MessManager";
    public const string MessAdministrator = "MessAdministrator";
    public const string FinanceOfficer = "FinanceOfficer";
    public const string SystemAdministrator = "SystemAdministrator";

    public static readonly string[] All =
    [
        Student, KitchenStaff, MessManager, MessAdministrator, FinanceOfficer, SystemAdministrator
    ];

    public static string DisplayName(string role) => role switch
    {
        KitchenStaff => "Kitchen Staff",
        MessManager => "Mess Manager",
        MessAdministrator => "Mess Administrator",
        FinanceOfficer => "Finance Officer",
        SystemAdministrator => "System Administrator",
        _ => "Student"
    };
}
