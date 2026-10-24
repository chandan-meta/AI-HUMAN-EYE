using UnityEngine;

namespace HumanEye
{
    public partial class Stage
    {
        public bool LandingActive { get; private set; }
        public Light landingAccent;
        float landingTime;
        readonly Vector3[] landingLightPositions = new Vector3[3];
        readonly Quaternion[] landingLightRotations = new Quaternion[3];

        void LandingSetup()
        {
            AwakeningSetup(); GradeOn(true);
            eye.SetPupil(EyeGeometry.PupilDefault, true);
            var lights = new[] { eyeKey, eyeRim, eyeFill };
            for (int i = 0; i < lights.Length; i++) if (lights[i]) { landingLightPositions[i] = lights[i].transform.position; landingLightRotations[i] = lights[i].transform.rotation; }
            LandingActive = true; landingTime = 0;
            LandingFrame();
            cams.Snap(cams.Has("Menu") ? "Menu" : "S02_IntroDolly");
        }

        void LandingReset()
        {
            if (LandingActive)
            {
                var lights = new[] { eyeKey, eyeRim, eyeFill };
                for (int i = 0; i < lights.Length; i++) if (lights[i]) lights[i].transform.SetPositionAndRotation(landingLightPositions[i], landingLightRotations[i]);
            }
            LandingActive = false;
            if (landingAccent) landingAccent.enabled = false;
        }

        void LandingFrame()
        {
            if (!LandingActive) return;
            if (!Short && !Clock.Paused) landingTime += Clock.VizDelta;
            eye.transform.localPosition = Vector3.up * (Short ? 0 : .018f * Mathf.Sin(landingTime * .65f));
            eye.transform.localRotation = Short ? Quaternion.identity : Quaternion.Euler(.35f * Mathf.Sin(landingTime * .28f), .7f * Mathf.Sin(landingTime * .32f), 0);
            var e = eye.transform; var at = e.position; float r = e.lossyScale.x;
            var forward = eyeRig.forward; var side = -eyeRig.right;
            var pos = at + (forward * Mathf.Cos(12 * Mathf.Deg2Rad) + side * Mathf.Sin(12 * Mathf.Deg2Rad)) * (4.2f * r) + Vector3.up * (.25f * r);
            var shift = Vector3.Cross((at - pos).normalized, Vector3.up) * ((Screen.width < 1200 ? 1.9f : .9f) * r);
            var menu = cams.Get(cams.Has("Menu") ? "Menu" : "S02_IntroDolly");
            if (menu) Dolly(menu, pos + shift, at + shift, 48, .01f);
            Room(.48f); AmbientScale(.8f); AwakeningSky(.22f);
            keyLight.intensity = keyIntensity * .4f; displayLight.intensity = displayIntensity * .2f;
            LandingLight(eyeKey, at + forward * (2.4f * r) + Vector3.up * (1.8f * r) - side * (1.3f * r), at, eyeKeyBase * .38f);
            LandingLight(eyeRim, at - forward * (1.4f * r) + Vector3.up * (1.1f * r) + side * (.6f * r), at, eyeRimBase * 1.7f);
            LandingLight(eyeFill, at + side * (2f * r) + forward * (.6f * r), at, eyeFillBase * .4f);
            if (landingAccent) landingAccent.enabled = true;
            Look(-.15f, .13f, 1, 0, 0, 1, Short ? 0 : Vector3.Distance(pos, at) + 1.7f * r);
        }

        static void LandingLight(Light light, Vector3 position, Vector3 target, float intensity)
        {
            if (!light) return;
            light.enabled = true; light.intensity = intensity;
            light.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
        }

        void InteriorFillFrame()
        {
            if (!interiorLight) return;
            bool open = eye.gameObject.activeInHierarchy && eye.IsCut && !LandingActive;
            interiorLight.enabled = open;
            if (!open) return;
            var e = eye.transform; float r = e.lossyScale.x;
            interiorLight.transform.position = e.position - e.right * (.65f * r) + e.forward * (.15f * r);
            interiorLight.range = 3 * r;
            interiorLight.intensity = 1.7f * r * r;
        }
    }
}
