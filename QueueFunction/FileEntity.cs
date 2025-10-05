using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueueFunction
{
    class FileEntity
    {
        public string? FileName { get; set; }
        public long Size { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public string DisplaySize { get; set; }
    }
}
