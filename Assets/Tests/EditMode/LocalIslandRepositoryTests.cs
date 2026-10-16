using System.IO;
using BeachTycoon.Data;
using BeachTycoon.Services;
using NUnit.Framework;

namespace BeachTycoon.Tests
{
    public sealed class LocalIslandRepositoryTests
    {
        private string _root;
        private LocalIslandRepository _repository;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "BeachTycoonIslands_" + System.Guid.NewGuid().ToString("N"));
            _repository = new LocalIslandRepository(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        private IslandSnapshot Load(string ownerId)
        {
            IslandSnapshot loaded = null;
            bool called = false;
            _repository.LoadSnapshot(ownerId, snapshot => { loaded = snapshot; called = true; });
            Assert.IsTrue(called, "load always answers");
            return loaded;
        }

        private bool Save(IslandSnapshot snapshot)
        {
            bool saved = false;
            _repository.SaveSnapshot(snapshot, ok => saved = ok);
            return saved;
        }

        [Test]
        public void SavedSnapshot_LoadsBackByOwner()
        {
            var snapshot = new IslandSnapshot { ownerId = "friend-1", tier = 3 };
            snapshot.stands.Add(new SnapshotStand { productId = "donuts", x = 2f });
            Assert.IsTrue(Save(snapshot));
            Assert.IsTrue(Save(new IslandSnapshot { ownerId = "friend-2", tier = 1 }));

            IslandSnapshot loaded = Load("friend-1");
            Assert.AreEqual(3, loaded.tier);
            Assert.AreEqual(2f, loaded.stands[0].x);
            Assert.AreEqual(1, Load("friend-2").tier);
        }

        [Test]
        public void SavingAgain_ReplacesTheSnapshot()
        {
            Save(new IslandSnapshot { ownerId = "me", tier = 1 });
            Save(new IslandSnapshot { ownerId = "me", tier = 2 });
            Assert.AreEqual(2, Load("me").tier);
            Assert.AreEqual(1, Directory.GetFiles(_repository.Folder).Length, "no temp files left behind");
        }

        [Test]
        public void MissingOrCorruptSnapshot_LoadsAsNull()
        {
            Assert.IsNull(Load("nobody"));
            Assert.IsNull(Load(null));
            Directory.CreateDirectory(_repository.Folder);
            File.WriteAllText(Path.Combine(_repository.Folder, "broken.json"), "{ this is not json");
            Assert.IsNull(Load("broken"));
        }

        [Test]
        public void OwnerIds_CannotEscapeTheFolder()
        {
            Assert.IsTrue(Save(new IslandSnapshot { ownerId = "../../evil", tier = 1 }));
            Assert.AreEqual(1, Directory.GetFiles(_repository.Folder).Length);
            Assert.IsFalse(File.Exists(Path.Combine(_root, "..", "evil.json")));
            Assert.AreEqual(1, Load("../../evil").tier);
        }

        [Test]
        public void InvalidOrOversizedSnapshots_AreNotSaved()
        {
            Assert.IsFalse(Save(null));
            Assert.IsFalse(Save(new IslandSnapshot()), "no owner id");
            Assert.IsFalse(Save(new IslandSnapshot { ownerId = "big", ownerName = new string('x', IslandSnapshotJson.MaxBytes) }));
            Assert.IsNull(Load("big"));
        }
    }
}
