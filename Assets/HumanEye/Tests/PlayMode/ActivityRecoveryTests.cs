using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace HumanEye.Tests
{
    public class ActivityRecoveryTests
    {
        string backup;
        bool hadSave;
        ChapterDefinition fixture, original;
        ChapterRunner runner;

        [SetUp]
        public void Backup()
        {
            hadSave = PlayerPrefs.HasKey(SaveSystem.Key);
            backup = PlayerPrefs.GetString(SaveSystem.Key, "");
            Clock.Paused = Clock.Frozen = false;
            Clock.FastForward = 6;
        }

        [TearDown]
        public void Restore()
        {
            if (runner)
            {
                if (original) runner.chapters[1] = original;
                runner.ReturnToMenu();
            }
            if (fixture) Object.Destroy(fixture);
            if (hadSave) PlayerPrefs.SetString(SaveSystem.Key, backup);
            else PlayerPrefs.DeleteKey(SaveSystem.Key);
            SaveSystem.Load();
            Clock.Paused = Clock.Frozen = false;
            Clock.FastForward = 1;
        }

        static IEnumerator Until(Func<bool> condition, string message, float seconds = 15)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
            Assert.IsTrue(condition(), message);
        }

        static void Burst(ActivityController activities, string answer)
        {
            for (int i = 0; i < 8; i++) activities.Click(answer);
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator RepeatedMissesDemonstrationAndRapidClicksRecover()
        {
            SceneManager.LoadScene("HumanEye_Observatory");
            yield return null; yield return null;
            Clock.FastForward = 6;
            runner = Object.FindFirstObjectByType<ChapterRunner>();
            var activities = runner.activities;
            var narrator = runner.narrator;
            var hud = runner.stage.hud;
            var item = activities.bank.Get("ext_iris_role");
            Assert.IsNotNull(item);
            Assert.AreEqual("replayCue:S02_L13", item.demo, "Exercise the authored Show me explanation");
            Assert.IsNotNull(narrator.database.Get("S02_L13").clip, "Show me must have its narration clip");
            var wrong = item.options.First(o => !o.correct);
            var right = item.options.First(o => o.correct);

            // Keep the real scene, question and demonstration; isolate this branch at a stable eye checkpoint.
            original = runner.chapters[1];
            fixture = Object.Instantiate(original);
            fixture.steps = new[] { new Step { kind = StepKind.Offer, key = "item:" + item.id }, new Step { kind = StepKind.Hold, value = 600 } };
            runner.chapters[1] = fixture;
            SaveSystem.ResetProgress(true);
            SaveSystem.Data.settings.waitForContinue = false;

            for (int recovery = 0; recovery < 3; recovery++)
            {
                int before = SaveSystem.Data.evidence.Count;
                hud.StartModule(1);
                yield return Until(() => activities.Item == item && activities.Card?.options.Count > 0, "Question must become interactive");
                Assert.IsFalse(activities.Card.buttons.Contains("Continue anyway"), "Recovery requires a real attempt");

                Burst(activities, wrong.text);
                yield return Until(() => SaveSystem.Data.evidence.Count == before + 1, "First wrong answer must be recorded once");
                Assert.AreEqual(item.hints[0], activities.Card.hint);
                Assert.Contains("Continue anyway", activities.Card.buttons);
                Assert.IsFalse(activities.Card.buttons.Contains("Show me"));
                Assert.IsFalse(SaveSystem.Data.evidence.Last().correct);

                if (recovery == 2)
                {
                    Burst(activities, "Continue anyway");
                }
                else
                {
                    for (int tap = 0; tap < 8; tap++) Assert.IsTrue(activities.PartTapped(wrong.partId));
                    yield return Until(() => SaveSystem.Data.evidence.Count == before + 2, "Rapid taps must produce one additional attempt");
                    Assert.AreEqual(item.hints[1], activities.Card.hint);
                    Assert.Contains("Show me", activities.Card.buttons);
                    var cardBeforeDemo = activities.Card;
                    Burst(activities, "Show me");
                    yield return Until(() => narrator.Current?.id == "S02_L13", "Show me must replay the iris explanation");
                    Assert.AreEqual(NarrationSource.Replay, narrator.Source);
                    Burst(activities, wrong.text);
                    yield return Until(() => activities.Card != cardBeforeDemo, "Demonstration must return to the question");
                    Assert.AreEqual(before + 2, SaveSystem.Data.evidence.Count, "Clicks during the demonstration must not become stale answers");
                    Assert.AreEqual(item.hints[2], activities.Card.hint);

                    Burst(activities, recovery == 0 ? wrong.text : right.text);
                    yield return Until(() => SaveSystem.Data.evidence.Count == before + 3, "Retry must be recorded exactly once");
                    if (recovery == 0)
                    {
                        Assert.Contains("Continue", activities.Card.buttons);
                        StringAssert.Contains("Answer: " + right.text, activities.Card.hint);
                        Assert.IsFalse(activities.Card.tapParts);
                        Burst(activities, "Continue");
                    }
                    else
                    {
                        Assert.IsTrue(SaveSystem.Data.evidence.Last().correct);
                        Assert.IsTrue(SaveSystem.Data.evidence.Last().assisted, "An answer after hints must not count as unaided mastery");
                        Assert.AreEqual(right.text, activities.Card.answer, "The right tile is marked");
                        Assert.IsEmpty(activities.Card.buttons, "A right answer moves on by itself: no Next button");
                    }
                }

                yield return Until(() => activities.Card == null && runner.State == LessonState.Cinematic, "Recovery must finish the activity and resume the chapter");
                Assert.IsNull(activities.Item);
                Assert.Contains(item.topic, SaveSystem.Data.needsPractice);
                Assert.AreEqual(0, LearnerModel.UnassistedCorrect(SaveSystem.Data, item.topic));
                Assert.IsTrue(item.options.All(o => !runner.stage.Part(o.partId).Selectable), "Question hotspots must be released");
                Assert.AreEqual(before + (recovery == 2 ? 1 : 3), SaveSystem.Data.evidence.Count, "Rapid clicks must not duplicate saved evidence");
            }
        }
    }
}
