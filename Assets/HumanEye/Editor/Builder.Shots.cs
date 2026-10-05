using System.Linq;
using UnityEngine;

namespace HumanEye.EditorTools
{
    public static partial class Builder
    {
        /// Every camera shot as a named CinemachineCamera. Shots under the eye are in eye-radius units in the eye's
        /// own frame (+Z cornea, cut face toward -X), so close-ups stay framed wherever the eye is.
        static void BuildShots()
        {
            var e = heroEye.transform;
            var head = stage.human.eyeRight.position;
            var nerve = EyeGeometry.NervePath(cutFrame.InverseTransformPoint(BrainEntry)).Select(p => cutFrame.TransformPoint(p)).ToList();
            Vector3 N(float t) => nerve[Mathf.Clamp(Mathf.RoundToInt(t * (nerve.Count - 1)), 0, nerve.Count - 1)];
            var vis = visualCentreWorld;
            Shot("Menu", null, new Vector3(.3f, 2.15f, -5.6f), new Vector3(.3f, 1.55f, .7f), 42);

            // Scene 2 — establishing wide, medium on Niko, the display, scan, anatomical close-up, hero eye.
            Shot("S02_EstablishA", null, new Vector3(0, 2.7f, -10.5f), new Vector3(0, 2.6f, 3), 44);
            Shot("S02_EstablishB", null, new Vector3(0, 2.0f, -7.2f), new Vector3(0, 1.6f, 2), 40);
            Shot("S02_NikoMedium", null, new Vector3(-.95f, 1.3f, -1.5f), new Vector3(-1.25f, 1.05f, .8f), 36);
            Shot("S02_DisplayWide", null, new Vector3(-.45f, 1.55f, -3.7f), new Vector3(-.4f, 1.15f, 1.7f), 40);
            Shot("S02_ScanFront", null, new Vector3(0, 1.2f, -2.8f), new Vector3(0, 1.0f, 2), 44);
            Shot("S02_EyeInSkull", null, head + new Vector3(.12f, .04f, -.8f), head + new Vector3(.03f, -.005f, 0), 32);
            Shot("S02_PinchNiko", null, new Vector3(-.85f, 1.35f, -1.05f), new Vector3(-1.25f, 1.2f, .8f), 34);
            Shot("S02_EyeHero", null, new Vector3(-.3f, 1.7f, -2.6f), new Vector3(-.25f, 1.35f, .6f), 36);
            Shot("S02_Part_sclera", e, new Vector3(1.6f, .55f, 1.6f), new Vector3(.55f, .2f, .5f), 30);
            Shot("S02_Part_cornea", e, new Vector3(1.9f, .2f, 1.05f), new Vector3(.1f, .05f, .95f), 27);
            Shot("S02_Part_iris", e, new Vector3(.3f, .18f, 2.7f), new Vector3(0, 0, .8f), 24);
            Shot("S02_Part_pupil", e, new Vector3(.12f, .06f, 2.5f), new Vector3(0, 0, .8f), 20);   // the opening framed inside the iris

            // Scene 3 — cut view (camera on the cut side, -X of the eye).
            Shot("S03_InsideHero", null, new Vector3(-.25f, 1.7f, -2.5f), new Vector3(-.2f, 1.35f, .6f), 36);
            Shot("S03_Part_aqueous", e, new Vector3(-1.35f, .22f, 1.0f), new Vector3(0, .1f, .88f), 25);
            Shot("S03_Part_lens", e, new Vector3(-2.7f, .12f, .62f), new Vector3(0, 0, .6f), 30);
            Shot("S03_Lens_Close", e, new Vector3(-2.25f, 0, .66f), new Vector3(0, -.1f, .65f), 28);   // lens sits above the subtitle band
            Shot("S03_Part_vitreous", e, new Vector3(-1.9f, .2f, .3f), new Vector3(0, 0, -.05f), 36);
            Shot("S03_Vitreous_Back", e, new Vector3(-1.05f, .1f, -.2f), new Vector3(0, 0, -.78f), 34);
            Shot("S03_Part_retina", e, new Vector3(-1.9f, -.05f, -.15f), new Vector3(0, -.3f, -.75f), 32);
            Shot("S03_Retina_Close", e, new Vector3(-1.25f, -.2f, -.4f), new Vector3(.05f, -.42f, -.8f), 28);
            Shot("S03_Part_choroid", e, new Vector3(-1.25f, -.2f, -.4f), new Vector3(.05f, -.42f, -.8f), 28);
            Shot("S03_Choroid_Deep", e, new Vector3(-.85f, -.5f, -.5f), new Vector3(0, -.62f, -.68f), 24);

            // Scene 4 — around the back of the eye, along the nerve, the visual processing area, then the wide view.
            var back = EyeCentre + Vector3.right * .42f;
            Shot("S04_EyeRear", null, EyeCentre + new Vector3(1.35f, .35f, -1.45f), back, 31);
            Shot("S04_EyeRearClose", null, EyeCentre + new Vector3(1.0f, .22f, -.95f), back + Vector3.down * .05f, 29);
            Shot("S04_NerveClose", null, N(.15f) + new Vector3(.12f, .28f, -.75f), N(.15f), 30);
            Shot("S04_NerveMid", null, N(.6f) + new Vector3(.25f, .3f, -1.15f), N(.6f), 34);
            Shot("S04_BrainClose", null, vis + new Vector3(1.0f, .55f, -2.1f), vis + new Vector3(-.3f, 0, 0), 36);
            Shot("S04_Wide", null, new Vector3(1.2f, 2.15f, -4.1f), new Vector3(1.2f, 1.55f, .95f), 38);
            Shot("S09_Wide", null, new Vector3(1.2f, 2.15f, -4.1f), new Vector3(1.2f, 1.55f, .95f), 38);

            // Scenes 5–7 — following light from the lit object through the eye.
            var c = cutFrame;
            Shot("S05_ObjectWide", null, new Vector3(-1.55f, 1.85f, -3.7f), new Vector3(-1.5f, 1.5f, .6f), 40);
            Shot("S05_FollowLight", null, new Vector3(-1.25f, 1.7f, -1.7f), new Vector3(-.9f, 1.5f, .6f), 34);
            Shot("S05_Cornea", c, new Vector3(-1.6f, .25f, 1.15f), new Vector3(0, 0, .95f), 26);
            Shot("S05_AqueousPupil", c, new Vector3(-1.2f, .12f, .95f), new Vector3(0, 0, .82f), 23);
            Shot("S05_PupilFront", c, new Vector3(-1.35f, .32f, 2.75f), new Vector3(0, 0, .8f), 23);
            Shot("S06_LensClose", c, new Vector3(-2.6f, -.04f, .55f), new Vector3(0, -.22f, .5f), 30);
            Shot("S06_NearWide", null, new Vector3(-1.05f, 1.75f, -2.4f), new Vector3(-.95f, 1.5f, .6f), 34);
            Shot("S06_FarView", null, new Vector3(1.2f, 1.75f, -.95f), new Vector3(-6, 1.5f, .9f), 40);
            Shot("S06_Vitreous", c, new Vector3(-3.4f, .25f, .1f), Vector3.zero, 34);
            Shot("S07_Vitreous", c, new Vector3(-2.0f, .15f, -.1f), new Vector3(0, 0, -.45f), 30);
            Shot("S07_Retina", c, new Vector3(-1.9f, .05f, -.35f), new Vector3(0, 0, -.8f), 28);
            Shot("S07_RetinaCloser", c, new Vector3(-1.25f, .02f, -.55f), new Vector3(0, 0, -.85f), 24);

            // Scene 8 — magnified retina.
            Shot("S08_RetinaPush", c, new Vector3(-.28f, 0, -.78f), new Vector3(0, 0, -.9f), 18);
            var m = microRoot;
            Shot("S08_MicroWide", m, new Vector3(.4f, 2.1f, -7.4f), new Vector3(.4f, 1.5f, 0), 42);
            Shot("S08_Rods", m, new Vector3(-1.6f, .95f, -3.1f), new Vector3(-1.6f, .6f, 0), 34);
            Shot("S08_Cones", m, new Vector3(1.2f, .95f, -3.1f), new Vector3(1.2f, .55f, 0), 34);
            Shot("S08_Signals", m, new Vector3(2.3f, 2.6f, -4.7f), new Vector3(2.3f, 1.9f, 0), 38);
            Shot("S08_PullBack", m, new Vector3(1.0f, 2.0f, -10.4f), new Vector3(1.0f, 1.2f, 0), 48);

            // Scene 9 — signals to the brain, then the object again as perception.
            Shot("S09_RetinaNerve", null, EyeCentre + new Vector3(1.2f, .3f, -1.3f), back + new Vector3(.05f, -.05f, .15f), 30);
            var apple = stage.appleRoot.position;
            Shot("S09_Perception", null, apple + new Vector3(0, .12f, -1.25f), apple, 24);

            // Scenes 10–11.
            Shot("S10_Complete", null, new Vector3(-.2f, 1.7f, -2.65f), new Vector3(0, 1.5f, .6f), 34);
            Shot("S10_Journey", null, new Vector3(-.3f, 2.35f, -6.1f), new Vector3(-.3f, 1.5f, .9f), 46);
            Shot("S11_Eye", null, new Vector3(.1f, 1.6f, -2.0f), new Vector3(0, 1.5f, .6f), 30);
            Shot("S11_NearFar", null, new Vector3(.65f, 1.62f, -.75f), new Vector3(-3, 1.5f, .35f), 40);

            stage.followShot = Shot("Follow (TRACE)", null, Vector3.zero, Vector3.forward, 34);
        }
    }
}
