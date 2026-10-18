using System;
using System.Collections.Generic;
using System.Text;

namespace Hibabejelento.Maui.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ProblemType { get; set; }
        public string Location { get; set; }
        public string AssetIdentifier { get; set; }
        public int CategoryId { get; set; }
        public int Priority { get; set; }
        public int StatusId { get; set; }
        public int ReporterId { get; set; }
        public int GroupId { get; set; }
        public int AssignedUserId { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime ClosedAt { get; set; }
    }
}
