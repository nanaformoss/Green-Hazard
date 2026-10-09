using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ToxicFloraMod
{
    public class ToxicFloraSettings : ModSettings
    {
        public bool enableHumanAllergy = true;
        public bool enableAnimalPoison = true;
        public bool enableTreeSlip = true;
        public bool enableTreeHit = true;

        public float harvestAllergyChance = 0.002f;
        public float animalGrazeChance = 0.0008f;
        public float poisonBranch = 0.10f;
        public float madnessChance = 0.10f;

        public float treeSlipNoSkill = 0.01f;
        public float treeSlipMaxSkill = 0.002f;
        public float treeHitNoSkill = 0.01f;
        public float treeHitMaxSkill = 0.002f;
        public float treeHeadHitShare = 0.30f;

        private void SetFlora(bool hard)
        {
            enableHumanAllergy = true;
            enableAnimalPoison = true;
            harvestAllergyChance = hard ? 0.01f : 0.002f;
            animalGrazeChance = hard ? 0.001f : 0.0008f;
            poisonBranch = hard ? 0.05f : 0.10f;
            madnessChance = hard ? 0.25f : 0.10f;
        }

        private void SetTree(bool hard)
        {
            enableTreeSlip = true;
            enableTreeHit = true;
            treeSlipNoSkill = hard ? 0.05f : 0.01f;
            treeSlipMaxSkill = 0.002f;
            treeHitNoSkill = hard ? 0.02f : 0.01f;
            treeHitMaxSkill = 0.002f;
            treeHeadHitShare = hard ? 0.20f : 0.30f;
        }

        public void ResetFlora() { SetFlora(false); }
        public void ResetTree() { SetTree(false); }
        public void ApplyNormal() { SetFlora(false); SetTree(false); }
        public void ApplyHard() { SetFlora(true); SetTree(true); }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enableHumanAllergy, "enableHumanAllergy", true);
            Scribe_Values.Look(ref enableAnimalPoison, "enableAnimalPoison", true);
            Scribe_Values.Look(ref enableTreeSlip, "enableTreeSlip", true);
            Scribe_Values.Look(ref enableTreeHit, "enableTreeHit", true);

            Scribe_Values.Look(ref harvestAllergyChance, "harvestAllergyChance", 0.002f);
            Scribe_Values.Look(ref animalGrazeChance, "animalGrazeChance", 0.0008f);
            Scribe_Values.Look(ref poisonBranch, "poisonBranch", 0.10f);
            Scribe_Values.Look(ref madnessChance, "madnessChance", 0.10f);

            Scribe_Values.Look(ref treeSlipNoSkill, "treeSlipNoSkill", 0.01f);
            Scribe_Values.Look(ref treeSlipMaxSkill, "treeSlipMaxSkill", 0.002f);
            Scribe_Values.Look(ref treeHitNoSkill, "treeHitNoSkill", 0.01f);
            Scribe_Values.Look(ref treeHitMaxSkill, "treeHitMaxSkill", 0.002f);
            Scribe_Values.Look(ref treeHeadHitShare, "treeHeadHitShare", 0.30f);
        }
    }

    public class ToxicFloraModMain : Mod
    {
        public static ToxicFloraSettings settings;
        private const float RowHeight = 50f;
        private const float TabTopGap = 12f;

        private enum Tab { Flora, Tree }
        private Tab curTab = Tab.Flora;

        public ToxicFloraModMain(ModContentPack content) : base(content)
        {
            settings = GetSettings<ToxicFloraSettings>();
            new Harmony("nana.toxicflora").PatchAll();
        }

        public override string SettingsCategory() => "Green Hazard: Toxic Flora and Woodcutting";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            const float tabRowH = 32f;
            const float frameBottomGap = 10f;
            float frameTop = inRect.y + TabTopGap + tabRowH;
            Rect frameRect = new Rect(inRect.x, frameTop, inRect.width, inRect.yMax - frameBottomGap - frameTop);

            Widgets.DrawBoxSolid(frameRect, new Color(0.1f, 0.1f, 0.1f, 0.4f));
            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            Widgets.DrawBox(frameRect, 1);
            GUI.color = Color.white;

            string tab1Label = "ToxicFlora_TabFlora".Translate();
            string tab2Label = "ToxicFlora_TabTree".Translate();
            Text.Font = GameFont.Small;
            
            float calcWidth1 = Text.CalcSize(tab1Label).x + 30f;
            float calcWidth2 = Text.CalcSize(tab2Label).x + 30f;
            float tabWidth = Mathf.Max(140f, Mathf.Max(calcWidth1, calcWidth2));

            Rect tab1Rect = new Rect(frameRect.x, frameRect.y - tabRowH, tabWidth, tabRowH);
            Rect tab2Rect = new Rect(frameRect.x + tabWidth + 2f, frameRect.y - tabRowH, tabWidth, tabRowH);

            DrawFlatTab(tab1Rect, tab1Label, curTab == Tab.Flora, () => curTab = Tab.Flora);
            DrawFlatTab(tab2Rect, tab2Label, curTab == Tab.Tree, () => curTab = Tab.Tree);

            Rect resetTabRect = new Rect(frameRect.xMax - 140f, frameRect.y - tabRowH, 140f, tabRowH - 2f);
            if (Widgets.ButtonText(resetTabRect, "ToxicFlora_ResetTab".Translate()))
            {
                if (curTab == Tab.Flora) settings.ResetFlora();
                else settings.ResetTree();
            }
            TooltipHandler.TipRegion(resetTabRect, "ToxicFlora_ResetTabTip".Translate());

            const float padX = 14f; 
            const float padY = 14f; 
            Rect innerRect = new Rect(frameRect.x + padX, frameRect.y + padY,
                frameRect.width - padX * 2f, frameRect.height - padY * 2f);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(innerRect);
            Text.Font = GameFont.Small;

            if (curTab == Tab.Flora)
            {
                DrawToggleHeader(listing, "ToxicFlora_EnableHumanAllergy".Translate(), ref settings.enableHumanAllergy);
                if (settings.enableHumanAllergy)
                {
                    DrawRow(listing, "ToxicFlora_HarvestAllergyChance".Translate(), ref settings.harvestAllergyChance, 0f, 0.02f, 0.001f, "P1", "ToxicFlora_FreqHarvest".Translate());
                }
                listing.Gap(24f);

                DrawToggleHeader(listing, "ToxicFlora_EnableAnimalPoison".Translate(), ref settings.enableAnimalPoison);
                if (settings.enableAnimalPoison)
                {
                    DrawRow(listing, "ToxicFlora_AnimalGrazeChance".Translate(), ref settings.animalGrazeChance, 0f, 0.002f, 0.0001f, "P2", "ToxicFlora_FreqGraze".Translate());
                    DrawRow(listing, "ToxicFlora_PoisonBranch".Translate(), ref settings.poisonBranch, 0f, 0.5f, 0.01f, "P0");
                    DrawRow(listing, "ToxicFlora_MadnessChance".Translate(), ref settings.madnessChance, 0f, 0.5f, 0.01f, "P0");
                    float mild = Mathf.Max(0f, 1f - settings.poisonBranch - settings.madnessChance);
                    DrawReadOnlySliderRow(listing, "ToxicFlora_MildLabel".Translate(), mild, "P0", "ToxicFlora_MildTooltip".Translate());
                }
            }
            else
            {
                DrawToggleHeader(listing, "ToxicFlora_EnableTreeSlip".Translate(), ref settings.enableTreeSlip);
                if (settings.enableTreeSlip)
                {
                    DrawRow(listing, "ToxicFlora_TreeSlipNoSkill".Translate(), ref settings.treeSlipNoSkill, 0f, 0.10f, 0.001f, "P1", "ToxicFlora_FreqTree".Translate());
                    DrawRow(listing, "ToxicFlora_TreeSlipMaxSkill".Translate(), ref settings.treeSlipMaxSkill, 0f, 0.03f, 0.001f, "P1", "ToxicFlora_FreqTree".Translate());
                    settings.treeSlipMaxSkill = Mathf.Min(settings.treeSlipMaxSkill, settings.treeSlipNoSkill);
                }
                listing.Gap(24f);

                DrawToggleHeader(listing, "ToxicFlora_EnableTreeHit".Translate(), ref settings.enableTreeHit);
                if (settings.enableTreeHit)
                {
                    DrawRow(listing, "ToxicFlora_TreeHitNoSkill".Translate(), ref settings.treeHitNoSkill, 0f, 0.05f, 0.001f, "P1", "ToxicFlora_FreqTree".Translate());
                    DrawRow(listing, "ToxicFlora_TreeHitMaxSkill".Translate(), ref settings.treeHitMaxSkill, 0f, 0.02f, 0.001f, "P1", "ToxicFlora_FreqTree".Translate());
                    settings.treeHitMaxSkill = Mathf.Min(settings.treeHitMaxSkill, settings.treeHitNoSkill);
                    DrawRow(listing, "ToxicFlora_TreeHeadHitShare".Translate(), ref settings.treeHeadHitShare, 0f, 0.5f, 0.05f, "P0");
                }
            }

            listing.End();

            float btnW = 120f;
            float btnH = 40f;
            float gap = 16f;
            float yOffset = 3f; 

            float closeX = inRect.x + inRect.width / 2f - btnW / 2f;
            float btnY = inRect.yMax + yOffset;

            Rect normalRect = new Rect(closeX - btnW - gap, btnY, btnW, btnH);
            Rect hardRect = new Rect(closeX + btnW + gap, btnY, btnW, btnH);

            if (Widgets.ButtonText(normalRect, "ToxicFlora_BtnNormal".Translate()))
            {
                settings.ApplyNormal();
            }
            TooltipHandler.TipRegion(normalRect, "ToxicFlora_BtnNormalTip".Translate());

            if (Widgets.ButtonText(hardRect, "ToxicFlora_BtnHard".Translate()))
            {
                settings.ApplyHard();
            }
            TooltipHandler.TipRegion(hardRect, "ToxicFlora_BtnHardTip".Translate());

            base.DoSettingsWindowContents(inRect);
        }

        private void DrawFlatTab(Rect rect, string label, bool selected, System.Action onClick)
        {
            if (selected)
            {
                Widgets.DrawAtlas(rect, Widgets.ButtonBGAtlas);
            }
            else
            {
                Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.8f));
                Widgets.DrawHighlightIfMouseover(rect);
                GUI.color = new Color(1f, 1f, 1f, 0.1f);
                Widgets.DrawBox(rect, 1);
                GUI.color = Color.white;

                if (Widgets.ButtonInvisible(rect))
                {
                    onClick?.Invoke();
                }
            }

            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = selected ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            Widgets.Label(rect, label);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawToggleHeader(Listing_Standard listing, string label, ref bool value)
        {
            Rect rect = listing.GetRect(36f);

            GameFont oldFont = Text.Font;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width - 60f, rect.height), label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = oldFont;

            float boxSize = 24f;
            float boxX = rect.xMax - 20f - boxSize;
            float boxY = rect.y + (rect.height - boxSize) / 2f;
            Widgets.Checkbox(boxX, boxY, ref value, boxSize);

            listing.Gap(8f);
        }

        private void GetRowRects(Rect rect, out Rect labelRect, out Rect valueRect, out Rect sliderRect)
        {
            sliderRect = new Rect(rect.x + rect.width / 2f, rect.y + 26f, rect.width / 2f - 20f, 20f);
            valueRect = new Rect(sliderRect.x, rect.y, sliderRect.width, 22f);
            labelRect = new Rect(rect.x + 24f, rect.y + 22f, rect.width / 2f - 24f, 28f);
        }

        private void DrawRow(Listing_Standard listing, string label, ref float val,
            float min, float max, float step, string format, string freqUnit = null)
        {
            val = Mathf.Clamp(val, min, max);

            Rect rect = listing.GetRect(RowHeight);
            Rect labelRect, valueRect, sliderRect;
            GetRowRects(rect, out labelRect, out valueRect, out sliderRect);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(valueRect, val.ToString(format));
            Text.Anchor = TextAnchor.UpperLeft;

            float newVal = Widgets.HorizontalSlider(sliderRect, val, min, max);
            val = Mathf.Clamp(Mathf.Round(newVal / step) * step, min, max);

            if (freqUnit != null)
            {
                string tip = val > 0f
                    ? "ToxicFlora_AverageFreq".Translate(Mathf.RoundToInt(1f / val), freqUnit)
                    : "ToxicFlora_NeverOccur".Translate();
                TooltipHandler.TipRegion(rect, tip);
            }

            listing.Gap(4f);
        }

        private void DrawReadOnlySliderRow(Listing_Standard listing, string label, float value,
            string format, string tooltip)
        {
            Rect rect = listing.GetRect(RowHeight);
            Rect labelRect, valueRect, sliderRect;
            GetRowRects(rect, out labelRect, out valueRect, out sliderRect);

            GUI.color = Color.gray;

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(valueRect, value.ToString(format));
            Text.Anchor = TextAnchor.UpperLeft;

            bool oldEnabled = GUI.enabled;
            GUI.enabled = false;
            Widgets.HorizontalSlider(sliderRect, value, 0f, 1f);
            GUI.enabled = oldEnabled;

            GUI.color = Color.white;

            if (!string.IsNullOrEmpty(tooltip))
                TooltipHandler.TipRegion(rect, tooltip);

            listing.Gap(4f);
        }
    }

    public static class Notify
    {
        public static bool ShouldShow(Pawn p) => p != null && p.Faction == Faction.OfPlayer;
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.PlantCollected))]
    public static class Patch_PlantCollected
    {
        static void AddOrRefresh(Pawn p, HediffDef def)
        {
            if (def == null) return;
            var old = p.health.hediffSet.GetFirstHediffOfDef(def);
            if (old != null) p.health.RemoveHediff(old);
            p.health.AddHediff(HediffMaker.MakeHediff(def, p));
        }

        public static void Postfix(Plant __instance, Pawn by)
        {
            if (by == null || by.Dead || !by.RaceProps.Humanlike) return;
            if (!ToxicFloraModMain.settings.enableHumanAllergy) return;
            
            if (!Rand.Chance(ToxicFloraModMain.settings.harvestAllergyChance)) return;

            // 呼叫共用的 TreeUtil 來精準判斷是否為伐木
            bool isWoodcutting = TreeUtil.IsWoodcutting(__instance, by);
            string type = isWoodcutting ? "WoodDust" : new[] { "Resp", "Skin", "Eye" }.RandomElement();

            HediffDef antibodyDef = DefDatabase<HediffDef>.GetNamedSilentFail("ToxicAllergyAntibody_" + type);
            HediffDef allergyDef = DefDatabase<HediffDef>.GetNamedSilentFail("ToxicPlantAllergy_" + type);
            if (allergyDef == null) return;

            if (antibodyDef != null && by.health.hediffSet.HasHediff(antibodyDef) && !Rand.Chance(0.1f))
                return;

            AddOrRefresh(by, allergyDef);
            AddOrRefresh(by, antibodyDef);

            if (Notify.ShouldShow(by))
                Messages.Message("ToxicFlora_AllergyMsg".Translate(by.Named("PAWN")), by, MessageTypeDefOf.NegativeHealthEvent);
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.Ingested))]
    public static class Patch_Ingested
    {
        public static void Postfix(Thing __instance, Pawn ingester)
        {
            if (!ToxicFloraModMain.settings.enableAnimalPoison) return;

            // 只處理植物，乾草、飼料、肉等其他食物直接略過
            if (!(__instance is Plant p)) return;

            if (ingester == null || ingester.Dead || !ingester.RaceProps.Animal) return;

            if (p.sown) return;

            // 恢復成讀取設定面板的中毒機率
            float finalChance = ToxicFloraModMain.settings.animalGrazeChance;

            HediffDef adaptationDef = DefDatabase<HediffDef>.GetNamedSilentFail("ToxicAnimalAdaptation");
            if (adaptationDef != null)
            {
                Hediff hediff = ingester.health.hediffSet.GetFirstHediffOfDef(adaptationDef);
                if (hediff != null && hediff.Visible)
                {
                    finalChance *= 0.2f;
                }
            }

            if (!Rand.Chance(finalChance)) return;

            // 恢復成讀取設定面板的症狀分支機率
            float poison = ToxicFloraModMain.settings.poisonBranch;
            float madness = ToxicFloraModMain.settings.madnessChance;
            float roll = Rand.Value;

            if (roll < poison)
            {
                HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail("ToxicPlantPoison");
                if (def != null)
                {
                    Hediff h = HediffMaker.MakeHediff(def, ingester);
                    h.Severity = 0.2f;
                    ingester.health.AddHediff(h);
                    if (Notify.ShouldShow(ingester))
                    {
                        Messages.Message("ToxicFlora_PoisonMsg".Translate(ingester.Named("PAWN")), ingester, MessageTypeDefOf.NegativeHealthEvent);
                    }
                }
            }
            else if (roll < poison + madness)
            {
                HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail("ToxicPlantMadness");
                if (def != null)
                {
                    ingester.health.AddHediff(HediffMaker.MakeHediff(def, ingester));
                    ingester.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, "ToxicFlora_MadnessReason".Translate(), true);

                    bool isTame = ingester.Faction == Faction.OfPlayer;
                    string letterTextKey = isTame ? "ToxicFlora_MadnessLetterText_Tame" : "ToxicFlora_MadnessLetterText_Wild";

                    Find.LetterStack.ReceiveLetter(
                        "ToxicFlora_MadnessLetterLabel".Translate(ingester.Named("PAWN")),
                        letterTextKey.Translate(ingester.Named("PAWN")),
                        LetterDefOf.ThreatBig,
                        ingester
                    );
                }
            }
            else
            {
                HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail("ToxicPlantMild");
                if (def != null)
                    ingester.health.AddHediff(HediffMaker.MakeHediff(def, ingester));
            }

            if (adaptationDef != null && !ingester.health.hediffSet.HasHediff(adaptationDef))
            {
                ingester.health.AddHediff(HediffMaker.MakeHediff(adaptationDef, ingester));
            }
        }
    }
}