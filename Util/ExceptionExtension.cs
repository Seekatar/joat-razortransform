using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazorTransform
{
    public static class ExceptionExtension
    {
        static public bool ShowStack { get; set; }

        static public string BuildMessage( this Exception e )
        {
            var msg = new StringBuilder();
            if (ShowStack)
            {
                msg.AppendLine(e.ToString());
            }
            else
            {
                msg.AppendLine(e.Message);
                while (e.InnerException != null)
                {
                    msg.AppendLine(e.InnerException.Message);
                    e = e.InnerException;
                }
            }

            return msg.ToString();
        }
    }
}
