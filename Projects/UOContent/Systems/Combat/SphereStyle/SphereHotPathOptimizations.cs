/*************************************************************************
 * ModernUO - Sphere 0.51a Combat System
 * File: SphereHotPathOptimizations.cs
 *
 * Description: Hot path optimizations for combat and spell systems.
 *              Implements aggressive inlining, method optimization, and
 *              early exit strategies for performance-critical methods.
 *
 * Reference: PHASE4_IMPLEMENTATION_REPORT.md
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Runtime.CompilerServices;
using Server.Items;

namespace Server.Systems.Combat.SphereStyle
{
    /// <summary>
    /// Extension methods with hot path optimizations for combat system.
    /// Uses aggressive inlining and early exit patterns.
    /// </summary>
    public static class SphereHotPathOptimizations
    {
        /// <summary>
        /// Fast check if a mobile can swing with early exit optimization.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanSwingOptimized(this Mobile mobile)
        {
            if (mobile == null) return false;
            if (mobile.Deleted) return false;
            if (!mobile.Alive) return false;

            var state = mobile.GetSphereState();
            if (state == null) return false;

            return Core.TickCount >= state.NextSwingTime;
        }

        /// <summary>
        /// Fast check if a mobile can cast with early exit optimization.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanCastOptimized(this Mobile mobile)
        {
            if (mobile == null) return false;
            if (mobile.Deleted) return false;
            if (!mobile.Alive) return false;

            var state = mobile.GetSphereState();
            if (state == null) return false;

            return Core.TickCount >= state.NextSpellTime && !state.IsCasting;
        }

        /// <summary>
        /// Optimized state retrieval with null check.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SphereCombatState GetSphereStateOptimized(this Mobile mobile)
        {
            if (mobile == null) return null;
            return mobile.GetSphereState();
        }

        /// <summary>
        /// Fast mana check with early exit.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasManaOptimized(this Mobile mobile, int manaRequired)
        {
            if (mobile == null) return false;
            if (manaRequired <= 0) return true;
            return mobile.Mana >= manaRequired;
        }

        /// <summary>
        /// Optimized spell fizzle check with restricted triggers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ShouldFizzleOptimized(Mobile caster, int magery)
        {
            // Early exit: no fizzle if damage-based fizzle is disabled
            if (!SphereConfigCache.DamageBasedFizzle)
                return false;

            // Fast fizzle calculation
            if (magery < 0) return true;
            if (magery > 100) magery = 100;

            int fizzleChance = 25 - (magery / 4);
            return Utility.Random(100) < fizzleChance;
        }

        /// <summary>
        /// Optimized cast delay calculation with caching.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCastDelayOptimized(int castSpeed, int intelligence)
        {
            // Clamp intelligence to valid range
            if (intelligence < 0) intelligence = 0;
            if (intelligence > 100) intelligence = 100;

            // Base delay minus intelligence modifier
            int delay = Math.Max(500, castSpeed - (intelligence * 10));
            return Math.Min(delay, 5000); // Cap at 5 seconds
        }

        /// <summary>
        /// Optimized cast recovery calculation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCastRecoveryOptimized(int baseRecovery, int focus)
        {
            // Clamp focus to valid range
            if (focus < 0) focus = 0;
            if (focus > 120) focus = 120;

            // Recovery reduced by focus
            int recovery = Math.Max(200, baseRecovery - (focus * 5));
            return Math.Min(recovery, 3000); // Cap at 3 seconds
        }

        /// <summary>
        /// Optimized damage calculation without LINQ.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CalculateDamageOptimized(int baseDamage, int strength, int tacticsSkill)
        {
            // Strength modifier
            int strengthMod = (strength - 10) / 2;
            if (strengthMod < 0) strengthMod = 0;

            // Tactics modifier (0-16.5 bonus from skill)
            int tacticsMod = (int)(tacticsSkill / 6.0);
            if (tacticsMod < 0) tacticsMod = 0;
            if (tacticsMod > 16) tacticsMod = 16;

            // Apply modifiers
            int damage = baseDamage + strengthMod + tacticsMod;

            // Add variance
            damage += Utility.Random(-3, 7);

            return Math.Max(1, damage);
        }

        /// <summary>
        /// Optimized hit chance calculation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckHitOptimized(int attackerSkill, int defenderSkill, double weapon_speed = 1.0)
        {
            // Fast hit calculation
            double diff = (attackerSkill - defenderSkill) / 400.0;

            // Base hit chance
            double hitChance = 0.5 + diff;

            // Clamp to valid range
            if (hitChance < 0.02) hitChance = 0.02;
            if (hitChance > 0.97) hitChance = 0.97;

            return Utility.RandomDouble() < hitChance;
        }

        /// <summary>
        /// Optimized critical hit check with skill modifier.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckCriticalOptimized(int anatomy, int lumberjack = 0)
        {
            // Base critical chance from anatomy (0-5%)
            int critChance = anatomy / 20;
            if (critChance < 0) critChance = 0;
            if (critChance > 5) critChance = 5;

            // Lumberjack bonus (0-10%)
            int lumberjackBonus = lumberjack / 10;
            if (lumberjackBonus < 0) lumberjackBonus = 0;
            if (lumberjackBonus > 10) lumberjackBonus = 10;

            int totalChance = critChance + lumberjackBonus;
            return Utility.Random(100) < totalChance;
        }

        /// <summary>
        /// Optimized parry check with shield consideration.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckParryOptimized(Mobile defender, double parrySkill)
        {
            if (defender == null) return false;

            // Check if defender has a shield
            var shield = defender.FindItemOnLayer(Layer.OneHanded);
            if (shield == null)
            {
                shield = defender.FindItemOnLayer(Layer.TwoHanded);
                if (shield == null || !(shield is BaseShield))
                    return false;
            }

            // Parry chance based on skill (0-25%)
            int parryChance = (int)(parrySkill / 4.0);
            if (parryChance < 0) parryChance = 0;
            if (parryChance > 25) parryChance = 25;

            return Utility.Random(100) < parryChance;
        }

        /// <summary>
        /// Optimized action validation with early exits.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ValidateActionOptimized(Mobile mobile, string action)
        {
            // Fast validation chain
            if (mobile == null) return false;
            if (mobile.Deleted) return false;
            if (!mobile.Alive && action != "die") return false;

            return true;
        }
    }
}
