using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HumanEye
{
    /// Interprets ChapterDefinition steps in storyboard order. Owns every routine a chapter starts, so leaving
    /// or restarting a chapter cancels tweens, waits, narration and side demonstrations together.
    public class ChapterRunner : MonoBehaviour
    {
        public ChapterDefinition[] chapters;
        public Narrator narrator;
        public CameraDirector cameras;
        public Stage stage;
        public AudioDirector audioDirector;
        public ActivityController activities;

        public LessonState State { get; private set; } = LessonState.Entering;
        public int Index { get; private set; } = -1;
        public ChapterDefinition Chapter => Index >= 0 && Index < chapters.Length ? chapters[Index] : null;
        public bool Started { get; private set; }
        public string Awaiting { get; private set; }
        public string FreeMode { get; private set; }
        public ExploreEntry Focused { get; private set; }
        public ExploreEntry[] ExploreSet { get; private set; }
        public readonly HashSet<string> ExploreDone = new();
        /// Set while Ask Niko is open: the chapter halts at the next safe boundary (after the current line).
        public bool AskHold { get; set; }
        public event Action Changed;

        Routine main;
        readonly List<Routine> side = new();
        ExploreEntry pendingPart;
        string pressed;
        bool continuePressed;
        int pendingChapter = -1;
        float vizTime;

        public void Begin(int chapterIndex) { Started = true; pendingChapter = Mathf.Clamp(chapterIndex, 0, chapters.Length - 1); }
        public void RestartChapter() { if (Index >= 0) pendingChapter = Index; }

        void StartChapter(int i)
        {
            Cancel();
            Index = i;
            SaveSystem.Data.checkpoint = chapters[i].id; SaveSystem.Save();
            main = new Routine(Run(chapters[i]));
            Changed?.Invoke();
        }

        void Cancel()
        {
            main?.Cancel();
            foreach (var r in side) r.Cancel();
            side.Clear();
            narrator.Stop();
            activities.Cancel();
            Awaiting = FreeMode = pressed = null; Focused = pendingPart = null; ExploreSet = null;
            ExploreDone.Clear(); continuePressed = false;
            Clock.Frozen = false;
        }

        IEnumerator Run(ChapterDefinition c)
        {
            SetState(LessonState.Entering);
            stage.Enter(c);
            yield return null;
            foreach (var s in c.steps) { yield return Boundary(); yield return Execute(s, c); }
            SetState(LessonState.Transitioning);
            SaveSystem.AddOnce(SaveSystem.Data.completedChapters, c.id);
            if (Index + 1 < chapters.Length) pendingChapter = Index + 1;
            else SetState(LessonState.Complete);
        }

        IEnumerator Boundary() { while (AskHold) yield return null; }

        IEnumerator Execute(Step s, ChapterDefinition c)
        {
            switch (s.kind)
            {
                case StepKind.Shot:
                    var shot = cameras.MoveTo(s.key, s.value);
                    if (s.async) side.Add(new Routine(shot));
                    else { SetState(LessonState.Cinematic); yield return shot; }
                    break;
                case StepKind.Say:
                    SetState(LessonState.Narrating);
                    stage.OnCue(s.key);
                    yield return narrator.Play(s.key, NarrationSource.Core);
                    break;
                case StepKind.Do:
                    var act = stage.Run(s.key, s.arg, s.value);
                    if (act == null) break;
                    if (s.async) side.Add(new Routine(act));
                    else { if (State != LessonState.Narrating) SetState(LessonState.Cinematic); yield return act; }
                    break;
                case StepKind.Await:
                    Awaiting = s.key; pressed = null;
                    SetState(LessonState.AwaitingAction);
                    stage.OnAwait(s.key, true);
                    yield return new Until(() => pressed == s.key);
                    Awaiting = pressed = null;
                    stage.OnAwait(s.key, false);
                    Changed?.Invoke();
                    break;
                case StepKind.Explore:
                    yield return Explore(c);
                    break;
                case StepKind.Hold:
                    SetState(LessonState.Cinematic);
                    yield return new Wait(s.value);
                    break;
                case StepKind.Offer:
                    SetState(LessonState.SupplementalCheck);
                    yield return activities.Offer(s.key, c);
                    break;
                case StepKind.Free:
                    FreeMode = s.key; continuePressed = false;
                    SetState(LessonState.Exploring);
                    stage.OnFree(s.key, true);
                    yield return new Until(() => continuePressed);
                    stage.OnFree(s.key, false);
                    FreeMode = null; continuePressed = false;
                    break;
            }
        }

        IEnumerator Explore(ChapterDefinition c)
        {
            ExploreSet = c.explore; ExploreDone.Clear();
            stage.OnExplore(true);
            while (ExploreDone.Count < ExploreSet.Length)
            {
                pendingPart = null; Focused = null;
                SetState(LessonState.Exploring);
                yield return new Until(() => pendingPart != null);
                var e = pendingPart; pendingPart = null; Focused = e;
                Changed?.Invoke();
                yield return stage.FocusPart(e.partId);
                foreach (var s in e.steps) { yield return Boundary(); yield return Execute(s, c); }
                // Completion is recorded only after the whole explanation finished (audio or explicit Read & Continue).
                if (ExploreDone.Add(e.partId)) stage.Discovered(c.id, e);
                yield return stage.ReturnFromPart(e.partId);
                Focused = null;
            }
            stage.OnExplore(false);
            ExploreSet = null;
        }

        /// Click/tap, part list, label hotspot or Ask Niko all route here; returns a reason when not possible.
        public bool RequestPart(string partId, out string reason)
        {
            reason = null;
            var e = ExploreSet == null ? null : Array.Find(ExploreSet, x => x.partId == partId);
            if (e == null) { reason = ExploreSet == null ? "Parts can be explored once Niko invites you to." : "That structure isn't part of this exploration."; return false; }
            if (State != LessonState.Exploring || Focused != null || pendingPart != null) { reason = "Niko is still explaining — try again in a moment."; return false; }
            pendingPart = e;
            return true;
        }

        public bool Press(string key)
        {
            if (Awaiting != key || pressed != null) return false;
            pressed = key; return true;
        }

        public void Continue() { if (FreeMode != null) continuePressed = true; }

        public Routine RunSide(IEnumerator e) { var r = new Routine(e); side.Add(r); return r; }

        public void SetPaused(bool paused)
        {
            if (Clock.Paused == paused) return;
            Clock.Paused = paused;
            narrator.SetPaused(paused); cameras.SetPaused(paused); audioDirector.SetPaused(paused);
            Changed?.Invoke();
        }

        public void SetFrozen(bool frozen) { Clock.Frozen = frozen; Changed?.Invoke(); }

        void SetState(LessonState s)
        {
            if (!LessonRules.CanMove(State, s)) Debug.LogWarning($"Unexpected lesson transition {State} -> {s}");
            if (State == s) return;
            State = s; Changed?.Invoke();
        }

        void Update()
        {
            vizTime += Clock.VizDelta;
            Shader.SetGlobalFloat("_HE_VizTime", vizTime);
            if (pendingChapter >= 0) { var n = pendingChapter; pendingChapter = -1; StartChapter(n); }
            main?.Tick();
            for (int i = side.Count - 1; i >= 0; i--) { if (i >= side.Count) continue; side[i].Tick(); if (i < side.Count && !side[i].Running) side.RemoveAt(i); }
        }

        void OnDestroy() { Clock.Paused = Clock.Frozen = false; Clock.FastForward = 1; }
    }
}
