using System;
using System.Collections.Generic;
using FStudio.Database;
using FStudio.Events;
using FStudio.MatchEngine;
using FStudio.MatchEngine.Events;
using FStudio.MatchEngine.Tactics;
using FStudio.MatchEngine.UI;
using FStudio.UI;
using FStudio.UI.Events;
using FStudio.UI.MatchThemes.MatchEvents;
using Shared.Responses;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace FStudio.UIToolkit {
    /// <summary>
    /// Everything shown around a match, built with UI Toolkit: pre-match team
    /// sheets, scoreboard, tactic meters, live stats, goal flash, info board,
    /// pause menu and the full-time report. Lives on the MatchUI prefab that
    /// UILoader.MatchUILoader instantiates.
    /// </summary>
    public class CISMatchUI : MonoBehaviour {
        private const long KickOffBoardMs = 3200;
        private const long GoalFlashMs = 3800;
        private const long TacticVisibleMs = 4000;
        private const long FullTimeStatsDelayMs = 3000;

        private UIDocument document;
        private VisualElement root;

        // screens
        private CISScreen prematch, scoreboard, tacticHome, tacticAway, goal, board, pause, leaveDialog, fulltime;

        // pre-match
        private CISTeamSheetView homeSheet, awaySheet;
        private Label prematchVenue, prematchDifficulty, prematchTime, prematchSide;
        private CISButton prematchStart;

        // scoreboard
        private Label sbHomeName, sbAwayName, sbHomeScore, sbAwayScore, sbTime;
        private VisualElement sbHomeC1, sbHomeC2, sbAwayC1, sbAwayC2;

        // tactics
        private Label tacticHomeTeam, tacticHomeName, tacticAwayTeam, tacticAwayName;
        private VisualElement tacticHomeMeter, tacticAwayMeter;
        private IVisualElementScheduledItem tacticHomeHide, tacticAwayHide;

        // goal flash
        private CISCrest goalCrest;
        private Label goalScorer, goalMeta, goalScore;
        private IVisualElementScheduledItem goalHide;

        // info board
        private Label boardEyebrow, boardTitle, boardHomeName, boardAwayName, boardHomeScore, boardAwayScore;
        private CISCrest boardHomeCrest, boardAwayCrest;
        private VisualElement boardScorers, boardHomeScorers, boardAwayScorers;
        private IVisualElementScheduledItem boardHide;

        // pause
        private CISCrest pauseHomeCrest, pauseAwayCrest;
        private Label pauseHomeName, pauseAwayName, pauseScore, pauseTime, pauseStatsHome, pauseStatsAway;
        private VisualElement pauseStats;
        private CISSpinner pauseCamera, pauseQuality, pauseVolume;
        private CISButton pauseResume;
        private float pauseOpenedAt;

        // full time
        private CISCrest ftHomeCrest, ftAwayCrest;
        private Label ftHomeName, ftAwayName, ftHomeScore, ftAwayScore;
        private VisualElement ftHomeScorers, ftAwayScorers, ftStatsLeft, ftStatsRight;
        private CISButton ftContinue;

        private CISLiveStats liveStats;

        // match state
        private UpcomingMatchEvent upcoming;
        private readonly bool[] awayKitSelected = { false, true };
        private TeamEntry homeTeam, awayTeam;
        private int homeScore, awayScore;
        private int lastMinute;
        private bool kickedOffOnce;
        private bool finished;
        private bool busy;

        private readonly List<(PlayerEntry player, int minute)> homeScorers = new List<(PlayerEntry, int)>();
        private readonly List<(PlayerEntry player, int minute)> awayScorers = new List<(PlayerEntry, int)>();

        private void Awake() {
            CISOverlayUI.Ensure();

            document = CISUI.CreateDocument(transform, "MatchUI", CISUI.SortMatch);
            root = document.rootVisualElement;

            BuildPreMatch();
            BuildHud();
            BuildPause();
            BuildFullTime();

            liveStats = new CISLiveStats(root);
        }

        private void OnEnable() {
            EventManager.Subscribe<UpcomingMatchEvent>(OnUpcomingMatch);
            EventManager.Subscribe<CloseAllPanelsEvent>(OnCloseAll);
            EventManager.Subscribe<MatchKitsEvent>(OnMatchKits);
            EventManager.Subscribe<ShowScoreboardEvent>(OnShowScoreboard);
            EventManager.Subscribe<GameTimeEvent>(OnGameTime);
            EventManager.Subscribe<FirstWhistleEvent>(OnFirstWhistle);
            EventManager.Subscribe<KickOffEvent>(OnKickOff);
            EventManager.Subscribe<GoalScoredEvent>(OnGoalScored);
            EventManager.Subscribe<TeamChangedTactic>(OnTacticChanged);
            EventManager.Subscribe<MatchPauseEvent>(OnMatchPause);
            EventManager.Subscribe<FinalWhistleEvent>(OnFinalWhistle);
            EventManager.Subscribe<InfoboardEvent>(OnInfoboard);
            EventManager.Subscribe<MatchStatisticsEvent>(OnMatchStatistics);
        }

        private void OnDisable() {
            EventManager.UnSubscribe<UpcomingMatchEvent>(OnUpcomingMatch);
            EventManager.UnSubscribe<CloseAllPanelsEvent>(OnCloseAll);
            EventManager.UnSubscribe<MatchKitsEvent>(OnMatchKits);
            EventManager.UnSubscribe<ShowScoreboardEvent>(OnShowScoreboard);
            EventManager.UnSubscribe<GameTimeEvent>(OnGameTime);
            EventManager.UnSubscribe<FirstWhistleEvent>(OnFirstWhistle);
            EventManager.UnSubscribe<KickOffEvent>(OnKickOff);
            EventManager.UnSubscribe<GoalScoredEvent>(OnGoalScored);
            EventManager.UnSubscribe<TeamChangedTactic>(OnTacticChanged);
            EventManager.UnSubscribe<MatchPauseEvent>(OnMatchPause);
            EventManager.UnSubscribe<FinalWhistleEvent>(OnFinalWhistle);
            EventManager.UnSubscribe<InfoboardEvent>(OnInfoboard);
            EventManager.UnSubscribe<MatchStatisticsEvent>(OnMatchStatistics);
        }

        private void Update() {
            liveStats?.Tick();

            // Start on a gamepad also resumes (Esc / B arrive as navigation cancel).
            if (pause != null && pause.IsOpen && !leaveDialog.IsOpen &&
                Time.unscaledTime - pauseOpenedAt > 0.3f) {
                var gamepad = Gamepad.current;
                if (gamepad != null && gamepad.startButton.wasPressedThisFrame) {
                    Resume();
                }
            }
        }

        // =====================================================================
        // PRE-MATCH
        // =====================================================================

        private void BuildPreMatch() {
            prematch = new CISScreen(CISUI.Q<VisualElement>(root, "prematch"), true) {
                OnCancel = BackToMenu
            };

            homeSheet = new CISTeamSheetView(CISUI.Q<VisualElement>(root, "home-sheet"), "HOME", () => SwitchKit(0));
            awaySheet = new CISTeamSheetView(CISUI.Q<VisualElement>(root, "away-sheet"), "AWAY", () => SwitchKit(1));

            prematchVenue = CISUI.Q<Label>(root, "prematch-venue");
            prematchDifficulty = CISUI.Q<Label>(root, "prematch-difficulty");
            prematchTime = CISUI.Q<Label>(root, "prematch-time");
            prematchSide = CISUI.Q<Label>(root, "prematch-side");

            prematchStart = CISUI.Q<CISButton>(root, "prematch-start");
            prematchStart.clicked += StartMatch;

            var back = CISUI.Q<CISButton>(root, "prematch-back");
            back.clicked += BackToMenu;

            prematch.DefaultFocus = prematchStart;
        }

        private void OnUpcomingMatch(UpcomingMatchEvent upcomingMatchEvent) {
            if (upcomingMatchEvent == null) {
                prematch.Hide();
                return;
            }

            upcoming = upcomingMatchEvent;
            homeTeam = upcoming.details.homeTeam;
            awayTeam = upcoming.details.awayTeam;

            awayKitSelected[0] = false; // home team wears its home kit.
            awayKitSelected[1] = true;  // away team wears its away kit.

            EventManager.Trigger(new LoadingEvent("PREPARING_MATCH"));

            var side = upcoming.details.userTeam;
            homeSheet.SetTeam(homeTeam, side == MatchCreateRequest.UserTeam.Home);
            awaySheet.SetTeam(awayTeam, side == MatchCreateRequest.UserTeam.Away);
            homeSheet.SetKit(awayKitSelected[0]);
            awaySheet.SetKit(awayKitSelected[1]);

            var dayTime = CISSettings.DayTimeLabels[(int)CISSettings.DayTime];
            prematchVenue.text = "CIS ARENA  ·  " + dayTime;
            prematchDifficulty.text = CISSettings.AILevelLabels[(int)CISSettings.AILevel];
            prematchTime.text = dayTime;
            prematchSide.text = CISSettings.SideLabels[(int)CISSettings.Side];

            GameInput.SwitchToUI();
            prematch.Show(prematchStart);

            EventManager.Trigger<LoadingEvent>(null);
        }

        private void SwitchKit(int teamIndex) {
            awayKitSelected[teamIndex] = !awayKitSelected[teamIndex];

            if (teamIndex == 0) {
                homeSheet.SetKit(awayKitSelected[0]);
            } else {
                awaySheet.SetKit(awayKitSelected[1]);
            }
        }

        private async void StartMatch() {
            if (busy || upcoming == null) {
                return;
            }

            busy = true;
            prematch.Hide();

            var details = upcoming.details;
            details.aiLevel = CISSettings.AILevel;
            details.dayTime = CISSettings.DayTime;
            details.userTeam = CISSettings.Side;
            upcoming.details = details;

            try {
                await MatchEngineLoader.Current.StartMatchEngine(upcoming, awayKitSelected[0], awayKitSelected[1]);
            } catch (Exception exception) {
                Debug.LogException(exception);
                EventManager.Trigger<BigLoadingEvent>(null);
                EventManager.Trigger(new ErrorEvent("The match could not be started. " + exception.Message));
            }
        }

        private async void BackToMenu() {
            if (busy) {
                return;
            }

            busy = true;
            prematch.Hide();

            var loader = UILoader.Current;
            if (loader == null) {
                return;
            }

            EventManager.Trigger(new LoadingEvent());
            await loader.GeneralUILoader.Load();
            EventManager.Trigger<LoadingEvent>(null);
            EventManager.Trigger(new MainMenuEvent());

            // unloads this UI (and this component).
            loader.MatchUILoader.Unload();
        }

        // =====================================================================
        // HUD
        // =====================================================================

        private void BuildHud() {
            scoreboard = new CISScreen(CISUI.Q<VisualElement>(root, "scoreboard"), false, false);
            sbHomeName = CISUI.Q<Label>(root, "sb-home-name");
            sbAwayName = CISUI.Q<Label>(root, "sb-away-name");
            sbHomeScore = CISUI.Q<Label>(root, "sb-home-score");
            sbAwayScore = CISUI.Q<Label>(root, "sb-away-score");
            sbTime = CISUI.Q<Label>(root, "sb-time");
            sbHomeC1 = CISUI.Q<VisualElement>(root, "sb-home-c1");
            sbHomeC2 = CISUI.Q<VisualElement>(root, "sb-home-c2");
            sbAwayC1 = CISUI.Q<VisualElement>(root, "sb-away-c1");
            sbAwayC2 = CISUI.Q<VisualElement>(root, "sb-away-c2");

            tacticHome = new CISScreen(CISUI.Q<VisualElement>(root, "tactic-home"), false, false);
            tacticAway = new CISScreen(CISUI.Q<VisualElement>(root, "tactic-away"), false, false);
            tacticHomeTeam = CISUI.Q<Label>(root, "tactic-home-team");
            tacticHomeName = CISUI.Q<Label>(root, "tactic-home-name");
            tacticAwayTeam = CISUI.Q<Label>(root, "tactic-away-team");
            tacticAwayName = CISUI.Q<Label>(root, "tactic-away-name");
            tacticHomeMeter = CISUI.Q<VisualElement>(root, "tactic-home-meter");
            tacticAwayMeter = CISUI.Q<VisualElement>(root, "tactic-away-meter");
            BuildMeter(tacticHomeMeter);
            BuildMeter(tacticAwayMeter);

            goal = new CISScreen(CISUI.Q<VisualElement>(root, "goal"), false);
            goalCrest = CISUI.Q<CISCrest>(root, "goal-crest");
            goalScorer = CISUI.Q<Label>(root, "goal-scorer");
            goalMeta = CISUI.Q<Label>(root, "goal-meta");
            goalScore = CISUI.Q<Label>(root, "goal-score");

            board = new CISScreen(CISUI.Q<VisualElement>(root, "board"), false);
            boardEyebrow = CISUI.Q<Label>(root, "board-eyebrow");
            boardTitle = CISUI.Q<Label>(root, "board-title");
            boardHomeName = CISUI.Q<Label>(root, "board-home-name");
            boardAwayName = CISUI.Q<Label>(root, "board-away-name");
            boardHomeScore = CISUI.Q<Label>(root, "board-home-score");
            boardAwayScore = CISUI.Q<Label>(root, "board-away-score");
            boardHomeCrest = CISUI.Q<CISCrest>(root, "board-home-crest");
            boardAwayCrest = CISUI.Q<CISCrest>(root, "board-away-crest");
            boardScorers = CISUI.Q<VisualElement>(root, "board-scorers");
            boardHomeScorers = CISUI.Q<VisualElement>(root, "board-home-scorers");
            boardAwayScorers = CISUI.Q<VisualElement>(root, "board-away-scorers");
        }

        private void OnMatchKits(MatchKitsEvent matchKits) {
            if (matchKits == null) {
                return;
            }

            SetColors(sbHomeC1, sbHomeC2, matchKits.HomeTeamKit);
            SetColors(sbAwayC1, sbAwayC2, matchKits.AwayTeamKit);
        }

        private static void SetColors(VisualElement primary, VisualElement secondary, KitEntry kit) {
            if (kit == null) {
                return;
            }

            primary.style.backgroundColor = CISTeamInfo.Opaque(kit.Color1);
            secondary.style.backgroundColor = CISTeamInfo.Opaque(kit.Color2);
        }

        private void OnShowScoreboard(ShowScoreboardEvent showScoreboardEvent) {
            if (showScoreboardEvent == null) {
                scoreboard.Hide();
                return;
            }

            sbHomeName.text = CISTeamInfo.ShortName(homeTeam);
            sbAwayName.text = CISTeamInfo.ShortName(awayTeam);
            RefreshScore();
            scoreboard.Show();
        }

        private void OnGameTime(GameTimeEvent gameTime) {
            var minutes = Mathf.FloorToInt(gameTime.GameTime);
            var seconds = Mathf.FloorToInt((gameTime.GameTime - minutes) * 60f);
            var text = $"{minutes:00}:{seconds:00}";

            lastMinute = minutes;
            sbTime.text = text;

            if (pause.IsOpen) {
                pauseTime.text = text;
            }
        }

        private void OnFirstWhistle(FirstWhistleEvent _) {
            homeScore = 0;
            awayScore = 0;
            homeScorers.Clear();
            awayScorers.Clear();
            finished = false;
            kickedOffOnce = false;

            RefreshScore();

            // user's preferred camera (CameraSystem resets to its default on load).
            CISSettings.ApplyCamera();

            liveStats.Begin(KickOffBoardMs + 700);
        }

        private void OnKickOff(KickOffEvent _) {
            if (kickedOffOnce) {
                // keep the restart board clear of the live stats ticker.
                liveStats.Suppress(6f);
            }

            if (!kickedOffOnce) {
                kickedOffOnce = true;
                ShowBoard("CIS ARENA  ·  " + CISSettings.DayTimeLabels[(int)CISSettings.DayTime], "KICK OFF", KickOffBoardMs);
            } else {
                ShowBoard($"{lastMinute}'  ·  PLAY RESTARTS", "SCORE UPDATE", KickOffBoardMs);
            }
        }

        private void OnGoalScored(GoalScoredEvent goalScored) {
            var awaySide = goalScored.Side;

            if (!awaySide) {
                homeScore++;
                homeScorers.Add((goalScored.Scorer, goalScored.Minute));
            } else {
                awayScore++;
                awayScorers.Add((goalScored.Scorer, goalScored.Minute));
            }

            RefreshScore();
            CISUI.Pulse(awaySide ? sbAwayScore : sbHomeScore, "scoreboard__goals--pop", 600);

            liveStats.Suppress(12f);

            // goal flash
            var scoringTeam = awaySide ? awayTeam : homeTeam;
            goalCrest.SetTeam(scoringTeam);
            goalScorer.text = goalScored.Scorer != null ? CISUI.Upper(goalScored.Scorer.Name) : "GOAL";
            goalMeta.text = $"{CISUI.Upper(scoringTeam != null ? scoringTeam.TeamName : string.Empty)}  ·  {goalScored.Minute}'";
            goalScore.text = $"{CISTeamInfo.ShortName(homeTeam)}  {homeScore} - {awayScore}  {CISTeamInfo.ShortName(awayTeam)}";

            goalHide?.Pause();
            goal.Show();
            goalHide = goal.Root.schedule.Execute(() => goal.Hide());
            goalHide.StartingIn(GoalFlashMs);
        }

        private void RefreshScore() {
            sbHomeScore.text = homeScore.ToString();
            sbAwayScore.text = awayScore.ToString();
        }

        // ---------- tactics ----------

        private static void BuildMeter(VisualElement meter) {
            meter.Clear();
            var count = (int)TacticPresetTypes.ParameterCount;
            for (int i = 0; i < count; i++) {
                var segment = CISUI.Element("tactic__seg", meter);
                segment.style.backgroundColor = TacticColor(i / (float)(count - 1));
                if (i == count - 1) {
                    segment.style.marginRight = 0;
                }
            }
        }

        private static Color TacticColor(float t) {
            return t < 0.5f
                ? Color.Lerp(CISTeamInfo.DefenceColor, CISTeamInfo.MidfieldColor, t * 2f)
                : Color.Lerp(CISTeamInfo.MidfieldColor, CISTeamInfo.AttackColor, (t - 0.5f) * 2f);
        }

        private static string TacticLabel(TacticPresetTypes tactic) {
            switch (tactic) {
                case TacticPresetTypes.ParkTheBus: return "PARK THE BUS";
                case TacticPresetTypes.Defensive: return "DEFENSIVE";
                case TacticPresetTypes.CounterAttack: return "COUNTER ATTACK";
                case TacticPresetTypes.Balanced: return "BALANCED";
                case TacticPresetTypes.HighPressure: return "HIGH PRESSURE";
                case TacticPresetTypes.Offensive: return "ALL-OUT ATTACK";
            }
            return CISUI.Upper(tactic.ToString());
        }

        private void OnTacticChanged(TeamChangedTactic changed) {
            if (changed == null || finished) {
                return;
            }

            var manager = MatchManager.Current;
            var isHome = manager != null && manager.GameTeam1 == changed.Team;

            var team = isHome ? homeTeam : awayTeam;
            var meter = isHome ? tacticHomeMeter : tacticAwayMeter;
            var screen = isHome ? tacticHome : tacticAway;

            (isHome ? tacticHomeTeam : tacticAwayTeam).text = $"{CISTeamInfo.ShortName(team)}  ·  TACTIC";
            (isHome ? tacticHomeName : tacticAwayName).text = TacticLabel(changed.TacticPreset);

            var active = (int)changed.TacticPreset;
            for (int i = 0; i < meter.childCount; i++) {
                meter[i].EnableInClassList("tactic__seg--on", i <= active);
            }

            screen.Show();

            var job = screen.Root.schedule.Execute(() => screen.Hide());
            job.StartingIn(TacticVisibleMs);

            if (isHome) {
                tacticHomeHide?.Pause();
                tacticHomeHide = job;
            } else {
                tacticAwayHide?.Pause();
                tacticAwayHide = job;
            }
        }

        // ---------- info board ----------

        private void ShowBoard(string eyebrow, string title, long hideAfterMs) {
            boardEyebrow.text = eyebrow;
            boardTitle.text = title;
            boardHomeName.text = CISUI.Upper(homeTeam != null ? homeTeam.TeamName : "HOME");
            boardAwayName.text = CISUI.Upper(awayTeam != null ? awayTeam.TeamName : "AWAY");
            boardHomeScore.text = homeScore.ToString();
            boardAwayScore.text = awayScore.ToString();
            boardHomeCrest.SetTeam(homeTeam);
            boardAwayCrest.SetTeam(awayTeam);

            FillScorers(boardHomeScorers, homeScorers, true);
            FillScorers(boardAwayScorers, awayScorers, false);
            CISUI.SetVisible(boardScorers, homeScorers.Count + awayScorers.Count > 0);

            boardHide?.Pause();
            board.Show();

            if (hideAfterMs > 0) {
                boardHide = board.Root.schedule.Execute(() => board.Hide());
                boardHide.StartingIn(hideAfterMs);
            }
        }

        private static void FillScorers(VisualElement container, List<(PlayerEntry player, int minute)> scorers, bool iconAfter) {
            container.Clear();

            foreach (var (player, minute) in scorers) {
                var row = CISUI.Element("scorer", container);

                if (!iconAfter) {
                    AddBall(row);
                }

                CISUI.Text(player != null ? CISTeamInfo.Surname(player.Name) : "-", "scorer__name", row);
                CISUI.Text(minute + "'", "scorer__minute", row);

                if (iconAfter) {
                    AddBall(row);
                }
            }
        }

        private static void AddBall(VisualElement row) {
            var ball = new CISIcon(CISIcon.Kind.Ball);
            ball.AddToClassList("scorer__icon");
            row.Add(ball);
        }

        private void OnInfoboard(InfoboardEvent infoboardEvent) {
            if (infoboardEvent == null) {
                board.Hide();
                return;
            }

            if (finished) {
                ShowBoard("FINAL WHISTLE", "FULL TIME", 0);
            } else {
                ShowBoard($"{lastMinute}'", "SCORE UPDATE", KickOffBoardMs);
            }
        }

        // =====================================================================
        // FINAL WHISTLE
        // =====================================================================

        private void OnFinalWhistle(FinalWhistleEvent _) {
            finished = true;

            liveStats.Stop();
            tacticHome.Hide();
            tacticAway.Hide();
            goal.Hide();

            // same sequence the old FinalWhistlePanel ran.
            EventManager.Trigger<ShowScoreboardEvent>(null);
            EventManager.Trigger(new InfoboardEvent());

            root.schedule.Execute(() => {
                if (finished) {
                    EventManager.Trigger(new MatchStatisticsEvent());
                }
            }).StartingIn(FullTimeStatsDelayMs);
        }

        private void OnMatchStatistics(MatchStatisticsEvent matchStatisticsEvent) {
            if (matchStatisticsEvent == null) {
                fulltime.Hide();
                return;
            }

            ShowFullTime();
        }

        // =====================================================================
        // PAUSE
        // =====================================================================

        private void BuildPause() {
            pause = new CISScreen(CISUI.Q<VisualElement>(root, "pause"), true) {
                OnCancel = Resume
            };

            pauseHomeCrest = CISUI.Q<CISCrest>(root, "pause-home-crest");
            pauseAwayCrest = CISUI.Q<CISCrest>(root, "pause-away-crest");
            pauseHomeName = CISUI.Q<Label>(root, "pause-home-name");
            pauseAwayName = CISUI.Q<Label>(root, "pause-away-name");
            pauseScore = CISUI.Q<Label>(root, "pause-score");
            pauseTime = CISUI.Q<Label>(root, "pause-time");
            pauseStats = CISUI.Q<VisualElement>(root, "pause-stats");
            pauseStatsHome = CISUI.Q<Label>(root, "pause-stats-home");
            pauseStatsAway = CISUI.Q<Label>(root, "pause-stats-away");

            pauseResume = CISUI.Q<CISButton>(root, "pause-resume");
            pauseResume.clicked += Resume;

            pauseCamera = CISUI.Q<CISSpinner>(root, "pause-camera");
            pauseCamera.SetOptions(CISSettings.CameraLabels, CISSettings.Camera);
            pauseCamera.changed += index => CISSettings.Camera = index;

            pauseQuality = CISUI.Q<CISSpinner>(root, "pause-quality");
            pauseQuality.SetOptions(CISSettings.QualityLabels, CISSettings.Quality);
            pauseQuality.changed += index => CISSettings.Quality = index;

            pauseVolume = CISUI.Q<CISSpinner>(root, "pause-volume");
            pauseVolume.SetOptions(CISSettings.VolumeLabels, CISSettings.Volume);
            pauseVolume.changed += index => CISSettings.Volume = index;

            var leave = CISUI.Q<CISButton>(root, "pause-leave");
            leave.clicked += () => leaveDialog.Show();

            pause.DefaultFocus = pauseResume;

            leaveDialog = new CISScreen(CISUI.Q<VisualElement>(root, "leave-dialog"), true) {
                OnCancel = () => leaveDialog.Hide()
            };

            var stay = CISUI.Q<CISButton>(root, "leave-no");
            var confirm = CISUI.Q<CISButton>(root, "leave-yes");
            stay.clicked += () => leaveDialog.Hide();
            confirm.clicked += LeaveMatch;
            leaveDialog.DefaultFocus = stay;
        }

        private void OnMatchPause(MatchPauseEvent matchPauseEvent) {
            if (matchPauseEvent == null) {
                leaveDialog.HideImmediate();
                pause.Hide();
                return;
            }

            GameInput.SwitchToUI();

            pauseHomeCrest.SetTeam(homeTeam);
            pauseAwayCrest.SetTeam(awayTeam);
            pauseHomeName.text = CISTeamInfo.ShortName(homeTeam);
            pauseAwayName.text = CISTeamInfo.ShortName(awayTeam);
            pauseScore.text = $"{homeScore} - {awayScore}";
            pauseTime.text = sbTime.text;
            pauseStatsHome.text = CISUI.Upper(homeTeam != null ? homeTeam.TeamName : "HOME");
            pauseStatsAway.text = CISUI.Upper(awayTeam != null ? awayTeam.TeamName : "AWAY");

            pauseCamera.SetIndexWithoutNotify(CISSettings.Camera);
            pauseQuality.SetIndexWithoutNotify(CISSettings.Quality);
            pauseVolume.SetIndexWithoutNotify(CISSettings.Volume);

            var (homeColor, awayColor) = KitColors();
            CISStatsView.Build(pauseStats, MatchManager.Statistics, homeColor, awayColor);

            pauseOpenedAt = Time.unscaledTime;
            pause.Show(pauseResume);
        }

        private void Resume() {
            if (!pause.IsOpen) {
                return;
            }

            leaveDialog.HideImmediate();
            pause.Hide();

            GameInput.SwitchToMatchEngine();

            if (MatchPause.IsPaused) {
                MatchPause.Pause(); // toggles back to playing.
            }
        }

        private async void LeaveMatch() {
            if (busy) {
                return;
            }

            busy = true;
            finished = true;

            leaveDialog.HideImmediate();
            pause.HideImmediate();
            liveStats.Stop();

            if (MatchPause.IsPaused) {
                MatchPause.Pause();
            }

            EventManager.Trigger(new BigLoadingEvent());

            if (MatchEngineLoader.Current != null) {
                await MatchEngineLoader.Current.UnloadMatch();
            }

            EventManager.Trigger(new CloseAllPanelsEvent());
            EventManager.Trigger(new MainMenuEvent());
            EventManager.Trigger<BigLoadingEvent>(null);
        }

        // =====================================================================
        // FULL TIME
        // =====================================================================

        private void BuildFullTime() {
            fulltime = new CISScreen(CISUI.Q<VisualElement>(root, "fulltime"), true) {
                OnCancel = null
            };

            ftHomeCrest = CISUI.Q<CISCrest>(root, "ft-home-crest");
            ftAwayCrest = CISUI.Q<CISCrest>(root, "ft-away-crest");
            ftHomeName = CISUI.Q<Label>(root, "ft-home-name");
            ftAwayName = CISUI.Q<Label>(root, "ft-away-name");
            ftHomeScore = CISUI.Q<Label>(root, "ft-home-score");
            ftAwayScore = CISUI.Q<Label>(root, "ft-away-score");
            ftHomeScorers = CISUI.Q<VisualElement>(root, "ft-home-scorers");
            ftAwayScorers = CISUI.Q<VisualElement>(root, "ft-away-scorers");
            ftStatsLeft = CISUI.Q<VisualElement>(root, "ft-stats-left");
            ftStatsRight = CISUI.Q<VisualElement>(root, "ft-stats-right");

            ftContinue = CISUI.Q<CISButton>(root, "ft-continue");
            ftContinue.clicked += QuitMatch;

            fulltime.DefaultFocus = ftContinue;
        }

        private void ShowFullTime() {
            board.Hide();
            scoreboard.Hide();

            ftHomeCrest.SetTeam(homeTeam);
            ftAwayCrest.SetTeam(awayTeam);
            ftHomeName.text = CISUI.Upper(homeTeam != null ? homeTeam.TeamName : "HOME");
            ftAwayName.text = CISUI.Upper(awayTeam != null ? awayTeam.TeamName : "AWAY");

            var manager = MatchManager.Current;
            if (manager != null) {
                // the engine is the source of truth for the final score.
                homeScore = Mathf.Max(homeScore, manager.homeTeamScore);
                awayScore = Mathf.Max(awayScore, manager.awayTeamScore);
            }

            ftHomeScore.text = homeScore.ToString();
            ftAwayScore.text = awayScore.ToString();

            FillScorers(ftHomeScorers, homeScorers, false);
            FillScorers(ftAwayScorers, awayScorers, false);

            var (homeColor, awayColor) = KitColors();
            CISStatsView.Build(ftStatsLeft, MatchManager.Statistics, homeColor, awayColor);

            // split the rows over two columns.
            ftStatsRight.Clear();
            var rows = ftStatsLeft.childCount;
            var keep = (rows + 1) / 2;
            while (ftStatsLeft.childCount > keep) {
                ftStatsRight.Add(ftStatsLeft[keep]);
            }

            GameInput.SwitchToUI();
            fulltime.Show(ftContinue);
        }

        private async void QuitMatch() {
            if (busy) {
                return;
            }

            busy = true;

            if (MatchEngineLoader.Current == null) {
                busy = false;
                return;
            }

            fulltime.Hide();

            EventManager.Trigger(new BigLoadingEvent());

            await MatchEngineLoader.Current.UnloadMatch();

            // back to main menu (same order the previous statistics panel used).
            EventManager.Trigger<InfoboardEvent>(null);
            EventManager.Trigger<MatchCompleteEvent>(null);
            EventManager.Trigger(new MainMenuEvent());
            EventManager.Trigger<BigLoadingEvent>(null);
        }

        // =====================================================================

        private (Color home, Color away) KitColors() {
            var details = MatchManager.CurrentMatchDetails;
            var home = details?.HomeKit != null ? details.HomeKit.Color1 : CISGradients.Accent;
            var away = details?.AwayKit != null ? details.AwayKit.Color1 : CISGradients.Accent2;
            return (home, away);
        }

        private void OnCloseAll(CloseAllPanelsEvent _) {
            prematch?.Hide();
            leaveDialog?.HideImmediate();
            pause?.Hide();
            fulltime?.Hide();
            scoreboard?.Hide();
            tacticHome?.Hide();
            tacticAway?.Hide();
            goal?.Hide();
            board?.Hide();
            liveStats?.Suppress(0f);
        }
    }
}
