using System.ComponentModel.DataAnnotations;

namespace Rochambot
{
    public class PlayerMove
    {
        [Required]
        public UserInfo Player { get; set; }

        public Shape Move { get; set; }
    }
}