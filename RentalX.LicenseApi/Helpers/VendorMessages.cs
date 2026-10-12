namespace RentalX.LicenseApi.Helpers;

public static class VendorMessages
{
    public const string VendorName = "Utkal Technologies";
    public const string ContactLine = "Please contact Utkal Technologies for assistance.";
    public const string ContactShort = "Contact Utkal Technologies.";

    public static string PendingApproval =>
        "Your application is under review. Please contact Utkal Technologies for activation status.";

    public static string NotRegistered =>
        "This computer is not registered. Use Apply for Access, then contact Utkal Technologies for approval.";

    public static string Rejected =>
        "Your application was not approved. Please contact Utkal Technologies.";

    public static string Revoked =>
        "Access for this computer was revoked. Please contact Utkal Technologies.";

    public static string Deactivated =>
        "Your account is deactivated. Please contact Utkal Technologies.";

    public static string Suspended =>
        "Your account is suspended. Please contact Utkal Technologies.";

    public static string Inactive =>
        "Your account is inactive. Please contact Utkal Technologies.";

    public static string NoLicense =>
        "No active subscription found. Please contact Utkal Technologies.";

    public static string SerialMismatch =>
        "This computer is not linked to your account. Please contact Utkal Technologies.";

    public static string ApplySubmitted =>
        "Application submitted successfully. Utkal Technologies will review and activate your access.";

    public static string ApplyAlreadyPending =>
        "This laptop already applied. Your request is pending approval — you cannot apply again. Contact Utkal Technologies for status.";

    public static string ApplyAlreadyApproved =>
        "This laptop is already registered and activated. You cannot apply again. Sign in with your Login ID, or contact Utkal Technologies.";

    public static string ApplyAlreadyRejected =>
        "This laptop already applied and was not approved. You cannot apply again. Contact Utkal Technologies.";

    public static string ApplyAlreadyDeactivated =>
        "This laptop was deactivated. You cannot apply again. Contact Utkal Technologies to re-activate.";

    public static string ApplyAlreadyRevoked =>
        "Access for this laptop was revoked. You cannot apply again. Contact Utkal Technologies.";

    public static string ApplyAlreadyExists =>
        "This laptop serial is already registered. Only one application per computer is allowed. Contact Utkal Technologies.";
}
