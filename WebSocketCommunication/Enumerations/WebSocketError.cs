using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketCommunication.Enumerations
{
    public enum WebSocketError
    {
        //
        // Summary:
        //     Indicates that there was no native error information for the exception.
        Success = 0,
        //
        // Summary:
        //     Indicates that a WebSocket frame with an unknown opcode was received.
        InvalidMessageType = 1,
        //
        // Summary:
        //     Indicates a general error.
        Faulted = 2,
        //
        // Summary:
        //     Indicates that an unknown native error occurred.
        NativeError = 3,
        //
        // Summary:
        //     Indicates that the incoming request was not a valid websocket request.
        NotAWebSocket = 4,
        //
        // Summary:
        //     Indicates that the client requested an unsupported version of the WebSocket protocol.
        UnsupportedVersion = 5,
        //
        // Summary:
        //     Indicates that the client requested an unsupported WebSocket subprotocol.
        UnsupportedProtocol = 6,
        //
        // Summary:
        //     Indicates an error occurred when parsing the HTTP headers during the opening
        //     handshake.
        HeaderError = 7,
        //
        // Summary:
        //     Indicates that the connection was terminated unexpectedly.
        ConnectionClosedPrematurely = 8,
        //
        // Summary:
        //     Indicates the WebSocket is an invalid state for the given operation (such as
        //     being closed or aborted).
        InvalidState = 9
    }
}
