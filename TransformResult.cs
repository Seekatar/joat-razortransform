using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazorTransform
{
    internal class TransformResult
    {
        public TransformResult()
        {
            TranformResult = ProcessingResult.failed;
            Elapsed = TimeSpan.FromSeconds(0);
            Count = 0;
        }
        public ProcessingResult TranformResult { get; set; }
        public TimeSpan Elapsed { get; set; }
        public int Count { get; set; }
    }
}
