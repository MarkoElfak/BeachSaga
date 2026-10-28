using System;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Platform;
using BeachTycoon.Services;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    public sealed partial class GameBootstrap
    {
        // Real store, analytics and ads in builds (when their packages are installed), mocks in the Editor. Ads stay
        // mocked until the LevelPlay keys are set; a bought "remove ads" grants rewards without the video either way.
        private IAnalyticsService AddPlatform(GameSaveData data, MonetizationConfig monetization, PrivacyService privacy,
            out IAdsService ads)
        {
            bool real = !Application.isEditor || monetization.storeServicesInEditor;
            IAnalyticsService analytics = (real ? PlatformServices.CreateAnalytics(privacy.AnalyticsAllowed) : null)
                ?? new MockAnalyticsService(monetization.logAnalyticsToConsole);
            ServiceLocator.Register(Track(analytics));

            string removeAds = monetization.removeAdsProductId;
            IIapService iap = (real ? PlatformServices.CreateIap(data.meta, removeAds, _save.Save) : null)
                ?? new MockIapService(data.meta, removeAds, monetization.mockRemoveAdsPrice);
            ServiceLocator.Register(Track(iap));

            IAdsService video = (real ? PlatformServices.CreateAds(monetization, privacy.HasAnswered, privacy.PersonalizedAds) : null)
                ?? new MockAdsService(monetization.mockAdSeconds);
            ads = new RemoveAdsGate(Track(video), iap, removeAds);
            ServiceLocator.Register(ads);
            return analytics;
        }

        // Ticks and disposes a platform service registered under its interface.
        private T Track<T>(T service) where T : class
        {
            if (service is ITickable tickable) _tickables.Add(tickable);
            if (service is IDisposable disposable) _disposables.Add(disposable);
            return service;
        }
    }
}
