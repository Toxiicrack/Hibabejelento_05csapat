using System;
using System.Collections.Generic;
using System.Text;

namespace Hibabejelento.Maui.Models
{
    public class TicketStatusHistory
    {
        public int Id{ get; set; }
        public int TicketId { get; set; }
        public int PreviousStatusId { get; set; }
        public int NewStatusId { get; set; }
        public int ChangedByUserId { get; set; }
        public string Reason { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}
