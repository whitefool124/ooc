using System.Collections.Generic;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class RogueliteSaveGatewayTests
    {
        private sealed class MemoryStore : IRogueliteSaveStore
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
            public int FlushCount;
            public bool HasKey(string key) => Values.ContainsKey(key);
            public string GetString(string key, string defaultValue = "") => Values.TryGetValue(key, out string value) ? value : defaultValue;
            public void SetString(string key, string value) => Values[key] = value;
            public void DeleteKey(string key) => Values.Remove(key);
            public void Flush() => FlushCount++;
        }

        [Test]
        public void MapRun_RoundTripsThroughStableKey()
        {
            MemoryStore store = new MemoryStore();
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            RogueliteMapRun source = new RogueliteMapRun(73);

            Assert.That(gateway.SaveMapRun(source), Is.True);
            Assert.That(store.Values.ContainsKey(RogueliteSaveGateway.MapRunKey), Is.True);
            Assert.That(gateway.TryLoadMapRun(out RogueliteMapRun loaded), Is.True);
            Assert.That(loaded.Seed, Is.EqualTo(73));
            Assert.That(store.FlushCount, Is.EqualTo(1));
        }

        [Test]
        public void CorruptMapRun_IsReportedBackedUpAndProtectedFromOverwrite()
        {
            MemoryStore store = new MemoryStore();
            store.Values[RogueliteSaveGateway.MapRunKey] = "not-json";
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);

            bool loaded = gateway.TryLoadMapRun(out RogueliteMapRun run);

            Assert.That(loaded, Is.False);
            Assert.That(run, Is.Null);
            Assert.That(gateway.LastError, Does.Contain(RogueliteSaveGateway.MapRunKey));
            Assert.That(gateway.LastLoadStatus, Is.EqualTo(RogueliteSaveLoadStatus.CorruptData));
            Assert.That(gateway.LastFailedKey, Is.EqualTo(RogueliteSaveGateway.MapRunKey));
            Assert.That(store.Values[RogueliteSaveGateway.CorruptBackupKey(RogueliteSaveGateway.MapRunKey)], Is.EqualTo("not-json"));
            Assert.That(store.Values[RogueliteSaveGateway.MapRunKey], Is.EqualTo("not-json"));

            Assert.That(gateway.SaveMapRun(new RogueliteMapRun(91)), Is.False);
            Assert.That(gateway.LastError, Does.Contain("write blocked"));
            Assert.That(store.Values[RogueliteSaveGateway.MapRunKey], Is.EqualTo("not-json"));
        }

        [Test]
        public void CorruptMapRun_ExplicitDeleteUnblocksReplacementAndKeepsBackup()
        {
            MemoryStore store = new MemoryStore();
            store.Values[RogueliteSaveGateway.MapRunKey] = "not-json";
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            gateway.TryLoadMapRun(out _);

            Assert.That(gateway.DeleteMapRun(), Is.True);
            Assert.That(gateway.SaveMapRun(new RogueliteMapRun(91)), Is.True);
            Assert.That(store.Values[RogueliteSaveGateway.CorruptBackupKey(RogueliteSaveGateway.MapRunKey)], Is.EqualTo("not-json"));
            Assert.That(gateway.TryLoadMapRun(out RogueliteMapRun loaded), Is.True);
            Assert.That(loaded.Seed, Is.EqualTo(91));
        }

        [Test]
        public void CorruptMapRun_RepeatedFailureDoesNotOverwriteFirstBackup()
        {
            MemoryStore store = new MemoryStore();
            store.Values[RogueliteSaveGateway.MapRunKey] = "first-corrupt-value";
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            gateway.TryLoadMapRun(out _);

            store.Values[RogueliteSaveGateway.MapRunKey] = "second-corrupt-value";
            gateway.TryLoadMapRun(out _);

            Assert.That(store.Values[RogueliteSaveGateway.CorruptBackupKey(RogueliteSaveGateway.MapRunKey)], Is.EqualTo("first-corrupt-value"));
            Assert.That(store.Values[RogueliteSaveGateway.MapRunKey], Is.EqualTo("second-corrupt-value"));
        }

        [Test]
        public void MissingMapRun_HasExplicitMissingStatusWithoutBackup()
        {
            MemoryStore store = new MemoryStore();
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);

            Assert.That(gateway.TryLoadMapRun(out RogueliteMapRun run), Is.False);
            Assert.That(run, Is.Null);
            Assert.That(gateway.LastLoadStatus, Is.EqualTo(RogueliteSaveLoadStatus.Missing));
            Assert.That(gateway.LastError, Is.Empty);
            Assert.That(store.Values.ContainsKey(RogueliteSaveGateway.CorruptBackupKey(RogueliteSaveGateway.MapRunKey)), Is.False);
        }

        [Test]
        public void DeleteShortRun_FlushesAndClearsSlot()
        {
            MemoryStore store = new MemoryStore();
            store.Values[RogueliteSaveGateway.ShortRunKey] = "old";
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);

            Assert.That(gateway.DeleteShortRun(), Is.True);
            Assert.That(gateway.HasShortRun, Is.False);
            Assert.That(store.FlushCount, Is.EqualTo(1));
        }
    }
}
