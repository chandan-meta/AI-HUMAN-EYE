using UnityEngine;

namespace HumanEye
{
    /// Things the storyboard leaves open (learner grade, session length, hardware) are configuration, not code.
    [CreateAssetMenu(menuName = "Human Eye/Module Config")]
    public class ModuleConfig : ScriptableObject
    {
        public string learnerGrade = "Not specified";
        [Tooltip("0 = self-paced, no session timer.")] public int suggestedSessionMinutes;
        public int pcTargetFps = 60, tabletTargetFps = 30;
        [Tooltip("Distinct unassisted correct items needed to mark a topic secure.")] public int secureThreshold = 2;
        [Tooltip("Quality level names in QualitySettings.")] public string pcQuality = "PC", tabletQuality = "Mobile";
    }
}
