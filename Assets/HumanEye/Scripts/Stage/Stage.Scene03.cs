using System.Collections;
using UnityEngine;

namespace HumanEye
{
    public partial class Stage
    {
        void RegisterScene03()
        {
            actions["corneaClear"] = (_, __) => { eye.Get("cornea").SetAlpha(.55f); return Waiting(.8f); };
            actions["turnToSide"] = (_, seconds) => TurnToSide(seconds > 0 ? seconds : 2.6f);
            actions["faceLearner"] = (_, seconds) => FaceLearner(seconds > 0 ? seconds : 1.6f);
            actions["cutaway"] = (open, seconds) => Cutaway(open != "close", seconds > 0 ? seconds : 1.8f);
            actions["lensFlex"] = (_, __) => LensFlex();
            actions["retinaDetail"] = (_, __) => { eye.Get("retina").SetFloat("_DetailStrength", 1); eye.Get("retina").SetGlow(.5f); return null; };
        }

        IEnumerator TurnToSide(float seconds)
        {
            spin = false;
            var r0 = eyeRig.rotation; var e0 = eye.transform.localRotation;
            if (Short)
            {
                yield return hud.Fade(1, .2f);
                PlaceEye(cutPoint);
                yield return hud.Fade(0, .3f);
                yield break;
            }
            yield return Routine.Tween(seconds, t =>
            {
                eyeRig.rotation = Quaternion.Slerp(r0, cutPoint.rotation, t);
                eyeRig.position = Vector3.Lerp(heroPoint.position, cutPoint.position, t);
                eye.transform.localRotation = Quaternion.Slerp(e0, Quaternion.identity, t);
            });
        }

        /// The whole eye turns its cornea back toward the learner (hero pose) on its display, for the closing words.
        IEnumerator FaceLearner(float seconds)
        {
            spin = false;
            var r0 = eyeRig.rotation; var e0 = eye.transform.localRotation;
            if (Short) { eyeRig.rotation = heroPoint.rotation; eye.transform.localRotation = Quaternion.identity; yield break; }
            yield return Routine.Tween(seconds, t =>
            {
                float k = Mathf.SmoothStep(0, 1, t);
                eyeRig.rotation = Quaternion.Slerp(r0, heroPoint.rotation, k);
                eye.transform.localRotation = Quaternion.Slerp(e0, Quaternion.identity, k);
            });
        }

        IEnumerator Cutaway(bool open, float seconds)
        {
            sfx.Play("layerreveal", .7f);   // the section opens as a layer reveal (cinematic sound set), not a whoosh
            eye.Get("cornea").SetAlpha(1);
            yield return eye.Cutaway(open, Short ? .4f : seconds);
        }

        IEnumerator LensFlex()
        {
            var lens = eye.Get("lens");
            lens.SetGlow(.6f); sfx.Play("lensfocus", .4f);
            eye.accommodationRate = .5f;
            eye.SetAccommodation(.95f, false);
            yield return new Until(() => Mathf.Abs(eye.Accommodation - .95f) < .01f);
            eye.SetAccommodation(.5f, false);
            yield return new Until(() => Mathf.Abs(eye.Accommodation - .5f) < .01f);
            lens.SetGlow(.85f);
        }
    }
}
