using System.Linq;
using FStudio.Data;
using FStudio.Database;
using FStudio.Events;
using FStudio.MatchEngine;
using FStudio.MatchEngine.Enums;
using FStudio.UI.Events;
using Shared.Responses;
using UnityEngine;
using UnityEngine.UIElements;

namespace FStudio.UIToolkit {
    /// <summary>
    /// Front-end menus built with UI Toolkit: hub, kick off (team selection +
    /// match settings), settings, controls. Lives on the UISystem prefab that
    /// UILoader.GeneralUILoader instantiates.
    /// </summary>
    public class CISMainMenuUI : MonoBehaviour {
        private const string PrefHome = "CIS_LAST_HOME_TEAM";
        private const string PrefAway = "CIS_LAST_AWAY_TEAM";

        private static readonly (string action, string[] keys)[] GamepadControls = {
            ("Move player", new[] { "L-STICK" }),
            ("Pass", new[] { "A" }),
            ("Through pass", new[] { "Y" }),
            ("Shoot", new[] { "X" }),
            ("Tackle / Cross", new[] { "B" }),
            ("Switch player", new[] { "LB" }),
            ("Tactic: defensive / attacking", new[] { "D-PAD LEFT", "D-PAD RIGHT" }),
            ("Pause", new[] { "START" })
        };

        private static readonly (string action, string[] keys)[] KeyboardControls = {
            ("Move player", new[] { "ARROWS" }),
            ("Pass", new[] { "S" }),
            ("Through pass", new[] { "W" }),
            ("Shoot", new[] { "D" }),
            ("Tackle / Cross", new[] { "A" }),
            ("Switch player", new[] { "Q" }),
            ("Tactic: defensive / attacking", new[] { "Z", "X" }),
            ("Pause", new[] { "ESC" })
        };

        private UIDocument document;
        private VisualElement root;

        private CISScreen shell;
        private CISScreen hub, kickoff, settings, controls;
        private CISScreen picker, quitDialog;
        private CISScreen currentPage;

        private Label pageTitle;
        private Label venue;
        private Label hubFixture;
        private Label pickerEyebrow;
        private VisualElement pickerGrid;
        private CISCrest hubHomeCrest, hubAwayCrest;

        private CISSpinner spinHome, spinAway, spinSide, spinDifficulty, spinTime;
        private CISSpinner spinQuality, spinCamera, spinVolume;

        private CISTeamCardView homeCard, awayCard;

        private TeamEntry[] teams = new TeamEntry[0];
        private int homeIndex, awayIndex;
        private bool pickerForHome;
        private bool isStarting;

        private void Awake() {
            CISOverlayUI.Ensure();

            document = CISUI.CreateDocument(transform, "MainMenu", CISUI.SortMenu);
            root = document.rootVisualElement;

            LoadTeams();
            BuildShell();
            BuildHub();
            BuildKickOff();
            BuildSettings();
            BuildControls();
            BuildQuitDialog();
        }

        private void OnEnable() {
            EventManager.Subscribe<MainMenuEvent>(OnMainMenu);
            EventManager.Subscribe<CloseAllPanelsEvent>(OnCloseAll);
        }

        private void OnDisable() {
            EventManager.UnSubscribe<MainMenuEvent>(OnMainMenu);
            EventManager.UnSubscribe<CloseAllPanelsEvent>(OnCloseAll);
        }

        // ------------------------------------------------------------------ events

        private void OnMainMenu(MainMenuEvent mainMenuEvent) {
            if (mainMenuEvent == null) {
                HideEverything();
                return;
            }

            isStarting = false;

            GameInput.SwitchToUI();

            RefreshHub();
            shell.Show();
            ShowPage(hub);
        }

        private void OnCloseAll(CloseAllPanelsEvent _) {
            HideEverything();
        }

        private void HideEverything() {
            picker?.HideImmediate();
            quitDialog?.HideImmediate();
            hub?.Hide();
            kickoff?.Hide();
            settings?.Hide();
            controls?.Hide();
            shell?.Hide();
            currentPage = null;
        }

        // ------------------------------------------------------------------ shell & pages

        private void BuildShell() {
            shell = new CISScreen(CISUI.Q<VisualElement>(root, "menu-shell"), false);
            pageTitle = CISUI.Q<Label>(root, "page-title");
        }

        private void ShowPage(CISScreen page) {
            if (currentPage == page && page.IsOpen) {
                return;
            }

            foreach (var other in new[] { hub, kickoff, settings, controls }) {
                if (other != page) {
                    other.Hide();
                }
            }

            currentPage = page;

            if (page == hub) {
                pageTitle.text = "MAIN MENU";
            } else if (page == kickoff) {
                pageTitle.text = "KICK OFF";
            } else if (page == settings) {
                pageTitle.text = "SETTINGS";
            } else if (page == controls) {
                pageTitle.text = "CONTROLS";
            }

            page.Show();
        }

        // ------------------------------------------------------------------ hub

        private void BuildHub() {
            hub = new CISScreen(CISUI.Q<VisualElement>(root, "page-hub"), true);

            var kickoffTile = CISUI.Q<VisualElement>(root, "tile-kickoff");
            var settingsTile = CISUI.Q<VisualElement>(root, "tile-settings");
            var controlsTile = CISUI.Q<VisualElement>(root, "tile-controls");
            var quitTile = CISUI.Q<VisualElement>(root, "tile-quit");

            hubHomeCrest = CISUI.Q<CISCrest>(root, "hub-home-crest");
            hubAwayCrest = CISUI.Q<CISCrest>(root, "hub-away-crest");
            hubFixture = CISUI.Q<Label>(root, "hub-fixture");

            CISUI.OnActivate(kickoffTile, () => ShowPage(kickoff));
            CISUI.OnActivate(settingsTile, () => ShowPage(settings));
            CISUI.OnActivate(controlsTile, () => ShowPage(controls));
            CISUI.OnActivate(quitTile, () => quitDialog.Show());

#if UNITY_WEBGL
            CISUI.SetVisible(quitTile, false);
#endif

            hub.DefaultFocus = kickoffTile;
        }

        private void RefreshHub() {
            if (teams.Length == 0) {
                return;
            }

            var home = teams[homeIndex];
            var away = teams[awayIndex];

            hubHomeCrest.SetTeam(home);
            hubAwayCrest.SetTeam(away);
            hubFixture.text = $"{home.TeamName}  vs  {away.TeamName}  ·  {CISSettings.DayTimeLabels[(int)CISSettings.DayTime].ToLowerInvariant()}";
        }

        // ------------------------------------------------------------------ kick off

        private void LoadTeams() {
            teams = DatabaseService.LoadTeams() ?? new TeamEntry[0];

            if (teams.Length == 0) {
                Debug.LogError("[CISMainMenuUI] No teams found in Resources/Database.");
                return;
            }

            homeIndex = FindTeam(PlayerPrefs.GetString(PrefHome, string.Empty), Random.Range(0, teams.Length));
            awayIndex = FindTeam(PlayerPrefs.GetString(PrefAway, string.Empty), -1);

            if (awayIndex < 0) {
                awayIndex = teams.Length > 1 ? (homeIndex + 1 + Random.Range(0, teams.Length - 1)) % teams.Length : homeIndex;
            }
        }

        private int FindTeam(string teamName, int fallback) {
            if (!string.IsNullOrEmpty(teamName)) {
                for (int i = 0; i < teams.Length; i++) {
                    if (teams[i].TeamName == teamName) {
                        return i;
                    }
                }
            }
            return fallback;
        }

        private void BuildKickOff() {
            kickoff = new CISScreen(CISUI.Q<VisualElement>(root, "page-kickoff"), true) {
                OnCancel = () => ShowPage(hub)
            };

            venue = CISUI.Q<Label>(root, "kickoff-venue");

            homeCard = new CISTeamCardView(CISUI.Q<VisualElement>(root, "home-slot"), "HOME",
                () => StepTeam(true, -1), () => StepTeam(true, 1), () => OpenPicker(true));

            awayCard = new CISTeamCardView(CISUI.Q<VisualElement>(root, "away-slot"), "AWAY",
                () => StepTeam(false, -1), () => StepTeam(false, 1), () => OpenPicker(false));

            var teamNames = teams.Select(t => CISUI.Upper(t.TeamName)).ToArray();

            spinHome = CISUI.Q<CISSpinner>(root, "spin-home");
            spinHome.SetOptions(teamNames, homeIndex);
            spinHome.changed += index => SetTeam(true, index);
            spinHome.submitted += () => OpenPicker(true);

            spinAway = CISUI.Q<CISSpinner>(root, "spin-away");
            spinAway.SetOptions(teamNames, awayIndex);
            spinAway.changed += index => SetTeam(false, index);
            spinAway.submitted += () => OpenPicker(false);

            spinSide = CISUI.Q<CISSpinner>(root, "spin-side");
            spinSide.SetOptions(CISSettings.SideLabels, (int)CISSettings.Side);
            spinSide.changed += index => {
                CISSettings.Side = (MatchCreateRequest.UserTeam)index;
                RefreshControllers();
            };

            spinDifficulty = CISUI.Q<CISSpinner>(root, "spin-difficulty");
            spinDifficulty.SetOptions(CISSettings.AILevelLabels, (int)CISSettings.AILevel);
            spinDifficulty.changed += index => CISSettings.AILevel = (AILevel)index;

            spinTime = CISUI.Q<CISSpinner>(root, "spin-time");
            spinTime.SetOptions(CISSettings.DayTimeLabels, (int)CISSettings.DayTime);
            spinTime.changed += index => {
                CISSettings.DayTime = (DayTimes)index;
                RefreshVenue();
            };

            var play = CISUI.Q<CISButton>(root, "kickoff-play");
            play.clicked += Play;

            kickoff.DefaultFocus = play;

            // team picker dialog
            picker = new CISScreen(CISUI.Q<VisualElement>(root, "team-picker"), true) {
                OnCancel = () => picker.Hide()
            };
            pickerGrid = CISUI.Q<VisualElement>(root, "team-picker-grid");
            pickerEyebrow = CISUI.Q<Label>(root, "team-picker-eyebrow");

            if (teams.Length > 0) {
                homeCard.SetTeam(teams[homeIndex]);
                awayCard.SetTeam(teams[awayIndex]);
            }

            RefreshControllers();
            RefreshVenue();
        }

        private void StepTeam(bool home, int delta) {
            if (teams.Length == 0) {
                return;
            }

            var index = ((home ? homeIndex : awayIndex) + delta + teams.Length) % teams.Length;
            SetTeam(home, index);
        }

        private void SetTeam(bool home, int index) {
            if (teams.Length == 0) {
                return;
            }

            if (home) {
                homeIndex = index;
                spinHome.SetIndexWithoutNotify(index);
                homeCard.SetTeam(teams[index]);
                PlayerPrefs.SetString(PrefHome, teams[index].TeamName);
            } else {
                awayIndex = index;
                spinAway.SetIndexWithoutNotify(index);
                awayCard.SetTeam(teams[index]);
                PlayerPrefs.SetString(PrefAway, teams[index].TeamName);
            }

            RefreshHub();
        }

        private void RefreshControllers() {
            var side = CISSettings.Side;
            homeCard.SetController(side == MatchCreateRequest.UserTeam.Home);
            awayCard.SetController(side == MatchCreateRequest.UserTeam.Away);
        }

        private void RefreshVenue() {
            venue.text = "CIS ARENA  ·  " + CISSettings.DayTimeLabels[(int)CISSettings.DayTime];
            RefreshHub();
        }

        private void OpenPicker(bool forHome) {
            if (teams.Length == 0) {
                return;
            }

            pickerForHome = forHome;
            pickerEyebrow.text = forHome ? "HOME TEAM" : "AWAY TEAM";
            pickerGrid.Clear();

            var selected = forHome ? homeIndex : awayIndex;
            VisualElement selectedTile = null;

            for (int i = 0; i < teams.Length; i++) {
                var index = i;
                var team = teams[i];

                var tile = CISUI.Element("pick-tile", pickerGrid);
                tile.focusable = true;
                tile.pickingMode = PickingMode.Position;

                var crest = new CISCrest { bakeSize = 256 };
                crest.AddToClassList("pick-tile__crest");
                tile.Add(crest);
                crest.SetTeam(team);

                CISUI.Text(CISUI.Upper(team.TeamName), "pick-tile__name", tile);

                var meta = CISUI.Element("pick-tile__meta", tile);
                var badge = new CISRatingBadge();
                badge.AddToClassList("cis-rating--small");
                badge.Value = CISTeamInfo.SafeOverall(team);
                meta.Add(badge);

                var (att, mid, def) = CISTeamInfo.LineRatings(team);
                CISUI.Text($"ATT {att}  MID {mid}  DEF {def}", "pick-tile__meta-text", meta);

                var check = CISUI.Element("pick-tile__check", tile);
                var checkIcon = new CISIcon(CISIcon.Kind.Check);
                checkIcon.AddToClassList("pick-tile__check-icon");
                check.Add(checkIcon);

                if (index == selected) {
                    tile.AddToClassList("pick-tile--selected");
                    selectedTile = tile;
                }

                CISUI.OnActivate(tile, () => {
                    SetTeam(pickerForHome, index);
                    picker.Hide();
                });
            }

            picker.Show(selectedTile);
        }

        private async void Play() {
            if (isStarting || teams.Length == 0) {
                return;
            }

            isStarting = true;

            PlayerPrefs.SetString(PrefHome, teams[homeIndex].TeamName);
            PlayerPrefs.SetString(PrefAway, teams[awayIndex].TeamName);
            PlayerPrefs.Save();

            var request = new MatchCreateRequest(teams[homeIndex], teams[awayIndex]) {
                aiLevel = CISSettings.AILevel,
                dayTime = CISSettings.DayTime,
                userTeam = CISSettings.Side
            };

            try {
                await MatchEngineLoader.CreateMatch(request);
            } catch (System.Exception exception) {
                Debug.LogException(exception);
                isStarting = false;
                EventManager.Trigger(new ErrorEvent("The match could not be created. " + exception.Message));
            }
        }

        // ------------------------------------------------------------------ settings

        private void BuildSettings() {
            settings = new CISScreen(CISUI.Q<VisualElement>(root, "page-settings"), true) {
                OnCancel = () => ShowPage(hub)
            };

            spinQuality = CISUI.Q<CISSpinner>(root, "spin-quality");
            spinQuality.SetOptions(CISSettings.QualityLabels, CISSettings.Quality);
            spinQuality.changed += index => CISSettings.Quality = index;

            spinCamera = CISUI.Q<CISSpinner>(root, "spin-camera");
            spinCamera.SetOptions(CISSettings.CameraLabels, CISSettings.Camera);
            spinCamera.changed += index => CISSettings.Camera = index;

            spinVolume = CISUI.Q<CISSpinner>(root, "spin-volume");
            spinVolume.SetOptions(CISSettings.VolumeLabels, CISSettings.Volume);
            spinVolume.changed += index => CISSettings.Volume = index;

            var back = CISUI.Q<CISButton>(root, "settings-back");
            back.clicked += () => ShowPage(hub);

            settings.DefaultFocus = spinQuality;
        }

        // ------------------------------------------------------------------ controls

        private void BuildControls() {
            controls = new CISScreen(CISUI.Q<VisualElement>(root, "page-controls"), true) {
                OnCancel = () => ShowPage(hub)
            };

            FillControls(CISUI.Q<VisualElement>(root, "controls-gamepad"), GamepadControls);
            FillControls(CISUI.Q<VisualElement>(root, "controls-keyboard"), KeyboardControls);

            var back = CISUI.Q<CISButton>(root, "controls-back");
            back.clicked += () => ShowPage(hub);

            controls.DefaultFocus = back;
        }

        private static void FillControls(VisualElement list, (string action, string[] keys)[] rows) {
            if (list == null) {
                return;
            }

            foreach (var (action, keys) in rows) {
                var row = CISUI.Element("control-row", list);
                CISUI.Text(action, "control-row__action", row);

                foreach (var key in keys) {
                    var cap = CISUI.Element("control-row__key", row);
                    CISUI.Text(key, "control-row__key-text", cap);
                }
            }
        }

        // ------------------------------------------------------------------ quit

        private void BuildQuitDialog() {
            quitDialog = new CISScreen(CISUI.Q<VisualElement>(root, "quit-dialog"), true) {
                OnCancel = () => quitDialog.Hide()
            };

            var stay = CISUI.Q<CISButton>(root, "quit-no");
            var quit = CISUI.Q<CISButton>(root, "quit-yes");

            stay.clicked += () => quitDialog.Hide();
            quit.clicked += Quit;

            quitDialog.DefaultFocus = stay;
        }

        private static void Quit() {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
