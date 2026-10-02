using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HumanEye
{
    /// Module clocks. Pause stops the app clock (narration, cinematics, routines); FREEZE only stops the
    /// visualization clock (pupil, lens, rays, pulses). Time.timeScale is never changed, so menus keep working.
    public static class Clock
    {
        public static bool Paused, Frozen;
        /// Test/autoplay hook only (narration and waits run faster); 1 for learners.
        public static float FastForward = 1;
        public static float Delta => Paused ? 0 : Time.unscaledDeltaTime * FastForward;
        public static float VizDelta => Paused || Frozen ? 0 : Time.unscaledDeltaTime * FastForward;
    }

    public sealed class Wait
    {
        public readonly float Seconds; public readonly bool Viz;
        public Wait(float seconds, bool viz = false) { Seconds = seconds; Viz = viz; }
    }

    public sealed class Until
    {
        public readonly Func<bool> Condition;
        public Until(Func<bool> condition) { Condition = condition; }
    }

    /// Owned coroutine scheduler. Nested IEnumerators run on one stack, so Cancel() stops every
    /// tween/wait a chapter started, and a paused routine resumes exactly where it stopped.
    public sealed class Routine
    {
        readonly Stack<IEnumerator> stack = new();
        float waitLeft; bool waitViz; Func<bool> until;
        public bool Running => stack.Count > 0;

        public Routine(IEnumerator root) { if (root != null) stack.Push(root); }

        public void Cancel() { stack.Clear(); until = null; waitLeft = 0; }

        public void Tick()
        {
            if (stack.Count == 0 || Clock.Paused) return;
            if (waitLeft > 0) { waitLeft -= waitViz ? Clock.VizDelta : Clock.Delta; if (waitLeft > 0) return; }
            if (until != null) { if (!until()) return; until = null; }
            for (int guard = 0; stack.Count > 0 && guard < 256; guard++)
            {
                var top = stack.Peek();
                bool moved;
                try { moved = top.MoveNext(); }
                catch (Exception e) { Debug.LogException(e); Cancel(); return; }
                if (stack.Count == 0 || stack.Peek() != top) return;   // cancelled from inside its own step
                if (!moved) { stack.Pop(); continue; }
                switch (top.Current)
                {
                    case IEnumerator nested: stack.Push(nested); continue;
                    case Wait w: waitLeft = w.Seconds; waitViz = w.Viz; if (waitLeft > 0) return; continue;
                    case Until u: if (u.Condition()) continue; until = u.Condition; return;
                    default: return;
                }
            }
        }

        /// Eased 0..1 driver for tweens (uses the viz clock when viz=true so FREEZE stops it mid-action).
        public static IEnumerator Tween(float seconds, Action<float> step, bool viz = false)
        {
            if (seconds <= 0) { step(1); yield break; }
            for (float t = 0; t < 1;)
            {
                t = Mathf.Min(1, t + (viz ? Clock.VizDelta : Clock.Delta) / seconds);
                step(Mathf.SmoothStep(0, 1, t));
                if (t < 1) yield return null;
            }
        }

        public static IEnumerator All(params IEnumerator[] parts)
        {
            var routines = Array.ConvertAll(parts, p => new Routine(p));
            while (Array.Exists(routines, r => r.Running)) { foreach (var r in routines) r.Tick(); yield return null; }
        }
    }
}
