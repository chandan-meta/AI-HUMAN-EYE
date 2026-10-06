using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    /// Tools > Human Eye > Validate Module: automated integrity checks (ledger, exact text, coverage, order, whitelists,
    /// references, audio mapping, build settings) plus the generated chapter/camera cue sheet.
    /// Batch: -executeMethod HumanEye.EditorTools.Validator.RunBatch (exit code 1 on failure).
    public static class Validator
    {
        [Serializable] class LedgerCue { public string id, scene, type, text, filename; public int sequence, docxParagraph; }
        [Serializable] class Ledger { public LedgerCue[] cues; }

        static readonly Dictionary<string, int> SceneCounts = new()
        { ["Scene02"] = 16, ["Scene03"] = 12, ["Scene04"] = 8, ["Scene05"] = 8, ["Scene06"] = 4, ["Scene07"] = 3, ["Scene08"] = 6, ["Scene09"] = 4, ["Scene10"] = 2, ["Scene11"] = 3 };
        static readonly string[] AwaitKeys = { "scan", "inside", "brighter", "dimmer", "near", "distant" };
        static readonly string[] FreeModes = { "brightness", "photoreceptors" };
        static readonly string[] PartIds = { "sclera", "cornea", "iris", "pupil", "aqueous", "lens", "vitreous", "retina", "choroid", "optic_nerve", "brain", "visual_area", "rods", "cones" };

        public class Report { public readonly List<string> errors = new(), warnings = new(), passed = new(); }

        [MenuItem("Tools/Human Eye/Validate Module", priority = 2)]
        public static void Menu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var r = Run();
            EditorUtility.DisplayDialog("Human Eye validation", $"{r.passed.Count} checks passed, {r.warnings.Count} warnings, {r.errors.Count} errors.\nSee Docs/HumanEye/VALIDATION_REPORT.md", "OK");
        }

        public static void RunBatch() { var r = Run(); EditorApplication.Exit(r.errors.Count == 0 ? 0 : 1); }

        static void Check(Report r, bool ok, string what) { if (ok) r.passed.Add(what); else r.errors.Add(what); }

        public static string Strip(string raw)
        {
            var t = Regex.Replace(raw.Trim(), @"^(Niko:|Prompt and VO:|Prompt \+ VO:)\s*", "");
            if (t.Length >= 2 && "“\"".IndexOf(t[0]) >= 0 && "”\"".IndexOf(t[^1]) >= 0) t = t.Substring(1, t.Length - 2);
            return t.Trim();
        }

        public static string FileName(int seq, string text)
        {
            var words = text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => new string(w.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray())).Where(w => w.Length > 0).Take(4);
            return $"{seq:00}_" + string.Join("_", words);
        }

        public static List<string> DocxParagraphs(string path)
        {
            using var zip = ZipFile.OpenRead(path);
            var doc = new XmlDocument();
            using (var s = zip.GetEntry("word/document.xml").Open()) doc.Load(s);
            var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");
            return doc.SelectNodes("//w:body//w:p", ns).Cast<XmlNode>()
                .Select(p => string.Concat(p.SelectNodes(".//w:t", ns).Cast<XmlNode>().Select(t => t.InnerText))).ToList();
        }

        public static List<List<string>> ReadCsv(string path)
        {
            var rows = new List<List<string>>(); var row = new List<string>(); var cell = new StringBuilder(); bool q = false;
            var text = File.ReadAllText(path, Encoding.UTF8);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (q) { if (c == '"') { if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; } else q = false; } else cell.Append(c); }
                else if (c == '"') q = true;
                else if (c == ',') { row.Add(cell.ToString()); cell.Clear(); }
                else if (c == '\n') { row.Add(cell.ToString()); cell.Clear(); rows.Add(row); row = new List<string>(); }
                else if (c != '\r') cell.Append(c);
            }
            if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); rows.Add(row); }
            return rows;
        }

        public static Report Run()
        {
            var r = new Report();
            var ledger = JsonUtility.FromJson<Ledger>(File.ReadAllText(Builder.Root + "Data/DialogueLedger.json")).cues;
            var db = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(Builder.Gen + "Data/DialogueDatabase.asset");
            var chapters = SceneCounts.Keys.Select(id => AssetDatabase.LoadAssetAtPath<ChapterDefinition>($"{Builder.Gen}Data/Chapters/{id}.asset")).ToList();
            var bank = AssetDatabase.LoadAssetAtPath<QuestionBank>(Builder.Gen + "Data/QuestionBank.asset");
            var knowledge = AssetDatabase.LoadAssetAtPath<NikoKnowledge>(Builder.Gen + "Data/NikoKnowledge.asset");

            // ---- ledger, exact text, filenames, CSV ----
            Check(r, ledger.Length == 66, $"Ledger has 66 cues (found {ledger.Length})");
            foreach (var kv in SceneCounts) Check(r, ledger.Count(c => c.scene == kv.Key) == kv.Value, $"{kv.Key} has {kv.Value} cues");
            Check(r, ledger.Count(c => c.type == "PromptVO") == 3, "Exactly 3 PROMPT VO cues");
            Check(r, ledger.Select(c => c.id).Distinct().Count() == 66, "Cue IDs unique");
            Check(r, ledger.Select(c => c.filename).Distinct().Count() == 66, "VO filenames unique");
            Check(r, ledger.Select((c, i) => c.sequence == i + 1).All(x => x), "Global sequence 1..66 in canonical order");
            foreach (var c in ledger) if (FileName(c.sequence, c.text) != c.filename) r.errors.Add($"Filename rule broken for {c.id}: {c.filename}");
            r.passed.Add("Filename rule (sequence + first four words) checked for all cues");
            var docx = "Docs/Storyboard/HUMAN EYE.docx";
            if (File.Exists(docx))
            {
                var paras = DocxParagraphs(docx);
                int mismatches = ledger.Count(c => c.docxParagraph >= paras.Count || Strip(paras[c.docxParagraph]) != c.text);
                Check(r, mismatches == 0, $"All 66 cue texts equal the storyboard paragraphs character-for-character ({mismatches} mismatches)");
            }
            else r.warnings.Add("Storyboard docx not found at " + docx + "; exact-text check skipped");
            Check(r, db && db.cues.Length == 66 && db.cues.Zip(ledger, (a, b) => a.id == b.id && a.text == b.text).All(x => x), "Dialogue database equals the ledger (ids, text, order)");
            var csv = ReadCsv("Docs/HumanEye/VO/HumanEye_VO.csv");
            Check(r, csv.Count == 67 && csv[0].Count == 2 && csv[0][0] == "filename" && csv[0][1] == "text", "VO CSV has exactly the columns filename,text and 66 rows");
            Check(r, csv.Skip(1).Select((row, i) => row.Count == 2 && row[0] == ledger[i].filename && row[1] == ledger[i].text).All(x => x), "VO CSV rows match the ledger in canonical order");
            var manifest = ReadCsv("Docs/HumanEye/VO/VO_MANIFEST.csv");
            Check(r, manifest.Skip(1).Select((row, i) => row[0] == ledger[i].id && row[5] == ledger[i].filename).All(x => x), "VO manifest maps every cue id to its filename");
            var missingAudio = db ? db.cues.Where(c => !c.clip).Select(c => c.id).ToList() : new List<string>();
            if (missingAudio.Count == 0) r.passed.Add("Every cue has an audio clip (provisional TTS — see MISSING.md)");
            else r.warnings.Add("Cues without audio (subtitle + Read & Continue fallback): " + string.Join(", ", missingAudio));

            // ---- chapters: coverage, order, whitelists ----
            Check(r, chapters.All(c => c), "All ten chapter definitions Scene02–Scene11 exist");
            var said = new List<(string cue, string chapter, string where)>();
            foreach (var ch in chapters.Where(c => c))
            {
                foreach (var s in ch.steps.Where(s => s.kind == StepKind.Say)) said.Add((s.key, ch.id, "main"));
                foreach (var e in ch.explore) foreach (var s in e.steps.Where(s => s.kind == StepKind.Say)) said.Add((s.key, ch.id, e.partId));
                var mainSeq = ch.steps.Where(s => s.kind == StepKind.Say).Select(s => ledger.First(l => l.id == s.key).sequence).ToList();
                Check(r, mainSeq.SequenceEqual(mainSeq.OrderBy(x => x)), $"{ch.id}: narration steps follow ledger order");
                foreach (var e in ch.explore)
                {
                    var es = e.steps.Where(s => s.kind == StepKind.Say).Select(s => ledger.First(l => l.id == s.key).sequence).ToList();
                    Check(r, es.SequenceEqual(es.OrderBy(x => x)), $"{ch.id}/{e.partId}: part explanation lines stay together in order");
                }
                foreach (var s in ch.steps.Concat(ch.explore.SelectMany(e => e.steps)))
                {
                    if (s.kind == StepKind.Await && !AwaitKeys.Contains(s.key)) r.errors.Add($"{ch.id}: unknown await '{s.key}'");
                    if (s.kind == StepKind.Free && !FreeModes.Contains(s.key)) r.errors.Add($"{ch.id}: unknown free mode '{s.key}'");
                    if (s.kind == StepKind.Offer && ResolveOffer(bank, s.key, ch).Count == 0) r.errors.Add($"{ch.id}: offer '{s.key}' resolves to no items");
                }
            }
            foreach (var c in ledger)
            {
                var uses = said.Where(x => x.cue == c.id).ToList();
                if (uses.Count != 1) r.errors.Add($"{c.id} is spoken {uses.Count} times in chapter steps (must be exactly once)");
                else if (uses[0].chapter != c.scene) r.errors.Add($"{c.id} belongs to {c.scene} but is used in {uses[0].chapter}");
            }
            r.passed.Add($"Coverage: {said.Select(x => x.cue).Distinct().Count()} of 66 cues placed exactly once in their own chapter");
            Check(r, said.All(x => ledger.Any(l => l.id == x.cue)), "No chapter references an unknown cue");

            // ---- questions and Ask Niko ----
            foreach (var t in LearnerModel.Topics) Check(r, bank.items.Count(i => i.topic == t) >= 2, $"Topic '{t}' has at least two distinct items");
            Check(r, bank.items.Select(i => i.id).Distinct().Count() == bank.items.Length, "Question ids unique");
            foreach (var i in bank.items)
            {
                if (!i.options.Any(o => o.correct)) r.errors.Add($"Question {i.id} has no correct option");
                if (i.hints == null || i.hints.Length < 3) r.errors.Add($"Question {i.id} needs three hint tiers");
                if (!i.tag.Contains("SupplementalAdaptive")) r.errors.Add($"Question {i.id} is not tagged SupplementalAdaptive");
                if (i.mode == QuestionMode.TapPart && i.options.Any(o => !PartIds.Contains(o.partId))) r.errors.Add($"Question {i.id} has an unknown part id");
            }
            foreach (var it in knowledge.intents)
            {
                try { _ = new Regex(it.pattern); } catch (Exception e) { r.errors.Add($"Intent {it.id} pattern invalid: {e.Message}"); }
                foreach (var c in it.cues ?? new string[0]) if (!ledger.Any(l => l.id == c)) r.errors.Add($"Intent {it.id} quotes unknown cue {c}");
            }
            r.passed.Add($"{knowledge.intents.Length} Ask Niko intents checked (patterns compile, quoted cues exist)");

            // ---- scene: shots, stage whitelist, references, build settings ----
            var scene = EditorSceneManager.OpenScene(Builder.ScenePath);
            var stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include);
            var cams = Object.FindFirstObjectByType<CameraDirector>(FindObjectsInactive.Include);
            stage.EnsureActions();
            var shotNames = cams.shots.Select(s => s.name).ToHashSet();
            foreach (var ch in chapters.Where(c => c))
                foreach (var s in ch.steps.Concat(ch.explore.SelectMany(e => e.steps)))
                {
                    if (s.kind == StepKind.Shot && !shotNames.Contains(s.key)) r.errors.Add($"{ch.id}: shot '{s.key}' not in scene");
                    if (s.kind == StepKind.Do && !stage.Has(s.key)) r.errors.Add($"{ch.id}: action '{s.key}' is not whitelisted");
                }
            foreach (var i in bank.items.Where(i => !string.IsNullOrEmpty(i.demo)))
                if (!stage.Has(i.demo.Split(':')[0])) r.errors.Add($"Question {i.id}: demo action '{i.demo}' is not whitelisted");
            r.passed.Add("All chapter shots exist and all chapter/question actions are whitelisted");
            foreach (var shot in new[] { "S02_EstablishA", "S02_EyeHero", "S03_InsideHero", "S05_ObjectWide", "S05_AqueousPupil", "S06_Vitreous", "S07_RetinaCloser", "S08_MicroWide", "S09_RetinaNerve", "S09_Wide", "S10_Complete" })
                Check(r, shotNames.Contains(shot), $"Checkpoint shot {shot} exists");
            var allowNull = new HashSet<string> { "irisPick", "lookTarget", "gestureTarget", "focusCut", "labelAnchorCut", "focus", "labelAnchor", "font", "lamp", "lampLight", "targetTop", "targetBottom", "space", "imageQuad" };
            int nulls = 0;
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(m => m.GetType().Namespace == "HumanEye"))
                foreach (var f in mb.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (allowNull.Contains(f.Name)) continue;
                    // Plain text state slots have no glass surface; buttons and progress dots do.
                    if (mb is HudState state && f.Name == nameof(HudState.surface) && state.target is UnityEngine.UI.Text && !state.GetComponent<GlassPanel>()) continue;
                    var v = f.GetValue(mb);
                    if (typeof(Object).IsAssignableFrom(f.FieldType) && !(Object)v) { r.errors.Add($"Missing reference {mb.GetType().Name}.{f.Name} on {mb.name}"); nulls++; }
                    if (f.FieldType.IsArray && typeof(Object).IsAssignableFrom(f.FieldType.GetElementType()) && v is Array arr)
                        foreach (var e in arr) if (!(Object)e) { r.errors.Add($"Null element in {mb.GetType().Name}.{f.Name} on {mb.name}"); nulls++; break; }
                }
            Check(r, nulls == 0, "No missing serialized references on module components");
            Check(r, EditorBuildSettings.scenes.Length > 0 && EditorBuildSettings.scenes[0].path == Builder.ScenePath && EditorBuildSettings.scenes[0].enabled, "Launch scene is first in Build Settings");
            Check(r, Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 1, "Exactly one AudioListener");
            Check(r, Object.FindObjectsByType<CinemachineBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 1, "One authoritative camera brain");

            WriteCueSheet(chapters, ledger, cams);
            var sb = new StringBuilder($"# Validation report\n\nGenerated {DateTime.Now:yyyy-MM-dd HH:mm} by Tools > Human Eye > Validate Module.\n\n**{r.passed.Count} passed · {r.warnings.Count} warnings · {r.errors.Count} errors**\n\n");
            if (r.errors.Count > 0) sb.Append("## Errors\n\n" + string.Join("\n", r.errors.Select(e => "- " + e)) + "\n\n");
            if (r.warnings.Count > 0) sb.Append("## Warnings\n\n" + string.Join("\n", r.warnings.Select(e => "- " + e)) + "\n\n");
            sb.Append("## Passed\n\n" + string.Join("\n", r.passed.Select(e => "- " + e)) + "\n");
            File.WriteAllText("Docs/HumanEye/VALIDATION_REPORT.md", sb.ToString());
            Debug.Log($"HUMAN_EYE_VALIDATION passed={r.passed.Count} warnings={r.warnings.Count} errors={r.errors.Count}\n" + string.Join("\n", r.errors));
            return r;
        }

        static List<QuestionBank.Item> ResolveOffer(QuestionBank bank, string key, ChapterDefinition ch)
        {
            if (key.StartsWith("item:")) return bank.items.Where(i => i.id == key.Substring(5)).ToList();
            var topic = key.StartsWith("topic:") ? key.Substring(6) : ch.topics.FirstOrDefault();
            return bank.items.Where(i => i.topic == topic).ToList();
        }

        // ---------------- cue sheet ----------------
        static readonly Dictionary<string, string> Subjects = new()
        {
            ["S02_EstablishA"] = "Dark room from the door end (intro start)", ["S02_IntroThreshold"] = "Dark room from the door end", ["S02_IntroIgnite"] = "Pushing toward the pedestal as the room powers up",
            ["S02_IntroCrane"] = "Crane above the pedestal: beam strike", ["S02_EstablishB"] = "Establishing view of the pedestal display",
            ["S02_NikoMedium"] = "Niko introduction", ["S02_DisplayWide"] = "Niko and the revealed human", ["S02_ScanFront"] = "Full figure for the scan wave",
            ["S02_EyeInSkull"] = "Selected eye inside the skull (anatomical position)", ["S02_PinchNiko"] = "Niko's pinch gesture", ["S02_EyeHero"] = "Enlarged complete eye",
            ["S02_Part_sclera"] = "Sclera surface", ["S02_Part_cornea"] = "Cornea in profile", ["S02_Part_iris"] = "Iris, front", ["S02_Part_pupil"] = "Pupil opening, front",
            ["S03_InsideHero"] = "Complete cutaway eye", ["S03_Part_aqueous"] = "Aqueous humour region", ["S03_Part_lens"] = "Lens in position", ["S03_Lens_Close"] = "Biconvex lens close-up",
            ["S03_Part_vitreous"] = "Vitreous cavity", ["S03_Vitreous_Back"] = "Through the vitreous toward the back", ["S03_Part_retina"] = "Retina (inner back layer)",
            ["S03_Retina_Close"] = "Retinal surface detail", ["S03_Part_choroid"] = "Retina section (connecting shot)", ["S03_Choroid_Deep"] = "Retina → choroid → sclera layers",
            ["S04_EyeRear"] = "Around to the back of the eye", ["S04_EyeRearClose"] = "Back of the eye, closer", ["S04_NerveClose"] = "Optic nerve leaving the eye",
            ["S04_NerveMid"] = "Following the nerve to the brain", ["S04_BrainClose"] = "Visual processing area (back of brain)", ["S04_Wide"] = "Eye, optic nerve and brain together",
            ["S05_ObjectWide"] = "Lit apple and eye", ["S05_FollowLight"] = "Following light to the eye", ["S05_Cornea"] = "Light bending at the cornea", ["S05_AqueousPupil"] = "Aqueous humour and pupil",
            ["S05_PupilFront"] = "Pupil response (front view)", ["S06_LensClose"] = "Rays through the lens", ["S06_NearWide"] = "Near object and eye", ["S06_FarView"] = "Distant object beyond the eye",
            ["S06_Vitreous"] = "Light continuing through the vitreous", ["S07_Vitreous"] = "Toward the back of the eye", ["S07_Retina"] = "Rays converging; inverted image", ["S07_RetinaCloser"] = "Retinal surface",
            ["S08_RetinaPush"] = "Into the retina", ["S08_MicroWide"] = "Magnified retina: photoreceptors", ["S08_Rods"] = "Rods", ["S08_Cones"] = "Cones", ["S08_Signals"] = "Signals through retinal cells",
            ["S08_PullBack"] = "Photoreceptors, retina and optic nerve together", ["S09_RetinaNerve"] = "Signals leaving through the optic nerve", ["S09_Perception"] = "Original object, clearly (perception)",
            ["S09_Wide"] = "Eye, nerve and brain (Scene 4 continuity)", ["S10_Complete"] = "Complete eye", ["S10_Journey"] = "Whole pathway: object → eye → nerve → brain",
            ["S11_Eye"] = "Complete eye", ["S11_NearFar"] = "Near apple and distant tree together (both sharp)",
            ["S10_Overview"] = "Overhead teaching layout: object, eye, route and brain together", ["S11_Closing"] = "Niko beside the eye for the closing line",
        };

        static string Framing(CinemachineCamera c) => c.Lens.FieldOfView >= 38 ? "wide" : c.Lens.FieldOfView >= 29 ? "medium" : c.Lens.FieldOfView >= 21 ? "close-up" : "extreme close-up";

        static string Pose(CinemachineCamera c) => c ? $"{c.transform.position.x:0.00},{c.transform.position.y:0.00},{c.transform.position.z:0.00} · yaw {c.transform.eulerAngles.y:0} pitch {c.transform.eulerAngles.x:0} · {c.Lens.FieldOfView:0}°" : "checkpoint";

        static string Input(StepKind k) => k switch
        {
            StepKind.Say => "Pause, Help, Settings, Ask Niko (held to the end of the line)",
            StepKind.Await => "The awaited control (click/tap); orbit/zoom; Pause; Ask Niko",
            StepKind.Explore => "Tap a highlighted part, its label or the Parts list; Ask Niko; orbit/zoom",
            StepKind.Offer => "Answer, hint, Show me (after two misses), Continue anyway (after a miss)",
            StepKind.Free => "Experiment controls, Freeze/Replay, view controls, Continue",
            _ => "Pause, Help, Settings",
        };

        static string Exit(Step s) => s.kind switch
        {
            StepKind.Say => "Clip finished (or explicit Read & Continue when audio is missing)",
            StepKind.Shot => s.async ? "Starts; continues during next steps" : $"Blend complete ({s.value:0.#} s; short fade when reduced motion)",
            StepKind.Do => s.async ? "Starts; runs alongside" : "Action complete",
            StepKind.Await => $"Learner action '{s.key}'",
            StepKind.Explore => "Every listed part's explanation finished",
            StepKind.Hold => $"{s.value:0.#} s inspection hold",
            StepKind.Offer => "Check answered (help after misses; answer shown after three)",
            StepKind.Free => "Learner taps Continue",
            _ => "",
        };

        static void WriteCueSheet(List<ChapterDefinition> chapters, LedgerCue[] ledger, CameraDirector cams)
        {
            var shots = cams.shots.ToDictionary(s => s.name);
            var sb = new StringBuilder("# Chapter / camera cue sheet\n\nGenerated from the chapter definitions and the scene's CinemachineCamera shots (Tools > Human Eye > Validate Module). Dialogue text is quoted exactly from the ledger.\n\n");
            sb.Append("Scene-to-chapter mapping: one launch scene `Assets/HumanEye/Scenes/HumanEye_Observatory.unity`; chapters `Scene02`–`Scene11` are `ChapterDefinition` assets in `Assets/HumanEye/Generated/Data/Chapters/`, played in order by `ChapterRunner`.\n\n");
            foreach (var ch in chapters.Where(c => c))
            {
                sb.Append($"## {ch.id} — {ch.title}\n\nCheckpoint (restart/resume state): {ch.checkpoint}\n\n| # | Step | Subject · framing | Start → end camera pose | Transition | Dialogue | Animation / visual cue | Permitted input | Exit condition |\n|---|---|---|---|---|---|---|---|---|\n");
                string current = "checkpoint"; int n = 1;
                void Row(Step s, string prefix)
                {
                    string subject = "", pose = "", transition = "", dialogue = "", anim = "";
                    if (s.kind == StepKind.Shot && shots.TryGetValue(s.key, out var cam))
                    {
                        subject = (Subjects.TryGetValue(s.key, out var sub) ? sub : s.key) + " · " + Framing(cam);
                        pose = $"{current} → {s.key} ({Pose(cam)})"; transition = $"Ease-in-out blend {s.value:0.#} s (reduced motion: fade cut)"; current = s.key;
                    }
                    if (s.kind == StepKind.Say) { var l = ledger.First(x => x.id == s.key); dialogue = $"{l.id}{(l.type == "PromptVO" ? " [PROMPT VO]" : "")}: “{l.text}”"; }
                    if (s.kind == StepKind.Do) anim = s.key + (string.IsNullOrEmpty(s.arg) ? "" : $"({s.arg})");
                    if (s.kind is StepKind.Offer or StepKind.Free or StepKind.Await) anim = s.key;
                    sb.Append($"| {prefix}{n++} | {s.kind} | {subject} | {pose} | {transition} | {dialogue.Replace("|", "/")} | {anim} | {Input(s.kind)} | {Exit(s)} |\n");
                }
                foreach (var s in ch.steps)
                {
                    Row(s, "");
                    if (s.kind == StepKind.Explore)
                        foreach (var e in ch.explore)
                        {
                            sb.Append($"| | **Part: {e.label}** | {(shots.ContainsKey((ch.id == "Scene02" ? "S02_Part_" : "S03_Part_") + e.partId) ? "close-up (focus shot), others translucent" : "")} | | | | isolate + highlight | | Explanation complete → return to complete eye |\n");
                            foreach (var ps in e.steps) Row(ps, "  ");
                        }
                }
                sb.Append('\n');
            }
            File.WriteAllText("Docs/HumanEye/CUE_SHEET.md", sb.ToString());
        }
    }
}
