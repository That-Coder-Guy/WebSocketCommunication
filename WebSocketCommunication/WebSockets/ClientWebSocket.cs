﻿using WebSocketException = System.Net.WebSockets.WebSocketException;
using SystemClientWebSocket = System.Net.WebSockets.ClientWebSocket;
using System.Diagnostics;

namespace WebSocketCommunication
{
    /// <summary>
    /// Represents a client-side WebSocket connection to a server.
    /// </summary>
    public class ClientWebSocket : WebSocket<SystemClientWebSocket>
    {
        #region Fields
        /// <summary>
        /// The URI of the server to which the client will connect.
        /// </summary>
        private Uri _serverUrl;

        /// <summary>
        /// A semaphore that ensures only one connection attempt is in progress at any given time.
        /// </summary>
        private SemaphoreSlim _connectionLock = new(1, 1);
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the underlying .NET ClientWebSocket instance.
        /// </summary>
        protected override SystemClientWebSocket InnerWebSocket { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientWebSocket"/> class.
        /// </summary>
        /// <param name="url">The URL of the server to connect to.</param>
        public ClientWebSocket(string url)
        {
            _serverUrl = new Uri(url);
            InnerWebSocket = new SystemClientWebSocket();
        }

        /// <summary>
        /// Asynchronously initiates a connection to the WebSocket server.
        /// </summary>
        /// <param name="token">A cancellation token to monitor while waiting for the connection to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous connection operation. The task result is a <see cref="WebSocketError"/>
        /// indicating the outcome of the connection attempt.
        /// </returns>
        private async Task<WebSocketError> ConnectAsync(CancellationToken token)
        {
            try
            {
                // Try to establish the connection to the WebSocket server using the provided cancellation token.
                await InnerWebSocket.ConnectAsync(_serverUrl, token);

                // Mark the WebSocket as connected.
                _isConnected = true;
            }
            catch (OperationCanceledException)
            {
                Debug.Print("(The above debug message indicates a timed out connection attempt)");
                // If the connection attempt is canceled (e.g., due to a timeout),
                // reset the InnerWebSocket instance and return a Timeout error.
                InnerWebSocket = new SystemClientWebSocket();
                return WebSocketError.Timeout;
            }
            catch (WebSocketException webSocketExc)
            {
                Debug.Print("(The above debug message indicates a failed connection attempt)");
                // If a WebSocket-specific exception occurs during the connection attempt,
                // reset the InnerWebSocket instance and return the error code associated with the exception.
                InnerWebSocket = new SystemClientWebSocket();
                return (WebSocketError)webSocketExc.ErrorCode;
            }
            catch (Exception exc)
            {
                // For any other exception that occurs during the connection attempt,
                // reset the InnerWebSocket instance, log the exception details,
                // and return a general NativeError code.
                InnerWebSocket = new SystemClientWebSocket();
                Debug.Print($"Irragulare exception caught during connection attempt: {exc.Message}");
                return WebSocketError.NativeError;
            }
            // If no exceptions occurred, the connection was successful.
            return WebSocketError.Success;
        }

        /// <summary>
        /// Attempts to establish a WebSocket connection within a specified timeout period.
        /// </summary>
        /// <param name="timeout">The maximum time (in milliseconds) allowed for the connection attempt.</param>
        /// <returns>A task representing the asynchronous connection attempt.</returns>
        public async Task AttemptConnectAsync(int timeout)
        {
            // Check if the WebSocket is closed .
            // If it is, then we don't need to attempt another connection.
            if (_isClosed)
            {
                RaiseConnectionFailedEvent(new ConnectionFailedEventArgs(WebSocketError.InvalidState));
                return;
            }

            // Check if the WebSocket is already connected.
            // If it is, then we don't need to attempt another connection.
            if (_isConnected)
            {
                Debug.Print("Failed during connection process because client is already connected.");
                RaiseConnectionFailedEvent(new ConnectionFailedEventArgs(WebSocketError.AlreadyConnected));
                return;
            }

            // Attempt to acquire the connection lock immediately.
            // If the lock isn't available, it means there's an active connection attempt already in progress.
            if (!_connectionLock.Wait(0))
            {
                Debug.Print("Failed during connection process because the connection lock was taken.");
                RaiseConnectionFailedEvent(new ConnectionFailedEventArgs(WebSocketError.AlreadyConnecting));
                return;
            }

            // Create a cancellation token source for the connection attempt.
            CancellationTokenSource connectionTokenSource = new CancellationTokenSource();

            // Begin the asynchronous connection attempt.
            Task<WebSocketError> connectionTask = ConnectAsync(connectionTokenSource.Token);

            // Create a separate cancellation token source for the timeout delay.
            CancellationTokenSource timeoutTokenSource = new CancellationTokenSource();

            // Begin the timeout task.
            Task timeoutTask = Task.Delay(timeout, timeoutTokenSource.Token);

            // Wait for either the connection attempt or the timeout task to complete.
            Task completedTask = await Task.WhenAny(timeoutTask, connectionTask);

            // If the timeout task completed first, cancel the connection attempt.
            // Otherwise, cancel the timeout delay task.
            if (completedTask == timeoutTask)
            {
                connectionTokenSource.Cancel();
            }
            else
            {
                timeoutTokenSource.Cancel();
            }

            // Release the connection lock regardless of which task completed first.
            _connectionLock.Release();

            // Retrieve the connection result.
            WebSocketError error = connectionTask.Result;

            // Handle the connection result.
            switch (error)
            {
                case WebSocketError.Success:
                    // On a successful connection, raise the connected event and begin listening for messages.
                    RaiseConnectedEvent();
                    await ListenAsync();
                    break;

                default:
                    // On failure, raise a connection failed event with details.
                    RaiseConnectionFailedEvent(new ConnectionFailedEventArgs(error));
                    break;
            }
        }

        /// <summary>
        /// Initiates a WebSocket connection attempt with the specified timeout.
        /// </summary>
        /// <param name="timeout">The maximum time (in milliseconds) to wait for a successful connection.</param>
        public void Connect(int timeout)
        {
            // Run the connection attempt asynchronously.
            Task.Run(() => AttemptConnectAsync(timeout));
        }
        #endregion
    }
}
