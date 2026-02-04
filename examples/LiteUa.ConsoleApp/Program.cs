using LiteUa.BuiltIn;
using LiteUa.Client;
using LiteUa.Security;
using LiteUa.Security.Policies;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;

namespace LiteUa.ConsoleApp
{
    internal class Program : IAsyncDisposable
    {
        private static UaClient? _client;

        private static async Task Main()
        {
            Console.WriteLine("=== LiteUa OPC UA Console Client ===");

            // 1. Connection Configuration
            _client = await ConfigureAndConnect();

            if (_client == null) return;

            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("\n--- Main Menu ---");
                Console.WriteLine("1. Browse Node");
                Console.WriteLine("2. Read Node (Scalar/Array)");
                Console.WriteLine("3. Write Node (Int16 Scalar)");
                Console.WriteLine("4. Subscribe to Node");
                Console.WriteLine("5. Call Method (Add Example)");
                Console.WriteLine("6. Exit");
                Console.Write("Select an option: ");

                var choice = Console.ReadLine();
                try
                {
                    switch (choice)
                    {
                        case "1": await BrowseMenu(); break;
                        case "2": await ReadMenu(); break;
                        case "3": await WriteMenu(); break;
                        case "4": await SubscribeMenu(); break;
                        case "5": await CallMethodMenu(); break;
                        case "6": exit = true; break;
                        default: Console.WriteLine("Invalid choice."); break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            await _client.DisposeAsync();
        }

        private static async Task<UaClient> ConfigureAndConnect()
        {
            Console.Write("Enter Server URL (default: opc.tcp://192.178.0.1:4840/): ");
            string url = Console.ReadLine() ?? "opc.tcp://192.178.0.1:4840/";

            Console.WriteLine("Select Security Policy: 0=None, 1=Basic256Sha256");
            var policy = Console.ReadLine() == "1" ? SecurityPolicyType.Basic256Sha256 : SecurityPolicyType.None;

            Console.WriteLine("Select Message Security Mode: 0=None, 1=Sign, 2=SignAndEncrypt");
            var modeChoice = Console.ReadLine();
            var mode = modeChoice switch { "1" => MessageSecurityMode.Sign, "2" => MessageSecurityMode.SignAndEncrypt, _ => MessageSecurityMode.None };

            Console.WriteLine("Select Identity: 0=Anonymous, 1=Username");
            bool isUser = Console.ReadLine() == "1";
            string user = "", pass = "";
            if (isUser)
            {
                Console.Write("Username: "); user = Console.ReadLine() ?? "";
                Console.Write("Password: "); pass = Console.ReadLine() ?? "";
            }

            var builder = UaClient.Create()
                .ForEndpoint(url)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.PolicyType = policy;
                    s.MessageSecurityMode = mode;
                    s.UserTokenType = isUser ? UserTokenType.Username : UserTokenType.Anonymous;
                    if (isUser)
                    {
                        s.Username = user;
                        s.Password = pass;
                        s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    }

                    s.ClientCertificate = CertificateFactory.CreateSelfSignedCertificate("LiteUa Client", "urn:LiteUa:client");
                })
                .WithSession(s => { s.ApplicationName = "LiteUa Console App"; })
                .WithPool(p => { p.MaxSize = 2; })
                .WithTransportLimits(t =>
                {
                    t.SupervisorIntervalMs = 1000;
                    t.HeartbeatIntervalMs = 1000;
                    t.HeartbeatTimeoutHintMs = 2000;
                    t.MinPublishTimeoutMs = 400;
                    t.PublishTimeoutMultiplier = 0;
                    t.ReconnectIntervalMs = 1000;
                    t.MaxPublishRequestCount = 3;
                });

            var client = builder.Build();
            Console.WriteLine("Connecting...");
            await client.ConnectAsync();
            Console.WriteLine("Connected successfully!");
            return client;
        }

        private static async Task BrowseMenu()
        {
            Console.Write("Enter NodeId to browse (e.g. ns=0;i=2253 for Server): ");
            var nodeStr = Console.ReadLine() ?? "ns=0;i=2253";
            var nodeId = NodeId.Parse(nodeStr);

            var results = await _client!.BrowseNodesAsync([nodeId]);
            if (results != null && results[0] != null)
            {
                Console.WriteLine($"Found {results[0].Length} references:");
                foreach (var desc in results[0])
                {
                    Console.WriteLine($" - {desc.BrowseName}: {desc.NodeId} ({desc.NodeClass})");
                }
            }
        }

        private static async Task ReadMenu()
        {
            Console.Write("Enter NodeId to read (e.g. ns=4;i=72): ");
            var nodeStr = Console.ReadLine() ?? "ns=4;i=72";
            var nodeId = NodeId.Parse(nodeStr);

            var results = await _client!.ReadNodesAsync([nodeId]);
            var val = results?[0];

            if (val != null && val.StatusCode.IsGood)
            {
                if (val.Value!.IsArray)
                {
                    var array = (Array)val.Value.Value!;
                    var elements = array.Cast<object>().Select(o => o.ToString());
                    Console.WriteLine($"Value (Array [{val.Value.Type}]): {string.Join(", ", elements)}");
                }
                else
                {
                    Console.WriteLine($"Value (Scalar [{val.Value.Type}]): {val.Value.Value}");
                }
            }
            else
            {
                Console.WriteLine($"Read Failed: {val?.StatusCode}");
            }
        }

        private static async Task WriteMenu()
        {
            Console.Write("Enter NodeId to write (must be Int16, e.g. ns=4;i=226): ");
            var nodeStr = Console.ReadLine() ?? "ns=4;i=226";
            Console.Write("Enter short value: ");
            if (short.TryParse(Console.ReadLine(), out short shortVal))
            {
                var nodeId = NodeId.Parse(nodeStr);
                var dataValue = new DataValue { Value = new Variant(shortVal, BuiltInType.Int16) };

                var results = await _client!.WriteNodesAsync([nodeId], [dataValue]);
                Console.WriteLine($"Write Result: {results?[0]}");
            }
            else
            {
                Console.WriteLine("Invalid short value.");
            }
        }

        private static async Task SubscribeMenu()
        {
            Console.Write("Enter NodeId to subscribe (e.g. ns=0;i=2254): ");
            var nodeStr = Console.ReadLine() ?? "ns=0;i=2254";
            var nodeId = NodeId.Parse(nodeStr);

            Console.WriteLine($"Subscribing to {nodeId}. Press any key to stop this specific subscription...");

            // The subscription runs on a background thread via the internal SubscriptionClient
            var handles = await _client!.SubscribeAsync([nodeId], (handle, val) =>
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                Console.WriteLine($"\n[SUB UPDATE {timestamp}] Handle: {handle}, Value: {val.Value?.Value}, Status: {val.StatusCode}");
            });

            Console.ReadKey(true);
            Console.WriteLine("Stopped listening to updates (Subscription remains active in background until dispose).");
        }

        private static async Task CallMethodMenu()
        {
            // Calling a simple Add method on an example object
            Console.WriteLine("Calling Example Add Method (Object ns=4;i=312, Method ns=4;i=313)");
            Console.Write("Enter Val A (short): ");
            short a = short.Parse(Console.ReadLine() ?? "0");
            Console.Write("Enter Val B (short): ");
            short b = short.Parse(Console.ReadLine() ?? "0");

            var objectNode = new NodeId(4, 312);
            var methodNode = new NodeId(4, 313);

            var output = await _client!.CallMethodAsync(
                objectNode,
                methodNode,
                default,
                new Variant(a, BuiltInType.Int16),
                new Variant(b, BuiltInType.Int16)
            );

            if (output != null && output.Length > 0)
            {
                Console.WriteLine($"Method Result: {output[0].Value}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_client != null)
            {
                await _client.DisposeAsync();
                _client = null;
            }
        }
    }
}