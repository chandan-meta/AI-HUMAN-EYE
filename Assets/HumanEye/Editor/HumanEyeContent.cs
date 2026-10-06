using System.Collections.Generic;
using UnityEngine;

namespace HumanEye.EditorTools
{
    /// Authored content compiled into the generated assets by Tools > Human Eye > Build Module.
    ///  • Chapters: StoryboardCore order — every Say step is an exact ledger cue id; only implementation steps
    ///    (camera, visual actions, checkpoint questions, two hands-on experiments) are added around them. Shots are
    ///    async wherever the subject is already framed, so narration starts while the camera settles (no dead air).
    ///  • Questions / Ask Niko: SupplementalAdaptive, newly authored, never part of the dialogue ledger.
    public static class HumanEyeContent
    {
        static Step Shot(string name, float blend = 1.5f, bool async = false) => new() { kind = StepKind.Shot, key = name, value = blend, async = async };
        static Step Say(string cue) => new() { kind = StepKind.Say, key = cue };
        static Step Do(string action, string arg = null, float value = 0, bool async = false) => new() { kind = StepKind.Do, key = action, arg = arg, value = value, async = async };
        static Step Await(string key) => new() { kind = StepKind.Await, key = key };
        static Step Explore() => new() { kind = StepKind.Explore };
        static Step Hold(float s) => new() { kind = StepKind.Hold, value = s };
        static Step Offer(string key) => new() { kind = StepKind.Offer, key = key };
        static Step Free(string mode) => new() { kind = StepKind.Free, key = mode };
        static ExploreEntry Part(string id, string label, params Step[] steps) => new() { partId = id, label = label, steps = steps };

        public class Chapter { public string id, title, checkpoint; public string[] topics; public Step[] steps; public ExploreEntry[] explore; }

        public static List<Chapter> Chapters() => new()
        {
            new Chapter
            {
                id = "Scene02", title = "Meet Niko", topics = new[] { "external_structures" },
                checkpoint = "Dark room from the door end; the directed entrance (skippable) ends on the establishing view with the spotlit human; Niko hidden; the selected eye in its socket; nothing explored.",
                steps = new[]
                {
                    Do("intro"),
                    Do("nikoAppear", null, 1.0f, true), Shot("S02_NikoMedium", 1.4f, true), Do("sfx", "materialise", .6f),
                    Say("S02_L01"), Say("S02_L02"),
                    Do("nikoLook", "human"), Do("niko", "Present", 2.4f), Shot("S02_DisplayWide", 1.6f, true), Do("humanReveal", null, 1.4f, true), Say("S02_L03"),
                    Say("S02_L04"),
                    Do("info", "Niko's scanner is ready. Press Scan Eye in the panel to look inside the subject."), Do("niko", "RaiseHand", 30), Say("S02_L05"), Await("scan"),
                    Shot("S02_ScanFront", 1.0f), Do("scan"),
                    Do("illuminate"), Shot("S02_EyeInSkull", 1.6f, true), Say("S02_L06"),
                    Say("S02_L07"),
                    Do("nikoLook", "eye"), Do("pinch"),
                    Shot("S02_EyeHero", 2.2f, true), Do("enlarge", null, 2.2f), Do("nikoMove", "EyeLeft", 1.2f, true), Do("info", ""),
                    Say("S02_L08"),
                    Do("niko", "Present", 2), Say("S02_L09"),
                    Do("niko", "Snap", 1.5f), Do("sequence", "sclera,cornea,iris,pupil", .5f, true),
                    Say("S02_L10"),
                    Explore(),
                    Offer("topic:external_structures"),
                    Shot("S02_EyeHero", 1, true), Do("niko", "Present", 2), Say("S02_L15"),
                    Do("info", "Press Explore Inside in the panel to open the eye."), Say("S02_L16"), Await("inside"),
                    Do("sfx", "unlock"),
                },
                explore = new[]
                {
                    Part("sclera", "Sclera", Say("S02_L11")),
                    Part("cornea", "Cornea", Say("S02_L12")),
                    Part("iris", "Iris", Say("S02_L13")),
                    Part("pupil", "Pupil", Say("S02_L14")),
                },
            },
            new Chapter
            {
                id = "Scene03", title = "Inside the Eye", topics = new[] { "internal_structures" },
                checkpoint = "Closed eye at the hero point in the Scene 2 orientation; Niko beside it; camera S02_EyeHero.",
                steps = new[]
                {
                    Do("corneaClear"), Shot("S03_InsideHero", 2.2f, true), Do("turnToSide", null, 2.2f), Do("cutaway", "open", 1.6f),
                    Do("niko", "Snap", 1.5f), Do("sequence", "aqueous,lens,vitreous,retina,choroid", .5f, true),
                    Say("S03_L01"),
                    Explore(),
                    Offer("topic:internal_structures"),
                    Shot("S03_InsideHero", 1.2f, true), Do("niko", "Present", 2), Say("S03_L12"),
                },
                explore = new[]
                {
                    Part("aqueous", "Aqueous humour", Say("S03_L02"), Say("S03_L03")),
                    Part("lens", "Crystalline lens", Say("S03_L04"), Shot("S03_Lens_Close", 1.2f, true), Say("S03_L05"), Do("lensFlex", null, 0, true), Say("S03_L06")),
                    Part("vitreous", "Vitreous humour", Shot("S03_Vitreous_Back", 1.8f, true), Say("S03_L07"), Say("S03_L08")),
                    Part("retina", "Retina", Say("S03_L09"), Shot("S03_Retina_Close", 1.2f, true), Do("retinaDetail"), Say("S03_L10")),
                    Part("choroid", "Choroid", Shot("S03_Choroid_Deep", 1.4f, true), Say("S03_L11")),
                },
            },
            new Chapter
            {
                id = "Scene04", title = "How Is the Eye Connected to the Brain?", topics = new[] { "optic_nerve" },
                checkpoint = "Cutaway eye at the cut point; only the nerve stub visible; brain hidden; camera S03_InsideHero.",
                steps = new[]
                {
                    Shot("S04_EyeRear", 2.4f, true), Say("S04_L01"),
                    Shot("S04_EyeRearClose", 1.8f, true), Say("S04_L02"),
                    Do("nerveReveal", null, 2.2f, true), Say("S04_L03"),
                    Do("glow", "optic_nerve", .8f), Do("labels", "focus:optic_nerve"), Say("S04_L04"),
                    Shot("S04_NerveClose", 1.4f, true), Say("S04_L05"),
                    Do("brainReveal", null, 1.4f, true), Shot("S04_NerveMid", 2.2f, true), Say("S04_L06"),
                    Shot("S04_BrainClose", 2.0f, true), Do("glow", "visual_area", .9f), Do("labels", "focus:visual_area"), Say("S04_L07"),
                    Do("glow", "optic_nerve,visual_area", 0), Shot("S04_Wide", 2.2f, true), Do("labels", "scan"), Say("S04_L08"),
                    Do("labels", ""),
                    Offer("item:nerve_tap"),
                },
            },
            new Chapter
            {
                id = "Scene05", title = "Light Enters the Eye", topics = new[] { "pupil_response" },
                checkpoint = "Cutaway eye, nerve and brain; no object yet; pupil at default size; brightness 50%; camera S05_ObjectWide.",
                steps = new[]
                {
                    Do("objectAppear", null, 1.0f), Say("S05_L01"),
                    Do("lightTravel", "1", 2.5f, true), Shot("S05_FollowLight", 2.5f, true), Say("S05_L02"),
                    Shot("S05_Cornea", 1.4f, true), Do("isolate", "cornea"), Do("labels", "focus:cornea"), Do("lightTravel", "1.5", 1.0f, true), Say("S05_L03"),
                    Shot("S05_AqueousPupil", 1.2f, true), Do("isolate", "aqueous"), Do("labels", "focus:aqueous"), Do("lightTravel", "2", 1.3f, true), Say("S05_L04"),
                    Do("restore"), Do("labels", ""), Do("pupilView"), Do("brightnessLive"),
                    Do("info", "Use the brightness controls in the panel: make it brighter, then dimmer, and watch the pupil."), Say("S05_L05"), Await("brighter"), Do("pupilSettle"), Say("S05_L06"),
                    Await("dimmer"), Do("pupilSettle"), Say("S05_L07"),
                    Do("glow", "iris", .8f), Do("labels", "focus:iris"), Say("S05_L08"), Do("glow", "iris", 0), Do("labels", ""),
                    Offer("item:pupil_predict"),
                    Free("brightness"),
                },
            },
            new Chapter
            {
                id = "Scene06", title = "The Lens Focuses the Light", topics = new[] { "accommodation" },
                checkpoint = "Cutaway eye; apple at medium distance with the lens matched; light reaching the pupil; camera S05_AqueousPupil.",
                steps = new[]
                {
                    Shot("S06_LensClose", 1.8f, true), Do("isolate", "lens"), Do("labels", "focus:lens"), Do("lightTravel", "4", 2.2f, true), Say("S06_L01"),
                    Do("restore"),
                    Do("distanceControls"), Do("info", "Press Near object in the panel to bring the apple close."), Await("near"),
                    Shot("S06_NearWide", 1.2f, true), Do("nearDemo"), Shot("S06_LensClose", 1.2f, true), Say("S06_L02"),
                    Do("info", "Now press Distant object."), Await("distant"),
                    Shot("S06_FarView", 1.4f, true), Do("distantDemo"), Shot("S06_LensClose", 1.2f, true), Say("S06_L03"),
                    Say("S06_L04"), Do("labels", ""),
                    Shot("S06_Vitreous", 2.0f, true), Do("lightTravel", "5", 2.2f),
                    Offer("topic:accommodation"),
                },
            },
            new Chapter
            {
                id = "Scene07", title = "Image Forms on the Retina", topics = new[] { "inverted_image" },
                checkpoint = "Cutaway eye; apple at 6 eye-radii with the lens matched; light through the lens; no image yet; camera S06_Vitreous.",
                steps = new[]
                {
                    Shot("S07_Vitreous", 2.2f, true), Do("isolate", "vitreous"), Do("labels", "focus:vitreous"), Do("lightTravel", "5", 2.2f, true), Say("S07_L01"),
                    Do("isolate", "retina"), Do("labels", "focus:retina"), Shot("S07_Retina", 1.4f, true), Do("imageForm", null, 1.2f, true), Do("imageInset"), Say("S07_L02"),
                    Do("restore"), Do("imageInset", "hide"), Shot("S07_RetinaCloser", 1.6f, true), Say("S07_L03"),
                    Do("labels", ""),
                },
            },
            new Chapter
            {
                id = "Scene08", title = "Photoreceptors Detect Light", topics = new[] { "rods_cones" },
                checkpoint = "Image on the retina (end of Scene 7); magnified retina prepared with light and signals off.",
                steps = new[]
                {
                    Do("intoRetina"), Say("S08_L01"),
                    Do("microLight", "0.3", 1), Do("microFocus", "rods"), Shot("S08_Rods", 1.4f, true), Do("labels", "focus:rods"), Say("S08_L02"), Say("S08_L03"),
                    Do("microLight", "0.95", 1), Do("microFocus", "cones"), Shot("S08_Cones", 1.4f, true), Do("labels", "focus:cones"), Say("S08_L04"),
                    Do("microFocus", "all"), Shot("S08_MicroWide", 1.4f, true), Do("microLight", "0.65", 1), Do("labels", "scan"), Do("microSignals", null, 1), Say("S08_L05"),
                    Shot("S08_Signals", 1.8f, true), Say("S08_L06"),
                    Shot("S08_PullBack", 2.2f, true),
                    Offer("topic:rods_cones"),
                    Free("photoreceptors"),
                },
            },
            new Chapter
            {
                id = "Scene09", title = "Signals Travel to the Brain", topics = new[] { "optic_nerve" },
                checkpoint = "Cutaway eye, full optic nerve and brain; apple placed at medium distance; camera on the back of the eye.",
                steps = new[]
                {
                    Do("labels", "focus:optic_nerve"), Do("signals", "nerve", 1), Say("S09_L01"),
                    Do("followSignal", "nerve", 1, true), Say("S09_L02"),
                    Shot("S04_BrainClose", 2, true), Do("glow", "visual_area", .9f), Do("labels", "focus:visual_area"), Say("S09_L03"),
                    Do("signals", "nerve", 0), Do("perception"), Say("S09_L04"),
                    Do("glow", "visual_area", 0), Do("labels", ""), Do("imageInset", "hide"), Shot("S09_Wide", 2, true),
                    Offer("topic:optic_nerve"),
                    Offer("topic:inverted_image"),
                },
            },
            new Chapter
            {
                id = "Scene10", title = "The Complete Working", topics = new[] { "complete_pathway", "inverted_image" },
                checkpoint = "Complete scene: apple, light path to the retina, eye, optic nerve and brain; camera S09_Wide.",
                steps = new[]
                {
                    Shot("S10_Complete", 2.2f, true), Say("S10_L01"), Say("S10_L02"),
                    Shot("S10_Journey", 2, true), Do("journey"),
                    Do("overview"),
                    Do("journey", "overview"),
                },
            },
            new Chapter
            {
                id = "Scene11", title = "Final Curiosity", topics = new string[0],
                checkpoint = "Cutaway eye closing to the complete eye; near apple and a distant tree placed, both sharp; camera S10_Complete.",
                steps = new[]
                {
                    Do("closeEye", null, 1.2f), Shot("S11_Eye", 1.8f, true), Say("S11_L01"),
                    Shot("S11_NearFar", 2.2f, true), Say("S11_L02"), Say("S11_L03"),
                    Do("closing"),
                },
            },
        };

        // ---------------- SupplementalAdaptive: questions (newly authored) ----------------
        static QuestionBank.Option O(string text, bool correct, string feedback, string part = null, string misconception = null) =>
            new() { text = text, correct = correct, feedback = feedback, partId = part, misconception = misconception };

        static QuestionBank.Item Q(string id, string topic, string chapter, QuestionLevel level, QuestionMode mode, string prompt, string correctFeedback, string[] hints, string demo, params QuestionBank.Option[] options) =>
            new() { id = id, topic = topic, chapter = chapter, level = level, mode = mode, prompt = prompt, correctFeedback = correctFeedback, hints = hints, demo = demo, options = options };

        public static List<QuestionBank.Item> Questions()
        {
            var list = new List<QuestionBank.Item>
            {
                Q("ext_iris_role", "external_structures", "Scene02", QuestionLevel.Recognition, QuestionMode.TapPart,
                  "Which part controls the size of the pupil?", "Yes — the iris controls the size of the pupil.",
                  new[] { "Look at the coloured ring at the front of the eye.", "Compare the opening (the pupil) with the coloured ring around it. Which one can change the opening?", "Listen to Niko's explanation of the iris again." }, "replayCue:S02_L13",
                  O("Iris", true, "", "iris"), O("Cornea", false, "The cornea is the clear front layer that bends light.", "cornea", "cornea-controls-pupil"),
                  O("Sclera", false, "The sclera is the tough white outer layer.", "sclera", "sclera-controls-pupil"), O("Pupil", false, "The pupil is the opening itself — something around it changes its size.", "pupil", "pupil-controls-itself")),
                Q("ext_cornea_job", "external_structures", "Scene02", QuestionLevel.Relationship, QuestionMode.TapPart,
                  "Which transparent, curved layer at the front protects the eye and bends incoming light?", "Right — the cornea protects the eye and bends incoming light.",
                  new[] { "Look for the clear, curved surface at the very front.", "The iris is coloured and the sclera is white. Which front layer is transparent?", "Listen to Niko's explanation of the cornea again." }, "replayCue:S02_L12",
                  O("Cornea", true, "", "cornea"), O("Iris", false, "The iris is coloured, not transparent.", "iris", "iris-bends-light"),
                  O("Sclera", false, "The sclera is white and tough, not transparent.", "sclera"), O("Pupil", false, "The pupil is an opening, not a layer.", "pupil", "pupil-is-a-layer")),
                Q("ext_sclera_shape", "external_structures", "Scene02", QuestionLevel.Recognition, QuestionMode.Choice,
                  "What does the sclera do?", "Yes — it protects the eye and helps maintain its shape.",
                  new[] { "Think about the tough white outer layer.", "One choice describes the iris and one describes the pupil. Which is left?", "Listen to Niko's explanation of the sclera again." }, "replayCue:S02_L11",
                  O("It protects the eye and helps maintain its shape", true, ""), O("It controls the size of the pupil", false, "That's the job of the iris.", null, "sclera-controls-pupil"),
                  O("It is the opening light enters through", false, "That describes the pupil.", null, "sclera-is-opening")),
                Q("ext_pupil_opening", "external_structures", "Scene02", QuestionLevel.Recognition, QuestionMode.TapPart,
                  "Tap the opening that light enters the eye through.", "Yes — light enters the eye through the pupil.",
                  new[] { "Look at the centre of the coloured ring.", "The iris is the ring; the pupil is the gap in the middle.", "Listen to Niko's explanation of the pupil again." }, "replayCue:S02_L14",
                  O("Pupil", true, "", "pupil"), O("Iris", false, "The iris is the coloured ring around the opening.", "iris"), O("Cornea", false, "The cornea covers the front; light passes through it before the opening.", "cornea")),

                Q("int_aqueous", "internal_structures", "Scene03", QuestionLevel.Recognition, QuestionMode.TapPart,
                  "Which space between the cornea and the lens contains a clear fluid?", "Yes — that's the aqueous humour.",
                  new[] { "Look at the front of the eye, between the cornea and the lens.", "The vitreous humour is behind the lens; which fluid is in front of it?", "Listen to Niko's explanation of the aqueous humour again." }, "replayCue:S03_L02",
                  O("Aqueous humour", true, "", "aqueous"), O("Vitreous humour", false, "The vitreous humour fills the space behind the lens.", "vitreous", "aqueous-vitreous-swap"), O("Crystalline lens", false, "The lens is the flexible, biconvex structure, not a fluid.", "lens")),
                Q("int_vitreous", "internal_structures", "Scene03", QuestionLevel.Relationship, QuestionMode.Choice,
                  "What does the vitreous humour help to do?", "Right — it helps maintain the shape of the eye.",
                  new[] { "Think about the large jelly-filled space behind the lens.", "Changing shape to focus is the lens's job, and supplying oxygen is the choroid's. What's left?", "Listen to Niko's explanation again." }, "replayCue:S03_L08",
                  O("Maintain the shape of the eye", true, ""), O("Change shape to focus light", false, "That's the crystalline lens.", null, "vitreous-focuses"),
                  O("Supply oxygen and nutrients", false, "That's the choroid, the layer rich in blood vessels.", null, "vitreous-nourishes")),
                Q("int_choroid", "internal_structures", "Scene03", QuestionLevel.Relationship, QuestionMode.TapPart,
                  "Which layer is rich in blood vessels and supplies the eye with oxygen and nutrients?", "Yes — the choroid, just behind the retina.",
                  new[] { "Look just behind the retina.", "The retina detects light; the layer behind it has the blood vessels.", "Listen to Niko's explanation of the choroid again." }, "replayCue:S03_L11",
                  O("Choroid", true, "", "choroid"), O("Retina", false, "The retina contains light-sensitive cells. The blood-vessel layer is behind it.", "retina", "retina-is-vascular"),
                  O("Vitreous humour", false, "The vitreous humour is clear jelly.", "vitreous")),
                Q("int_retina", "internal_structures", "Scene03", QuestionLevel.Recognition, QuestionMode.TapPart,
                  "Which inner layer at the back contains light-sensitive cells?", "Yes — the retina contains light-sensitive cells.",
                  new[] { "Look at the inner layer at the back of the eye.", "The choroid has blood vessels; which layer in front of it detects light?", "Listen to Niko's explanation of the retina again." }, "replayCue:S03_L10",
                  O("Retina", true, "", "retina"), O("Choroid", false, "The choroid is behind the retina and supplies it.", "choroid"), O("Crystalline lens", false, "The lens focuses light; it doesn't detect it.", "lens", "lens-detects-light")),

                Q("nerve_tap", "optic_nerve", "Scene04", QuestionLevel.Recognition, QuestionMode.TapPart,
                  "Tap the structure that carries information from the retina to the brain.", "Yes — the optic nerve carries visual information to the brain.",
                  new[] { "Look at what leaves the back of the eye.", "The retina detects light and the brain processes it. What connects them?", "Watch the route traced again." }, "traceNerve",
                  O("Optic nerve", true, "", "optic_nerve"), O("Retina", false, "The retina detects the light; something else carries the information away.", "retina"), O("Brain", false, "The brain receives the information at the end of the route.", "brain")),
                Q("nerve_carries", "optic_nerve", "Scene09", QuestionLevel.Recognition, QuestionMode.Choice,
                  "What carries information from the retina to the brain?", "Right — the optic nerve.",
                  new[] { "Think about the bundle leaving the back of the eye.", "The lens focuses and the choroid supplies nutrients. Which one connects eye and brain?", "Watch the route traced again." }, "traceNerve",
                  O("The optic nerve", true, ""), O("The crystalline lens", false, "The lens focuses light inside the eye.", null, "lens-to-brain"), O("The choroid", false, "The choroid supplies oxygen and nutrients.", null)),
                Q("nerve_signals", "optic_nerve", "Scene09", QuestionLevel.Relationship, QuestionMode.Choice,
                  "What travels along the optic nerve to the brain?", "Yes — electrical signals carrying the visual information.",
                  new[] { "Think about what the photoreceptors turn light into.", "Light stops at the retina. What does it become there?", "Watch the signals travel again." }, "traceNerve",
                  O("Electrical signals carrying visual information", true, ""),
                  O("The image itself, like a photograph", false, "The image isn't sent like a photograph — the nerve carries signals with the information.", null, "image-travels"),
                  O("Light rays", false, "Light reaches the retina, where it is converted into electrical signals.", null, "light-travels-to-brain")),

                Q("pupil_predict", "pupil_response", "Scene05", QuestionLevel.Prediction, QuestionMode.Choice,
                  "Prediction (then we'll test it): if the light becomes brighter, what will the pupil do?", "Your prediction matched: in bright light the pupil becomes smaller.",
                  new[] { "Remember what you saw when you raised the brightness.", "In dim light the pupil became larger. What about bright light?", "Watch the trial again." }, "pupilTrial",
                  O("Become smaller", true, ""), O("Become larger", false, "In the trial the pupil became smaller in brighter light.", null, "pupil-dilates-in-bright"),
                  O("Stay the same size", false, "In the trial the pupil changed size.", null, "pupil-fixed")),
                Q("pupil_bright", "pupil_response", "Scene05", QuestionLevel.Recognition, QuestionMode.Choice,
                  "What happens to the pupil in brighter light?", "Yes — it becomes smaller.",
                  new[] { "Think back to the brightness experiment.", "In dimmer light it became larger. Bright light is the opposite.", "Watch the pupil respond again." }, "pupilTrial",
                  O("It becomes smaller", true, ""), O("It becomes larger", false, "That happens in dimmer light.", null, "pupil-dilates-in-bright"), O("It disappears", false, "The pupil gets smaller but it is still an opening.", null)),
                Q("pupil_iris_role", "pupil_response", "Scene05", QuestionLevel.Relationship, QuestionMode.Choice,
                  "By controlling the size of the pupil, what does the iris regulate?", "Right — how much light enters the eye.",
                  new[] { "Think about what passes through the pupil.", "Bending light is the cornea and lens; detecting light is the retina. What does the opening's size change?", "Listen to Niko's line about the iris again." }, "replayCue:S05_L08",
                  O("How much light enters the eye", true, ""), O("How much the light bends", false, "The cornea and lens bend light.", null, "iris-bends"), O("Which cells detect the light", false, "The retina's cells detect light.", null)),

                Q("acc_near", "accommodation", "Scene06", QuestionLevel.Recognition, QuestionMode.Choice,
                  "How does the lens change for a nearby object?", "Yes — it becomes more curved so the light can be focused correctly.",
                  new[] { "Remember the Near demonstration.", "For a distant object the lens became less curved. Near is the opposite.", "Watch the near and distant demonstrations again." }, "replayAccommodation",
                  O("It becomes more curved", true, ""), O("It becomes less curved", false, "That happens for a distant object.", null, "near-flatter"), O("It doesn't change", false, "The lens changes shape to focus at different distances.", null, "lens-rigid")),
                Q("acc_distant", "accommodation", "Scene06", QuestionLevel.Prediction, QuestionMode.Choice,
                  "Prediction: an object moves far away. What will the lens do?", "Yes — for a distant object the lens becomes less curved.",
                  new[] { "Think about the Distant demonstration.", "Near made the lens more curved; far is the opposite.", "Watch the demonstrations again." }, "replayAccommodation",
                  O("Become less curved", true, ""), O("Become more curved", false, "That is what happens for a nearby object.", null, "far-rounder"), O("Stay exactly the same", false, "The lens keeps adjusting to the distance.", null, "lens-rigid")),
                Q("acc_name", "accommodation", "Scene06", QuestionLevel.Relationship, QuestionMode.Choice,
                  "What is the continuous adjustment of the lens to focus objects at different distances called?", "Correct — accommodation.",
                  new[] { "Niko named it at the end of the lens demonstration.", "Changing pupil size and bending light at the cornea are different processes.", "Listen to Niko's line again." }, "replayCue:S06_L04",
                  O("Accommodation", true, ""), O("Changing the pupil size", false, "That's the iris's job, controlled by brightness.", null), O("Bending light at the cornea", false, "The cornea bends light but doesn't change shape.", null)),

                Q("img_orientation", "inverted_image", "Scene07", QuestionLevel.Recognition, QuestionMode.Choice,
                  "What kind of image does the lens form on the retina?", "Yes — a real, inverted image.",
                  new[] { "Look at the image where the rays meet the retina.", "Compare the apple's stem in the image with the real apple.", "Watch the image form again." }, "replayImage",
                  O("A real, inverted image", true, ""), O("An upright image, like a photograph", false, "Compare the stem: in the retinal image it points down.", null, "upright-retinal-image"), O("No image at all", false, "The rays meet to form an image on the retina.", null)),
                Q("img_perception", "inverted_image", "Scene09", QuestionLevel.Relationship, QuestionMode.Choice,
                  "The image on the retina is inverted. What does Niko say produces our visual experience?", "Yes — the brain processes the information carried by the signals.",
                  new[] { "Think about where the signals go.", "The lens forms the inverted image. What happens to the information afterwards?", "Listen to Niko's explanation again." }, "replayCue:S09_L04",
                  O("The brain processing the information carried by the signals", true, ""),
                  O("The lens turning the image back the right way", false, "The lens is what forms the inverted image.", null, "lens-reinverts"),
                  O("A photograph of the retinal image sent to the brain", false, "Niko said our visual experience is not simply a photograph of the retinal image.", null, "photograph-to-brain")),

                Q("rc_dim", "rods_cones", "Scene08", QuestionLevel.Recognition, QuestionMode.TapPart,
                  "Which photoreceptors help us see in dim conditions?", "Yes — rods are especially sensitive to low levels of light.",
                  new[] { "Remember the tall, rod-shaped cells.", "Cones work best in brighter light. Which cells are the opposite?", "Listen to Niko's explanation of rods again." }, "replayCue:S08_L02",
                  O("Rods", true, "", "rods"), O("Cones", false, "Cones work best in brighter light.", "cones", "cones-for-dim")),
                Q("rc_colour", "rods_cones", "Scene08", QuestionLevel.Recognition, QuestionMode.TapPart,
                  "Which cells allow us to see colours and fine details?", "Yes — cones.",
                  new[] { "Remember the cone-shaped cells.", "Rods mainly provide information about brightness rather than colour.", "Listen to Niko's explanation of cones again." }, "replayCue:S08_L04",
                  O("Cones", true, "", "cones"), O("Rods", false, "Rods mainly provide information about brightness rather than colour.", "rods", "rods-see-colour")),
                Q("rc_predict", "rods_cones", "Scene08", QuestionLevel.Prediction, QuestionMode.Choice,
                  "Prediction: in very dim light, which photoreceptors will respond better?", "Yes — rods respond well to low levels of light.",
                  new[] { "Think about which cells are sensitive to low light.", "Cones work best in brighter light.", "Compare the cells in dim light again." }, "replayCue:S08_L02",
                  O("Rods", true, ""), O("Cones", false, "Cones work best in brighter light.", null, "cones-for-dim")),

                Q("path_after_retina", "complete_pathway", "Scene10", QuestionLevel.Relationship, QuestionMode.Choice,
                  "When light reaches the photoreceptors, what happens next?", "Yes — it is converted into electrical signals.",
                  new[] { "Think about what the photoreceptors do with light.", "Light doesn't travel to the brain as light.", "Listen to Niko's line again." }, "replayCue:S08_L05",
                  O("It is converted into electrical signals", true, ""), O("It travels as light to the brain", false, "Light stops at the retina; signals travel on.", null, "light-travels-to-brain"),
                  O("It is stored as a picture in the eye", false, "The information is carried away as signals.", null, "image-stored")),
            };
            // Predictions: answered before the demonstration, then tested by it (trialAfterAnswer).
            list.Add(Q("img_predict", "inverted_image", "Scene07", QuestionLevel.Prediction, QuestionMode.Choice,
                "Prediction: the light from the apple is about to reach the retina. How will the image there look?", "Your prediction matched: the lens forms a real, inverted image on the retina.",
                new[] { "Follow the rays from the top of the apple through the lens.", "Rays from the top of the apple cross as they pass through the lens. Where do they end up?", "Watch the image form again." }, "replayImage",
                O("Upside down (inverted)", true, ""), O("The right way up, like a photograph", false, "Look at the stem: in the image on the retina it points down.", null, "upright-retinal-image"),
                O("No image, just a spot of light", false, "The rays meet on the retina and form an image of the apple.", null)));
            list.Add(Q("brain_predict", "optic_nerve", "Scene09", QuestionLevel.Prediction, QuestionMode.TapPart,
                "Prediction: the signals are about to reach the brain. Tap where you think they are processed for vision.", "Your prediction matched: visual information is processed in the visual area at the back of the brain.",
                new[] { "Follow the optic nerve all the way into the brain.", "The eyes are at the front, but the route ends far from them.", "Watch the signals travel again." }, "traceNerve",
                O("Visual area, at the back of the brain", true, "", "visual_area"),
                O("Elsewhere in the brain, such as the front", false, "Surprisingly, the visual area is at the back of the brain, far from the eyes.", "brain", "vision-at-front"),
                O("In the optic nerve itself", false, "The optic nerve carries the signals; the brain processes them.", "optic_nerve", "nerve-processes")));

            // Application: new situations, offered only once a topic is already Secure (challenge instead of repetition).
            list.Add(Q("app_ext_torch", "external_structures", "Scene02", QuestionLevel.Application, QuestionMode.Choice,
                "Challenge: a doctor shines a small torch into a patient's eye and the black centre shrinks. Which part made it shrink?", "Exactly: the iris changed the size of the pupil.",
                new[] { "The black centre is the opening.", "Something around the opening controls its size.", "Listen to Niko's explanation of the iris again." }, "replayCue:S02_L13",
                O("The iris", true, ""), O("The pupil", false, "The pupil is the opening that shrank; the iris around it changed its size.", null, "pupil-controls-itself"),
                O("The cornea", false, "The cornea is the clear front layer; it doesn't change size.", null, "cornea-controls-pupil")));
            list.Add(Q("app_int_clear", "internal_structures", "Scene03", QuestionLevel.Application, QuestionMode.Choice,
                "Challenge: an eye doctor looks through the pupil at the retina. Which clear parts inside the eye does the light pass through on the way?", "Exactly: the aqueous humour, the lens and the vitreous humour are all transparent.",
                new[] { "Think about the structures between the cornea and the retina.", "The sclera and choroid aren't transparent.", "Listen to Niko's explanation again." }, "replayCue:S03_L08",
                O("Aqueous humour, lens and vitreous humour", true, ""), O("Sclera and choroid", false, "The sclera is the tough white layer and the choroid holds blood vessels; light doesn't pass through them to the retina.", null, "choroid-in-light-path"),
                O("Only the lens", false, "The lens is clear, but so are the fluids in front of and behind it.", null)));
            list.Add(Q("app_nerve_damage", "optic_nerve", "Scene09", QuestionLevel.Application, QuestionMode.Choice,
                "Challenge: imagine an eye focuses light perfectly, but its optic nerve is damaged. What goes wrong?", "Exactly: the image still forms, but the information can't travel properly to the brain.",
                new[] { "Think about what the optic nerve carries.", "The lens and retina still work in this example.", "Watch the signals travel again." }, "traceNerve",
                O("The visual information can't travel properly to the brain", true, ""),
                O("The lens can no longer form an image", false, "The lens still forms the image on the retina; the problem is carrying the signals onward.", null, "lens-to-brain"),
                O("Nothing: the eye sees on its own", false, "Seeing needs the brain to process the signals the optic nerve carries.", null, "eye-sees-alone")));
            list.Add(Q("app_pupil_cinema", "pupil_response", "Scene05", QuestionLevel.Application, QuestionMode.Choice,
                "Challenge: you walk from a sunny playground into a dark cinema hall. What do your pupils do?", "Exactly: they become larger, letting more light in.",
                new[] { "Is it brighter or dimmer inside?", "Think about the brightness experiment: what happened in dim light?", "Watch the pupil respond again." }, "pupilTrial",
                O("Become larger, letting more light in", true, ""), O("Become smaller", false, "Smaller pupils happen in bright light; the cinema is dim.", null, "pupil-dilates-in-bright"),
                O("Stay the same size", false, "The pupil keeps adjusting to the light level.", null, "pupil-fixed")));
            list.Add(Q("app_acc_clock", "accommodation", "Scene06", QuestionLevel.Application, QuestionMode.Choice,
                "Challenge: you look up from your phone to read a clock on the far wall. How does your lens change?", "Exactly: for the distant clock the lens becomes less curved.",
                new[] { "Is the clock nearer or farther than the phone?", "Near objects need a more curved lens; distant ones the opposite.", "Watch the near and distant demonstrations again." }, "replayAccommodation",
                O("It becomes less curved", true, ""), O("It becomes more curved", false, "That's what happens when you look at something near.", null, "far-rounder"),
                O("It doesn't change", false, "The lens keeps adjusting to the distance.", null, "lens-rigid")));
            list.Add(Q("app_img_world", "inverted_image", "Scene10", QuestionLevel.Application, QuestionMode.Choice,
                "Challenge: the image on your retina is inverted, yet the world never looks upside down to you. Why?", "Exactly: the brain processes the information carried by the signals.",
                new[] { "Think about where the signals go.", "The retinal image isn't sent to the brain like a photograph.", "Listen to Niko's explanation again." }, "replayCue:S09_L04",
                O("The brain processes the information carried by the signals", true, ""),
                O("The cornea flips the image the right way up", false, "The cornea bends light; the lens forms an inverted image.", null, "lens-reinverts"),
                O("The image on the retina is actually upright", false, "The retinal image is inverted; you saw the stem pointing down.", null, "upright-retinal-image")));
            list.Add(Q("app_rc_night", "rods_cones", "Scene08", QuestionLevel.Application, QuestionMode.Choice,
                "Challenge: at night, under a dim streetlight, it's hard to tell the colour of a parked car. Why?", "Exactly: in dim light mainly rods respond, and rods give brightness rather than colour.",
                new[] { "Which cells work well in dim light?", "Which cells let us see colour, and when do they work best?", "Listen to Niko's explanation of rods again." }, "replayCue:S08_L03",
                O("In dim light mainly rods respond, and rods give brightness rather than colour", true, ""),
                O("Cones work best in dim light", false, "Cones work best in brighter light.", null, "cones-for-dim"),
                O("The pupil becomes too small at night", false, "In dim light the pupil becomes larger.", null, "pupil-dilates-in-bright")));
            list.Add(Q("app_path_convert", "complete_pathway", "Scene10", QuestionLevel.Application, QuestionMode.Choice,
                "Challenge: light reaches the photoreceptors, but they can't convert it. Which step of seeing fails?", "Exactly: without electrical signals, nothing can travel along the optic nerve.",
                new[] { "What do photoreceptors do with light?", "Focusing happens before the retina; this step happens at it.", "Listen to Niko's line again." }, "replayCue:S08_L05",
                O("Turning light into electrical signals", true, ""), O("Focusing light with the lens", false, "Focusing happens earlier, at the lens.", null),
                O("Letting light in through the pupil", false, "The light already got in; it reached the photoreceptors.", null)));

            foreach (var id in new[] { "pupil_predict", "img_predict", "brain_predict" }) list.Find(i => i.id == id).trialAfterAnswer = true;
            return list;
        }

        // ---------------- SupplementalAdaptive: Ask Niko (local curated) ----------------
        static NikoKnowledge.Intent I(string id, IntentKind kind, string pattern, string target, string[] cues, string supplemental, string notYet = null) =>
            new() { id = id, kind = kind, pattern = pattern, target = target, cues = cues, supplemental = supplemental, notYet = notYet };

        public static NikoKnowledge.Intent[] Intents() => new[]
        {
            I("act_replay_demo", IntentKind.Action, @"\breplay (the )?(demo|demonstration)\b", "REPLAY", null, "Replaying the demonstration."),
            I("act_replay", IntentKind.Action, @"\b(replay|repeat|again|say that|one more time)\b", "replay", null, "Here it is again."),
            I("act_brighter", IntentKind.Action, @"\b(brighter|more light|lighter|bright)\b", "brighter", null, "Making it brighter — watch the pupil."),
            I("act_dimmer", IntentKind.Action, @"\b(dimmer|darker|less light|dim)\b", "dimmer", null, "Making it dimmer — watch the pupil."),
            I("act_near", IntentKind.Action, @"\b(near|closer|nearby)\b", "near", null, "Bringing the object closer."),
            I("act_far", IntentKind.Action, @"\b(far|distant|further|farther)\b", "distant", null, "Moving the object far away."),
            I("act_freeze", IntentKind.Action, @"\b(freeze|unfreeze|hold it)\b", "FREEZE", null, "Freezing the biology mid-action."),
            I("act_scan", IntentKind.Action, @"\b(scan|labels?)\b", "SCAN", null, "Scanning — identifying the structures."),
            I("act_inside", IntentKind.Action, @"\binside\b", "INSIDE", null, "Moving inside the eye."),
            I("act_reset", IntentKind.Action, @"\b(reset (the )?view|zoom out)\b", "resetView", null, "Resetting the view."),

            I("open_blur", IntentKind.OpenQuestion, @"\b(blurry|blur|blurred|can ?t focus|cannot focus|focus properly)\b", null, null,
              "That's the big question to think about! You've seen how the lens focuses light onto the retina — keep wondering what could change that. I'm not going to give away the answer."),

            I("c_seeing", IntentKind.Concept, @"\b(how (do|does) (we|you|i) see|light becomes? sight|how .*seeing work|complete process|whole journey)\b", null, new[] { "S10_L01", "S10_L02" }, null,
              "That's the whole investigation! Keep following the light — we'll put it all together at the end."),
            I("c_pupil_light", IntentKind.Concept, @"\bpupil\b.*\b(bright|dim|small|smaller|larger|bigger|light|size|change)\b|\b(bright|dim)\b.*\bpupil\b", "pupil", new[] { "S05_L06", "S05_L07", "S05_L08" }, null,
              "We'll test how the pupil responds to light when we follow light into the eye."),
            I("c_accommodation", IntentKind.Concept, @"\baccommodation\b|\blens\b.*\b(near|far|distant|curved|shape|focus)\b|\bfocus\b", "lens", new[] { "S06_L02", "S06_L03", "S06_L04" }, null,
              "We'll see how the lens focuses light when we follow light into the eye."),
            I("c_inverted", IntentKind.Concept, @"\b(upside down|inverted|flipped|flip)\b", null, new[] { "S07_L02", "S09_L04" }, null,
              "We'll see what kind of image forms inside the eye later on."),
            I("c_signals", IntentKind.Concept, @"\b(electrical|signals?|electric)\b", null, new[] { "S08_L05", "S08_L06", "S09_L01" }, null,
              "We'll find out what happens after light reaches the retina."),
            I("c_photoreceptors", IntentKind.Concept, @"\bphotoreceptors?\b|light.sensitive", null, new[] { "S03_L10", "S08_L01" }, null,
              "We'll look closely at the cells that detect light later on."),
            I("c_brain", IntentKind.Concept, @"\bbrain\b|\bvisual (processing )?area\b|\bperception\b", "visual_area", new[] { "S04_L07", "S09_L03" }, null,
              "We'll follow the information all the way to the brain soon."),

            I("p_sclera", IntentKind.Part, @"\bsclera\b|\bwhite (outer )?(layer|part)\b", "sclera", new[] { "S02_L11" }, "Let's look at the sclera."),
            I("p_cornea", IntentKind.Part, @"\bcornea\b|\bclear (front )?(layer|window|part)\b|\bfront layer\b", "cornea", new[] { "S02_L12" }, "Let's look at the cornea."),
            I("p_iris", IntentKind.Part, @"\biris\b|\bcolou?r(ed)? (part|ring)\b", "iris", new[] { "S02_L13" }, "Let's look at the iris."),
            I("p_pupil", IntentKind.Part, @"\bpupil\b|\bopening\b|\bblack (dot|circle|hole)\b", "pupil", new[] { "S02_L14" }, "Let's look at the pupil."),
            I("p_aqueous", IntentKind.Part, @"\baqueous\b|\bfluid\b", "aqueous", new[] { "S03_L02", "S03_L03" }, "Let's look at the aqueous humour.", "We'll find the aqueous humour when we look inside the eye."),
            I("p_lens", IntentKind.Part, @"\blens\b|\bcrystalline\b", "lens", new[] { "S03_L04", "S03_L05", "S03_L06" }, "Let's look at the crystalline lens.", "We'll meet the lens when we look inside the eye."),
            I("p_vitreous", IntentKind.Part, @"\bvitreous\b|\bjelly\b", "vitreous", new[] { "S03_L07", "S03_L08" }, "Let's look at the vitreous humour.", "We'll find the vitreous humour when we look inside the eye."),
            I("p_retina", IntentKind.Part, @"\bretina\b", "retina", new[] { "S03_L09", "S03_L10" }, "Let's look at the retina.", "We'll reach the retina when we look inside the eye."),
            I("p_choroid", IntentKind.Part, @"\bchoroid\b|\bblood vessels?\b", "choroid", new[] { "S03_L11" }, "Let's look at the choroid.", "We'll reach the choroid when we look inside the eye."),
            I("p_nerve", IntentKind.Part, @"\boptic\b|\bnerve\b|\bbundle\b", "optic_nerve", new[] { "S04_L05", "S04_L06" }, null, "We'll find out how the eye connects to the brain soon."),
            I("p_rods", IntentKind.Part, @"\brods?\b", "rods", new[] { "S08_L02", "S08_L03" }, null, "We'll zoom into the retina to meet the light-sensitive cells later."),
            I("p_cones", IntentKind.Part, @"\bcones?\b", "cones", new[] { "S08_L04" }, null, "We'll zoom into the retina to meet the light-sensitive cells later."),

            I("g_niko", IntentKind.Greeting, @"\b(who are you|your name|niko|what can you do)\b", null, null,
              "I'm Niko, your Vision Investigator Officer! I scan the subject, take you inside the eye, run the brightness and light experiments with you, and I can freeze or replay a demonstration."),
            I("g_hello", IntentKind.Greeting, @"\b(hi|hello|hey|thanks|thank you)\b", null, null, "Hello, investigator! Ask me about any part you've explored."),
            I("h_help", IntentKind.Help, @"\b(help|how do i|what do i do|what should i do|what now|what next|hint|stuck|lost)\b", null, null,
              "Tap the glowing parts, their labels or the Parts list to explore. The panel on the right shows what to do next, and you can ask me about anything you've explored so far."),
        };

        public static string[] Suggestions() => new[]
        {
            "*|What can you do?", "Scene02|What does the iris do?", "Scene02|Show me the cornea", "Scene03|What is the vitreous humour?",
            "Scene03|Where is the retina?", "Scene04|What is the optic nerve?", "Scene05|Make it brighter", "Scene05|Why does the pupil change size?",
            "Scene06|How does the lens focus near objects?", "Scene07|Why is the image upside down?", "Scene08|What do rods do?",
            "Scene09|How does the brain see?", "Scene10|How does seeing work?", "Scene11|Why might things look blurry?", "*|Replay that",
        };

        public const string Fallback = "I can only answer questions about what we're investigating in this observatory — the parts of the eye, light, the lens, the retina, signals and the brain. Try one of the suggestions!";
    }
}
