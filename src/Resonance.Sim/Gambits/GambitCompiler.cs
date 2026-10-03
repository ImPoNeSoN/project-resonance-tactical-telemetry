using Resonance.Sim.Core;
using Resonance.Sim.Data;

namespace Resonance.Sim.Gambits;

/// <summary>
/// Compiles the 04 §4.12 text form into a slot array. Compilation may allocate.
/// <see cref="GambitMachine"/> does not.
/// </summary>
public static class GambitCompiler
{
    public static GambitCompileResult Compile(string text, GambitCompileOptions options)
    {
        var errors = new List<string>();
        var slots = new List<GambitSlot>();
        int conditions = 0;
        int defers = 0;
        string[] lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            if (!TryParseLine(line, i + 1, options, errors, out GambitSlot slot))
            {
                continue;
            }

            conditions += slot.Count;
            if (slot.Verb == GambitVerb.Defer)
            {
                defers++;
            }

            slots.Add(slot);
        }

        if (slots.Count == 0)
        {
            errors.Add("Deck has no gambits.");
        }

        if (slots.Count > options.MaxSlots)
        {
            errors.Add($"Deck has {slots.Count} slots; this hero's limit is {options.MaxSlots}.");
        }

        if (conditions > SimConst.GambitLogicBudget)
        {
            errors.Add($"Deck uses {conditions} conditions; the logic budget is {SimConst.GambitLogicBudget}.");
        }

        if (defers > SimConst.GambitMaxDeferSlots)
        {
            errors.Add($"Deck has {defers} DEFER slots; the limit is {SimConst.GambitMaxDeferSlots}.");
        }

        return new GambitCompileResult
        {
            Ok = errors.Count == 0,
            Program = new GambitProgram
            {
                Slots = slots.ToArray(),
                Source = text,
            },
            Errors = errors.ToArray(),
        };
    }

    private static bool TryParseLine(string line, int lineNo, GambitCompileOptions options, List<string> errors, out GambitSlot slot)
    {
        slot = default;
        var scan = new Scan(line);
        scan.Skip();
        if (scan.PeekDigit())
        {
            scan.ReadInt();
            scan.Skip();
            if (scan.Try('.' ) || scan.Try(')'))
            {
                scan.Skip();
            }
        }

        int mark = scan.Index;
        if (scan.TryWord(out string word) && !IsKeyword(word, "IF") && scan.Try(':'))
        {
            scan.Skip();
        }
        else
        {
            scan.Index = mark;
        }

        if (!scan.TryWord(out word) || !IsKeyword(word, "IF"))
        {
            errors.Add($"Line {lineNo}: expected IF.");
            return false;
        }

        scan.Skip();
        var conditions = new List<GambitCondition>();
        while (true)
        {
            if (!TryCondition(ref scan, lineNo, errors, out GambitCondition condition))
            {
                return false;
            }

            conditions.Add(condition);
            scan.Skip();
            if (scan.StartsWith("->"))
            {
                break;
            }

            if (scan.TryWord(out word) && IsKeyword(word, "AND"))
            {
                scan.Skip();
                continue;
            }

            errors.Add($"Line {lineNo}: expected AND or ->.");
            return false;
        }

        if (conditions.Count < 1 || conditions.Count > SimConst.GambitConditionsPerSlot)
        {
            errors.Add($"Line {lineNo}: a slot needs 1 to {SimConst.GambitConditionsPerSlot} conditions.");
            return false;
        }

        scan.Skip();
        if (!scan.StartsWith("->"))
        {
            errors.Add($"Line {lineNo}: expected ->.");
            return false;
        }

        scan.Index += 2;
        scan.Skip();
        if (!scan.TryWord(out string verbText))
        {
            errors.Add($"Line {lineNo}: expected an action verb.");
            return false;
        }

        GambitVerb verb = VerbOf(verbText);
        if (verb == GambitVerb.Cast && !IsCastVerb(verbText))
        {
            errors.Add($"Line {lineNo}: unknown verb {verbText}.");
            return false;
        }

        scan.Skip();
        if (!scan.Try('[') || !scan.ReadUntil(']', out string actionBody))
        {
            errors.Add($"Line {lineNo}: expected [action].");
            return false;
        }

        scan.Skip();
        slot.Verb = verb;
        slot.Count = (byte)conditions.Count;
        slot.C0 = conditions[0];
        if (conditions.Count > 1)
        {
            slot.C1 = conditions[1];
        }

        if (conditions.Count > 2)
        {
            slot.C2 = conditions[2];
        }

        if (!BindAction(ref slot, verb, actionBody.Trim(), lineNo, options, errors))
        {
            return false;
        }

        if (scan.TryWord(out word) && IsKeyword(word, "ON"))
        {
            scan.Skip();
            if (!TryTarget(ref scan, lineNo, errors, out slot.TargetKind, out slot.Ally, out slot.TargetArg, out slot.TargetText))
            {
                return false;
            }

            scan.Skip();
        }

        if (scan.Try('{'))
        {
            if (!scan.ReadUntil('}', out string flagBody))
            {
                errors.Add($"Line {lineNo}: unclosed flags.");
                return false;
            }

            if (!BindFlags(flagBody, lineNo, errors, out slot.Flags))
            {
                return false;
            }
        }

        scan.Skip();
        if (!scan.End)
        {
            errors.Add($"Line {lineNo}: unexpected text '{scan.Rest()}'.");
            return false;
        }

        return true;
    }

    private static bool BindAction(ref GambitSlot slot, GambitVerb verb, string body, int lineNo, GambitCompileOptions options, List<string> errors)
    {
        if (verb == GambitVerb.Defer)
        {
            if (!int.TryParse(body, out int ticks) || ticks < 1 || ticks > SimConst.DeferMaxTicks)
            {
                errors.Add($"Line {lineNo}: DEFER needs a tick count from 1 to {SimConst.DeferMaxTicks}.");
                return false;
            }

            slot.Number = ticks;
            return true;
        }

        if (verb == GambitVerb.Wait)
        {
            return true;
        }

        if (verb == GambitVerb.Retarget)
        {
            if (!TryPartName(body, out int part))
            {
                errors.Add($"Line {lineNo}: RETARGET needs Core, Shield, or WeaponArm.");
                return false;
            }

            slot.Number = part;
            slot.TargetArg = part;
            return true;
        }

        if (IsKeyword(body, "Attack"))
        {
            slot.AbilityId = AbilityCatalog.Attack;
            return true;
        }

        if (IsKeyword(body, "Shield Bash"))
        {
            if (!options.BulwarkBash)
            {
                errors.Add($"Line {lineNo}: Shield Bash requires the Bulwark Bash Schematic Chip.");
                return false;
            }

            slot.AbilityId = AbilityCatalog.ShieldBash;
            return true;
        }

        if (!AbilityCatalog.TryGet(body, out AbilityDef ability))
        {
            errors.Add($"Line {lineNo}: unknown ability '{body}'.");
            return false;
        }

        slot.AbilityId = ability.Id;
        return true;
    }

    private static bool BindFlags(string body, int lineNo, List<string> errors, out GambitFlags flags)
    {
        flags = GambitFlags.None;
        string[] parts = body.Split(',');
        for (int i = 0; i < parts.Length; i++)
        {
            string name = parts[i].Trim();
            if (name.Length == 0)
            {
                continue;
            }

            if (IsKeyword(name, "SpendHeat"))
            {
                flags |= GambitFlags.SpendHeat;
            }
            else if (IsKeyword(name, "NoOverwrite"))
            {
                flags |= GambitFlags.NoOverwrite;
            }
            else if (IsKeyword(name, "BurstOnly"))
            {
                flags |= GambitFlags.BurstOnly;
            }
            else
            {
                errors.Add($"Line {lineNo}: unknown flag {name}.");
                return false;
            }
        }

        return true;
    }

    private static bool TryCondition(ref Scan scan, int lineNo, List<string> errors, out GambitCondition condition)
    {
        condition = default;
        scan.Skip();
        int notAt = scan.Index;
        if (scan.TryWord(out string word) && IsKeyword(word, "NOT"))
        {
            condition.Not = true;
            scan.Skip();
        }
        else
        {
            scan.Index = notAt;
        }

        if (!scan.TryWord(out word))
        {
            errors.Add($"Line {lineNo}: expected a condition subject.");
            return false;
        }

        if (!BindSubject(ref scan, word, out condition.Subject, out condition.SubjectArg, out condition.Ally, out condition.Text))
        {
            errors.Add($"Line {lineNo}: unknown subject {word}.");
            return false;
        }

        scan.Skip();
        if (!scan.Try('[') || !scan.ReadUntil(']', out string body))
        {
            errors.Add($"Line {lineNo}: expected [predicate].");
            return false;
        }

        return BindPredicate(body.Trim(), lineNo, errors, ref condition);
    }

    private static bool BindSubject(ref Scan scan, string word, out GambitSubject subject, out int arg, out AllyPick ally, out string? text)
    {
        subject = GambitSubject.Self;
        arg = -1;
        ally = AllyPick.LowestHp;
        text = null;
        if (IsKeyword(word, "Self"))
        {
            subject = GambitSubject.Self;
            return true;
        }

        if (IsKeyword(word, "Target"))
        {
            subject = GambitSubject.Target;
            return true;
        }

        if (IsKeyword(word, "Party"))
        {
            subject = GambitSubject.Party;
            return true;
        }

        if (IsKeyword(word, "Field"))
        {
            subject = GambitSubject.Field;
            return true;
        }

        if (IsKeyword(word, "Boss"))
        {
            subject = GambitSubject.Boss;
            scan.Skip();
            if (scan.Try('.'))
            {
                scan.Skip();
                if (!scan.TryWord(out string part))
                {
                    return false;
                }

                if (IsKeyword(part, "Weapon"))
                {
                    int saved = scan.Index;
                    if (scan.TryWord(out string arm) && IsKeyword(arm, "Arm"))
                    {
                        part = "Weapon Arm";
                    }
                    else
                    {
                        scan.Index = saved;
                    }
                }

                if (!TryPartName(part, out arg))
                {
                    return false;
                }
            }

            return true;
        }

        if (IsKeyword(word, "Ally"))
        {
            subject = GambitSubject.Ally;
            scan.Skip();
            if (!scan.Try('(') || !scan.ReadUntil(')', out string selector))
            {
                return false;
            }

            return BindAlly(selector.Trim(), out ally, out text);
        }

        return false;
    }

    private static bool BindAlly(string selector, out AllyPick ally, out string? text)
    {
        text = null;
        if (IsKeyword(selector, "Lowest HP%"))
        {
            ally = AllyPick.LowestHp;
            return true;
        }

        if (IsKeyword(selector, "Lowest MP%"))
        {
            ally = AllyPick.LowestMp;
            return true;
        }

        if (IsKeyword(selector, "Tank"))
        {
            ally = AllyPick.Tank;
            return true;
        }

        if (IsKeyword(selector, "Casting"))
        {
            ally = AllyPick.Casting;
            return true;
        }

        if (IsKeyword(selector, "Targeted"))
        {
            ally = AllyPick.Targeted;
            return true;
        }

        ally = AllyPick.Named;
        text = selector;
        return selector.Length > 0;
    }

    private static bool BindPredicate(string body, int lineNo, List<string> errors, ref GambitCondition condition)
    {
        if (body.StartsWith("MP", StringComparison.OrdinalIgnoreCase) && body.Contains("Cost:", StringComparison.OrdinalIgnoreCase))
        {
            int cost = body.IndexOf("Cost:", StringComparison.OrdinalIgnoreCase);
            string abilityName = body[(cost + 5)..].Trim();
            if (!AbilityCatalog.TryGet(abilityName, out AbilityDef ability))
            {
                errors.Add($"Line {lineNo}: unknown ability '{abilityName}'.");
                return false;
            }

            condition.Predicate = GambitPredicate.MpForAbility;
            condition.Arg0 = ability.Id;
            condition.Compare = GambitCompare.GreaterOrEqual;
            return true;
        }

        if (body.StartsWith("CountBelowHP%", StringComparison.OrdinalIgnoreCase))
        {
            int colon = body.IndexOf(':');
            if (colon < 0 || !SplitCompare(body[(colon + 1)..].Trim(), out _, out GambitCompare compare, out int threshold, out int percent))
            {
                errors.Add($"Line {lineNo}: CountBelowHP% needs ': N >= M'.");
                return false;
            }

            condition.Predicate = GambitPredicate.CountBelowHp;
            condition.Arg0 = percent;
            condition.Compare = compare;
            condition.Threshold = threshold;
            return true;
        }

        if (!SplitPredicate(body, out string name, out string argument, out GambitCompare cmp, out int number))
        {
            errors.Add($"Line {lineNo}: could not read predicate '{body}'.");
            return false;
        }

        condition.Compare = cmp;
        condition.Threshold = number;
        if (!BindName(name, argument, lineNo, errors, ref condition))
        {
            return false;
        }

        return true;
    }

    private static bool BindName(string name, string argument, int lineNo, List<string> errors, ref GambitCondition condition)
    {
        if (IsKeyword(name, "Always"))
        {
            condition.Predicate = GambitPredicate.Always;
            return true;
        }

        if (IsKeyword(name, "Resonance"))
        {
            if (!TryElement(argument, out int element))
            {
                errors.Add($"Line {lineNo}: Resonance needs an element.");
                return false;
            }

            condition.Predicate = GambitPredicate.Resonance;
            condition.Arg0 = element;
            return true;
        }

        if (IsKeyword(name, "Window"))
        {
            if (TryProperty(argument, out int property))
            {
                condition.Predicate = GambitPredicate.Window;
                condition.Arg0 = 0;
                condition.Arg1 = property;
                return true;
            }

            if (TryResonance(argument, out int resonance))
            {
                condition.Predicate = GambitPredicate.Window;
                condition.Arg0 = 1;
                condition.Arg1 = resonance;
                return true;
            }

            errors.Add($"Line {lineNo}: unknown window '{argument}'.");
            return false;
        }

        if (IsKeyword(name, "WindowLeft"))
        {
            condition.Predicate = GambitPredicate.WindowLeft;
            return true;
        }

        if (IsKeyword(name, "BurstLeft"))
        {
            condition.Predicate = GambitPredicate.BurstLeft;
            return true;
        }

        if (IsKeyword(name, "Casting"))
        {
            condition.Predicate = GambitPredicate.Casting;
            condition.Arg0 = -1;
            if (argument.Length > 0)
            {
                if (!AbilityCatalog.TryGet(argument, out AbilityDef ability))
                {
                    errors.Add($"Line {lineNo}: unknown ability '{argument}'.");
                    return false;
                }

                condition.Arg0 = ability.Id;
            }

            return true;
        }

        if (IsKeyword(name, "CastResolvesIn"))
        {
            condition.Predicate = GambitPredicate.CastResolvesIn;
            return true;
        }

        if (IsKeyword(name, "HP%"))
        {
            condition.Predicate = GambitPredicate.HpPercent;
            return true;
        }

        if (IsKeyword(name, "MP%"))
        {
            condition.Predicate = GambitPredicate.MpPercent;
            return true;
        }

        if (IsKeyword(name, "MP"))
        {
            condition.Predicate = GambitPredicate.Mp;
            return true;
        }

        if (IsKeyword(name, "AP"))
        {
            condition.Predicate = GambitPredicate.Ap;
            return true;
        }

        if (IsKeyword(name, "HasBuff"))
        {
            condition.Predicate = GambitPredicate.HasBuff;
            condition.Text = argument;
            return argument.Length > 0;
        }

        if (IsKeyword(name, "HasDebuff"))
        {
            condition.Predicate = GambitPredicate.HasDebuff;
            condition.Text = argument;
            return argument.Length > 0;
        }

        if (IsKeyword(name, "Disabled"))
        {
            condition.Predicate = GambitPredicate.Disabled;
            return true;
        }

        if (IsKeyword(name, "TargetingMe"))
        {
            condition.Predicate = GambitPredicate.TargetingMe;
            return true;
        }

        if (IsKeyword(name, "MyEnmityRank"))
        {
            condition.Predicate = GambitPredicate.MyEnmityRank;
            return true;
        }

        if (IsKeyword(name, "TankMargin"))
        {
            condition.Predicate = GambitPredicate.TankMargin;
            return true;
        }

        if (IsKeyword(name, "Heat"))
        {
            condition.Predicate = GambitPredicate.Heat;
            return true;
        }

        if (IsKeyword(name, "Primed"))
        {
            condition.Predicate = GambitPredicate.Primed;
            return true;
        }

        if (IsKeyword(name, "Aquifer"))
        {
            condition.Predicate = GambitPredicate.Aquifer;
            return true;
        }

        if (IsKeyword(name, "Deferred"))
        {
            condition.Predicate = GambitPredicate.Deferred;
            return true;
        }

        if (IsKeyword(name, "ReadyIn"))
        {
            condition.Predicate = GambitPredicate.ReadyIn;
            return true;
        }

        if (IsKeyword(name, "KO"))
        {
            condition.Predicate = GambitPredicate.Ko;
            return true;
        }

        if (IsKeyword(name, "ChainParticipants"))
        {
            condition.Predicate = GambitPredicate.ChainParticipants;
            return true;
        }

        if (IsKeyword(name, "AetherDensity"))
        {
            condition.Predicate = GambitPredicate.AetherDensity;
            return true;
        }

        if (IsKeyword(name, "Tick"))
        {
            condition.Predicate = GambitPredicate.Tick;
            return true;
        }

        if (IsKeyword(name, "GlobalBurst"))
        {
            condition.Predicate = GambitPredicate.GlobalBurst;
            condition.Arg0 = -1;
            if (argument.Length > 0)
            {
                if (!TryElement(argument, out int element))
                {
                    errors.Add($"Line {lineNo}: unknown element '{argument}'.");
                    return false;
                }

                condition.Arg0 = element;
            }

            return true;
        }

        errors.Add($"Line {lineNo}: unknown predicate {name}.");
        return false;
    }

    private static bool TryTarget(ref Scan scan, int lineNo, List<string> errors, out GambitTargetKind kind, out AllyPick ally, out int arg, out string? text)
    {
        kind = GambitTargetKind.Default;
        ally = AllyPick.LowestHp;
        arg = -1;
        text = null;
        if (!scan.TryWord(out string word))
        {
            errors.Add($"Line {lineNo}: expected a target.");
            return false;
        }

        if (IsKeyword(word, "Target"))
        {
            kind = GambitTargetKind.Default;
            return true;
        }

        if (IsKeyword(word, "Self"))
        {
            kind = GambitTargetKind.Self;
            return true;
        }

        if (TryPartName(word, out arg))
        {
            kind = GambitTargetKind.Part;
            return true;
        }

        if (IsKeyword(word, "Ally"))
        {
            kind = GambitTargetKind.Ally;
            scan.Skip();
            if (!scan.Try('(') || !scan.ReadUntil(')', out string selector) || !BindAlly(selector.Trim(), out ally, out text))
            {
                errors.Add($"Line {lineNo}: Ally target needs a selector.");
                return false;
            }

            return true;
        }

        errors.Add($"Line {lineNo}: unknown target {word}.");
        return false;
    }

    private static bool SplitPredicate(string body, out string name, out string argument, out GambitCompare compare, out int number)
    {
        argument = "";
        compare = GambitCompare.None;
        number = 0;
        int op = FindOp(body, out compare);
        string head = op < 0 ? body.Trim() : body[..op].Trim();
        if (op >= 0 && !int.TryParse(body[(op + OpLength(compare))..].Trim(), out number))
        {
            name = head;
            return false;
        }

        int colon = head.IndexOf(':');
        if (colon >= 0)
        {
            name = head[..colon].Trim();
            argument = head[(colon + 1)..].Trim();
        }
        else
        {
            name = head;
        }

        return name.Length > 0;
    }

    private static bool SplitCompare(string body, out string unused, out GambitCompare compare, out int threshold, out int first)
    {
        unused = "";
        compare = GambitCompare.None;
        threshold = 0;
        first = 0;
        int op = FindOp(body, out compare);
        if (op < 0 || !int.TryParse(body[..op].Trim(), out first) || !int.TryParse(body[(op + OpLength(compare))..].Trim(), out threshold))
        {
            return false;
        }

        return true;
    }

    private static int FindOp(string body, out GambitCompare compare)
    {
        int at = IndexOfOp(body, "<=", out compare, GambitCompare.LessOrEqual);
        int next = IndexOfOp(body, ">=", out GambitCompare ge, GambitCompare.GreaterOrEqual);
        if (next >= 0 && (at < 0 || next < at))
        {
            at = next;
            compare = ge;
        }

        next = IndexOfOp(body, "==", out GambitCompare eq, GambitCompare.Equal);
        if (next >= 0 && (at < 0 || next < at))
        {
            at = next;
            compare = eq;
        }

        next = IndexOfOp(body, "<", out GambitCompare lt, GambitCompare.Less);
        if (next >= 0 && (at < 0 || next < at) && !TwoChar(body, next))
        {
            at = next;
            compare = lt;
        }

        next = IndexOfOp(body, ">", out GambitCompare gt, GambitCompare.Greater);
        if (next >= 0 && (at < 0 || next < at) && !TwoChar(body, next))
        {
            at = next;
            compare = gt;
        }

        return at;
    }

    private static bool TwoChar(string body, int at) => at + 1 < body.Length && body[at + 1] == '=';

    private static int IndexOfOp(string body, string op, out GambitCompare compare, GambitCompare value)
    {
        compare = value;
        return body.IndexOf(op, StringComparison.Ordinal);
    }

    private static int OpLength(GambitCompare compare) => compare is GambitCompare.Less or GambitCompare.Greater ? 1 : 2;

    private static bool TryPartName(string name, out int part)
    {
        if (IsKeyword(name, "Core"))
        {
            part = 0;
            return true;
        }

        if (IsKeyword(name, "WeaponArm") || IsKeyword(name, "Weapon Arm"))
        {
            part = 1;
            return true;
        }

        if (IsKeyword(name, "Shield"))
        {
            part = 2;
            return true;
        }

        part = -1;
        return false;
    }

    private static bool TryElement(string name, out int element)
    {
        if (Enum.TryParse(name.Replace(" ", "", StringComparison.Ordinal), true, out ElementId id) && id != ElementId.None)
        {
            element = (int)id;
            return true;
        }

        element = -1;
        return false;
    }

    private static bool TryProperty(string name, out int property)
    {
        if (Enum.TryParse(name.Replace(" ", "", StringComparison.Ordinal), true, out ChainProperty id) && id != ChainProperty.None)
        {
            property = (int)id;
            return true;
        }

        property = -1;
        return false;
    }

    private static bool TryResonance(string name, out int resonance)
    {
        string compact = name.Replace(" ", "", StringComparison.Ordinal);
        if (compact.Equals("SolarApex", StringComparison.OrdinalIgnoreCase)
            || compact.Equals("UmbralZero", StringComparison.OrdinalIgnoreCase)
            || compact.Equals("MagmaCore", StringComparison.OrdinalIgnoreCase)
            || compact.Equals("TempestCrown", StringComparison.OrdinalIgnoreCase))
        {
            resonance = -1;
            return false;
        }

        if (Enum.TryParse(compact, true, out ResonanceId id) && id != ResonanceId.None)
        {
            resonance = (int)id;
            return true;
        }

        resonance = -1;
        return false;
    }

    private static GambitVerb VerbOf(string word)
    {
        if (IsKeyword(word, "DEFER"))
        {
            return GambitVerb.Defer;
        }

        if (IsKeyword(word, "WAIT"))
        {
            return GambitVerb.Wait;
        }

        if (IsKeyword(word, "RETARGET"))
        {
            return GambitVerb.Retarget;
        }

        return GambitVerb.Cast;
    }

    private static bool IsCastVerb(string word) => IsKeyword(word, "CAST") || IsKeyword(word, "USE");

    private static bool IsKeyword(string word, string expected) => word.Equals(expected, StringComparison.OrdinalIgnoreCase);

    private struct Scan
    {
        public Scan(string text)
        {
            Text = text;
            Index = 0;
        }

        public string Text;
        public int Index;

        public readonly bool End => Index >= Text.Length;

        public void Skip()
        {
            while (Index < Text.Length && char.IsWhiteSpace(Text[Index]))
            {
                Index++;
            }
        }

        public readonly bool PeekDigit() => Index < Text.Length && char.IsDigit(Text[Index]);

        public int ReadInt()
        {
            int start = Index;
            while (Index < Text.Length && char.IsDigit(Text[Index]))
            {
                Index++;
            }

            return int.Parse(Text[start..Index]);
        }

        public bool Try(char c)
        {
            if (Index < Text.Length && Text[Index] == c)
            {
                Index++;
                return true;
            }

            return false;
        }

        public readonly bool StartsWith(string token) => Text.AsSpan(Index).StartsWith(token, StringComparison.Ordinal);

        public bool TryWord(out string word)
        {
            Skip();
            int start = Index;
            if (Index >= Text.Length || !(char.IsLetter(Text[Index]) || Text[Index] == '%'))
            {
                word = "";
                return false;
            }

            while (Index < Text.Length)
            {
                char c = Text[Index];
                if (char.IsLetterOrDigit(c) || c == '%' || c == '-' || c == '\'')
                {
                    Index++;
                    continue;
                }

                break;
            }

            word = Text[start..Index];
            return word.Length > 0;
        }

        public bool ReadUntil(char end, out string body)
        {
            int start = Index;
            while (Index < Text.Length && Text[Index] != end)
            {
                Index++;
            }

            if (Index >= Text.Length)
            {
                body = "";
                return false;
            }

            body = Text[start..Index];
            Index++;
            return true;
        }

        public readonly string Rest() => Index >= Text.Length ? "" : Text[Index..];
    }
}
