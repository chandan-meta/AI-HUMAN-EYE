using System.Linq;
using UnityEditor;
using UnityEngine;
using G = HumanEye.EyeGeometry;

namespace HumanEye.EditorTools
{
    /// Cinematic pass step: shot re-aims so every explained structure is framed. Called by Builder.ApplyCinematic (Builder.Cinematic.cs).
    /// Eye-local shots are posed in the eye's own frame (eye radii; +Z toward the cornea, cut face toward −X) on the existing
    /// CinemachineCamera transforms, like Builder.Production.EyeShots; world shots go through WowPatch.Shot. Idempotent.
    public static partial class Builder
    {
        static void CinematicFraming()
        {
            if (!stage || !cams) { Debug.LogWarning("CINEMATIC framing: no Stage / CameraDirector in the scene"); return; }
            void Local(string name, Vector3 pos, Vector3 look, float fov)
            {
                var s = cams.shots.FirstOrDefault(x => x.name == name);
                if (!s) { Debug.LogWarning("CINEMATIC framing: shot missing " + name); return; }
                s.transform.localPosition = pos; s.transform.localRotation = Quaternion.LookRotation(look - pos, Vector3.up);
                var lens = s.Lens; lens.FieldOfView = fov; s.Lens = lens;
                EditorUtility.SetDirty(s); EditorUtility.SetDirty(s.transform);
            }
            float lz = G.LensZ;
            // S06_L01–L04: the lens fills more of the frame so its change of curvature reads (near ↔ distant); the rays still bend through it.
            Local("S06_LensClose", new Vector3(-2.2f, .02f, lz), new Vector3(0, -.1f, lz - .04f), 26);
            // S07_L01 names the lens, the vitreous and the retina: frame the whole span from the lens to the back wall.
            Local("S07_Vitreous", new Vector3(-2.5f, .2f, -.05f), new Vector3(0, -.05f, -.15f), 32);
            // S10_L01/L02 (the whole process) now play on the pathway shot: a little wider and biased right so the lit apple
            // on the left and the visual area on the right both sit inside the area left of the panel column.
            WowPatch.Shot(stage, "S10_Journey", new Vector3(-.1f, 2.4f, -6.4f), new Vector3(.2f, 1.5f, .9f), 48);
            // S08_L05 (phototransduction): one photoreceptor column from outer segment to ganglion cell, where a signal leaves.
            var micro = stage.micro ? stage.micro.transform : null;
            if (micro) WowPatch.Shot(stage, "S08_Transduce", micro.TransformPoint(new Vector3(-.3f, 1.9f, -4.6f)), micro.TransformPoint(new Vector3(-.3f, 1.5f, 0)), 36);
            else Debug.LogWarning("CINEMATIC framing: no magnified retina in the scene; S08_Transduce not created");
            EditorUtility.SetDirty(cams);
        }
    }
}
