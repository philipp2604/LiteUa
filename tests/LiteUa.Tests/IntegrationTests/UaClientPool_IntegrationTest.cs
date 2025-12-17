using LiteUa.Client.Pooling;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;

namespace LiteUa.Tests.IntegrationTests
{
    public class UaClientPool_IntegrationTest(ITestOutputHelper output)
    {
        private readonly ITestOutputHelper _output = output;
        public const string TestServerUrl = "opc.tcp://192.178.0.1:4840/";

        [Fact]
        public async Task Pool_Test()
        {
            await using var pool = new UaClientPool(TestServerUrl, maxSize: 10);
            using (var lease1 = await pool.RentAsync())
            {
                await lease1.InnerClient.GetEndpointsAsync();
            }
            using (var lease2 = await pool.RentAsync())
            {
                await lease2.InnerClient.GetEndpointsAsync();
            }

            var t1 = pool.RentAsync();
            var t2 = pool.RentAsync();
            var t3 = pool.RentAsync();
            var t4 = pool.RentAsync();
            var t5 = pool.RentAsync();
            var t6 = pool.RentAsync();
            var t7 = pool.RentAsync();
            var t8 = pool.RentAsync();
            var t9 = pool.RentAsync();
            var t10 = pool.RentAsync();
            var t11 = pool.RentAsync(); // this one has to wait

            var c1 = await t1;
            var c2 = await t2;
            var c3 = await t3;
            var c4 = await t4;
            var c5 = await t5;
            var c6 = await t6;
            var c7 = await t7;
            var c8 = await t8;
            var c9 = await t9;
            var c10 = await t10;

            Assert.False(t11.IsCompleted); // t11 should be waiting

            c1.Dispose(); // return one

            var c11 = await t11;


            Assert.True(t11.IsCompleted); // t11 should be waiting

            c2.Dispose();
            c3.Dispose();
            c4.Dispose();
            c5.Dispose();
            c6.Dispose();
            c7.Dispose();
            c8.Dispose();
            c9.Dispose();
            c10.Dispose();
            c11.Dispose();
        }
    }
}
