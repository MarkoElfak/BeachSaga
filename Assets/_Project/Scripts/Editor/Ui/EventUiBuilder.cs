using BeachTycoon.UI;
using TMPro;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>Milestone 9 HUD: the beach event banner under the quest tracker (title, effect, countdown); toasts show below it.</summary>
    internal static class EventUiBuilder
    {
        public static void BuildEventBanner(Transform parent)
        {
            RectTransform host = UiKit.Rect("BeachEventBanner", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -315f), new Vector2(660f, 120f));
            RectTransform banner = UiKit.Stretch("Banner", host);
            var background = UiKit.AddImage(banner, new Color(0.98f, 0.62f, 0.3f), false);

            RectTransform titleRect = UiKit.Rect("Title", banner, new Vector2(0f, 0.52f), new Vector2(0.74f, 1f), new Vector2(0f, 0.5f), new Vector2(24f, -2f), new Vector2(-24f, -6f));
            TextMeshProUGUI title = UiKit.AddText(titleRect, "Event", 38f, TextAlignmentOptions.BottomLeft, UiKit.TextDark, FontStyles.Bold);
            RectTransform detailRect = UiKit.Rect("Detail", banner, new Vector2(0f, 0.06f), new Vector2(0.74f, 0.52f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(-24f, 0f));
            TextMeshProUGUI detail = UiKit.AddText(detailRect, "", 26f, TextAlignmentOptions.TopLeft, UiKit.TextDark);
            detail.enableAutoSizing = true; // longer descriptions shrink instead of spilling out
            detail.fontSizeMin = 18f;
            detail.fontSizeMax = 26f;
            RectTransform timerRect = UiKit.Rect("Timer", banner, new Vector2(0.74f, 0f), Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, 0f));
            TextMeshProUGUI timer = UiKit.AddText(timerRect, "0:00", 52f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);

            var view = host.gameObject.AddComponent<BeachEventBannerView>();
            PrimitiveKit.SetReference(view, "banner", banner.gameObject);
            PrimitiveKit.SetReference(view, "background", background);
            PrimitiveKit.SetReference(view, "title", title);
            PrimitiveKit.SetReference(view, "detail", detail);
            PrimitiveKit.SetReference(view, "timer", timer);
            banner.gameObject.SetActive(false);
        }
    }
}
