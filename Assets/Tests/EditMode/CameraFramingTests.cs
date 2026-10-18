using BeachTycoon.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace BeachTycoon.Tests
{
    /// <summary>Whole-map zoom: the fit distance, the focus bounds that centre it, and the rubber-band zoom limits.</summary>
    public sealed class CameraFramingTests
    {
        private static readonly Rect Land = Rect.MinMaxRect(-19.2f, -16.9f, 19.2f, 6.5f);

        [Test]
        public void LookingStraightDown_FitIsHalfTheAreaOverTheLens()
        {
            float distance = CameraFraming.FitDistance(new Rect(-5f, -5f, 10f, 10f), Quaternion.Euler(90f, 0f, 0f), 90f, 1f, 0f);
            Assert.AreEqual(5f, distance, 0.001f);
        }

        [TestCase(720f / 1280f, 0f)]
        [TestCase(1280f / 720f, 0f)]
        [TestCase(720f / 1280f, 4f)]
        public void TiltedCamera_AtTheFitDistance_SeesEveryCornerJustInside(float aspect, float height)
        {
            Quaternion rotation = Quaternion.Euler(55f, 0f, 0f);
            float distance = CameraFraming.FitDistance(Land, rotation, 35f, aspect, height);
            var go = new GameObject("FitCamera");
            try
            {
                Camera camera = go.AddComponent<Camera>();
                camera.fieldOfView = 35f;
                camera.aspect = aspect;
                Vector3 centre = new Vector3(Land.center.x, 0f, Land.center.y);
                go.transform.SetPositionAndRotation(centre - rotation * Vector3.forward * distance, rotation);

                float edge = 1f;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? Land.xMin : Land.xMax, (i & 4) == 0 ? 0f : height, (i & 2) == 0 ? Land.yMin : Land.yMax);
                    Vector3 view = camera.WorldToViewportPoint(corner);
                    Assert.That(view.x, Is.InRange(-0.001f, 1.001f), $"corner {i} x");
                    Assert.That(view.y, Is.InRange(-0.001f, 1.001f), $"corner {i} y");
                    edge = Mathf.Min(edge, view.x, 1f - view.x, view.y, 1f - view.y);
                }

                Assert.Less(edge, 0.002f, "at least one corner touches the screen edge, so it is not farther than needed");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PortraitScreen_NeedsMoreZoomOutThanLandscape_AndMoreLandNeedsMore()
        {
            Quaternion rotation = Quaternion.Euler(55f, 0f, 0f);
            float portrait = CameraFraming.FitDistance(Land, rotation, 35f, 0.5625f, 0f);
            float landscape = CameraFraming.FitDistance(Land, rotation, 35f, 1.7778f, 0f);
            float tent = CameraFraming.FitDistance(Rect.MinMaxRect(-11.8f, -15.6f, 11.8f, 6.5f), rotation, 35f, 0.5625f, 0f);
            Assert.Greater(portrait, landscape);
            Assert.Greater(portrait, tent);
        }

        [Test]
        public void FocusBounds_LookPastTheEdgeCloseUp_AndCentreAtFullZoomOut()
        {
            Rect close = CameraFraming.FocusBounds(Land, 3f, 0f);
            Assert.AreEqual(Land.xMin - 3f, close.xMin, 0.001f);
            Assert.AreEqual(Land.yMax + 3f, close.yMax, 0.001f);

            Rect whole = CameraFraming.FocusBounds(Land, 3f, 1f);
            Assert.AreEqual(Land.center, whole.center);
            Assert.AreEqual(0f, whole.width, 0.001f);
            Assert.AreEqual(0f, whole.height, 0.001f);

            Rect half = CameraFraming.FocusBounds(Land, 3f, 0.5f);
            Assert.Greater(half.width, close.width * 0.5f, "most of the zoom range still roams freely");
        }

        [Test]
        public void Soften_LeavesTheRangeAlone_AndEasesToAStopPastIt()
        {
            Assert.AreEqual(30f, CameraFraming.Soften(30f, 18f, 60f, 1.5f, 5f));
            float previous = 60f;
            for (float raw = 61f; raw < 100f; raw += 6f)
            {
                float soft = CameraFraming.Soften(raw, 18f, 60f, 1.5f, 5f);
                Assert.Greater(soft, previous, "keeps moving the way the fingers go");
                Assert.Less(soft, 65f, "never beyond the give");
                previous = soft;
            }

            Assert.That(CameraFraming.Soften(0f, 18f, 60f, 1.5f, 5f), Is.InRange(16.5f, 18f));
            Assert.AreEqual(60f, CameraFraming.Soften(80f, 18f, 60f, 0f, 0f), "no give clamps hard");
        }
    }
}
