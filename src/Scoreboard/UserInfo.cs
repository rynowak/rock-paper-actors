using System.ComponentModel.DataAnnotations;

namespace Scoreboard
{
    public class UserInfo
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }
    }
}