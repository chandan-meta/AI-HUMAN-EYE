using UnityEngine;

namespace HumanEye
{
    /// Illustrative (not clinical) ray diagram in the eye's cut plane: light from the illuminated object bends at the
    /// cornea, passes through the aqueous humour and pupil, is bent more by the lens and travels through the
    /// vitreous humour to the retina. Focus uses a thin-lens model whose power follows the lens's current
    /// accommodation, so lens shape, object distance and ray convergence stay coherent.
    public class LightRays : MonoBehaviour
    {
        public EyeModel eye;
        public Transform targetTop, targetBottom;
        public Transform lamp;
        public Light lampLight;
        public LineRenderer[] rays;       // 0-2 from the top of the object (solid), 3-5 from the bottom (dashed)
        public LineRenderer[] lampRays;   // illumination reaching the object (it reflects light; it doesn't glow)
        public Transform imageQuad;       // inverted image where the rays meet the retina
        public Camera objectCamera;

        [Range(0, 5)] public float reach = 5;   // 1 cornea, 2 pupil, 3 lens, 4 through lens, 5 retina
        [Range(0, 1)] public float brightness = .5f;
        public bool showLamp = true, showImage;
        public float nearDistance = 4, farDistance = 25, planeX = -.03f;
        public float FocusError { get; private set; }
        public float ImageTop { get; private set; }
        public float ImageBottom { get; private set; }

        const float RetinaDistance = EyeGeometry.LensZ + EyeGeometry.RetinaIn, PupilZ = .79f;
        readonly Vector3[] pts = new Vector3[6];
        readonly Vector3[] draw = new Vector3[6];

        /// Accommodation (0 = distant/flatter lens, 1 = near/more curved) that focuses an object at distance u.
        public static float RequiredAccommodation(float u, float near, float far) =>
            Mathf.Clamp01((1 / Mathf.Max(u, .01f) - 1 / far) / (1 / near - 1 / far));

        public float ImageDistance(float u, float accommodation)
        {
            float power = 1 / RetinaDistance + 1 / farDistance + accommodation * (1 / nearDistance - 1 / farDistance);
            float inv = power - 1 / Mathf.Max(u, .01f);
            return inv > 1e-3f ? 1 / inv : 1000;
        }

        public float ObjectDistance => targetTop ? Mathf.Max(.5f, Local(targetTop.position).z - EyeGeometry.LensZ) : farDistance;

        Vector3 Local(Vector3 world) => eye.transform.InverseTransformPoint(world);
        Vector3 World(Vector2 zy) => eye.transform.TransformPoint(new Vector3(planeX, zy.y, zy.x));

        void LateUpdate()
        {
            bool active = targetTop && targetTop.gameObject.activeInHierarchy;
            foreach (var r in rays) r.enabled = active && reach > 0;
            foreach (var r in lampRays) r.enabled = active && showLamp;
            if (imageQuad) imageQuad.gameObject.SetActive(active && showImage);
            if (objectCamera) objectCamera.enabled = active && showImage;
            if (!active) return;

            var top = Local(targetTop.position); var bottom = Local(targetBottom.position);
            float u = Mathf.Max(.5f, (top.z + bottom.z) * .5f - EyeGeometry.LensZ);
            float v = ImageDistance(u, eye.Accommodation);
            FocusError = Mathf.Abs(v - RetinaDistance);
            ImageTop = -top.y * v / u; ImageBottom = -bottom.y * v / u;

            float width = Mathf.Lerp(.004f, .011f, brightness) * eye.transform.lossyScale.x / .4f;
            var tint = new Color(1, .93f, .7f, Mathf.Lerp(.3f, 1, brightness));
            for (int k = 0; k < rays.Length; k++)
            {
                var o = k < 3 ? top : bottom;
                Build(new Vector2(o.z, o.y), (k % 3 - 1) * .55f, u, v);
                Draw(rays[k], width, tint);
            }
            for (int k = 0; k < lampRays.Length && lamp; k++)
            {
                var hit = Vector3.Lerp(targetTop.position, targetBottom.position, .2f + k * .3f);
                lampRays[k].positionCount = 2;
                lampRays[k].SetPosition(0, lamp.position); lampRays[k].SetPosition(1, hit);
                lampRays[k].widthMultiplier = width * .8f;
                lampRays[k].startColor = lampRays[k].endColor = new Color(1, .9f, .65f, Mathf.Lerp(.15f, .7f, brightness));
            }
            if (lampLight) lampLight.intensity = Mathf.Lerp(.25f, 5f, brightness);
            if (imageQuad && showImage)
            {
                float h = Mathf.Max(.05f, Mathf.Abs(ImageTop - ImageBottom));
                imageQuad.localPosition = new Vector3(planeX - .01f, (ImageTop + ImageBottom) * .5f, -EyeGeometry.RetinaIn + .03f);
                imageQuad.localScale = new Vector3(h * 1.15f, h * 1.15f, 1);
            }
            if (objectCamera && showImage)
            {
                var center = (targetTop.position + targetBottom.position) * .5f;
                objectCamera.transform.position = eye.transform.TransformPoint(new Vector3(0, 0, EyeGeometry.LensZ));
                objectCamera.transform.rotation = Quaternion.LookRotation(center - objectCamera.transform.position, eye.transform.up);
                float dist = Vector3.Distance(center, objectCamera.transform.position);
                objectCamera.fieldOfView = 2 * Mathf.Atan(Vector3.Distance(targetTop.position, targetBottom.position) * .75f / dist) * Mathf.Rad2Deg;
            }
        }

        void Build(Vector2 o, float s, float u, float v)
        {
            float rp = eye.PupilRadius, yP = s * rp;
            var aim = new Vector2(PupilZ, yP * 1.18f);
            var c = HitCircle(o, aim, new Vector2(EyeGeometry.CorneaCenterZ, 0), EyeGeometry.CorneaOuter);
            float sx = eye.lens.localScale.x, sz = eye.lens.localScale.z, a = EyeGeometry.LensRadius * sx;
            float y1 = yP * .97f, z1 = EyeGeometry.LensZ + EyeGeometry.LensFront * sz * Mathf.Sqrt(Mathf.Max(0, 1 - y1 * y1 / (a * a)));
            float y2 = y1 * .93f, z2 = EyeGeometry.LensZ - EyeGeometry.LensBack * sz * Mathf.Sqrt(Mathf.Max(0, 1 - y2 * y2 / (a * a)));
            var l2 = new Vector2(z2, y2);
            var image = new Vector2(EyeGeometry.LensZ - v, -o.y * v / u);
            var dir = (image - l2).normalized;
            float rr = EyeGeometry.RetinaIn * EyeGeometry.RetinaIn - planeX * planeX;
            float b = Vector2.Dot(l2, dir), cc = l2.sqrMagnitude - rr, disc = b * b - cc;
            float t = disc > 0 ? -b + Mathf.Sqrt(disc) : 1;
            pts[0] = World(o); pts[1] = World(c); pts[2] = World(new Vector2(PupilZ, yP));
            pts[3] = World(new Vector2(z1, y1)); pts[4] = World(l2); pts[5] = World(l2 + dir * t);
        }

        static Vector2 HitCircle(Vector2 from, Vector2 to, Vector2 center, float radius)
        {
            var d = to - from; var f = from - center;
            float a = Vector2.Dot(d, d), b = 2 * Vector2.Dot(f, d), c = Vector2.Dot(f, f) - radius * radius, disc = b * b - 4 * a * c;
            if (disc < 0) return Vector2.Lerp(from, to, .9f);
            float t = (-b - Mathf.Sqrt(disc)) / (2 * a);
            return from + d * Mathf.Clamp01(t);
        }

        void Draw(LineRenderer line, float width, Color tint)
        {
            int n = 1; draw[0] = pts[0];
            for (int i = 0; i < 5 && reach > i; i++) draw[n++] = Vector3.Lerp(pts[i], pts[i + 1], Mathf.Clamp01(reach - i));
            line.positionCount = n;
            line.SetPositions(draw);
            line.widthMultiplier = width;
            line.startColor = line.endColor = tint;
        }
    }
}
