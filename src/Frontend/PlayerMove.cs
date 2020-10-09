using System.ComponentModel.DataAnnotations;

namespace Frontend
{
    public class PlayerMove
    {
        [Required]
        public UserInfo Player { get; set; }

        public Shape Move { get; set; }
    }
}