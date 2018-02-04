using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Test
{
    [Table("Users")]
    public class User
    {
        private readonly string[] _roles = { "Admin", "User", "Moderator" };

        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Role { get; set; }

        public string Address { get; set; }

        public User()
        {
            SetData();
        }

        public void SetData()
        {
            Name = RandomUtil.GetRandomString(10);
            Address = RandomUtil.GetRandomString(100);
            Email = RandomUtil.GetRandomString(10) + "@" + RandomUtil.GetRandomString(5) + ".com";
            Role = _roles[RandomUtil.GetRandomNumber(0, 2)];
        }

        public override string ToString()
        {
            return "User:\n" +
                   $"Id={Id}\n" +
                   $"Name={Name}\n" +
                   $"Email={Email}\n" +
                   $"Role={Role}\n";
        }
    }
}
