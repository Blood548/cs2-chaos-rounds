using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using System;
using System.Linq;

namespace ChaosRounds;

public class ChaosRoundsPlugin : BasePlugin
{
    public override string ModuleName => "Chaos Rounds";
    public override string ModuleVersion => "1.1.0";
    public override string ModuleAuthor => "Developer";

    private enum RoundType
    {
        Normal,
        Speed,
        Vampire,
        LowGravity
    }

    private RoundType _currentRoundType = RoundType.Normal;
    private const int MaxVampireHealth = 150;
    private const float SpeedMultiplierPerKill = 1.25f;
    private readonly Random _random = new Random();

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Array values = Enum.GetValues(typeof(RoundType));
        _currentRoundType = (RoundType)values.GetValue(_random.Next(values.Length))!;

        switch (_currentRoundType)
        {
            case RoundType.Speed:
                Server.PrintToChatAll($" {ChatColors.Green}[Chaos]{ChatColors.Default} СПИД-РАУНД! Скорость за убийство.");
                break;
            case RoundType.Vampire:
                Server.PrintToChatAll($" {ChatColors.Green}[Chaos]{ChatColors.Default} ВАМПИРИЗМ! +30 HP за убийство, +10 HP за урон.");
                break;
            case RoundType.LowGravity:
                Server.PrintToChatAll($" {ChatColors.Green}[Chaos]{ChatColors.Default} НУЛЕВАЯ ГРАВИТАЦИЯ!");
                ApplyGravityScaleToAll(0.4f);
                break;
            case RoundType.Normal:
                Server.PrintToChatAll($" {ChatColors.Green}[Chaos]{ChatColors.Default} Обычный раунд.");
                break;
        }
        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (_currentRoundType != RoundType.Speed && _currentRoundType != RoundType.Vampire)
            return HookResult.Continue;

        var killer = @event.Attacker;
        var victim = @event.Userid;

        if (killer == null || !killer.IsValid || killer == victim) 
            return HookResult.Continue;

        if (_currentRoundType == RoundType.Speed)
        {
            var pawn = killer.PlayerPawn.Value;
            if (pawn != null && pawn.IsValid && killer.PawnIsAlive)
            {
                pawn.VelocityModifier *= SpeedMultiplierPerKill;
            }
        }
        else if (_currentRoundType == RoundType.Vampire)
        {
            HealPlayer(killer, 30);
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        if (_currentRoundType != RoundType.Vampire) 
            return HookResult.Continue;

        var attacker = @event.Attacker;
        if (attacker == null || !attacker.IsValid) 
            return HookResult.Continue;

        HealPlayer(attacker, 10);
        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        if (_currentRoundType == RoundType.LowGravity)
        {
            ApplyGravityScaleToAll(1.0f);
        }
        return HookResult.Continue;
    }

    private static void HealPlayer(CCSPlayerController player, int amount)
    {
        if (player == null || !player.IsValid) return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || !player.PawnIsAlive) return;

        pawn.Health = Math.Min(MaxVampireHealth, pawn.Health + amount);
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
    }

    private static void ApplyGravityScaleToAll(float gravityScale)
    {
        foreach (var player in Utilities.GetPlayers().Where(p => p != null && p.IsValid && p.PawnIsAlive))
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn != null && pawn.IsValid)
            {
                pawn.GravityScale = gravityScale;
            }
        }
    }
}
