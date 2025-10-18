// ViewModels/Auth/AssignRoleVM.cs
namespace dortageDB.ViewModels
{
    public class AssignRoleVM
    {
        public int UserId { get; set; }
        public IList<string> SelectedRoles { get; set; } = new List<string>();
        public IList<string> AllRoles { get; set; } = new List<string>();
    }
}