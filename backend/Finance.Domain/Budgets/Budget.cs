using System;
using System.Collections.Generic;
using System.Text;

namespace Finance.Domain.Budgets
{
    public class Budget
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid CategoryId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }  // 1..12
        public decimal Amount { get; set; }
    }
}
