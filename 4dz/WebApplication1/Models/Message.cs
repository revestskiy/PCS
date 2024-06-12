using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Message
    {
        public int Id { get; set; }

        public int FromUserId { get; set; }
        public User FromUser { get; set; }

        public int ToUserId { get; set; }
        public User ToUser { get; set; }

        [Required]
        public string Title { get; set; }

        public string Text { get; set; }

        public DateTime SendDate { get; set; }

        [Required]
        public string Status { get; set; }
    }
}
