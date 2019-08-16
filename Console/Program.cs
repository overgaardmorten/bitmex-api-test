using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ConsoleApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Create instance of program and start getting strem from BitMex
            new Program().Start();
        }

        private void Start()
        {
            using (var clientWs = new ClientWebSocket())
            {
                // Connect to WebSocket and subscribe for 2 instruments
                clientWs.ConnectAsync(
                    new Uri("wss://testnet.bitmex.com/realtime?subscribe=instrument:XBTUSD"),
                    CancellationToken.None).Wait();


                // Subscribe to OrderBookL2_25 for XBTUSD after 10 seconds --> Orderbook data for XBTUSD printed to Console
                SendCommand(clientWs, "{\"op\": \"subscribe\", \"args\": [\"orderBookL2_25:XBTUSD\"]}", 10);

                // Subscribe to OrderBookL2_25 for ETHUSD after 20 seconds --> Orderbook data for ETHUSD printed to Console
                SendCommand(clientWs, "{\"op\": \"subscribe\", \"args\": [\"orderBookL2_25:ETHUSD\"]}", 20);

                // Loop forever and print all messages received from BitMex WebSocket
                foreach (var res in ReceiveFullMessage(clientWs, new CancellationToken()))
                {
                    var receivedMessage = Encoding.UTF8.GetString(res.Item2.ToArray());
                    var jobj = JObject.Parse(receivedMessage);

                    Debug.WriteLine(receivedMessage);
                }
            }
        }


        private IEnumerable<(WebSocketReceiveResult, IEnumerable<byte>)> ReceiveFullMessage(
            WebSocket socket, CancellationToken cancelToken)
        {
            WebSocketReceiveResult response;

            var message = new List<byte>();

            // Just keep looping forever.
            while (true)
            {
                message.Clear();
                var buffer = new byte[4096];

                do
                {
                    response = socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancelToken).Result;
                    message.AddRange(new ArraySegment<byte>(buffer, 0, response.Count));
                } while (!response.EndOfMessage);


                yield return (response, message);
            }
        }

        private void SendCommand(WebSocket socket, string command, int delayInSeconds)
        {
            var bytes = Encoding.UTF8.GetBytes(command);

            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(delayInSeconds), CancellationToken.None);
                socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None)
                    .Wait();
            });
        }
    }
}