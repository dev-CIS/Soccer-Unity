using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;

namespace FStudio.Diagnostics {
    /// <summary>
    /// TEMPORARY performance probe. Records Unity's built-in player-loop markers and the
    /// custom sections timed with Begin/End, and writes Logs/perf-report.txt every few seconds.
    /// Remove this file and its Begin/End calls once the lag is found.
    /// </summary>
    public class PerfProbe : MonoBehaviour {
        private const float REPORT_EVERY_SECONDS = 5f;

        private static readonly string[] MarkerPrefixes = {
            "Initialization.", "EarlyUpdate.", "FixedUpdate.", "PreUpdate.", "Update.",
            "PreLateUpdate.", "PostLateUpdate."
        };

        private static readonly string[] MarkerKeywords = {
            "EditorLoop", "Physics.", "Animat", "Director.", "UIElements", "UIR.", "GC.Collect",
            "Gfx.Wait", "Camera.Render", "Render.OpaqueGeometry", "Shadows.", "Inl_", "UniversalRenderPipeline",
            "Canvas.", "Audio", "Debug.Log", "LogStringToConsole"
        };

        private class Section {
            public long ticks;
            public int calls;
        }

        private static readonly Dictionary<object, Section> sections = new Dictionary<object, Section>();

        private readonly List<(string name, ProfilerRecorder recorder, double totalNs)> markers =
            new List<(string, ProfilerRecorder, double)>();

        private ProfilerRecorder mainThread;
        private ProfilerRecorder gcAllocated;
        private double mainThreadTotalNs;
        private double gcAllocatedTotal;
        private int frames;
        private float nextReport;
        private int reportIndex;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create() {
            var go = new GameObject("PerfProbe");
            DontDestroyOnLoad(go);
            go.AddComponent<PerfProbe>();
        }

        public static long Begin() => Stopwatch.GetTimestamp();

        /// <param name="key">a string, or a Type (shown by its name).</param>
        public static void End(object key, long begin) {
            var elapsed = Stopwatch.GetTimestamp() - begin;
            if (!sections.TryGetValue(key, out var section)) {
                section = new Section();
                sections.Add(key, section);
            }
            section.ticks += elapsed;
            section.calls++;
        }

        private void OnEnable() {
            mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
            gcAllocated = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);

            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);

            var names = new HashSet<string>();
            foreach (var handle in handles) {
                var description = ProfilerRecorderHandle.GetDescription(handle);
                var name = description.Name;
                if (description.UnitType != ProfilerMarkerDataUnit.TimeNanoseconds || !names.Add(name)) {
                    continue;
                }

                if (MarkerPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal)) ||
                    MarkerKeywords.Any(k => name.Contains(k))) {
                    markers.Add((name, ProfilerRecorder.StartNew(description.Category, name, 1), 0));
                }

                if (markers.Count >= 400) {
                    break;
                }
            }

            nextReport = Time.realtimeSinceStartup + REPORT_EVERY_SECONDS;
        }

        private void OnDisable() {
            mainThread.Dispose();
            gcAllocated.Dispose();
            foreach (var marker in markers) {
                marker.recorder.Dispose();
            }
            markers.Clear();
        }

        private void Update() {
            frames++;
            mainThreadTotalNs += mainThread.LastValue;
            gcAllocatedTotal += gcAllocated.LastValue;

            for (int i = 0; i < markers.Count; i++) {
                var marker = markers[i];
                marker.totalNs += marker.recorder.LastValue;
                markers[i] = marker;
            }

            if (Time.realtimeSinceStartup < nextReport) {
                return;
            }

            nextReport = Time.realtimeSinceStartup + REPORT_EVERY_SECONDS;
            WriteReport();
        }

        private void WriteReport() {
            if (frames == 0) {
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"=== Report {++reportIndex} at {DateTime.Now:HH:mm:ss}  frames={frames}  " +
                $"avgMainThread={mainThreadTotalNs / frames / 1e6:F2} ms  " +
                $"gcAllocPerFrame={gcAllocatedTotal / frames / 1024:F1} KB  " +
                $"timeScale={Time.timeScale}  fixedDT={Time.fixedDeltaTime}");

            sb.AppendLine("--- Unity markers (avg ms per frame, top 45) ---");
            foreach (var marker in markers.Where(m => m.totalNs > 0).OrderByDescending(m => m.totalNs).Take(45)) {
                sb.AppendLine($"{marker.totalNs / frames / 1e6,8:F3}  {marker.name}");
            }

            sb.AppendLine("--- Game code sections (avg ms per frame | calls per frame) ---");
            foreach (var pair in sections.OrderByDescending(s => s.Value.ticks)) {
                var ms = pair.Value.ticks * 1000.0 / Stopwatch.Frequency / frames;
                var name = pair.Key is Type type ? "  behaviour " + type.Name : pair.Key.ToString();
                sb.AppendLine($"{ms,8:F3}  {(float)pair.Value.calls / frames,7:F1}  {name}");
            }

            sb.AppendLine();

            try {
                var path = Path.Combine(Application.isEditor ? Path.Combine(Application.dataPath, "..", "Logs") : Application.persistentDataPath, "perf-report.txt");
                File.AppendAllText(path, sb.ToString());
            } catch (Exception e) {
                UnityEngine.Debug.LogWarning("[PerfProbe] " + e.Message);
            }

            // reset window.
            frames = 0;
            mainThreadTotalNs = 0;
            gcAllocatedTotal = 0;
            for (int i = 0; i < markers.Count; i++) {
                var marker = markers[i];
                marker.totalNs = 0;
                markers[i] = marker;
            }
            foreach (var section in sections.Values) {
                section.ticks = 0;
                section.calls = 0;
            }
        }
    }
}
