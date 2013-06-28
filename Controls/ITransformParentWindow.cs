using System.Collections.Generic;
using RtPsHost;

namespace RazorTransform
{
    /// <summary>
    /// passed into control to let it talk to parent
    /// </summary>
    public interface ITransformParentWindow
    {
        /// <summary>
        /// called after init to give a suffix to title bar 
        /// </summary>
        /// <param name="titleSuffix"></param>
        void SetTitle(string titleSuffix);

        /// <summary>
        /// called after the transform is complete
        /// </summary>
        /// Parent should close on canceld
        /// <param name="results"></param>
        void ProcessingComplete(ProcessingResult results);
		
        /// <summary>
        /// send data to parent
        /// </summary>
        /// <param name="data"></param>
        void SendData(Dictionary<string, string> data);
    }
}
