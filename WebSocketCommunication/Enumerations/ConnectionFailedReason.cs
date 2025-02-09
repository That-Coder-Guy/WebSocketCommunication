using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketCommunication.Enumerations
{
    /// <summary>
    /// Specifies the reason why a connection attempt failed.
    /// </summary>
    public enum ConnectionFailedReason
    {
        /// <summary>
        /// The connection attempt timed out.
        /// </summary>
        Timeout,

        /// <summary>
        /// No internet connection was detected.
        /// </summary>
        NoInternet,

        /// <summary>
        /// The target server or host was unreachable.
        /// </summary>
        Unreachable,

        /// <summary>
        /// The connection was actively refused by the server.
        /// </summary>
        Refused,

        /// <summary>
        /// The reason for failure is unknown.
        /// </summary>
        Unknown
    }

}
