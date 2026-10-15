using System;
using System.Collections.Generic;
using System.Linq;
using FStudio.MatchEngine;
using FStudio.MatchEngine.Enums;
using FStudio.MatchEngine.Players;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace FStudio.UIToolkit {
    /// <summary>
    /// TV-style "live stats" lower third that pops up during play.
    /// Port of the previous LiveStatistics panel logic.
    /// </summary>
    public class CISLiveStats {
        private enum StatType {
            PlayerToWatch,
            TeamPossession,
            TeamAttempts,
            TeamPassing,
            TeamRunDistance,
            PlayerBestRunDistance,
            PlayerBestDribblingDistance,
            PlayerAttempts,
            PlayerPassing,
            PlayerRating,
            PlayerBallWinning,
            TeamBallWinning,
            TeamBallLosing
        }

        private static readonly (StatType type, int weight)[] Options = {
            (StatType.TeamPossession, 16),
            (StatType.TeamAttempts, 10),
            (StatType.TeamPassing, 10),
            (StatType.TeamRunDistance, 6),
            (StatType.PlayerBestRunDistance, 7),
            (StatType.PlayerBestDribblingDistance, 7),
            (StatType.PlayerAttempts, 8),
            (StatType.PlayerPassing, 8),
            (StatType.PlayerRating, 10),
            (StatType.PlayerBallWinning, 7),
            (StatType.TeamBallWinning, 7),
            (StatType.TeamBallLosing, 4)
        };

        private const float IntervalSeconds = 12f;
        private const float RollEverySeconds = 1f;
        private const long LifetimeMs = 5500;
        private const int RollChance = 35;
        private const int RecentMemory = 5;

        private readonly CISScreen screen;
        private readonly Label title;
        private readonly CISCrest homeCrest, awayCrest;
        private readonly VisualElement homePlayer, awayPlayer;
        private readonly Label homePlayerName, awayPlayerName, homePlayerMeta, awayPlayerMeta;
        private readonly Label homeValue, awayValue;
        private readonly VisualElement timer;

        private readonly Queue<StatType> recent = new Queue<StatType>();

        private bool active;
        private bool busy;
        private float nextCheck;
        private IVisualElementScheduledItem hideJob;

        public CISLiveStats(VisualElement root) {
            screen = new CISScreen(root.Q("ticker"), false, false);

            title = root.Q<Label>("ticker-title");
            homeCrest = root.Q<CISCrest>("ticker-home-crest");
            awayCrest = root.Q<CISCrest>("ticker-away-crest");
            homePlayer = root.Q("ticker-home-player");
            awayPlayer = root.Q("ticker-away-player");
            homePlayerName = root.Q<Label>("ticker-home-player-name");
            awayPlayerName = root.Q<Label>("ticker-away-player-name");
            homePlayerMeta = root.Q<Label>("ticker-home-player-meta");
            awayPlayerMeta = root.Q<Label>("ticker-away-player-meta");
            homeValue = root.Q<Label>("ticker-home-value");
            awayValue = root.Q<Label>("ticker-away-value");
            timer = root.Q("ticker-timer");
        }

        /// <summary>
        /// Starts the ticker; the "players to watch" card opens after the given delay
        /// so it doesn't collide with the kick-off board.
        /// </summary>
        public void Begin(long delayMs) {
            active = true;
            busy = true;
            recent.Clear();

            var details = MatchManager.CurrentMatchDetails;
            if (details != null) {
                homeCrest.SetTeam(details.homeTeam);
                awayCrest.SetTeam(details.awayTeam);
            }

            hideJob?.Pause();
            hideJob = screen.Root.schedule.Execute(() => {
                busy = false;

                if (!active) {
                    return;
                }

                try {
                    if (Open(StatType.PlayerToWatch)) {
                        Present();
                        return;
                    }
                } catch (Exception exception) {
                    Debug.Log("[CISLiveStats] Could not open player to watch: " + exception.Message);
                }

                nextCheck = Time.time + IntervalSeconds;
            });
            hideJob.StartingIn(delayMs);
        }

        public void Stop() {
            active = false;
            Suppress(IntervalSeconds);
        }

        /// <summary>
        /// Hides the ticker now and keeps it away for the given (game) seconds.
        /// </summary>
        public void Suppress(float seconds) {
            hideJob?.Pause();
            screen.Hide();
            busy = false;
            nextCheck = Time.time + seconds;
        }

        public void Tick() {
            if (!active || busy || Time.time < nextCheck) {
                return;
            }

            var manager = MatchManager.Current;
            if (manager == null || MatchManager.Statistics == null || !manager.MatchFlags.HasFlag(MatchStatus.Playing)) {
                return;
            }

            nextCheck = Time.time + RollEverySeconds;

            if (Random.Range(0, 100) >= RollChance) {
                return;
            }

            try {
                var type = PickType();
                if (Open(type)) {
                    Remember(type);
                    Present();
                }
            } catch (Exception exception) {
                Debug.Log("[CISLiveStats] Skipped a stat: " + exception.Message);
            }
        }

        private StatType PickType() {
            var pool = Options.Where(o => !recent.Contains(o.type)).ToArray();
            if (pool.Length == 0) {
                pool = Options;
            }

            var total = pool.Sum(o => o.weight);
            var roll = Random.Range(0, total);
            foreach (var option in pool) {
                if (roll < option.weight) {
                    return option.type;
                }
                roll -= option.weight;
            }

            return pool[0].type;
        }

        private void Remember(StatType type) {
            recent.Enqueue(type);
            while (recent.Count > RecentMemory) {
                recent.Dequeue();
            }
        }

        private void Present() {
            busy = true;

            timer.AddToClassList("ticker__timer--reset");
            timer.style.width = Length.Percent(100);

            screen.Show();

            timer.schedule.Execute(() => {
                timer.RemoveFromClassList("ticker__timer--reset");
                timer.style.width = Length.Percent(0);
            }).StartingIn(40);

            hideJob?.Pause();
            hideJob = screen.Root.schedule.Execute(() => {
                screen.Hide();
                busy = false;
                nextCheck = Time.time + IntervalSeconds;
            });
            hideJob.StartingIn(LifetimeMs);
        }

        private bool Open(StatType type) {
            var statistics = MatchManager.Statistics;
            var manager = MatchManager.Current;

            if (statistics == null || manager == null) {
                return false;
            }

            var homeIds = manager.GameTeam1.GamePlayers.Select(x => x.MatchPlayer.Player.id).ToList();
            var awayIds = manager.GameTeam2.GamePlayers.Select(x => x.MatchPlayer.Player.id).ToList();

            KeyValuePair<int, T> Best<T>(List<int> ids, Dictionary<int, T> values) where T : IComparable {
                return values.Where(x => ids.Contains(x.Key)).OrderByDescending(x => x.Value).FirstOrDefault();
            }

            switch (type) {
                case StatType.PlayerToWatch: {
                    (int id, int height) Pick(GameTeam team) =>
                        team.GamePlayers
                            .OrderByDescending(x => CISTeamInfo.SafeOverall(x.MatchPlayer.Player))
                            .Take(4)
                            .OrderBy(x => Guid.NewGuid())
                            .Select(x => (x.MatchPlayer.Player.id, x.MatchPlayer.Player.height))
                            .FirstOrDefault();

                    var home = Pick(manager.GameTeam1);
                    var away = Pick(manager.GameTeam2);
                    Fill("PLAYERS TO WATCH", home.height + " CM", away.height + " CM", home.id, away.id);
                    return true;
                }

                case StatType.TeamPossession:
                    Fill("POSSESSION",
                        statistics.possesioning.TeamPositioning[0] + "%",
                        statistics.possesioning.TeamPositioning[1] + "%");
                    return true;

                case StatType.TeamAttempts:
                    if (statistics.shooting.Attempts[0] == 0 && statistics.shooting.Attempts[1] == 0) {
                        return false;
                    }
                    Fill("SHOTS (ON TARGET)",
                        $"{statistics.shooting.Attempts[0]} ({statistics.shooting.AttemptsOnTarget[0]})",
                        $"{statistics.shooting.Attempts[1]} ({statistics.shooting.AttemptsOnTarget[1]})");
                    return true;

                case StatType.TeamPassing:
                    if (statistics.passing.Passes[0] == 0 || statistics.passing.Passes[1] == 0) {
                        return false;
                    }
                    Fill("PASSES COMPLETED",
                        $"{statistics.passing.SuccesfulPasses[0]}/{statistics.passing.Passes[0]}",
                        $"{statistics.passing.SuccesfulPasses[1]}/{statistics.passing.Passes[1]}");
                    return true;

                case StatType.TeamRunDistance:
                    Fill("DISTANCE COVERED",
                        CISTeamInfo.FormatDistance(statistics.runningDistance.TeamDistances[0]),
                        CISTeamInfo.FormatDistance(statistics.runningDistance.TeamDistances[1]));
                    return true;

                case StatType.TeamBallWinning:
                    if (statistics.ballWinning.Winnings[0] == 0 && statistics.ballWinning.Winnings[1] == 0) {
                        return false;
                    }
                    Fill("BALL RECOVERIES",
                        statistics.ballWinning.Winnings[0].ToString(),
                        statistics.ballWinning.Winnings[1].ToString());
                    return true;

                case StatType.TeamBallLosing:
                    if (statistics.ballLosing.Losing[0] == 0 && statistics.ballLosing.Losing[1] == 0) {
                        return false;
                    }
                    Fill("POSSESSION LOST",
                        statistics.ballLosing.Losing[0].ToString(),
                        statistics.ballLosing.Losing[1].ToString());
                    return true;

                case StatType.PlayerRating: {
                    var home = homeIds.Select(x => (id: x, rating: statistics.PlayerRating(x))).OrderByDescending(x => x.rating).FirstOrDefault();
                    var away = awayIds.Select(x => (id: x, rating: statistics.PlayerRating(x))).OrderByDescending(x => x.rating).FirstOrDefault();
                    Fill("TOP RATED", home.rating.ToString("0.0"), away.rating.ToString("0.0"), home.id, away.id);
                    return true;
                }

                case StatType.PlayerPassing: {
                    int BestPasser(List<int> ids) => ids
                        .OrderByDescending(x => Get(statistics.passing.PlayerPassing, x) + Get(statistics.passing.PlayerSuccesfulPasses, x))
                        .FirstOrDefault();

                    var home = BestPasser(homeIds);
                    var away = BestPasser(awayIds);

                    if (Get(statistics.passing.PlayerPassing, home) == 0 || Get(statistics.passing.PlayerPassing, away) == 0) {
                        return false;
                    }

                    Fill("BEST PASSERS",
                        $"{Get(statistics.passing.PlayerSuccesfulPasses, home)}/{Get(statistics.passing.PlayerPassing, home)}",
                        $"{Get(statistics.passing.PlayerSuccesfulPasses, away)}/{Get(statistics.passing.PlayerPassing, away)}",
                        home, away);
                    return true;
                }

                case StatType.PlayerAttempts: {
                    int BestShooter(List<int> ids) => ids
                        .OrderByDescending(x => Get(statistics.shooting.PlayerAttempt, x) + Get(statistics.shooting.PlayerAttemptOnTarget, x))
                        .ThenByDescending(x => Get(statistics.shooting.PlayerAttemptOnTarget, x))
                        .FirstOrDefault();

                    var home = BestShooter(homeIds);
                    var away = BestShooter(awayIds);

                    if (Get(statistics.shooting.PlayerAttempt, home) == 0 || Get(statistics.shooting.PlayerAttempt, away) == 0) {
                        return false;
                    }

                    Fill("SHOTS (ON TARGET)",
                        $"{Get(statistics.shooting.PlayerAttempt, home)} ({Get(statistics.shooting.PlayerAttemptOnTarget, home)})",
                        $"{Get(statistics.shooting.PlayerAttempt, away)} ({Get(statistics.shooting.PlayerAttemptOnTarget, away)})",
                        home, away);
                    return true;
                }

                case StatType.PlayerBallWinning: {
                    var home = Best(homeIds, statistics.ballWinning.PlayerBallWinning);
                    var away = Best(awayIds, statistics.ballWinning.PlayerBallWinning);

                    if (home.Value == 0 || away.Value == 0) {
                        return false;
                    }

                    Fill("TOP BALL WINNERS", home.Value.ToString(), away.Value.ToString(), home.Key, away.Key);
                    return true;
                }

                case StatType.PlayerBestRunDistance: {
                    var home = Best(homeIds, statistics.runningDistance.PlayerDistances);
                    var away = Best(awayIds, statistics.runningDistance.PlayerDistances);

                    if (home.Value < 100 || away.Value < 100) {
                        return false;
                    }

                    Fill("MOST DISTANCE",
                        CISTeamInfo.FormatDistance(home.Value),
                        CISTeamInfo.FormatDistance(away.Value),
                        home.Key, away.Key);
                    return true;
                }

                case StatType.PlayerBestDribblingDistance: {
                    var home = Best(homeIds, statistics.dribbling.PlayerDribbling);
                    var away = Best(awayIds, statistics.dribbling.PlayerDribbling);

                    if (home.Value < 10 || away.Value < 10) {
                        return false;
                    }

                    Fill("BEST DRIBBLERS",
                        CISTeamInfo.FormatDistance(home.Value),
                        CISTeamInfo.FormatDistance(away.Value),
                        home.Key, away.Key);
                    return true;
                }
            }

            return false;
        }

        private static int Get(Dictionary<int, int> values, int id) {
            return values != null && values.TryGetValue(id, out var value) ? value : 0;
        }

        private void Fill(string heading, string home, string away, int homePlayerId = -1, int awayPlayerId = -1) {
            title.text = heading;
            homeValue.text = home;
            awayValue.text = away;

            SetPlayer(homePlayerId, homePlayer, homePlayerName, homePlayerMeta);
            SetPlayer(awayPlayerId, awayPlayer, awayPlayerName, awayPlayerMeta);
        }

        private static void SetPlayer(int playerId, VisualElement holder, Label nameLabel, Label metaLabel) {
            PlayerBase player = null;

            if (playerId != -1 && MatchManager.AllPlayers != null) {
                player = MatchManager.AllPlayers.FirstOrDefault(x => x.MatchPlayer.Player.id == playerId);
            }

            if (player == null) {
                holder.style.display = DisplayStyle.None;
                return;
            }

            holder.style.display = DisplayStyle.Flex;

            var matchPlayer = player.MatchPlayer;
            nameLabel.text = CISUI.Upper(CISTeamInfo.Surname(matchPlayer.Player.Name));
            metaLabel.text = $"#{matchPlayer.Number}  ·  {CISTeamInfo.PositionLabel(matchPlayer.Position)}  ·  {CISTeamInfo.SafeOverall(matchPlayer.Player)} OVR";
        }
    }
}
