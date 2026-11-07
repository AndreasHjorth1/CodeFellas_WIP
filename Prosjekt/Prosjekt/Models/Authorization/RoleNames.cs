namespace Prosjekt.Models.Authorization
{
    public class RoleNames
    {
        public const string Admin = "Admin";
        public const string Coordinator = "Coordinator";
        public const string AdminOrCoordinator = Admin + "," + Coordinator;
    }
}
