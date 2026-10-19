using System;
using System.Collections.Generic;
using System.Text;

namespace Hibabejelento.Maui.Models
{
    public class TicketCreation
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string AssetIdentifier { get; set; }
        public int CategoryId { get; set; }
    }
}
