using System;
using System.Collections.Generic;
using System.Text;

namespace Hibabejelento.Maui.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TicketId { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
