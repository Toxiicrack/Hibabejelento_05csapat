using System;
using System.Collections.Generic;
using System.Text;

namespace Hibabejelento.Maui.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public bool IsAdmin { get; set; }
        public int GroupId { get; set; }
        public bool IsActive { get; set; }
        public string RegistrationStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ApprovedAt { get; set; }
        public DateTime LastLoginAt { get; set; }
    }
}
