using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMITInvoiceProcessorApp
{
    public class SMITFileUploadLog
    {
        public long ID { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string Status { get; set; }
        public string FileDetailsJson { get; set; }
        public string ErrorMessage { get; set; }
        public string UploadedFrom { get; set; }
        public string AddedBy { get; set; }
        public DateTime DateAdded { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? DateUpdated { get; set; }
        public string DeletedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DateDeleted { get; set; }
    }
    public class DbResult
    {
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; }
    }
}
