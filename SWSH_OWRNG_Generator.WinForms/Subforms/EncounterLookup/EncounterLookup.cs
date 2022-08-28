using SWSH_OWRNG_Generator.Core.Encounters;
using System.Collections.Generic;
using System.Windows.Forms;


namespace SWSH_OWRNG_Generator.WinForms
{
    public partial class EncounterLookup : Form
    {
        private static MainWindow MainWindow;

        private readonly static EncounterData Encounters = new();
        public EncounterLookup(MainWindow f)
        {
            InitializeComponent();
            SelectedGame.SelectedIndex = 0;
            foreach (string SpeciesName in Encounters.Data.Keys)
            {
                SelectedSpecies.Items.Add(SpeciesName);
            }
            MainWindow = f;
        }

        private void PopulateEncounterDataTable(object sender, System.EventArgs e)
        {
            if (SelectedSpecies.SelectedItem != null)
            {
                string SpeciesName = (string)SelectedSpecies.SelectedItem;
                PkmInfo Data = Encounters.Data[SpeciesName];

                if (Data != null)
                {
                    if (Data.Encounters != null)
                    {
                        List<PkmResult> Results = new();
                        foreach (EncounterDetails Encounter in Data.Encounters)
                        {
                            if (Encounter.Game! != (string)SelectedGame.SelectedItem) continue;
                            string Item = "なし";
                            int FixedIVs = 0;
                            string AbilityLocked = "なし";
                            string ShinyLocked = "なし";
                            List<string> EggMoves = new();
                            string Ability = "-";
                            string Slot = "-";
                            string Level = (Encounter.Level[0] == Encounter.Level[1]) ? $"{Encounter.Level[0]}" : $"{Encounter.Level[0]} - {Encounter.Level[1]}";
                            if (Data.Item != null && Encounter.EncounterType! != "固定シンボル") Item = (string)Data.Item;
                            if (Data.EggMoves != null) EggMoves = (List<string>)Data.EggMoves;
                            if (Encounter.FixedIVs != null) FixedIVs = (int)Encounter.FixedIVs;
                            if (Encounter.LockedAbility != null) AbilityLocked = (string)Encounter.LockedAbility;
                            if (Encounter.Ability != null) Ability = Encounter.Ability.ToString();
                            if (Encounter.Slots != null) Slot = $"{Encounter.Slots[0]} - {Encounter.Slots[1]}";
                            if (Encounter.ShinyLocked != null) ShinyLocked = (string)Encounter.ShinyLocked;

                            Results.Add(
                                new PkmResult
                                {
                                    Species = SpeciesName,
                                    EncounterType = Encounter.EncounterType!,
                                    Item = Item,
                                    EggMoveCount = EggMoves.Count,
                                    Level = Level,
                                    Slots = Slot,
                                    FixedIVs = FixedIVs,
                                    Weather = Encounter.Weather,
                                    Location = Encounter.Location,
                                    LockedAbility = AbilityLocked,
                                    ShinyLocked = ShinyLocked,
                                    Ability = Ability,
                                }
                            );
                        }
                        BindingSource Source = new() { DataSource = Results };
                        EncounterLookupResults.DataSource = Source;
                        EncounterLookupResults.Columns["EggMoves"].Visible = false;
                        EncounterLookupResults.Columns["Game"].Visible = false;
                        Source.ResetBindings(false);

                        EncounterLookupResults.Columns["Species"].HeaderText = "種族名";
                        EncounterLookupResults.Columns["Item"].HeaderText = "持ち物";
                        EncounterLookupResults.Columns["EggMoveCount"].HeaderText = "タマゴ技数";
                        EncounterLookupResults.Columns["Ability"].HeaderText = "特性";
                        EncounterLookupResults.Columns["Level"].HeaderText = "Lv.";
                        EncounterLookupResults.Columns["Slots"].HeaderText = "スロット";
                        EncounterLookupResults.Columns["EncounterType"].HeaderText = "エンカウントタイプ";
                        EncounterLookupResults.Columns["Weather"].HeaderText = "天候";
                        EncounterLookupResults.Columns["Location"].HeaderText = "場所";
                        EncounterLookupResults.Columns["LockedAbility"].HeaderText = "特性固定";
                        EncounterLookupResults.Columns["FixedIVs"].HeaderText = "V固定数";

                    }
                }
            }
        }

        private void EncounterLookupResults_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            //MainWindow.CheckHeldItem.Checked = (bool)EncounterLookupResults["Item", e.RowIndex].Value;
            MainWindow.CheckHeldItem.Checked = (string)EncounterLookupResults["Item", e.RowIndex].Value == "あり";
            MainWindow.InputEMs.Text = EncounterLookupResults["EggMoveCount", e.RowIndex].Value.ToString();
            MainWindow.CheckIsAbilityLocked.Checked = (string)EncounterLookupResults["Ability", e.RowIndex].Value != "-";

            string[] LevelSubs = ((string)EncounterLookupResults[5, e.RowIndex].Value).Split(" - ");
            string LevelMin = LevelSubs[0], LevelMax;
            if (LevelSubs.Length == 2)
            {
                LevelMax = LevelSubs[1];
            }
            else
            {
                LevelMax = LevelSubs[0];
            }
            MainWindow.InputLevelMin.Text = LevelMin;
            MainWindow.InputLevelMax.Text = LevelMax;

            string[] SlotsSubs = ((string)EncounterLookupResults["Slots", e.RowIndex].Value).Split(" - ");
            string SlotMin = "0", SlotMax = "99";
            if (SlotsSubs.Length == 2)
            {
                SlotMin = SlotsSubs[0];
                SlotMax = SlotsSubs[1];
            }
            MainWindow.InputSlotMin.Text = SlotMin;
            MainWindow.InputSlotMax.Text = SlotMax;

            string EncounterType = (string)EncounterLookupResults["EncounterType", e.RowIndex].Value;//8
            bool Static = EncounterType == "固定シンボル";
            bool Hidden = EncounterType == "ランダムエンカウント";

            string WeatherType = EncounterLookupResults["Weather", e.RowIndex].Value.ToString();
            bool Fishing = WeatherType == "釣り";
            bool Weather = ((WeatherType != "晴れ") & (WeatherType != "全天候"));
            MainWindow.CheckFishing.Checked = Fishing;
            MainWindow.CheckWeather.Checked = Weather;
            MainWindow.CheckStatic.Checked = Static;
            MainWindow.CheckHidden.Checked = Hidden && !Fishing;

            MainWindow.CheckIsAbilityLocked.Checked = (string)EncounterLookupResults["LockedAbility", e.RowIndex].Value == "あり";
            MainWindow.CheckShinyLocked.Checked = (string)EncounterLookupResults["ShinyLocked", e.RowIndex].Value =="あり";
            MainWindow.InputFlawlessIVs.Text = EncounterLookupResults["FixedIVs", e.RowIndex].Value.ToString();
        }
    }
}
