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

        public event EventHandler<ChatRoomMessageEventArgs> MessageArrived;
        public event EventHandler<ChatRoomMessageEventArgs> MessageSent;
        public event EventHandler<ChatRoomMessageEventArgs> MessageFailedToSend;

        public ChatRoomModel(string classID, string username)
        {
            _classID = classID;
            _username = username;
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
                    Uri server = new Uri(Constants.GetChatroomAddr(_username, _classID));

                    webSocket = new MessageWebSocket();
                    // MessageWebSocket supports both utf8 and binary messages.
                    // When utf8 is specified as the messageType, then the developer
                    // promises to only send utf8-encoded data.
                    webSocket.Control.MessageType = SocketMessageType.Utf8;
                    // Set up callbacks
                    webSocket.MessageReceived += webSocket_MessageReceived;
                    webSocket.Closed += webSocket_Closed;

                    await webSocket.ConnectAsync(server);
                    _websocket = webSocket; // Only store it after successfully connecting.
                    _websocketWriter = new DataWriter(webSocket.OutputStream);
                }
            }
            catch (Exception ex) // For debugging
            {
                WebErrorStatus status = WebSocketError.GetStatus(ex.GetBaseException().HResult);
                // Add your specific error-handling code here.
            }
        }

        void webSocket_Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            throw new NotImplementedException();
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
                    // Add your specific error-handling code here.
                }
            }
        }

        public async Task SendMessage(ChatContent chatContent)
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

        private void RaiseEvent(EventHandler<ChatRoomMessageEventArgs> handler, ChatRoomMessageEventArgs arg)
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
}
