using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace BeachTycoon.Tests
{
    public sealed class QueueLayoutTests
    {
        private VisitorConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<VisitorConfig>();
            _config.queueSpacing = 1f;
            _config.queuePerLine = 3;
            _config.queueLines = 2;
            _config.queueLineSpacing = 0.8f;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void FirstThree_StandInOneLine()
        {
            Assert.AreEqual(new Vector2(-0.4f, 0f), QueueLayout.Slot(0, _config));
            Assert.AreEqual(new Vector2(-0.4f, 1f), QueueLayout.Slot(1, _config));
            Assert.AreEqual(new Vector2(-0.4f, 2f), QueueLayout.Slot(2, _config));
        }

        [Test]
        public void SecondLine_RunsBackTowardsTheStand()
        {
            Assert.AreEqual(new Vector2(0.4f, 2f), QueueLayout.Slot(3, _config), "beside the third visitor");
            Assert.AreEqual(new Vector2(0.4f, 1f), QueueLayout.Slot(4, _config));
            Assert.AreEqual(new Vector2(0.4f, 0f), QueueLayout.Slot(5, _config));
        }

        [Test]
        public void Overflow_ExtendsTheLastLine()
        {
            Assert.AreEqual(new Vector2(0.4f, 3f), QueueLayout.Slot(6, _config));
        }

        [Test]
        public void OneLine_IsAStraightQueue()
        {
            _config.queueLines = 1;
            Assert.AreEqual(new Vector2(0f, 4f), QueueLayout.Slot(4, _config));
        }

        [Test]
        public void Area_CoversEveryLine()
        {
            Assert.AreEqual(1.8f, QueueLayout.Width(_config, 1f), 1e-5f);
            Assert.AreEqual(3f, QueueLayout.Length(_config), 1e-5f);
        }
    }
}
