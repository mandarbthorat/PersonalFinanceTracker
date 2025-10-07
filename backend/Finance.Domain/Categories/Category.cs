using System;
using System.Collections.Generic;
using System.Text;

namespace Finance.Domain.Categories
{
    public class Category
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Name { get; set; } = default!;
        public bool IsIncome { get; set; }
        public bool IsArchived { get; set; }
    }

}
