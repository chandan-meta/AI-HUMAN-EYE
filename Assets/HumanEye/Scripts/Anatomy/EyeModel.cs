using System;
using System.Collections;
using UnityEngine;

namespace HumanEye
{
    /// The anatomical eye: separately addressable layers, a live pupil opening, an accommodating lens
    /// and a clean cutaway (removable half slides away, section caps appear).
    public class EyeModel : MonoBehaviour
    {
        public AnatomyPart[] parts;
        public Transform removableRoot, lens, lensCut, pupil, pupilCut;
        [Tooltip("Pick targets for the closed eye / the cut section; switched with the cutaway so hidden halves never take taps.")]
        public GameObject pickClosed, pickCut;
        public MeshFilter irisKeep, irisCut, irisCaps;
        public MeshCollider irisPick;
        public Renderer[] caps;

        public float pupilTarget = EyeGeometry.PupilDefault, accommodationTarget = .5f;
        [Tooltip("Eye-radius units per second; slow enough to watch.")] public float pupilRate = .1f;
        public float accommodationRate = .45f;
        public float PupilRadius { get; private set; } = EyeGeometry.PupilDefault;
        public float Accommodation { get; private set; } = .5f;
        public bool IsCut { get; private set; }

        float builtPupil = -1, bakedPupil = -1;
        Mesh keepMesh, cutMesh, capsMesh, pickMesh;

        void Awake()
        {
            keepMesh = new Mesh { name = "Iris keep" }; cutMesh = new Mesh { name = "Iris cut" };
            capsMesh = new Mesh { name = "Iris caps" }; pickMesh = new Mesh { name = "Iris pick" };
            irisKeep.sharedMesh = keepMesh; irisCut.sharedMesh = cutMesh; irisCaps.sharedMesh = capsMesh;
            RebuildIris();
        }

        public AnatomyPart Get(string id) => Array.Find(parts, p => p.id == id);

        public void SetPupil(float radius, bool instant)
        {
            pupilTarget = Mathf.Clamp(radius, EyeGeometry.PupilMin, EyeGeometry.PupilMax);
            if (instant) { PupilRadius = pupilTarget; RebuildIris(); }
        }

        public void SetAccommodation(float value, bool instant)
        {
            accommodationTarget = Mathf.Clamp01(value);
            if (instant) { Accommodation = accommodationTarget; ApplyLens(); }
        }

        void Update()
        {
            float d = Clock.VizDelta;
            PupilRadius = Mathf.MoveTowards(PupilRadius, pupilTarget, pupilRate * d);
            if (Mathf.Abs(PupilRadius - builtPupil) > .0015f || (PupilRadius == pupilTarget && builtPupil != PupilRadius)) RebuildIris();
            Accommodation = Mathf.MoveTowards(Accommodation, accommodationTarget, accommodationRate * d);
            ApplyLens();
            if (PupilRadius == pupilTarget && bakedPupil != PupilRadius && irisPick)
            {
                Lathe.Revolve(EyeGeometry.IrisProfile(PupilRadius), 0, 360, 32, pickMesh);
                irisPick.sharedMesh = null; irisPick.sharedMesh = pickMesh; bakedPupil = PupilRadius;
            }
        }

        void RebuildIris()
        {
            var profile = EyeGeometry.IrisProfile(PupilRadius);
            Lathe.Revolve(profile, EyeGeometry.KeepFrom, EyeGeometry.KeepTo, 36, keepMesh);
            Lathe.Revolve(profile, EyeGeometry.CutFrom, EyeGeometry.CutTo, 36, cutMesh);
            Lathe.Caps(profile, EyeGeometry.KeepFrom, EyeGeometry.KeepTo, capsMesh);
            pupil.localScale = pupilCut.localScale = new Vector3(PupilRadius, PupilRadius, 1);
            builtPupil = PupilRadius;
        }

        void ApplyLens()
        {
            float z = Mathf.Lerp(.76f, 1.24f, Accommodation), xy = Mathf.Lerp(1.05f, .94f, Accommodation);   // visibly flatter ↔ rounder
            lens.localScale = lensCut.localScale = new Vector3(xy, xy, z);
        }

        /// Opens (or closes) the cutaway: the near half slides toward the viewer and fades; caps show the section.
        public IEnumerator Cutaway(bool open, float seconds)
        {
            if (IsCut == open) yield break;
            if (open) foreach (var c in caps) c.enabled = true;
            yield return Routine.Tween(seconds, t => SetCutAmount(open ? t : 1 - t));
            if (!open) foreach (var c in caps) c.enabled = false;
            IsCut = open;
        }

        public void SetCutInstant(bool open)
        {
            foreach (var c in caps) c.enabled = open;
            SetCutAmount(open ? 1 : 0);
            IsCut = open;
        }

        void SetCutAmount(float k)
        {
            removableRoot.localPosition = new Vector3(-.9f * k, .12f * k, 0);
            pickClosed.SetActive(k < .5f); pickCut.SetActive(k >= .5f);
            foreach (var p in parts) p.SetCut(1 - k, true);
            Shader.SetGlobalFloat("_HE_InteriorDark", 1 - k);
        }

        public void SetNerveReveal(float amount) => Get("optic_nerve")?.SetFloat("_Reveal", amount);

        public Transform LabelOf(AnatomyPart p) => IsCut && p.labelAnchorCut ? p.labelAnchorCut : p.labelAnchor ? p.labelAnchor : p.transform;
        public Transform FocusOf(AnatomyPart p) => IsCut && p.focusCut ? p.focusCut : p.focus ? p.focus : p.transform;
    }
}
