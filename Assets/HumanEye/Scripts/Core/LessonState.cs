using System.Collections.Generic;

namespace HumanEye
{
    public enum LessonState { Entering, Cinematic, Narrating, AwaitingAction, Exploring, SupplementalCheck, Transitioning, Complete }

    /// Valid state transitions and what learner input each state allows.
    public static class LessonRules
    {
        static readonly LessonState[] Flow =
            { LessonState.Cinematic, LessonState.Narrating, LessonState.AwaitingAction, LessonState.Exploring, LessonState.SupplementalCheck, LessonState.Transitioning };

        static readonly Dictionary<LessonState, LessonState[]> Next = new()
        {
            [LessonState.Entering] = Flow,
            [LessonState.Cinematic] = Flow,
            [LessonState.Narrating] = Flow,
            [LessonState.AwaitingAction] = Flow,
            [LessonState.Exploring] = Flow,
            [LessonState.SupplementalCheck] = Flow,
            [LessonState.Transitioning] = new[] { LessonState.Complete },
            [LessonState.Complete] = new LessonState[0],
        };

        /// Entering (chapter start, restart from checkpoint) is reachable from every state.
        public static bool CanMove(LessonState from, LessonState to) =>
            to == LessonState.Entering || from == to || System.Array.IndexOf(Next[from], to) >= 0;

        public static bool WorldSelect(LessonState s) => s is LessonState.Exploring or LessonState.SupplementalCheck;
        public static bool Orbit(LessonState s) => s is LessonState.Exploring or LessonState.AwaitingAction or LessonState.SupplementalCheck;
        public static bool FreeDemo(LessonState s) => s is LessonState.Exploring or LessonState.AwaitingAction;
        public static bool ReplayExplanation(LessonState s) => s is LessonState.Exploring or LessonState.AwaitingAction or LessonState.SupplementalCheck or LessonState.Complete;
        public static bool AskNiko(LessonState s) => s is not (LessonState.Entering or LessonState.Transitioning);
    }
}
