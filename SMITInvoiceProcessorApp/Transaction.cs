using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMITInvoiceProcessorApp
{
    public class Transaction
    {
        public string OwnersCorporation { get; set; }
        public string DRItemType { get; set; }
        public string DRItem { get; set; }
        public string GSTAmount { get; set; }
        public string Description { get; set; }
        public string DateEntered { get; set; }
        public string Suppress { get; set; }
        public string DueDate { get; set; }
        public string RefNumber { get; set; }
        public string DRAccount { get; set; }
        public string CRAccount { get; set; }
        public string CRItem { get; set; }
        public string CRItemType { get; set; }
        public string Amount { get; set; }
        public string TranDate { get; set; }
        public string TranType { get; set; }
        public string Status { get; set; }
        public string BPAYCRN { get; set; }
    }
}
