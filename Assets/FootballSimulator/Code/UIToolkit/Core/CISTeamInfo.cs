using System;
using System.Collections.Generic;
using System.Linq;
using FStudio.Data;
using FStudio.Database;
using UnityEngine;

namespace FStudio.UIToolkit {
    /// <summary>
    /// Presentation helpers for teams and players (short names, line ratings,
    /// rating tiers, lineup coordinates...).
    /// </summary>
    public static class CISTeamInfo {
        public enum Line { Goalkeeper, Defence, Midfield, Attack }

        public static readonly Color Gold = new Color32(0xFF, 0xC9, 0x3C, 0xFF);
        public static readonly Color Silver = new Color32(0xD3, 0xDC, 0xE8, 0xFF);
        public static readonly Color Bronze = new Color32(0xD9, 0x92, 0x5B, 0xFF);

        public static readonly Color GoalkeeperColor = new Color32(0xF5, 0xB7, 0x2E, 0xFF);
        public static readonly Color DefenceColor = new Color32(0x3E, 0x8B, 0xFF, 0xFF);
        public static readonly Color MidfieldColor = new Color32(0x00, 0xD6, 0x86, 0xFF);
        public static readonly Color AttackColor = new Color32(0xFF, 0x5A, 0x5F, 0xFF);

        public static string ShortName(TeamEntry team) {
            if (team == null || string.IsNullOrEmpty(team.TeamName)) {
                return "---";
            }

            var letters = new string(team.TeamName.Where(char.IsLetter).ToArray()).ToUpperInvariant();
            if (letters.Length >= 3) {
                return letters.Substring(0, 3);
            }

            return letters.PadRight(3, '-');
        }

        public static string Initials(TeamEntry team) {
            if (team == null || string.IsNullOrEmpty(team.TeamName)) {
                return "?";
            }

            var words = team.TeamName.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2) {
                return (words[0].Substring(0, 1) + words[1].Substring(0, 1)).ToUpperInvariant();
            }

            return team.TeamName.Substring(0, Mathf.Min(2, team.TeamName.Length)).ToUpperInvariant();
        }

        public static string FormationLabel(Formations formation) {
            // "_4_2_3_1_A" -> "4-2-3-1 A"
            var parts = formation.ToString().Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            var digits = parts.Where(p => p.All(char.IsDigit));
            var suffix = parts.Where(p => !p.All(char.IsDigit));
            var label = string.Join("-", digits);
            var extra = string.Join(" ", suffix);
            return string.IsNullOrEmpty(extra) ? label : label + " " + extra;
        }

        public static Positions BasePosition(Positions position) {
            return PositionRules.GetBasePosition(position);
        }

        public static string PositionLabel(Positions position) {
            return BasePosition(position).ToString();
        }

        public static Line LineOf(Positions position) {
            switch (BasePosition(position)) {
                case Positions.GK:
                    return Line.Goalkeeper;
                case Positions.CB:
                case Positions.LB:
                case Positions.RB:
                    return Line.Defence;
                case Positions.LW:
                case Positions.RW:
                case Positions.ST:
                    return Line.Attack;
                default:
                    return Line.Midfield;
            }
        }

        public static Color LineColor(Line line) {
            switch (line) {
                case Line.Goalkeeper: return GoalkeeperColor;
                case Line.Defence: return DefenceColor;
                case Line.Attack: return AttackColor;
                default: return MidfieldColor;
            }
        }

        public static Color RatingColor(int rating) {
            if (rating >= 80) return Gold;
            if (rating >= 70) return Silver;
            return Bronze;
        }

        public static int SafeOverall(PlayerEntry player) {
            try {
                return player != null ? player.Overall : 0;
            } catch (Exception) {
                return 0;
            }
        }

        public static int SafeOverall(TeamEntry team) {
            try {
                return team != null ? team.Overall : 0;
            } catch (Exception) {
                return 0;
            }
        }

        /// <summary>
        /// Average overall of the players on each line of the team's formation.
        /// </summary>
        public static (int attack, int midfield, int defence) LineRatings(TeamEntry team) {
            if (team == null || team.Players == null) {
                return (0, 0, 0);
            }

            var positions = Formation(team);
            var att = new List<int>();
            var mid = new List<int>();
            var def = new List<int>();

            for (int i = 0; i < team.Players.Length; i++) {
                var position = i < positions.Length ? positions[i] : Positions.CM;
                var overall = SafeOverall(team.Players[i]);

                switch (LineOf(position)) {
                    case Line.Attack: att.Add(overall); break;
                    case Line.Midfield: mid.Add(overall); break;
                    default: def.Add(overall); break; // keeper counts towards defence.
                }
            }

            int avg(List<int> list) => list.Count == 0 ? 0 : Mathf.RoundToInt((float)list.Average());

            return (avg(att), avg(mid), avg(def));
        }

        public static Positions[] Formation(TeamEntry team) {
            try {
                return FormationRules.GetTeamFormation(team.Formation).Positions;
            } catch (Exception) {
                return new Positions[0];
            }
        }

        public static KitEntry Kit(TeamEntry team, bool awayKit) {
            if (team == null) {
                return null;
            }

            return awayKit ? team.AwayKit : team.HomeKit;
        }

        public static Color PrimaryColor(TeamEntry team, bool awayKit = false) {
            var kit = Kit(team, awayKit);
            if (kit != null) {
                return Opaque(kit.Color1);
            }

            return team?.TeamLogo != null ? Opaque(team.TeamLogo.TeamLogoColor1) : CISGradients.Accent;
        }

        public static Color SecondaryColor(TeamEntry team, bool awayKit = false) {
            var kit = Kit(team, awayKit);
            if (kit != null) {
                return Opaque(kit.Color2);
            }

            return team?.TeamLogo != null ? Opaque(team.TeamLogo.TeamLogoColor2) : CISGradients.Accent2;
        }

        public static Color Opaque(Color color) {
            color.a = 1f;
            return color;
        }

        /// <summary>
        /// Readable text colour (dark or light) on top of the given background.
        /// </summary>
        public static Color InkFor(Color background) {
            var luminance = 0.299f * background.r + 0.587f * background.g + 0.114f * background.b;
            return luminance > 0.6f ? new Color32(0x0A, 0x0E, 0x16, 0xFF) : Color.white;
        }

        public static string Surname(string fullName) {
            if (string.IsNullOrEmpty(fullName)) {
                return string.Empty;
            }

            var parts = fullName.Trim().Split(' ');
            return parts[parts.Length - 1];
        }

        /// <summary>
        /// Same unit conversion the match statistics used before.
        /// </summary>
        public static string FormatDistance(float distance) {
            const float divider = 100f;

            if (distance < divider) {
                return Math.Round(distance * 1000 / divider, 0) + " M";
            }

            return Math.Round(distance / divider, 2).ToString("0.00") + " KM";
        }

        /// <summary>
        /// Normalised lineup position: x 0 = left touchline, 1 = right;
        /// y 0 = own goal line, 1 = opponent goal line.
        /// </summary>
        public static Vector2 LineupPosition(Positions position) {
            switch (position) {
                case Positions.GK: return new Vector2(0.50f, 0.07f);

                case Positions.LB: return new Vector2(0.13f, 0.27f);
                case Positions.CB_L: return new Vector2(0.34f, 0.21f);
                case Positions.CB: return new Vector2(0.50f, 0.20f);
                case Positions.CB_R: return new Vector2(0.66f, 0.21f);
                case Positions.RB: return new Vector2(0.87f, 0.27f);

                case Positions.DMF_L: return new Vector2(0.36f, 0.39f);
                case Positions.DMF: return new Vector2(0.50f, 0.37f);
                case Positions.DMF_R: return new Vector2(0.64f, 0.39f);

                case Positions.LMF: return new Vector2(0.13f, 0.55f);
                case Positions.CM_L: return new Vector2(0.33f, 0.51f);
                case Positions.CM: return new Vector2(0.50f, 0.51f);
                case Positions.CM_R: return new Vector2(0.67f, 0.51f);
                case Positions.RMF: return new Vector2(0.87f, 0.55f);

                case Positions.AMF_L: return new Vector2(0.30f, 0.67f);
                case Positions.AMF: return new Vector2(0.50f, 0.66f);
                case Positions.AMF_R: return new Vector2(0.70f, 0.67f);

                case Positions.LW: return new Vector2(0.15f, 0.79f);
                case Positions.RW: return new Vector2(0.85f, 0.79f);

                case Positions.ST_L: return new Vector2(0.37f, 0.87f);
                case Positions.ST: return new Vector2(0.50f, 0.88f);
                case Positions.ST_R: return new Vector2(0.63f, 0.87f);
            }

            return new Vector2(0.5f, 0.5f);
        }
    }
}
