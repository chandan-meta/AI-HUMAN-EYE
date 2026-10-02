using System.Collections;
using UnityEngine;

namespace HumanEye
{
    public partial class Stage
    {
        void EnterScene02()
        {
            human.ResetVisual(true, .06f);
            displayLight.intensity = displayIntensity * .15f;
            Socket(human.eyeRight, eyeRig);
            otherEye.gameObject.SetActive(true);
            Socket(human.eyeLeft, otherEye.transform);
            eye.SetCutInstant(false); otherEye.SetCutInstant(false);
            eye.SetNerveReveal(.12f); otherEye.SetNerveReveal(.12f);
            niko.Place(Anchor("Intro"));
            niko.gameObject.SetActive(false);
            cams.Snap("S02_EstablishA");
        }

        static void Socket(Transform socket, Transform t)
        {
            t.SetPositionAndRotation(socket.position, socket.rotation);
            t.localScale = socket.lossyScale;
        }

        void RegisterScene02()
        {
            actions["nikoAppear"] = (_, seconds) => niko.Appear(seconds > 0 ? seconds : 1.2f);
            actions["humanReveal"] = (_, seconds) => HumanReveal(seconds > 0 ? seconds : 1.6f);
            actions["scan"] = (_, __) => ScanSequence();
            actions["illuminate"] = (_, __) => { foreach (var id in new[] { "sclera", "iris", "cornea" }) eye.Get(id).SetGlow(1); sfx.Play("ping"); return null; };
            actions["pinch"] = (_, __) => Pinch();
            actions["enlarge"] = (_, seconds) => Enlarge(seconds > 0 ? seconds : 2.4f);
        }

        IEnumerator HumanReveal(float seconds)
        {
            sfx.Play("whoosh", .5f);
            yield return Routine.All(human.Reveal(seconds), Routine.Tween(seconds, t => displayLight.intensity = Mathf.Lerp(displayIntensity * .15f, displayIntensity, t)));
        }

        IEnumerator ScanSequence()
        {
            sfx.Play("scan");
            yield return human.Scan(Short ? .6f : 1);
        }

        IEnumerator Pinch()
        {
            niko.gestureTarget = eye.transform;
            niko.Play("Pinch", 1.4f);
            yield return new Wait(.7f);
            sfx.Play("whoosh");
        }

        /// The human freezes and disappears; the selected eye leaves its socket, enlarges in front of the learner.
        IEnumerator Enlarge(float seconds)
        {
            foreach (var id in new[] { "sclera", "iris", "cornea" }) eye.Get(id).SetGlow(0);
            var p0 = eyeRig.position; var r0 = eyeRig.rotation; var s0 = eyeRig.localScale;
            var dissolve = human.Dissolve(Short ? .3f : .9f);
            if (Short)
            {
                yield return dissolve;
                yield return hud.Fade(1, .2f);
                PlaceEye(heroPoint);
                yield return hud.Fade(0, .3f);
            }
            else
            {
                yield return Routine.All(dissolve, Routine.Tween(seconds, t =>
                {
                    float k = Mathf.SmoothStep(0, 1, t);
                    eyeRig.SetPositionAndRotation(Vector3.Lerp(p0, heroPoint.position, k), Quaternion.Slerp(r0, heroPoint.rotation, k));
                    eyeRig.localScale = Vector3.Lerp(s0, heroPoint.lossyScale, k * k);
                }));
            }
            otherEye.gameObject.SetActive(false);
            spin = true;
        }

        public void OnCue(string cue)
        {
            if (cue.EndsWith("_L01") || cue == "S02_L09" || cue == "S03_L12" || cue == "S04_L08") niko.Play("Present", 2.2f);
            else if (narrator.database.Get(cue)?.IsPrompt == true) niko.Play("Point", 2);
        }
    }
}
