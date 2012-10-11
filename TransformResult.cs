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
            TranformOk = false;
            Elapsed = TimeSpan.FromSeconds(0);
            Count = 0;
        }
        public bool TranformOk { get; set; }
        public TimeSpan Elapsed { get; set; }
        public int Count { get; set; }
    }
}
