using PostSharp.Patterns.Model;
using System.Net;
using System.Net.Sockets;

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