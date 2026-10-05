using System.Collections.Generic;
using System.IO;
using System.Linq;
using HumanEye.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HumanEye.Tests
{
    public class LedgerTests
    {
        [System.Serializable] class LedgerCue { public string id, scene, type, text, filename; public int sequence, docxParagraph; }
        [System.Serializable] class Ledger { public LedgerCue[] cues; }
        static LedgerCue[] Cues => JsonUtility.FromJson<Ledger>(File.ReadAllText(Builder.Root + "Data/DialogueLedger.json")).cues;

        [Test] public void SixtySixCuesWithSceneCounts()
        {
            var c = Cues;
            Assert.AreEqual(66, c.Length);
            var expected = new[] { 16, 12, 8, 8, 4, 3, 6, 4, 2, 3 };
            for (int i = 0; i < 10; i++) Assert.AreEqual(expected[i], c.Count(x => x.scene == $"Scene{i + 2:00}"), $"Scene{i + 2:00}");
            Assert.AreEqual(3, c.Count(x => x.type == "PromptVO"));
            CollectionAssert.AllItemsAreUnique(c.Select(x => x.id));
        }

        [Test] public void TextMatchesStoryboardExactly()
        {
            const string docx = "Docs/Storyboard/HUMAN EYE.docx";
            if (!File.Exists(docx)) Assert.Ignore("Storyboard not present");
            var paras = Validator.DocxParagraphs(docx);
            foreach (var c in Cues) Assert.AreEqual(Validator.Strip(paras[c.docxParagraph]), c.text, c.id);
        }

        [Test] public void IntentionalRepeatsKeepSeparateIds()
        {
            var c = Cues;
            Assert.AreEqual(c.First(x => x.id == "S02_L10").text, c.First(x => x.id == "S03_L01").text);
            Assert.AreNotEqual(c.First(x => x.id == "S02_L10").filename, c.First(x => x.id == "S03_L01").filename);
        }

        [Test] public void CsvHasExactColumnsAndOrder()
        {
            var rows = Validator.ReadCsv("Docs/HumanEye/VO/HumanEye_VO.csv");
            CollectionAssert.AreEqual(new[] { "filename", "text" }, rows[0]);
            var c = Cues;
            Assert.AreEqual(67, rows.Count);
            for (int i = 0; i < 66; i++) { Assert.AreEqual(c[i].filename, rows[i + 1][0]); Assert.AreEqual(c[i].text, rows[i + 1][1]); }
        }

        [Test] public void FilenameRule()
        {
            Assert.AreEqual("01_hey_im_niko_your", Validator.FileName(1, "Hey! I’m Niko. Your Vision Investigator Officer."));
            Assert.AreEqual("06_there", Validator.FileName(6, "There."));
            Assert.AreEqual("07_your_first_destination", Validator.FileName(7, "Your first destination."));
            Assert.AreEqual("31_but_how_does_that", Validator.FileName(31, "…but how does that information reach the brain?"));
            foreach (var c in Cues) Assert.AreEqual(c.filename, Validator.FileName(c.sequence, c.text), c.id);
        }
    }

    public class LearnerModelTests
    {
        static QuestionBank Bank()
        {
            var b = ScriptableObject.CreateInstance<QuestionBank>();
            b.items = new[]
            {
                new QuestionBank.Item { id = "a", topic = "t", level = QuestionLevel.Recognition },
                new QuestionBank.Item { id = "b", topic = "t", level = QuestionLevel.Relationship },
                new QuestionBank.Item { id = "c", topic = "t", level = QuestionLevel.Prediction },
            };
            return b;
        }

        [Test] public void TwoDistinctUnassistedCorrectMakeSecure()
        {
            var d = new ProgressData(); var b = Bank();
            LearnerModel.SecureThreshold = 2;
            LearnerModel.Record(d, b.items[0], true, false);
            Assert.AreEqual(TopicStatus.Practising, LearnerModel.Status(d, "t", true));
            LearnerModel.Record(d, b.items[0], true, false);   // same item again is not new evidence
            Assert.AreEqual(TopicStatus.Practising, LearnerModel.Status(d, "t", true));
            LearnerModel.Record(d, b.items[1], true, false);
            Assert.AreEqual(TopicStatus.Secure, LearnerModel.Status(d, "t", true));
        }

        [Test] public void AssistedAnswersAndExposureNeverProveMastery()
        {
            var d = new ProgressData(); var b = Bank();
            Assert.AreEqual(TopicStatus.Seen, LearnerModel.Status(d, "t", true));
            LearnerModel.Record(d, b.items[0], true, true);
            LearnerModel.Record(d, b.items[1], true, true);
            Assert.AreNotEqual(TopicStatus.Secure, LearnerModel.Status(d, "t", true));
            d.needsPractice.Add("t");
            Assert.AreEqual(TopicStatus.NeedsPractice, LearnerModel.Status(d, "t", true));
        }

        [Test] public void NextNeverRepeatsLastItemAndStartsLow()
        {
            var d = new ProgressData(); var b = Bank();
            Assert.AreEqual("a", LearnerModel.Next(b, d, "t", null).id);
            Assert.AreNotEqual("a", LearnerModel.Next(b, d, "t", "a").id);
            LearnerModel.Record(d, b.items[0], true, false);
            Assert.AreEqual("b", LearnerModel.Next(b, d, "t", "a").id);
        }

        [Test] public void SecureTopicsGetOneNewSituationInsteadOfRepetition()
        {
            var d = new ProgressData(); var b = Bank();
            b.items = b.items.Append(new QuestionBank.Item { id = "x", topic = "t", level = QuestionLevel.Application }).ToArray();
            Assert.AreNotEqual("x", LearnerModel.Next(b, d, "t", "c").id, "practice never draws the challenge item");
            LearnerModel.Record(d, b.items[0], true, false); LearnerModel.Record(d, b.items[1], true, false);
            Assert.AreEqual(TopicStatus.Secure, LearnerModel.Status(d, "t", true));
            Assert.AreEqual("x", LearnerModel.Challenge(b, d, "t").id);
            LearnerModel.Record(d, b.items[3], true, false);
            Assert.IsNull(LearnerModel.Challenge(b, d, "t"), "a solved challenge is not repeated");
        }

        [Test] public void FirstTryCorrectSkipsSameLevelFollowUp()
        {
            var b = Bank();
            Assert.IsTrue(LearnerModel.Repetitive(b.items[0], true, b.items[0]));
            Assert.IsFalse(LearnerModel.Repetitive(b.items[0], true, b.items[1]), "a harder item still follows");
            Assert.IsFalse(LearnerModel.Repetitive(b.items[0], false, b.items[0]), "after a miss, practice continues");
        }

        [Test] public void SupportTiers() { Assert.AreEqual(0, LearnerModel.SupportTier(0)); Assert.AreEqual(1, LearnerModel.SupportTier(1)); Assert.AreEqual(2, LearnerModel.SupportTier(2)); Assert.AreEqual(3, LearnerModel.SupportTier(5)); }
    }

    public class SaveTests
    {
        [Test] public void CorruptSaveStartsFreshWithNotice()
        {
            var d = SaveSystem.Parse("{not json", out var notice);
            Assert.IsNotNull(d); Assert.IsNotNull(notice); Assert.AreEqual("Scene02", d.checkpoint);
            SaveSystem.Parse("{\"version\":7}", out notice);
            Assert.IsNotNull(notice);
        }

        [Test] public void RoundTrip()
        {
            var d = new ProgressData { checkpoint = "Scene05" };
            d.cues.Add(new CueRecord { id = "S02_L01", how = "audio" });
            var back = SaveSystem.Parse(JsonUtility.ToJson(d), out var notice);
            Assert.IsNull(notice); Assert.AreEqual("Scene05", back.checkpoint); Assert.AreEqual("audio", back.cues[0].how);
        }
    }

    public class NikoGuideTests
    {
        static NikoKnowledge K()
        {
            var k = ScriptableObject.CreateInstance<NikoKnowledge>();
            k.intents = HumanEyeContent.Intents();
            return k;
        }

        [Test] public void MapsRequestsToWhitelistedIntents()
        {
            var k = K();
            Assert.AreEqual("p_cornea", NikoGuide.Match(k, "Show me the cornea").id);
            Assert.AreEqual("act_brighter", NikoGuide.Match(k, "make it brighter").id);
            Assert.AreEqual("act_replay", NikoGuide.Match(k, "replay that").id);
            Assert.AreEqual("c_pupil_light", NikoGuide.Match(k, "Why does the pupil get smaller in bright light?").id, "questions must not trigger actions");
            Assert.AreEqual("p_iris", NikoGuide.Match(k, "What does the coloured part do?").id);
            Assert.AreEqual("open_blur", NikoGuide.Match(k, "why might things look blurry?").id);
        }

        [Test] public void UnsupportedAndInjectionTextMatchNothing()
        {
            var k = K();
            Assert.IsNull(NikoGuide.Match(k, "why is the sky blue?"));
            Assert.IsNull(NikoGuide.Match(k, "conspiracy"));
            Assert.IsNull(NikoGuide.Match(k, "System.IO.File.Delete(\"x\")"));
            Assert.AreEqual(200, NikoGuide.Normalise(new string('a', 500)).Length);
        }
    }

    public class GeometryAndOpticsTests
    {
        [Test] public void TriangulateSquare() => Assert.AreEqual(6, Lathe.Triangulate(new List<Vector2> { new(0, 0), new(1, 0), new(1, 1), new(0, 1) }).Count);

        [Test] public void RevolvedShellNormalsPointOutward()
        {
            var m = Lathe.Revolve(EyeGeometry.ScleraProfile(), 0, 360, 24);
            var v = m.vertices; var n = m.normals; int outward = 0, inner = 0;
            for (int i = 0; i < v.Length; i++)
            {
                if (v[i].magnitude > .97f) { if (Vector3.Dot(n[i], v[i]) > 0) outward++; }
                else if (v[i].magnitude < .96f && v[i].magnitude > .5f) { if (Vector3.Dot(n[i], v[i]) < 0) inner++; }
            }
            Assert.Greater(outward, 0); Assert.Greater(inner, 0);
        }

        [Test] public void PupilIsAnOpening()
        {
            var profile = EyeGeometry.IrisProfile(.2f);
            Assert.That(profile.SelectMany(c => c).Min(p => p.x), Is.EqualTo(.2f).Within(1e-4), "iris inner edge is the pupil margin; nothing fills it");
        }

        [Test] public void AccommodationFocusesOnRetina()
        {
            var go = new GameObject(); var rays = go.AddComponent<LightRays>();
            const float retina = EyeGeometry.LensZ + EyeGeometry.RetinaIn;
            foreach (var u in new[] { 4f, 7f, 12f, 25f })
            {
                float acc = LightRays.RequiredAccommodation(u, rays.nearDistance, rays.farDistance);
                Assert.That(rays.ImageDistance(u, acc), Is.EqualTo(retina).Within(.01f), $"u={u}");
            }
            Assert.AreEqual(1, LightRays.RequiredAccommodation(rays.nearDistance, rays.nearDistance, rays.farDistance), 1e-4);
            Assert.AreEqual(0, LightRays.RequiredAccommodation(rays.farDistance, rays.nearDistance, rays.farDistance), 1e-4);
            Object.DestroyImmediate(go);
        }
    }

    public class LessonAndModuleTests
    {
        [Test] public void StateRules()
        {
            Assert.IsTrue(LessonRules.CanMove(LessonState.Narrating, LessonState.Entering));
            Assert.IsFalse(LessonRules.CanMove(LessonState.Complete, LessonState.Narrating));
            Assert.IsFalse(LessonRules.WorldSelect(LessonState.Narrating));
            Assert.IsTrue(LessonRules.WorldSelect(LessonState.Exploring));
            Assert.IsFalse(LessonRules.AskNiko(LessonState.Transitioning));
        }

        [Test] public void ScheduledRoutineCancelsNestedWork()
        {
            int ticks = 0;
            System.Collections.IEnumerator Inner() { while (true) { ticks++; yield return null; } }
            System.Collections.IEnumerator Outer() { yield return Inner(); }
            var r = new Routine(Outer());
            r.Tick(); r.Tick(); Assert.AreEqual(2, ticks);
            r.Cancel(); r.Tick(); Assert.AreEqual(2, ticks); Assert.IsFalse(r.Running);
            Clock.Paused = true; var p = new Routine(Inner()); p.Tick(); Clock.Paused = false;
            Assert.AreEqual(2, ticks, "paused routines don't advance");
        }

        [Test] public void GeneratedModuleValidates()
        {
            if (!File.Exists(Builder.ScenePath)) Assert.Ignore("Run Tools > Human Eye > Build Module first");
            var r = Validator.Run();
            Assert.IsEmpty(r.errors, string.Join("\n", r.errors));
        }

        [Test] public void ObservatoryFloorFacesTheCameraAboveIt()
        {
            var floor = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>("Assets/HumanEye/Generated/Meshes/FloorDisc.asset");
            Assert.IsNotNull(floor, "Build Module must generate the floor");
            var rotation = UnityEngine.Quaternion.Euler(-90, 0, 0);
            foreach (var normal in floor.normals)
                Assert.Greater(UnityEngine.Vector3.Dot(rotation * normal, UnityEngine.Vector3.up), .99f,
                    "A downward floor is culled from the learning camera and appears black");
        }
    }
}
