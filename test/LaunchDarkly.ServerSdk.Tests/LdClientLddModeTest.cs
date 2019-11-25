﻿using LaunchDarkly.Client;
using LaunchDarkly.Client.Interfaces;
using LaunchDarkly.Common;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LaunchDarkly.Tests
{
    public class LdClientLddModeTest
    {
        [Fact]
        public void LddModeClientHasNullDataSource()
        {
            var config = Configuration.Builder("SDK_KEY").UseLdd(true).Build();
            using (var client = new LdClient(config))
            {
                Assert.IsType<NullDataSource>(client._dataSource);
            }
        }

        [Fact]
        public void LddModeClientHasDefaultEventProcessor()
        {
            var config = Configuration.Builder("SDK_KEY").UseLdd(true).Build();
            using (var client = new LdClient(config))
            {
                Assert.IsType<DefaultEventProcessor>(client._eventProcessor);
            }
        }

        [Fact]
        public void LddModeClientIsInitialized()
        {
            var config = Configuration.Builder("SDK_KEY").UseLdd(true).Build();
            using (var client = new LdClient(config))
            {
                Assert.True(client.Initialized());
            }
        }

        [Fact]
        public void LddModeClientGetsFlagFromDataStore()
        {
            var dataStore = new InMemoryDataStore();
            dataStore.Upsert(VersionedDataKind.Features,
                new FeatureFlagBuilder("key").OffWithValue(new JValue(true)).Build());
            var config = Configuration.Builder("SDK_KEY")
                .UseLdd(true)
                .DataStore(TestUtils.SpecificDataStore(dataStore))
                .Build();
            using (var client = new LdClient(config))
            {
                Assert.True(client.BoolVariation("key", User.WithKey("user"), false));
            }
        }
    }
}
