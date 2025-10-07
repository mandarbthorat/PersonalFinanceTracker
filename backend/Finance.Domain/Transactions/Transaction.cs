using System;
using System.Collections.Generic;
using System.Text;

namespace Finance.Domain.Transactions
{
    public enum TransactionType { Income = 1, Expense = 2 }

    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid CategoryId { get; set; }
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public DateTime OccurredOn { get; set; }
        public string? Note { get; set; }
    }
}
