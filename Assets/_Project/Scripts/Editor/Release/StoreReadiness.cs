using System.Collections.Generic;
using BeachTycoon.Data;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// What a Google Play upload still needs, in plain words: the upload key and its passwords, the privacy policy
    /// link (Google Play asks for one from every app with ads) and the LevelPlay keys, without which players would
    /// get rewards from the mock ads.
    /// </summary>
    public static class StoreReadiness
    {
        public static List<string> Problems(MonetizationConfig monetization, bool customKeystore, bool keystoreFound, bool passwordsSet)
        {
            var problems = new List<string>();
            if (!customKeystore || !keystoreFound)
            {
                problems.Add("no upload key: pick or create one in Project Settings > Player > Android > Publishing Settings > Keystore Manager");
            }
            else if (!passwordsSet)
            {
                problems.Add("enter the keystore and key passwords in Publishing Settings (Unity forgets them when it restarts)");
            }

            if (monetization == null) return problems;
            if (string.IsNullOrWhiteSpace(monetization.privacyPolicyUrl))
            {
                problems.Add("set privacyPolicyUrl in MonetizationConfig (Google Play needs a privacy policy for apps with ads)");
            }

            if (string.IsNullOrWhiteSpace(monetization.androidAppKey) || string.IsNullOrWhiteSpace(monetization.androidRewardedAdUnit))
            {
                problems.Add("set the LevelPlay Android app key and rewarded ad unit in MonetizationConfig");
            }

            return problems;
        }
    }
}
