using LiteUa.BuiltIn;
using LiteUa.Client;
using LiteUa.Stack.Session.Identity;
using LiteUa.Transport;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Xunit.Abstractions;

namespace LiteUa.Tests.IntegrationTests
{
    /*
    [Category("IntegrationTests_S7-1200")]
    public class SubscriptionClient_IntegrationTest_S7_1200(ITestOutputHelper output)
    {
        private readonly ITestOutputHelper _output = output;
        public const string TestServerUrl = "opc.tcp://192.178.0.1:4840/";

        [Fact]
        public async Task Should_Reconnect_And_Restore_Subscription()
        {
            var tcsInitial = new TaskCompletionSource<bool>();
            var tcsAfterReconnect = new TaskCompletionSource<bool>();

            await using var client = new SubscriptionClient(
                TestServerUrl,
                new AnonymousIdentity("Anonymous")
            );

            int updateCount = 0;

            client.DataChanged += (h, v) =>
            {
                updateCount++;

                if (!tcsInitial.Task.IsCompleted) tcsInitial.TrySetResult(true);
                else tcsAfterReconnect.TrySetResult(true);
            };

            client.Start();

            // 1. Subscribe
            await Task.Delay(2000); // Wait until connected
            await client.SubscribeAsync(new NodeId(4, 72)); // TestInt

            // 2. Wait for initial data
            await Task.WhenAny(tcsInitial.Task, Task.Delay(5000));
            Assert.True(tcsInitial.Task.IsCompleted, "Did not receive initial data");

            // 3. Connection loss
            var field = typeof(SubscriptionClient).GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var channel = (UaTcpClientChannel?)field?.GetValue(client);
            if (channel != null)
                await channel.DisposeAsync();

            // 4. Wait for reconnection
            // (Timeout Exception in Loop + 5s Backoff).

            await Task.Delay(8000);

            // Check: New data?
            // Value would have to change or we wait for the initial value of the NEW subscription

            var result = await Task.WhenAny(tcsAfterReconnect.Task, Task.Delay(10000));
            Assert.Equal(tcsAfterReconnect.Task, result);
        }

        [Fact]
        public async Task Multi_Speed_Subscription()
        {
            await using var client = new SubscriptionClient(TestServerUrl, new AnonymousIdentity("Anonymous"));
            client.Start();

            var tcsFast = new TaskCompletionSource<bool>();
            var tcsSlow = new TaskCompletionSource<bool>();

            // Callbacks
            client.DataChanged += (h, v) =>
            {
                if (h == 1) // Fast
                {
                    if (!tcsFast.Task.IsCompleted) tcsFast.TrySetResult(true);
                }
                else if (h == 2) // Slow
                {
                    if (!tcsSlow.Task.IsCompleted) tcsSlow.TrySetResult(true);
                }
            };

            // Warten auf Connect
            await Task.Delay(2000);

            // 1. Fast Subscription (100ms)
            await client.SubscribeAsync(new NodeId(4, 72), 100.0); // Handle 1 (da erstes)

            // 2. Slow Subscription (1000ms)
            // Nutzt dieselbe NodeId, ist egal. Handle wird 2.
            await client.SubscribeAsync(new NodeId(4, 72), 1000.0);

            // Wir erwarten, dass beide Daten liefern
            await Task.WhenAll(tcsFast.Task, tcsSlow.Task);

            Console.WriteLine("Both subscriptions received data.");
        }

    }
    */
}
