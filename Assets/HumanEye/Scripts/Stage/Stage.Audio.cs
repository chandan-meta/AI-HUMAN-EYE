using UnityEngine;

namespace HumanEye
{
    /// Music, ambience and the travelling-signal sound follow the module's state every frame, so a chapter jump, restart,
    /// skip, the menu or a quiz can never leave the wrong bed playing (audio overhaul, 15 October 2026):
    /// • music: the start theme on the menu; the opening's own score while it runs (Stage.Opening); a check's bed while a quiz
    ///   card is up; otherwise the chapter's bed, set on entry (BedFor), by Do("music", key) steps, by Scene 4's signal stream
    ///   and by the closing (finale). AudioDirector crossfades and ducks under Niko.
    /// • ambience: the lab's room tone, the eye's inner hum inside the cut eye or the magnified retina, silence in the
    ///   eye-to-brain cinematic.
    /// • the gold signal: one quiet loop rides the lead pulse in 3D while any stream travels, with a lift once per stream as
    ///   the lead nears the brain; the pupil's aperture sounds when it starts to widen or narrow.
    public partial class Stage
    {
        string bed = "lab";
        bool nearHeard, pupilMoving;

        static string BedFor(string chapter) => chapter switch
        {
            "Scene02" => "start",   // the opening carries the start theme; IntroEnd hands over to the lab bed
            "Scene03" or "Scene06" or "Scene07" => "eye",
            "Scene08" or "Scene09" => "pulse",
            _ => "niko",
        };

        void AudioFrame()
        {
            if (!sfx || !runner) return;
            sfx.listening = runner.AskHold;
            if (!IntroPlaying) sfx.SetMusic(!runner.Started ? "start" : activities && activities.Card != null ? "quiz" : bed);
            var chapter = runner.Chapter ? runner.Chapter.id : "";
            bool inside = micro.gameObject.activeInHierarchy || eye.IsCut && chapter is "Scene03" or "Scene06" or "Scene07";
            sfx.SetAmbience(EyeBrainPlaying ? null : inside ? "ambeye" : "amblab");
            if (Clock.Paused) return;   // loops hold where they are (AudioDirector pauses them)

            var lead = signals.LeadPulse("nerve") ?? signals.LeadPulse("vp_RR");
            if (lead != null && signals.Lead(lead.Path.id, out var at))
            {
                sfx.LoopOn("pulsetravel", .9f);
                sfx.LoopAt("pulsetravel", at);
                if (!nearHeard && lead.Progress >= .67f) { nearHeard = true; sfx.Play("pulsenear", .45f); }
            }
            else { sfx.LoopOff("pulsetravel"); nearHeard = false; }

            if (brightnessLive)
            {   // Scene 5: a cue each time the pupil starts to move, by direction (not every frame of the move)
                float d = eye.pupilTarget - eye.PupilRadius; bool moving = !Clock.Frozen && Mathf.Abs(d) > .01f;
                if (moving && !pupilMoving) sfx.Play(d > 0 ? "pupilwide" : "pupilnarrow", .5f);
                pupilMoving = moving;
            }
            else pupilMoving = false;
        }
    }
}
