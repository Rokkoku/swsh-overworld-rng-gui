using PKHeX.Core;

namespace SWSH_OWRNG_Generator.Core
{
    public static class Generator
    {
        private static readonly IReadOnlyList<string> Natures = GameInfo.GetStrings(1).Natures;
        private static readonly string[] Natures_JPN = { "がんばりや", "さみしがり", "ゆうかん", "いじっぱり", "やんちゃ", "ずぶとい", "すなお", "のんき", "わんぱく", "のうてんき", "おくびょう", "せっかち", "まじめ", "ようき", "むじゃき", "ひかえめ", "おっとり", "れいせい", "てれや", "うっかりや", "おだやか", "おとなしい", "なまいき", "しんちょう", "きまぐれ" };
        private static readonly string[] PersonalityMarks = { "Rowdy", "AbsentMinded", "Jittery", "Excited", "Charismatic", "Calmness", "Intense", "ZonedOut", "Joyful", "Angry", "Smiley", "Teary", "Upbeat", "Peeved", "Intellectual", "Ferocious", "Crafty", "Scowling", "Kindly", "Flustered", "PumpedUp", "ZeroEnergy", "Prideful", "Unsure", "Humble", "Thorny", "Vigor", "Slump" };

        // Heavily derived from https://github.com/Lincoln-LM/PyNXReader/
        public static List<Frame> Generate(ulong state0, ulong state1, ulong advances, ulong InitialAdvances, IProgress<int> progress, Overworld.Filter Filters, uint NPCs)
        {
            List<Frame> Results = new();

            uint[] IVs;
            bool GenerateLevel = Filters.LevelMin != Filters.LevelMax;
            uint LevelDelta = Filters.LevelMax - Filters.LevelMin + 1;
            uint EC;
            uint PID;
            uint SlotRand = 0;
            uint Level = 0;
            uint BrilliantRand;
            uint Nature;
            uint AbilityRoll;
            uint FixedSeed;
            uint ShinyXOR;
            uint BrilliantThreshold;
            uint BrilliantRolls;
            int BrilliantIVs;
            string Gender;
            bool PassIVs, Brilliant, Shiny;
            ulong advance = 0;
            string Jump = string.Empty;

            (BrilliantThreshold, BrilliantRolls) = Util.Common.GenerateBrilliantInfo(Filters.KOs);

            ulong ProgressUpdateInterval = advances / 100;
            if (ProgressUpdateInterval == 0)
                ProgressUpdateInterval++;

            Xoroshiro128Plus go = new(state0, state1);

            for (ulong i = 0; i < InitialAdvances; i++)
            {
                go.Next();
            }

            while (advance < advances)
            {
                if (progress != null && (advance % ProgressUpdateInterval == 0))
                    progress.Report(1);
                long totalRolls = 1; // Placeholder to ensure this is run once normally, also accounts for rolls not being determined yet
                for (long rollToCheck = 0; rollToCheck < totalRolls; rollToCheck++)
                {
                    // Init new RNG
                    (ulong s0, ulong s1) = go.GetState();
                    Xoroshiro128Plus rng = new(s0, s1);
                    if (Filters.MenuClose)
                    {
                        Jump = $"+{MenuClose.Generator.GetAdvances(rng, NPCs)}";
                        rng = MenuClose.Generator.Advance(ref rng, NPCs);
                    }
                    Brilliant = false;
                    Gender = "";
                    uint LeadRand;
                    if (Filters.Static)
                    {
                        LeadRand = (uint)rng.NextInt(100);
                        if (Filters.CuteCharm && LeadRand < 66)
                            Gender = "先頭メロボ";
                    }
                    else
                    {
                        if (!Filters.Fishing)
                            rng.NextInt();
                        rng.NextInt(100);

                        LeadRand = (uint)rng.NextInt(100);
                        if (Filters.CuteCharm && LeadRand < 66)
                            Gender = "先頭メロボ";

                        SlotRand = (uint)rng.NextInt(100);
                        if (Filters.SlotMin > SlotRand || Filters.SlotMax < SlotRand)
                            continue;

                        if (GenerateLevel)
                        {
                            Level = Filters.LevelMin + (uint)rng.NextInt(LevelDelta);
                        }
                        else
                        {
                            Level = Filters.LevelMin;
                        }

                        Util.Common.GenerateMark(ref rng, Filters.Weather, Filters.Fishing, Filters.MarkRolls); // Double Mark Gen happens always

                        if (!Filters.Hidden)
                        {
                            BrilliantRand = (uint)rng.NextInt(1000);
                            if (BrilliantRand < BrilliantThreshold)
                            {
                                Brilliant = true;
                                Level = Filters.LevelMax;
                            }
                            if ((Filters.DesiredAura == "あり" && !Brilliant) || (Filters.DesiredAura == "なし" && Brilliant))
                                continue;
                        }
                    }

                    Shiny = false;
                    uint MockPID = 0;
                    if (Filters.TIDSIDSearch)
                        totalRolls = Filters.ShinyRolls + (Brilliant ? BrilliantRolls : 0);
                    if (!Filters.ShinyLocked)
                    {
                        for (int roll = 0; roll < Filters.ShinyRolls + (Brilliant ? BrilliantRolls : 0); roll++)
                        {
                            MockPID = (uint)rng.Next();
                            if (Filters.TIDSIDSearch)
                            {
                                Shiny = roll == rollToCheck; // Account for pid loop ending early
                                Filters.TSV = Util.Common.GetTSV(MockPID >> 16, MockPID & 0xFFFF);
                            }
                            else
                                Shiny = Util.Common.GetTSV(Util.Common.GetTSV(MockPID >> 16, MockPID & 0xFFFF), Filters.TSV) < 16;
                            if (Shiny)
                                break;
                        }
                    }

                    // Gender
                    if (Gender != "先頭メロボ")
                        Gender = rng.NextInt(2) == 0 ? "♀" : "♂";
                    // Nature
                    Nature = (uint)rng.NextInt(25);
                    // Ability
                    AbilityRoll = 2;
                    if (!Filters.AbilityLocked)
                        AbilityRoll = (uint)rng.NextInt(2);

                    // Held Item
                    if (!Filters.Static && Filters.HeldItem)
                        rng.NextInt(100);

                    BrilliantIVs = 0;
                    if (Brilliant)
                    {
                        // Brilliant IVs
                        BrilliantIVs = (int)rng.NextInt(2) | 2;
                        // Brilliant Egg Move
                        if (Filters.EggMoveCount > 1)
                            rng.NextInt(Filters.EggMoveCount);
                    }

                    FixedSeed = (uint)rng.Next();
                    (EC, PID, IVs, ShinyXOR, PassIVs) = Util.Common.CalculateFixed(FixedSeed, Filters.TSV, Shiny, (int)(Filters.FlawlessIVs + BrilliantIVs), Filters.MinIVs!, Filters.MaxIVs!);

                    if (!PassIVs ||
                        (Filters.DesiredShiny == "◆" && ShinyXOR != 0) ||
                        (Filters.DesiredShiny == "★" && (ShinyXOR > 15 || ShinyXOR == 0)) ||
                        (Filters.DesiredShiny == "★/◆" && ShinyXOR > 15) ||
                        (Filters.DesiredShiny == "通常色" && ShinyXOR < 16)
                        )
                        continue;

                    string Mark = Util.Common.GenerateMark(ref rng, Filters.Weather, Filters.Fishing, Filters.MarkRolls);

                    if (!PassesMarkFilter(Mark, Filters.DesiredMark!))
                        continue;

                    if (!PassesNatureFilter(Natures_JPN[(int)Nature], Filters.DesiredNature!))
                        continue;

                    if(Filters.DesiredGender == "性別不明")
                    {
                        Gender = "-";
                    }
                    if (!PassesGenderFilter(Gender, Filters.DesiredGender!))
                        continue;

                    // Passes all filters!
                    (ulong _s0, ulong _s1) = go.GetState();
                    Results.Add(
                        new Frame
                        {
                            Advances = Filters.TIDSIDSearch ? $"{(-(long)(advance + InitialAdvances)):N0} | Roll: {rollToCheck:N0}" : (advance + InitialAdvances).ToString("N0"),
                            TID = (ushort)(MockPID >> 16),
                            SID = (ushort)MockPID,
                            Animation = (_s0 & 1) ^ (_s1 & 1),
                            Jump = Jump,
                            Level = Level,
                            Slot = SlotRand,
                            PID = PID.ToString("X8"),
                            EC = EC.ToString("X8"),
                            Shiny = ShinyXOR == 0 ? "◆" : (ShinyXOR < 16 ? "★" : "-"),
                            Brilliant = Brilliant ? "あり" : "-",
                            Ability = AbilityRoll == 0 ? 1 : 0,
                            Nature = Natures_JPN[(int)Nature],
                            Gender = Gender,
                            HP = IVs[0],
                            Atk = IVs[1],
                            Def = IVs[2],
                            SpA = IVs[3],
                            SpD = IVs[4],
                            Spe = IVs[5],
                            Mark = Mark,
                            State0 = _s0.ToString("X16"),
                            State1 = _s1.ToString("X16"),
                        }
                    );
                }
                if (Filters.TIDSIDSearch)
                    go.Prev();
                else
                    go.Next();
                advance++;
            }

            return Results;
        }


        private static bool PassesMarkFilter(string Mark, string DesiredMark)
        {
            return !((DesiredMark == "証あり" && Mark == "-") || (DesiredMark == "雰囲気証" && (Mark == "-" || Mark == "ときどきみる(ひとになれている)" || Mark == "時間帯" || Mark == "天候" || Mark == "つりあげられた(つりたてピチピチの)" || Mark == "みたことのない(ひとをしらない)")) || (DesiredMark != "(指定なし)" && DesiredMark != "証あり" && DesiredMark != "雰囲気証" && Mark != DesiredMark));
        }

        private static bool PassesNatureFilter(string Nature, string DesiredNature)
        {
            return (DesiredNature == Nature) || (DesiredNature == "(指定なし)");
        }

        private static bool PassesGenderFilter(string Gender, string DesiredGender)
        {
            return (DesiredGender == Gender) || (DesiredGender == "(指定なし)")||(Gender == "-");
        }

        public static string GenerateRetailSequence(ulong state0, ulong state1, uint start, uint max, IProgress<int> progress)
        {
            Xoroshiro128Plus go = new(state0, state1);
            for (int i = 0; i < start; i++)
                go.Next();

            string ret = string.Empty;
            ulong ProgressUpdateInterval = (start + max) / 100;
            if (ProgressUpdateInterval == 0)
                ProgressUpdateInterval++;

            for (uint i = 0; i < max; i++)
            {
                if (progress != null && (i % ProgressUpdateInterval == 0))
                    progress.Report(1);
                ret += go.Next() & 1;
            }

            return ret;
        }
    }
}
