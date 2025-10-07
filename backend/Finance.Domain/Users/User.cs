using System;
using System.Collections.Generic;
using System.Text;

namespace Finance.Domain.Users
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
    }
}
