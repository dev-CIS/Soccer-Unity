using System;
using FStudio.Database;
using UnityEngine;
using UnityEngine.UIElements;

namespace FStudio.UIToolkit {
    /// <summary>
    /// Binds one instance of TeamCard.uxml (kick off screen).
    /// </summary>
    public class CISTeamCardView {
        private readonly VisualElement card;
        private readonly VisualElement glow;
        private readonly VisualElement stripe;
        private readonly VisualElement controller;
        private readonly Label controllerText;
        private readonly Label nameLabel;
        private readonly Label formationLabel;
        private readonly CISCrest crest;
        private readonly CISRatingBadge overall;

        private readonly Label attack, midfield, defence;
        private readonly VisualElement attackBar, midfieldBar, defenceBar;

        public CISTeamCardView(VisualElement slot, string side, Action previous, Action next, Action browse) {
            card = slot.Q("card");
            glow = slot.Q("glow");
            stripe = slot.Q("stripe");
            controller = slot.Q("controller");
            controllerText = slot.Q<Label>("controller-text");
            nameLabel = slot.Q<Label>("name");
            formationLabel = slot.Q<Label>("formation");
            crest = slot.Q<CISCrest>("crest");
            overall = slot.Q<CISRatingBadge>("ovr");

            attack = slot.Q<Label>("att");
            midfield = slot.Q<Label>("mid");
            defence = slot.Q<Label>("def");
            attackBar = slot.Q("att-bar");
            midfieldBar = slot.Q("mid-bar");
            defenceBar = slot.Q("def-bar");

            slot.Q<Label>("side").text = side;

            var prevArrow = slot.Q("prev");
            var nextArrow = slot.Q("next");

            prevArrow.RegisterCallback<ClickEvent>(evt => {
                evt.StopPropagation();
                CISUI.PlayClickSound();
                previous();
            });

            nextArrow.RegisterCallback<ClickEvent>(evt => {
                evt.StopPropagation();
                CISUI.PlayClickSound();
                next();
            });

            card.RegisterCallback<ClickEvent>(evt => {
                CISUI.PlayClickSound();
                browse();
            });
        }

        public void SetTeam(TeamEntry team) {
            crest.SetTeam(team);
            nameLabel.text = CISUI.Upper(team != null ? team.TeamName : "-");
            formationLabel.text = team != null ? CISTeamInfo.FormationLabel(team.Formation) : string.Empty;
            overall.Value = CISTeamInfo.SafeOverall(team);

            var (att, mid, def) = CISTeamInfo.LineRatings(team);
            SetLine(attack, attackBar, att);
            SetLine(midfield, midfieldBar, mid);
            SetLine(defence, defenceBar, def);

            var primary = CISStatsView.Readable(CISTeamInfo.PrimaryColor(team));
            glow.style.unityBackgroundImageTintColor = primary;
            stripe.style.backgroundColor = primary;

            CISUI.Pulse(card, "team-card--swap", 140);
        }

        public void SetController(bool userControlled) {
            controllerText.text = userControlled ? "YOU" : "CPU";
            controller.EnableInClassList("team-card__controller--cpu", !userControlled);
        }

        private static void SetLine(Label value, VisualElement bar, int rating) {
            value.text = rating > 0 ? rating.ToString() : "-";
            var fill = Mathf.InverseLerp(40f, 95f, rating);
            bar.style.width = Length.Percent(Mathf.Clamp01(fill) * 100f);
        }
    }
}
