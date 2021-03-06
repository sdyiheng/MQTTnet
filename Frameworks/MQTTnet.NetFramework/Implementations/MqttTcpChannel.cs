using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Client;
using System.IO;

namespace MQTTnet.Implementations
{
    public sealed class MqttTcpChannel : IMqttCommunicationChannel, IDisposable
    {
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public static int BufferSize { get; set; } = 4096 * 20; // Can be changed for fine tuning by library user.

        private Socket _socket;
        private SslStream _sslStream;

        /// <summary>
        /// called on client sockets are created in connect
        /// </summary>
        public MqttTcpChannel()
        {

        }

        /// <summary>
        /// called on server, sockets are passed in
        /// connect will not be called
        /// </summary>
        public MqttTcpChannel(Socket socket, SslStream sslStream)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _sslStream = sslStream;
            CreateStreams(socket, sslStream);
        }

        public Stream SendStream { get; private set; }
        public Stream ReceiveStream { get; private set; }
        public Stream RawReceiveStream { get; private set; }

        public async Task ConnectAsync(MqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (_socket == null)
            {
                _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            }

            await Task.Factory.FromAsync(_socket.BeginConnect, _socket.EndConnect, options.Server, options.GetPort(), null).ConfigureAwait(false);

            if (options.TlsOptions.UseTls)
            {
                _sslStream = new SslStream(new NetworkStream(_socket, true));

                await _sslStream.AuthenticateAsClientAsync(options.Server, LoadCertificates(options), SslProtocols.Tls12, options.TlsOptions.CheckCertificateRevocation).ConfigureAwait(false);
            }

            CreateStreams(_socket, _sslStream);
        }

        public Task DisconnectAsync()
        {
            Dispose();
            return Task.FromResult(0);
        }

        public void Dispose()
        {
            RawReceiveStream?.Dispose();
            RawReceiveStream = null;

            ReceiveStream?.Dispose();
            ReceiveStream = null;

            SendStream?.Dispose();
            SendStream = null;

            _socket?.Dispose();
            _socket = null;

            _sslStream?.Dispose();
            _sslStream = null;
        }

        private void CreateStreams(Socket socket, Stream sslStream)
        {
            RawReceiveStream = sslStream ?? new NetworkStream(socket);

            //cannot use this as default buffering prevents from receiving the first connect message
            //need two streams otherwise read and write have to be synchronized
            SendStream = new BufferedStream(RawReceiveStream, BufferSize);
            ReceiveStream = new BufferedStream(RawReceiveStream, BufferSize);
        }

        private static X509CertificateCollection LoadCertificates(MqttClientOptions options)
        {
            var certificates = new X509CertificateCollection();
            if (options.TlsOptions.Certificates == null)
            {
                return certificates;
            }

            foreach (var certificate in options.TlsOptions.Certificates)
            {
                certificates.Add(new X509Certificate(certificate));
            }

            return certificates;
        }
    }
}