using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ToxicFloraMod
{
    public static class TreeUtil
    {
        // 是否為「真正的伐木」：是樹，且是木材樹種或正在執行砍除指令
        public static bool IsWoodcutting(Plant plant, Pawn by)
        {
            if (plant == null || by == null || plant.def?.plant == null) return false;
            if (!plant.def.plant.IsTree) return false;
            if (plant.def.plant.harvestTag == "Wood") return true;
            
            return by.CurJob != null && by.CurJob.def == JobDefOf.CutPlant;
        }
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.PlantCollected))]
    public static class Patch_TreeCut
    {
        public static void Postfix(Plant __instance, Pawn by)
        {
            if (!TreeUtil.IsWoodcutting(__instance, by)) return;
            TreeAccident.Roll(by);
        }
    }

    public static class TreeAccident
    {
        static void AddRecord(Pawn pawn, string defName)
        {
            if (pawn == null || pawn.Dead || !Notify.ShouldShow(pawn)) return;
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
            if (def == null) return;
            pawn.health.AddHediff(HediffMaker.MakeHediff(def, pawn));
        }

        public static void Roll(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || !pawn.RaceProps.Humanlike) return;

            int skill = pawn.skills?.GetSkill(SkillDefOf.Plants)?.Level ?? 0;

            // 1. 斧頭滑脫
            if (ToxicFloraModMain.settings.enableTreeSlip)
            {
                float failChance = UnityEngine.Mathf.Lerp(
                    ToxicFloraModMain.settings.treeSlipNoSkill,
                    ToxicFloraModMain.settings.treeSlipMaxSkill,
                    skill / 20f);

                if (Rand.Chance(failChance))
                {
                    BodyPartRecord part = pawn.health.hediffSet.GetRandomNotMissingPart(
                        DamageDefOf.Cut, BodyPartHeight.Undefined, BodyPartDepth.Outside);

                    pawn.TakeDamage(new DamageInfo(DamageDefOf.Cut, Rand.Range(4, 10), 0f, -1f, null, part));
                    AddRecord(pawn, "ToxicTreeRecordSlip");

                    if (Notify.ShouldShow(pawn))
                    {
                        Messages.Message("ToxicFlora_TreeSlipMsg".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.NegativeHealthEvent);
                    }
                    return; 
                }
            }

            // 2. 樹幹砸中
            if (ToxicFloraModMain.settings.enableTreeHit)
            {
                float hitChance = UnityEngine.Mathf.Lerp(
                    ToxicFloraModMain.settings.treeHitNoSkill,
                    ToxicFloraModMain.settings.treeHitMaxSkill,
                    skill / 20f);

                if (Rand.Chance(hitChance))
                {
                    bool hitHead = Rand.Chance(ToxicFloraModMain.settings.treeHeadHitShare);
                    float dmgAmount = hitHead ? Rand.Range(10, 22) : Rand.Range(8, 18);
                    
                    BodyPartRecord part = null;

                    // 如果判定為砸中頭部，精準鎖定「Head」部位，避免波及眼睛或耳朵等脆弱器官
                    if (hitHead)
                    {
                        part = pawn.health.hediffSet.GetNotMissingParts().FirstOrDefault(x => x.def == BodyPartDefOf.Head);
                    }
                    
                    // 備用方案：如果不是砸中頭，或是該生物因模組/異星人設定沒有標準的頭部
                    if (part == null)
                    {
                        part = pawn.health.hediffSet.GetRandomNotMissingPart(
                            DamageDefOf.Blunt,
                            hitHead ? BodyPartHeight.Top : BodyPartHeight.Middle,
                            BodyPartDepth.Outside);
                    }
                    
                    pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, dmgAmount, 0f, -1f, null, part));

                    if (!Notify.ShouldShow(pawn)) return;

                    if (hitHead)
                    {
                        AddRecord(pawn, "ToxicTreeRecordHead");
                        
                        float concussionChance = dmgAmount > 15 ? 0.8f : 0.3f;
                        if (Rand.Chance(concussionChance))
                        {
                            HediffDef concDef = DefDatabase<HediffDef>.GetNamedSilentFail("ToxicTreeConcussion");
                            if (concDef != null)
                            {
                                Hediff concussion = HediffMaker.MakeHediff(concDef, pawn);
                                concussion.Severity = dmgAmount * 0.05f; 
                                pawn.health.AddHediff(concussion);
                            }
                        }

                        Find.LetterStack.ReceiveLetter(
                            "ToxicFlora_TreeHeadLabel".Translate(),
                            "ToxicFlora_TreeHeadText".Translate(pawn.Named("PAWN")),
                            LetterDefOf.NegativeEvent,
                            pawn);
                    }
                    else
                    {
                        AddRecord(pawn, "ToxicTreeRecordHit");
                        Messages.Message("ToxicFlora_TreeHitMsg".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.NegativeHealthEvent);
                    }
                }
            }
        }
    }
}