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
            public int SetCount;
            public bool ThrowOnGet;
            public bool CorruptMapReadbackAfterNextSet;
            private bool corruptMapReadback;
            public bool HasKey(string key) => Values.ContainsKey(key);
            public string GetString(string key, string defaultValue = "")
            {
                if (ThrowOnGet) throw new System.InvalidOperationException("store unavailable");
                string value = Values.TryGetValue(key, out string found) ? found : defaultValue;
                return corruptMapReadback && key == RogueliteSaveGateway.MapRunKey ? value + "-mismatch" : value;
            }
            public void SetString(string key, string value)
            {
                SetCount++; Values[key] = value;
                if (key == RogueliteSaveGateway.MapRunKey && CorruptMapReadbackAfterNextSet)
                {
                    CorruptMapReadbackAfterNextSet = false; corruptMapReadback = true;
                }
            }
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
            Assert.That(store.Values.ContainsKey(RogueliteSaveGateway.WriteLockKey(RogueliteSaveGateway.MapRunKey)), Is.True);
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
            Assert.That(store.Values.ContainsKey(RogueliteSaveGateway.WriteLockKey(RogueliteSaveGateway.MapRunKey)), Is.False);
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
        public void PersistentProtection_BlocksASecondGatewayUntilExplicitDelete()
        {
            MemoryStore store = new MemoryStore();
            store.Values[RogueliteSaveGateway.MapRunKey] = "not-json";
            new RogueliteSaveGateway(store).TryLoadMapRun(out _);

            RogueliteSaveGateway second = new RogueliteSaveGateway(store);
            Assert.That(second.SaveMapRun(new RogueliteMapRun(92)), Is.False);
            Assert.That(second.LastError, Does.Contain("persistent protection"));
            Assert.That(store.Values[RogueliteSaveGateway.MapRunKey], Is.EqualTo("not-json"));

            Assert.That(second.DeleteMapRun(), Is.True);
            Assert.That(second.SaveMapRun(new RogueliteMapRun(92)), Is.True);
            Assert.That(store.Values[RogueliteSaveGateway.CorruptBackupKey(RogueliteSaveGateway.MapRunKey)], Is.EqualTo("not-json"));
        }

        [Test]
        public void InvalidMapObject_IsRejectedBeforeAnyStorageWrite()
        {
            MemoryStore store = new MemoryStore();
            string[] fields = new RogueliteMapRun(93).ToJson().Split('|');
            fields[9] = "-1";
            RogueliteMapRun invalid = RogueliteMapRun.FromJson(string.Join("|", fields));

            Assert.That(new RogueliteSaveGateway(store).SaveMapRun(invalid), Is.False);
            Assert.That(store.SetCount, Is.Zero);
            Assert.That(store.Values.ContainsKey(RogueliteSaveGateway.MapRunKey), Is.False);
        }

        [Test]
        public void ReadbackMismatch_RollsBackMainSlotAndPersistsProtection()
        {
            MemoryStore store = new MemoryStore();
            string original = new RogueliteMapRun(94).ToJson();
            store.Values[RogueliteSaveGateway.MapRunKey] = original;
            store.CorruptMapReadbackAfterNextSet = true;
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);

            Assert.That(gateway.SaveMapRun(new RogueliteMapRun(95)), Is.False);
            Assert.That(store.Values[RogueliteSaveGateway.MapRunKey], Is.EqualTo(original));
            Assert.That(store.Values.ContainsKey(RogueliteSaveGateway.WriteLockKey(RogueliteSaveGateway.MapRunKey)), Is.True);
            Assert.That(store.Values.ContainsKey(RogueliteSaveGateway.CorruptBackupKey(RogueliteSaveGateway.MapRunKey)), Is.True);
            Assert.That(new RogueliteSaveGateway(store).SaveMapRun(new RogueliteMapRun(96)), Is.False);
        }

        [Test]
        public void StoreFailure_IsNotReportedAsMissingOrCorrupt()
        {
            MemoryStore store = new MemoryStore();
            store.Values[RogueliteSaveGateway.MapRunKey] = new RogueliteMapRun(97).ToJson();
            store.ThrowOnGet = true;
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);

            Assert.That(gateway.TryLoadMapRun(out _), Is.False);
            Assert.That(gateway.LastLoadStatus, Is.EqualTo(RogueliteSaveLoadStatus.StoreError));
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
