using System.Net;
using System.Net.Sockets;
using PostSharp.Patterns.Model;

namespace AppliedMotion.Stepper
{
    [NotifyPropertyChanged]
    public partial class StepperController
    {
        #region Structs

        public struct UdpState
        {
            #region Fields

            public IPEndPoint IpEndPoint;
            public UdpClient udpState;

            #endregion Fields
        }

        #endregion Structs
    }
}