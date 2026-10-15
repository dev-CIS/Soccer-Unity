using FStudio.MatchEngine.Statistics;
using UnityEngine;
using UnityEngine.UIElements;

namespace FStudio.UIToolkit {
    /// <summary>
    /// Builds the home-vs-away statistic comparison rows used by the pause
    /// menu and the full-time screen.
    /// </summary>
    public static class CISStatsView {
        public static void Build(VisualElement container, StatisticsManager stats, Color homeColor, Color awayColor) {
            container.Clear();

            if (stats == null) {
                CISUI.Text("STATISTICS UNAVAILABLE", "cis-eyebrow", container);
                return;
            }

            Pick(ref homeColor, ref awayColor);

            Row(container, "POSSESSION",
                stats.possesioning.TeamPositioning[0], stats.possesioning.TeamPositioning[1],
                stats.possesioning.TeamPositioning[0] + "%", stats.possesioning.TeamPositioning[1] + "%",
                homeColor, awayColor);

            Row(container, "SHOTS",
                stats.shooting.Attempts[0], stats.shooting.Attempts[1],
                null, null, homeColor, awayColor);

            Row(container, "SHOTS ON TARGET",
                stats.shooting.AttemptsOnTarget[0], stats.shooting.AttemptsOnTarget[1],
                null, null, homeColor, awayColor);

            Row(container, "PASSES",
                stats.passing.Passes[0], stats.passing.Passes[1],
                null, null, homeColor, awayColor);

            Row(container, "PASS ACCURACY",
                stats.passing.PassingPercentage[0], stats.passing.PassingPercentage[1],
                stats.passing.PassingPercentage[0] + "%", stats.passing.PassingPercentage[1] + "%",
                homeColor, awayColor);

            Row(container, "BALL RECOVERIES",
                stats.ballWinning.Winnings[0], stats.ballWinning.Winnings[1],
                null, null, homeColor, awayColor);

            Row(container, "CORNERS",
                stats.corners.CornerCount[0], stats.corners.CornerCount[1],
                null, null, homeColor, awayColor);

            Row(container, "SAVES",
                stats.saves.BallSaves[0], stats.saves.BallSaves[1],
                null, null, homeColor, awayColor);

            Row(container, "DISTANCE COVERED",
                stats.runningDistance.TeamDistances[0], stats.runningDistance.TeamDistances[1],
                CISTeamInfo.FormatDistance(stats.runningDistance.TeamDistances[0]),
                CISTeamInfo.FormatDistance(stats.runningDistance.TeamDistances[1]),
                homeColor, awayColor);
        }

        /// <summary>
        /// Keeps team colours readable on the dark UI and distinct from each other.
        /// </summary>
        public static void Pick(ref Color home, ref Color away) {
            home = Readable(home);
            away = Readable(away);

            var difference = Mathf.Abs(home.r - away.r) + Mathf.Abs(home.g - away.g) + Mathf.Abs(home.b - away.b);
            if (difference < 0.35f) {
                home = CISGradients.Accent;
                away = CISGradients.Accent2;
            }
        }

        public static Color Readable(Color color) {
            var luminance = 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
            if (luminance < 0.22f) {
                color = Color.Lerp(color, Color.white, 0.55f);
            }
            color.a = 1f;
            return color;
        }

        private static void Row(
            VisualElement container,
            string label,
            float home,
            float away,
            string homeText,
            string awayText,
            Color homeColor,
            Color awayColor) {

            var row = CISUI.Element("cis-stat", container);

            var head = CISUI.Element("cis-stat__head", row);
            var homeValue = CISUI.Text(homeText ?? Mathf.RoundToInt(home).ToString(), "cis-stat__value", head);
            CISUI.Text(label, "cis-stat__label", head);
            var awayValue = CISUI.Text(awayText ?? Mathf.RoundToInt(away).ToString(), "cis-stat__value cis-stat__value--away", head);

            if (home > away) {
                homeValue.AddToClassList("cis-stat__value--lead");
            } else if (away > home) {
                awayValue.AddToClassList("cis-stat__value--lead");
            }

            var bars = CISUI.Element("cis-stat__bars", row);
            var homeTrack = CISUI.Element("cis-stat__track cis-stat__track--home", bars);
            var awayTrack = CISUI.Element("cis-stat__track cis-stat__track--away", bars);

            var homeFill = CISUI.Element("cis-stat__fill", homeTrack);
            var awayFill = CISUI.Element("cis-stat__fill", awayTrack);

            homeFill.style.backgroundColor = homeColor;
            awayFill.style.backgroundColor = awayColor;

            var total = home + away;
            var homeShare = total > 0 ? home / total : 0f;
            var awayShare = total > 0 ? away / total : 0f;

            homeFill.style.width = Length.Percent(0);
            awayFill.style.width = Length.Percent(0);

            // animate the bars in once the row is laid out.
            row.schedule.Execute(() => {
                homeFill.style.width = Length.Percent(homeShare * 100f);
                awayFill.style.width = Length.Percent(awayShare * 100f);
            }).StartingIn(80);
        }
    }
}
