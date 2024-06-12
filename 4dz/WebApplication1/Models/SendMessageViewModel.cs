using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class SendMessageViewModel
    {
        [Required]
        public string RecipientLogin { get; set; }

        [Required]
        public string MessageTitle { get; set; }

        [Required]
        public string MessageText { get; set; }
    }
}
