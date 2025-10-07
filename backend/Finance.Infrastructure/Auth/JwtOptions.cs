using System;
using System.Collections.Generic;
using System.Text;

namespace Finance.Infrastructure.Auth
{
    public class JwtOptions
    {
        public string Issuer { get; set; } = default!;
        public string Audience { get; set; } = default!;
        public string Key { get; set; } = default!; // symmetric
    }

}
