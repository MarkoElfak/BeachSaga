using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using BeachTycoon.UI;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>The village playtest's checks: first aid and its ring, the restaurant team, the chapel and its wedding, the hospital.</summary>
    internal static partial class VillagePlaytestSteps
    {
        private const string Key = "BeachTycoon.AutoRun.Village.";
        private static readonly List<long> Weddings = new List<long>();

        private static UpgradePanelView Panel => PlaytestTools.Find<UpgradePanelView>();

        private static void OnWedding(WeddingHeld evt) => Weddings.Add(evt.Amount);

        private static void CheckFirstAid()
        {
            StandController aid = FirstAid;
            float effect = Services.Effect(aid);
            Check(effect > 0f, $"with {Staff.WorkerAt(aid.Index).Model.name} at work, first aid helps ({effect:P0} more patience)");
            CheckSpread(aid, s => s.PatienceMultiplier, effect, "wait longer in line");

            StandView view = ViewOf("first_aid");
            ReachRingView ring = view.GetComponent<ReachRingView>();
            Select(view);
            Check(ring != null && ring.IsShown, "selecting first aid shows its reach on the ground");
            PlaytestTools.FocusCamera(view.transform.position, 34f);
            PlaytestTools.CapturePortraitWithHud("m20_3_first_aid_reach.png");
            OpenPanel(view);
            Check(Panel.IsOpen && Panel.BodyText.Contains("longer in line") && Panel.BodyText.Contains("Grows into a Hospital"),
                "its panel names the reach, the longer wait and the hospital to come");
            PlaytestTools.CapturePortraitWithHud("m20_4_first_aid_panel.png");
            Panel.Close();
            ServiceLocator.Get<SelectionService>().Clear();
            Check(!ring.IsShown, "the ring hides once nothing is selected");

            Open("restaurant");
            Open("chapel");
            StaffMember cook = BestAt(StaffSkill.Cooking);
            EventBus.Publish(new WorkerCardRequested(cook.Id));
            PlaytestTools.Find<WorkerCardView>().StandButton(Restaurant.Index).onClick.Invoke();
            Check(Staff.CountAt(Restaurant.Index) == 1 && Staff.IsStandFree(Restaurant.Index), $"{cook.Model.name} hired for the restaurant from their card; room for one more");
            StaffMember second = BestAt(StaffSkill.Cooking);
            Check(Staff.TryHire(second.Id, Restaurant.Index) && !Staff.IsStandFree(Restaurant.Index), $"{second.Model.name} joins them; the restaurant is full");
            StaffMember carer = BestAt(StaffSkill.Care);
            Check(Staff.TryHire(carer.Id, Chapel.Index), $"{carer.Model.name} hired for the chapel");
        }

        private static void CheckRestaurantAndChapel()
        {
            StandController restaurant = Restaurant;
            float fastest = 0f, slowest = float.MaxValue;
            foreach (StaffMember member in Staff.Staff)
            {
                if (member.Model.stand != restaurant.Index) continue;
                float speed = StaffMath.ServeSpeed(Staff.Config, member.Model, StaffSkill.Cooking, Staff.IsFromHome(member.Model));
                fastest = Mathf.Max(fastest, speed);
                slowest = Mathf.Min(slowest, speed);
            }

            float team = StaffMath.TeamSpeed(Staff.Config, fastest, slowest);
            Check(restaurant.People == 2 && Mathf.Approximately(restaurant.ServeSpeed, team) && team > fastest,
                $"two cooks serve together at {restaurant.ServeSpeed:0.00}x (the best alone: {fastest:0.00}x)");
            PlaytestTools.FocusCamera(ViewOf("restaurant").transform.position, 9f);
            PlaytestTools.CapturePortrait("m20_5_restaurant.png");

            StandController chapel = Chapel;
            StandView view = ViewOf("chapel");
            Check(view.name.StartsWith("ChapelGreek"), $"the Greek village's chapel is white with a blue dome ({view.name})");
            float effect = Services.Effect(chapel);
            Check(effect > 0f, $"the chapel adds {effect:P0} coins per sale in reach");
            CheckSpread(chapel, s => s.SaleBonus, effect, "earn more");

            Weddings.Clear();
            Services.Tick(Services.SecondsToWedding(chapel) + 0.01f); // skip ahead to the next wedding
            Check(Weddings.Count == 1 && Weddings[0] == Services.WeddingPayout(chapel), $"a wedding pays {Services.WeddingPayout(chapel)} coins at the chapel ({Weddings.Count} held)");
            PlaytestTools.FocusCamera(view.transform.position, 12f);
            PlaytestTools.CapturePortraitWithHud("m20_6_chapel.png");
            OpenPanel(view);
            Check(Panel.IsOpen && Panel.BodyText.Contains("Next wedding"), "the chapel's panel shows the next wedding and its payout");
            PlaytestTools.CapturePortraitWithHud("m20_7_chapel_panel.png");
            Panel.Close();
            ServiceLocator.Get<SelectionService>().Clear();
            Check(Buildings.TryUpgrade(0), "hotel started");
        }

        private static void CheckHospital()
        {
            StandController aid = FirstAid;
            StandView view = ViewOf("first_aid");
            Check(aid.Product.IsGrownAt(Buildings.MainTier) && view.IsGrown && aid.Product.NameAt(Buildings.MainTier) == "Hospital",
                "with the Resort, first aid has grown into a hospital");
            Check(Mathf.Approximately(Services.Reach(aid), aid.Product.service.reach * aid.Product.service.grownScale),
                $"the hospital reaches further ({Services.Reach(aid):0} m)");
            PlaytestTools.FocusCamera(view.transform.position, 12f);
            PlaytestTools.CapturePortrait("m20_8_hospital.png");

            SessionState.SetInt(Key + "Stands", Stands.Count);
            ServiceLocator.Get<ISaveService>().Save();
            AutoTestRunner.RestartPlayMode();
        }

        private static void CheckAfterRestart()
        {
            StartScreenView screen = PlaytestTools.Find<StartScreenView>();
            if (screen != null && screen.IsOpen) screen.Close();
            Check(Stands.Count == SessionState.GetInt(Key + "Stands", -1), $"after a restart all {Stands.Count} places are back");
            Check(ViewOf("first_aid").IsGrown && ViewOf("chapel").name.StartsWith("ChapelGreek"), "the hospital and the Greek chapel are still there");
            Check(Staff.CountAt(Restaurant.Index) == 2 && Staff.CountAt(Chapel.Index) == 1, "both cooks and the chapel's carer kept their places");
            Bounds village = new Bounds(Vector3.zero, Vector3.one);
            foreach (StandView view in Object.FindObjectsByType<StandView>()) village.Encapsulate(view.transform.position);
            PlaytestTools.FocusCamera(village.center, 30f + village.size.x);
            PlaytestTools.CapturePortrait("m20_9_village.png");
        }

        // Every stand in reach (not other service buildings) gets the building's effect on top of 1; the rest none.
        private static void CheckSpread(StandController building, System.Func<StandController, float> value, float effect, string what)
        {
            int inReach = 0, wrong = 0;
            for (int i = 0; i < Stands.Count; i++)
            {
                StandController stand = Stands.Get(i);
                if (stand.Product.IsService) continue;
                bool helped = Services.InReach(building, stand);
                if (helped) inReach++;
                if (!Mathf.Approximately(value(stand), helped ? 1f + effect : 1f) && !OtherServiceReaches(building, stand)) wrong++;
            }

            Check(inReach > 0 && wrong == 0, $"{inReach} stands in reach {what}");
        }

        private static bool OtherServiceReaches(StandController building, StandController stand)
        {
            for (int i = 0; i < Stands.Count; i++)
            {
                StandController other = Stands.Get(i);
                if (other != building && other.Product.service.kind == building.Product.service.kind && Services.InReach(other, stand)) return true;
            }

            return false;
        }

        private static void Select(StandView view)
        {
            ServiceLocator.Get<SelectionService>().Clear();
            PlaytestTools.FocusCamera(view.transform.position, 14f);
            PlaytestTools.TapWorld(view.transform.position + Vector3.up * 0.8f);
        }

        private static void OpenPanel(StandView view)
        {
            Select(view);
            PlaytestTools.Click(PlaytestTools.Find<SelectionArrowsView>(), "UpgradeButton");
        }
    }
}
