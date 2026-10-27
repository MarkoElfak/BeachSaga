using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using NUnit.Framework;
using UnityEngine;

namespace BeachTycoon.Tests
{
    /// <summary>The default map (M16): which land each tier opens, the islet and its boardwalk, and old saves.</summary>
    public sealed class LandMapTests
    {
        private MapConfig _map;

        [SetUp]
        public void SetUp() => _map = ScriptableObject.CreateInstance<MapConfig>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_map);

        [Test]
        public void EachTier_OpensOneMorePieceAndTheMainlandStaysARectangle()
        {
            Rect previous = new Rect();
            for (int tier = 1; tier <= 5; tier++)
            {
                Assert.IsTrue(LandMap.TryMainland(_map, tier, out Rect land), $"tier {tier}");
                Assert.IsTrue(tier == 1 || PlacementRules.Inside(previous, land, 0f), $"tier {tier} keeps the earlier land");
                float pieces = 0f;
                int opened = 0;
                foreach (LandPiece piece in _map.pieces)
                {
                    if (piece.unlockTier == tier) opened++;
                    if (!piece.island && LandMap.IsOpen(piece, tier)) pieces += piece.area.width * piece.area.height;
                }

                Assert.AreEqual(1, opened, $"tier {tier} opens one piece");
                Assert.AreEqual(land.width * land.height, pieces, 0.01f, $"tier {tier} mainland has no gaps");
                previous = land;
            }
        }

        [Test]
        public void LaterPieces_AreAQuarterOfTheFirstBeach_SoTheMapDoubles()
        {
            Rect first = _map.pieces[0].area;
            float quarter = first.width * first.height * 0.25f;
            float total = 0f;
            foreach (LandPiece piece in _map.pieces)
            {
                float size = piece.area.width * piece.area.height;
                total += size;
                if (piece.unlockTier > 1) Assert.AreEqual(quarter, size, quarter * 0.05f, piece.id);
            }

            Assert.AreEqual(first.width * first.height * 2f, total, quarter * 0.1f);
        }

        [Test]
        public void Islet_OpensLast_WithItsBoardwalk()
        {
            int islet = LandMap.IslandAt(_map, new Vector2(0f, 22f));
            Assert.GreaterOrEqual(islet, 0);
            LandPiece piece = _map.pieces[islet];
            Assert.IsTrue(LandMap.HasBoardwalk(piece));
            Assert.AreEqual(-1, LandMap.IslandAt(_map, new Vector2(0f, 0f)), "mainland");
            Assert.IsTrue(LandMap.TryMainland(_map, 5, out Rect mainland));
            Assert.AreEqual(mainland, LandMap.Bounds(_map, 4, mainland), "not shown before it opens");
            Assert.IsTrue(LandMap.Bounds(_map, 5, mainland).Contains(piece.area.center), "shown once open");
            Assert.IsFalse(LandMap.InsideOpenIsland(_map, 4, new Rect(-1f, 20f, 2f, 2f)));
            Assert.IsTrue(LandMap.InsideOpenIsland(_map, 5, new Rect(-1f, 20f, 2f, 2f)));

            Rect walk = LandMap.BoardwalkArea(_map, piece);
            Assert.LessOrEqual(walk.yMin, mainland.yMax, "starts on the mainland");
            Assert.GreaterOrEqual(walk.yMax, piece.area.yMin, "ends on the islet");
            Assert.IsTrue(LandMap.FogArea(_map, islet).Contains(new Vector2(0f, piece.area.yMin - _map.islandBeach * 0.5f)), "fog covers its beach");
        }

        [Test]
        public void Version15Save_PinsHousesOnMovedLots()
        {
            const string v15 = "{\"version\":15,\"wallet\":{\"coins\":7},\"lots\":[" +
                "{\"index\":0,\"state\":2,\"tier\":4}," +
                "{\"index\":5,\"state\":2,\"tier\":1}," +
                "{\"index\":6,\"state\":0}," +
                "{\"index\":7,\"state\":2,\"tier\":1,\"placed\":true,\"x\":3.0,\"z\":-4.0}," +
                "{\"index\":12,\"state\":1,\"targetTier\":1}]}";
            var data = JsonUtility.FromJson<GameSaveData>(new SaveMigrator(SaveMigrations.All).MigrateToCurrent(v15, 15));

            Assert.AreEqual(GameSaveData.CurrentVersion, data.version);
            Assert.AreEqual(7, data.wallet.coins, "progress is kept");
            Assert.IsFalse(data.lots[0].placed, "the main lot did not move");
            Assert.IsTrue(data.lots[1].placed);
            Assert.AreEqual(new Vector2(-10.5f, -10f), new Vector2(data.lots[1].x, data.lots[1].z), "house 5 stays put");
            Assert.IsFalse(data.lots[2].placed, "an empty lot takes the new spot");
            Assert.AreEqual(new Vector2(3f, -4f), new Vector2(data.lots[3].x, data.lots[3].z), "a moved house keeps its spot");
            Assert.IsTrue(data.lots[4].placed, "a house being built stays put too");
            Assert.AreEqual(new Vector2(10.5f, -12.2f), new Vector2(data.lots[4].x, data.lots[4].z));
        }
    }
}
