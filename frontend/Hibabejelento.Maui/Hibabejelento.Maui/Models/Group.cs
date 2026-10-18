using System;
using System.Collections.Generic;
using System.Text;

namespace Hibabejelento.Maui.Models
{
    public class Group
    {
        public int Id{ get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
