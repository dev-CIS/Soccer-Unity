using System;
using System.Collections.Generic;
using FStudio.Data;
using FStudio.Database;
using UnityEngine;
using UnityEngine.UIElements;

namespace FStudio.UIToolkit {
    /// <summary>
    /// Binds one instance of TeamSheet.uxml (pre-match line-up, kit selection).
    /// </summary>
    public class CISTeamSheetView {
        public readonly CISButton SwitchKitButton;

        private readonly VisualElement stripe;
        private readonly VisualElement glow;
        private readonly VisualElement controller;
        private readonly Label controllerText;
        private readonly Label nameLabel;
        private readonly Label formationLabel;
        private readonly Label kitName;
        private readonly CISCrest crest;
        private readonly CISRatingBadge overall;
        private readonly CISPitch pitch;
        private readonly VisualElement list;
        private readonly CISKitView kitView;
        private readonly VisualElement kitColor1, kitColor2;

        private TeamEntry team;

        public CISTeamSheetView(VisualElement slot, string side, Action switchKit) {
            stripe = slot.Q("stripe");
            glow = slot.Q("glow");
            controller = slot.Q("controller");
            controllerText = slot.Q<Label>("controller-text");
            nameLabel = slot.Q<Label>("name");
            formationLabel = slot.Q<Label>("formation");
            kitName = slot.Q<Label>("kit-name");
            crest = slot.Q<CISCrest>("crest");
            overall = slot.Q<CISRatingBadge>("ovr");
            pitch = slot.Q<CISPitch>("pitch");
            list = slot.Q("list");
            kitView = slot.Q<CISKitView>("kit");
            kitColor1 = slot.Q("kit-color-1");
            kitColor2 = slot.Q("kit-color-2");

            slot.Q<Label>("side").text = side;

            SwitchKitButton = slot.Q<CISButton>("kit-switch");
            SwitchKitButton.clicked += switchKit;
        }

        public void SetTeam(TeamEntry teamEntry, bool userControlled) {
            team = teamEntry;

            crest.SetTeam(team);
            nameLabel.text = CISUI.Upper(team.TeamName);
            formationLabel.text = CISTeamInfo.FormationLabel(team.Formation);
            overall.Value = CISTeamInfo.SafeOverall(team);

            controllerText.text = userControlled ? "YOU" : "CPU";
            controller.EnableInClassList("sheet__controller--cpu", !userControlled);

            var accent = CISStatsView.Readable(CISTeamInfo.PrimaryColor(team));
            glow.style.unityBackgroundImageTintColor = accent;

            BuildList();
        }

        public void SetKit(bool awayKit) {
            if (team == null) {
                return;
            }

            var kit = CISTeamInfo.Kit(team, awayKit);

            kitView.SetKit(kit);
            kitName.text = awayKit ? "AWAY KIT" : "HOME KIT";

            var primary = kit != null ? CISTeamInfo.Opaque(kit.Color1) : CISGradients.Accent;
            var secondary = kit != null ? CISTeamInfo.Opaque(kit.Color2) : CISGradients.Accent2;

            kitColor1.style.backgroundColor = primary;
            kitColor2.style.backgroundColor = secondary;
            stripe.style.backgroundColor = CISStatsView.Readable(primary);

            BuildPitch(kit);
        }

        private Positions PositionOf(int index, Positions[] formation) {
            if (index < formation.Length) {
                return formation[index];
            }

            try {
                return team.Players[index].Position;
            } catch (Exception) {
                return Positions.CM;
            }
        }

        private void BuildPitch(KitEntry kit) {
            var formation = CISTeamInfo.Formation(team);
            var markers = new List<CISPitch.Marker>();

            for (int i = 0; i < team.Players.Length && i < 11; i++) {
                var position = PositionOf(i, formation);
                var line = CISTeamInfo.LineOf(position);
                var isKeeper = line == CISTeamInfo.Line.Goalkeeper;

                Color fill = Color.white, outline = Color.gray;
                if (kit != null) {
                    fill = CISTeamInfo.Opaque(isKeeper ? kit.GKColor1 : kit.Color1);
                    outline = CISTeamInfo.Opaque(isKeeper ? kit.GKColor2 : kit.Color2);
                }

                markers.Add(new CISPitch.Marker {
                    position = CISTeamInfo.LineupPosition(position),
                    number = (i + 1).ToString(),
                    positionLabel = CISTeamInfo.PositionLabel(position),
                    fill = fill,
                    outline = outline,
                    lineColor = CISTeamInfo.LineColor(line)
                });
            }

            pitch.SetMarkers(markers);
        }

        private void BuildList() {
            list.Clear();

            var formation = CISTeamInfo.Formation(team);

            for (int i = 0; i < team.Players.Length && i < 11; i++) {
                var player = team.Players[i];
                var position = PositionOf(i, formation);

                var row = CISUI.Element("lineup-row", list);
                CISUI.Text((i + 1).ToString(), "lineup-row__num", row);

                var chip = CISUI.Element("lineup-row__pos", row);
                chip.style.backgroundColor = CISTeamInfo.LineColor(CISTeamInfo.LineOf(position));
                CISUI.Text(CISTeamInfo.PositionLabel(position), "lineup-row__pos-text", chip);

                CISUI.Text(player != null ? player.Name : "-", "lineup-row__name", row);

                var rating = CISTeamInfo.SafeOverall(player);
                var ovr = CISUI.Text(rating.ToString(), "lineup-row__ovr", row);
                ovr.style.color = CISTeamInfo.RatingColor(rating);
            }
        }
    }
}
