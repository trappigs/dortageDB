using System.ComponentModel.DataAnnotations;

namespace dortageDB.Entities
{
    public class Role
    {
        public int Id { get; set; }
        [Required, StringLength(50)] public string Name { get; set; } = null!;
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
