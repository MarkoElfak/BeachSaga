using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BeachTycoon.Data;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Writes the economy balance table (Docs/Economy.md): earnings, wages and the time to the next tier at every
    /// tier, what each new place adds and how soon it pays back, and the three countries side by side.
    /// </summary>
    public static class EconomyReport
    {
        public const string RelativePath = "Docs/Economy.md";

        public static string FullPath => Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", RelativePath);

        [MenuItem("Beach Tycoon/Economy Report", priority = 59)]
        public static void WriteFromMenu()
        {
            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(ProjectPaths.Data + "/GameDatabase.asset");
            Write(database);
            Debug.Log($"[EconomyReport] Wrote {FullPath}");
        }

        /// <summary>"economy" writes the table; "economy donuts=8" writes a what-if with those prices to Logs instead.</summary>
        public static string WriteRequest(string[] words)
        {
            if (words.Length < 2)
            {
                WriteFromMenu();
                return "Wrote " + FullPath + "\nRESULT: done";
            }

            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(ProjectPaths.Data + "/GameDatabase.asset");
            var old = new Dictionary<ProductDefinition, long>();
            try
            {
                for (int i = 1; i < words.Length; i++)
                {
                    string[] pair = words[i].Split('=');
                    ProductDefinition product = pair.Length == 2 ? database.FindProduct(pair[0]) : null;
                    if (product == null || !long.TryParse(pair[1], out long price)) return "RESULT: FAIL bad price " + words[i];
                    old[product] = product.price;
                    product.price = price; // in memory only, put back below
                }

                File.WriteAllText("Logs/Economy-whatif.md", Build(database));
                return "Wrote Logs/Economy-whatif.md\nRESULT: done";
            }
            finally
            {
                foreach (KeyValuePair<ProductDefinition, long> pair in old) pair.Key.price = pair.Value;
            }
        }

        public static string Write(GameDatabase database)
        {
            string text = Build(database);
            Directory.CreateDirectory(Path.GetDirectoryName(FullPath) ?? ".");
            File.WriteAllText(FullPath, text);
            return text;
        }

        public static string Build(GameDatabase database)
        {
            CultureDefinition first = database.cultures != null && database.cultures.Length > 0 ? database.cultures[0] : null;
            var balance = new EconomyBalance(database, first);
            List<TierBalance> rows = balance.Build();
            var text = new StringBuilder();
            text.AppendLine("# Beach Tycoon economy").AppendLine();
            text.AppendLine($"Generated from the game data by Beach Tycoon > Economy Report. Village: {Name(first)}. Every place open to it is open at level 0, ")
                .AppendLine($"and staff go where they add more than their wage (a typical hire: {Coins(balance.Model.WagePerHour)} coins an hour, serving at {balance.Model.WorkerSpeed:0.00}x). ")
                .AppendLine("No boosts, events, mini-games, seasons or stand upgrades; coins per hour of play.").AppendLine();

            text.AppendLine("## Every tier").AppendLine();
            text.AppendLine("| Tier | Visitors/h | Sales/h | Weddings + rent/h | Staff | Wages/h | Net/h | Next (open + build) | Minutes |");
            text.AppendLine("|---|---:|---:|---:|---|---:|---:|---:|---:|");
            foreach (TierBalance row in rows)
            {
                EconomyResult r = row.Result;
                text.AppendLine($"| {row.Tier} {row.Name} | {Coins(r.Visitors)} | {Coins(r.Sales)} | {Coins(r.Weddings + r.Rent)} | {Staff(row.Staffed)} | {Coins(r.Wages)} | " +
                                $"**{Coins(r.Net)}** | {Coins(row.CostToNext)} | {Minutes(row.MinutesToNext)} |");
            }

            text.AppendLine().AppendLine("## New places").AppendLine();
            text.AppendLine("| Place | Opens at | Cost | Sells/h | Adds to the beach/h | Pays back in |");
            text.AppendLine("|---|---|---:|---:|---:|---:|");
            foreach (TierBalance row in rows)
            {
                foreach (PlaceBalance place in row.NewPlaces)
                {
                    if (place.Product.unlockCost <= 0) continue;
                    text.AppendLine($"| {place.Product.displayName} | {row.Name} | {Coins(place.Product.unlockCost)} | {Coins(place.SalesPerHour)} | " +
                                    $"{Coins(place.AddsPerHour)} | {Minutes(place.PaybackMinutes)} |");
                }
            }

            AppendCountries(text, database);
            return text.ToString();
        }

        private static void AppendCountries(StringBuilder text, GameDatabase database)
        {
            if (database.cultures == null || database.cultures.Length == 0) return;
            text.AppendLine().AppendLine("## Countries (net per hour)").AppendLine();
            text.Append("| Tier |");
            foreach (CultureDefinition culture in database.cultures) text.Append($" {Name(culture)} |");
            text.AppendLine().Append("|---|");
            foreach (CultureDefinition unused in database.cultures) text.Append("---:|");
            text.AppendLine();
            var tables = new List<List<TierBalance>>();
            foreach (CultureDefinition culture in database.cultures) tables.Add(new EconomyBalance(database, culture).Build());
            for (int i = 0; i < tables[0].Count; i++)
            {
                text.Append($"| {tables[0][i].Name} |");
                foreach (List<TierBalance> table in tables) text.Append($" {Coins(table[i].Result.Net)} |");
                text.AppendLine();
            }
        }

        private static string Name(CultureDefinition culture)
        {
            if (culture == null) return "none";
            string ability = culture.ability != null && culture.ability.kind != CultureAbilityKind.None ? $" ({culture.ability.name})" : string.Empty;
            return culture.displayName + ability;
        }

        private static string Staff(List<ProductDefinition> staffed)
        {
            if (staffed.Count == 0) return "none";
            var names = new List<string>();
            foreach (ProductDefinition place in staffed) names.Add(place.displayName);
            names.Sort(string.CompareOrdinal);
            return $"{staffed.Count}: " + string.Join(", ", names);
        }

        public static string Coins(double value) => System.Math.Round(value).ToString("#,0", CultureInfo.InvariantCulture);

        private static string Minutes(double minutes) =>
            double.IsInfinity(minutes) || minutes > 9999.0 ? "never" : minutes.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
