﻿// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2017 Jaben Cargman
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//  
// http://www.apache.org/licenses/LICENSE-2.0
//  
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License. 

namespace Papercut.Network
{
    using System;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Newtonsoft.Json;
    using Papercut.Common.Domain;
    using Papercut.Common.Extensions;
    using Papercut.Core.Infrastructure.Json;
    using Papercut.Network.Protocols;

    using Serilog;

    public class PapercutClient : IDisposable
    {
        public const string Localhost = "127.0.0.1";

        public const int ClientPort = 37402;

        public const int ServerPort = 37403;

        readonly ILogger _logger;

        static readonly JsonSerializerSettings _clientJsonSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Include,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
        };

        public PapercutClient(ILogger logger)
        {
            Client = new TcpClient();

            _logger = logger;
            Host = Localhost;
            Port = ClientPort;
        }

        public string Host { get; set; }

        public int Port { get; set; }

        public TcpClient Client { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Client != null)
                {
                    Client.Close();
                    Client = null;
                }
            }
        }

        public async Task<TEvent> ExchangeEventServer<TEvent>(TEvent @event) where TEvent : class, IEvent
        {
            try
            {
                Client.Connect(Host, Port);
            }
            catch (SocketException)
            {
                // no listener
                return null;
            }

            try
            {
                using (var stream = Client.GetStream())
                {
                    _logger.Debug("Exchanging {@Event} with Remote", @event);

                    var isSuccessful = await HandlePublishEvent(stream, @event, ProtocolCommandType.Exchange);

                    TEvent returnEvent = null;

                    if (isSuccessful)
                    {
                        var response = (await stream.ReadString()).Trim();
                        if (response == "REPLY")
                        {
                            // get exchanged event
                            returnEvent = (await stream.ReadString()).FromJson<TEvent>();
                        }
                    }

                    stream.Flush();

                    return returnEvent;
                }
            }
            finally
            {
                Client.Close();
            }
        }

        public async Task<bool> PublishEventServer<TEvent>(TEvent @event) where TEvent : IEvent
        {
            try
            {
                Client.Connect(Host, Port);
            }
            catch (SocketException)
            {
                // no listener
                return false;
            }

            try
            {
                using (var stream = Client.GetStream())
                {
                    _logger.Debug("Publishing {@Event} to Remote", @event);

                    var isSuccessful = await HandlePublishEvent(stream, @event, ProtocolCommandType.Publish);

                    stream.Flush();

                    return isSuccessful;
                }
            }
            finally
            {
                Client.Close();
            }
        }

        async Task<bool> HandlePublishEvent<TEvent>(
            NetworkStream stream,
            TEvent @event,
            ProtocolCommandType protocolCommandType) where TEvent : IEvent
        {
            string response = (await stream.ReadString()).Trim();

            if (response != "PAPERCUT") return false;

            _logger.Debug("Publishing {@Event} to Remote", @event);

            var eventJsonBytes = Encoding.UTF8.GetBytes(this.ToJson(@event));

            await stream.WriteLine(
                ToJson(
                    new PapercutProtocolRequest
                    {
                        CommandType = protocolCommandType,
                        Type = @event.GetType(),
                        ByteSize = eventJsonBytes.Length
                    }));

            response = (await stream.ReadString()).Trim();

            if (response == "ACK") await stream.WriteBytes(eventJsonBytes);

            return true;
        }

        private string ToJson<TObject>(TObject @object)
        {
            return
                JsonConvert.SerializeObject(@object, Formatting.None, _clientJsonSettings);
        }
    }
}