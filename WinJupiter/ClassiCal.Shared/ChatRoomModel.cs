using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Web;

namespace ClassiCal
{
    public class ChatRoomModel
    {
        private string _classID;
        private string _username;
        private DataContractJsonSerializer _chatContentSerializer =
                                          new DataContractJsonSerializer(typeof(ChatContent));
        private MessageWebSocket _websocket;
        private DataWriter _websocketWriter;
        private Object _lockMessageToSend = new Object();
        private List<ChatContent> _messagesToSend = new List<ChatContent>();

        public event EventHandler<ChatRoomMessageEventArgs> MessageArrived;
        public event EventHandler<ChatRoomMessageEventArgs> MessageSent;
        public event EventHandler<ChatRoomMessageEventArgs> MessageFailedToSend;
        public event EventHandler<ChatRoomConnectionStateChangedArgs> ServerConnectionStateChanged;

        public ConnectionState State { get; private set; }

        public enum ConnectionState
        {
            Disconnected,
            Connecting,
            Connected
        }

        public ChatRoomModel(string classID, string username)
        {
            _classID = classID;
            _username = username;
            State = ConnectionState.Disconnected;
        }

        private void ChangeConnectionState(ConnectionState state)
        {
            State = state;
            RaiseEvent(ServerConnectionStateChanged, new ChatRoomConnectionStateChangedArgs(state));
        }

        public async Task Connect()
        {
            try
            {
                // Make a local copy to avoid races with Closed events.
                MessageWebSocket webSocket = _websocket;
                // Have we connected yet?
                if (webSocket == null)
                {
                    ChangeConnectionState(ConnectionState.Connecting);
                    Uri server = new Uri(Constants.GetChatroomAddr(_username, _classID));

                    webSocket = new MessageWebSocket();
                    webSocket.Control.MessageType = SocketMessageType.Utf8;

                    webSocket.MessageReceived += webSocket_MessageReceived;
                    webSocket.Closed += webSocket_Closed;

                    await webSocket.ConnectAsync(server);
                    _websocket = webSocket;
                    _websocketWriter = new DataWriter(webSocket.OutputStream);
                    ChangeConnectionState(ConnectionState.Connected);
                    await SendMessagesQueue();
                }
            }
            catch (Exception ex) // For debugging
            {
                WebErrorStatus status = WebSocketError.GetStatus(ex.GetBaseException().HResult);
                // Add your specific error-handling code here.
                _websocket = null;
                ChangeConnectionState(ConnectionState.Disconnected);
            }
        }

        public void Disconnect()
        {
            if (_websocket != null)
                _websocket.Close(1000, "Client initiated close");
        }

        void webSocket_Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            _websocket = null;
            ChangeConnectionState(ConnectionState.Disconnected);
        }

        void webSocket_MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            if (args.MessageType == SocketMessageType.Utf8)
            {
                try
                {
                    using (DataReader reader = args.GetDataReader())
                    {
                        reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                        string json = reader.ReadString(reader.UnconsumedBufferLength);
                        MemoryStream stream = new MemoryStream();
                        StreamWriter writer = new StreamWriter(stream);
                        writer.Write(json);
                        writer.Flush();
                        stream.Position = 0;

                        ChatContent chatcontent = (ChatContent)_chatContentSerializer.ReadObject(stream);
                        RaiseEvent(MessageArrived, new ChatRoomMessageEventArgs(chatcontent));
                    }
                }
                catch (Exception ex) // For debugging
                {
                    WebErrorStatus status = WebSocketError.GetStatus(ex.GetBaseException().HResult);
                    _websocket = null;
                    ChangeConnectionState(ConnectionState.Disconnected);
                }
            }
        }

        private async Task SendMessagesQueue()
        {
            while (_messagesToSend.Count != 0)
            {
                if (State != ConnectionState.Connected)
                    return;

                ChatContent chatContent;
                lock (_lockMessageToSend)
                {
                    chatContent = _messagesToSend[0];
                    _messagesToSend.Remove(chatContent);
                }
                await SendMessageSocket(chatContent);
            }
        }

        public async Task SendMessage(ChatContent chatContent)
        {
            lock (_lockMessageToSend)
            {
                _messagesToSend.Add(chatContent);
            }
            await SendMessagesQueue();
        }

        private async Task SendMessageSocket(ChatContent chatContent)
        {
            bool messageSent = false;
            string json;

            // ChatContent to JSON string
            using (MemoryStream memStm = new MemoryStream())
            {
                _chatContentSerializer.WriteObject(memStm, chatContent);

                memStm.Seek(0, SeekOrigin.Begin);

                using (var streamReader = new StreamReader(memStm))
                {
                    json = streamReader.ReadToEnd();
                }
            }

            try
            {
                _websocketWriter.WriteString(json);
                await _websocketWriter.StoreAsync();
                messageSent = true;
            }
            catch (AggregateException)
            {
                messageSent = false;
            }
            chatContent.Sent = messageSent;
            chatContent.SendFailed = !messageSent;

            if (messageSent)
            {
                RaiseEvent(MessageSent, new ChatRoomMessageEventArgs(chatContent));
            }
            else
            {
                RaiseEvent(MessageFailedToSend, new ChatRoomMessageEventArgs(chatContent));
            }
        }

        private void RaiseEvent<TEventArgs>(EventHandler<TEventArgs> handler, TEventArgs arg)
            where TEventArgs : EventArgs
        {
            if (handler != null)
            {
                handler(this, arg);
            }
        }
    }

    public class ChatRoomMessageEventArgs : EventArgs
    {
        public ChatContent Content { get; private set; }

        public ChatRoomMessageEventArgs(ChatContent content = null)
        {
            Content = content;
        }
    }

    public class ChatRoomConnectionStateChangedArgs : EventArgs
    {
        public ChatRoomModel.ConnectionState State { get; private set; }

        public ChatRoomConnectionStateChangedArgs(ChatRoomModel.ConnectionState state)
        {
            State = state;
        }
    }
}
