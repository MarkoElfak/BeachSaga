using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>Island scene HUD: whose island it is and its flag, the visit bonus, Like and Gift (friends only) and Home.</summary>
    public sealed class IslandHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text title = null;
        [SerializeField] private TMP_Text details = null;
        [SerializeField] private RawImage flag = null;
        [SerializeField] private TMP_Text bonusLabel = null;
        [SerializeField] private Button likeButton = null;
        [SerializeField] private TMP_Text likeLabel = null;
        [SerializeField] private Button giftButton = null;
        [SerializeField] private TMP_Text giftLabel = null;
        [SerializeField] private Button homeButton = null;

        private VisitService _visits;

        public string TitleText => title.text;
        public Texture Flag => flag.gameObject.activeSelf ? flag.texture : null;
        private ISocialService _social;

        private void Start()
        {
            if (!ServiceLocator.TryGet(out _visits) || _visits.Current == null) return;
            _social = ServiceLocator.Get<ISocialService>();
            IslandSnapshot island = _visits.Current;
            TierDefinition tier = ServiceLocator.Get<GameDatabase>().GetTier(island.tier);
            title.text = Title(island, _visits.IsOwnIsland);
            ServiceLocator.TryGet(out CultureService cultures);
            CultureDefinition culture = cultures?.Find(island.cultureId);
            flag.texture = culture != null ? cultures.Flag(culture) : null;
            flag.gameObject.SetActive(culture != null);
            string owner = _visits.IsOwnIsland || string.IsNullOrEmpty(island.villageName) ? string.Empty : $"{island.ownerName}  -  ";
            details.text = $"{owner}{(tier != null ? tier.displayName : "Empty beach")}  -  Popularity {island.popularity:0.00}";
            bonusLabel.text = _visits.LastBonus > 0
                ? $"Visit bonus +{NumberFormatter.Format(_visits.LastBonus)} coins!"
                : "Visit bonus already collected today";

            likeButton.gameObject.SetActive(!_visits.IsOwnIsland);
            giftButton.gameObject.SetActive(!_visits.IsOwnIsland);
            likeButton.onClick.AddListener(Like);
            giftButton.onClick.AddListener(Gift);
            homeButton.onClick.AddListener(Home);
            EventBus.Subscribe<SocialActionDone>(OnSocialDone);
            RefreshSocial();
        }

        // The village name when the island has one; snapshots from before villages show the owner instead.
        private static string Title(IslandSnapshot island, bool own)
        {
            bool named = !string.IsNullOrEmpty(island.villageName);
            if (own) return named ? $"{island.villageName} (your saved snapshot)" : "Your island (saved snapshot)";
            return named ? island.villageName : $"{island.ownerName}'s island";
        }

        private void OnDestroy()
        {
            likeButton.onClick.RemoveListener(Like);
            giftButton.onClick.RemoveListener(Gift);
            homeButton.onClick.RemoveListener(Home);
            EventBus.Unsubscribe<SocialActionDone>(OnSocialDone);
        }

        private void Like()
        {
            EventBus.Publish(new SoundRequested(SoundId.UiClick));
            _visits.Like();
        }

        private void Gift()
        {
            EventBus.Publish(new SoundRequested(SoundId.UiClick));
            _visits.SendGift();
        }

        private void Home()
        {
            EventBus.Publish(new SoundRequested(SoundId.UiClick));
            homeButton.interactable = false;
            _visits.ReturnHome();
        }

        private void OnSocialDone(SocialActionDone evt) => RefreshSocial();

        private void RefreshSocial()
        {
            string owner = _visits.Current != null ? _visits.Current.ownerId : null;
            bool liked = _social.HasLiked(owner);
            bool gifted = _social.HasSentGift(owner);
            likeLabel.text = liked ? "Liked!" : "Like";
            likeButton.interactable = !liked;
            giftLabel.text = gifted ? "Gift sent" : "Send gift";
            giftButton.interactable = !gifted;
        }
    }
}
