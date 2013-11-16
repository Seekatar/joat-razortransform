using System;
using System.Text;

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
