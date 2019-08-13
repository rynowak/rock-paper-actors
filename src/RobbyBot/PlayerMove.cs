using System.ComponentModel.DataAnnotations;

namespace RobbyBot
{
    public class PlayerMove
    {
        [Required]
        public UserInfo Player { get; set; }

        public Shape Move { get; set; }
    }
}