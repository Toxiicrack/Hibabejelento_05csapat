using System;
using System.Collections.Generic;
using System.Text;

namespace Hibabejelento.Maui.Models
{
    public class TicketReopening
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int UserId { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
