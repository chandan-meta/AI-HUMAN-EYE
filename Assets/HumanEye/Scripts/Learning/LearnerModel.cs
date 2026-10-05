using System.Linq;

namespace HumanEye
{
    public enum TopicStatus { NotStarted, Seen, Practising, NeedsPractice, Secure }

    /// Inspectable mastery rules (no opaque score). Exposure (watching/hearing) never counts as mastery;
    /// only distinct items answered correctly on the first try without hints do.
    public static class LearnerModel
    {
        public static readonly string[] Topics =
            { "external_structures", "internal_structures", "optic_nerve", "pupil_response", "accommodation", "inverted_image", "rods_cones", "complete_pathway" };

        public static int SecureThreshold = 2;

        public static int UnassistedCorrect(ProgressData d, string topic) =>
            d.evidence.Where(e => e.topic == topic && e.correct && !e.assisted).Select(e => e.item).Distinct().Count();

        public static TopicStatus Status(ProgressData d, string topic, bool exposed)
        {
            if (UnassistedCorrect(d, topic) >= SecureThreshold) return TopicStatus.Secure;
            if (d.needsPractice.Contains(topic)) return TopicStatus.NeedsPractice;
            if (d.evidence.Any(e => e.topic == topic)) return TopicStatus.Practising;
            return exposed ? TopicStatus.Seen : TopicStatus.NotStarted;
        }

        /// Support after the k-th mistake on one item: 1 = direct attention + scaffolded retry,
        /// 2 = comparison + worked demonstration offer + assisted retry, 3+ = show answer, record needs practice.
        public static int SupportTier(int mistakes) => mistakes <= 0 ? 0 : mistakes >= 3 ? 3 : mistakes;

        /// Next item for a topic: never the item just asked, not one already secured, lowest unsecured level first;
        /// after a recent miss stay at (or drop to) the level that was missed.
        public static QuestionBank.Item Next(QuestionBank bank, ProgressData d, string topic, string lastItem)
        {
            var secured = d.evidence.Where(e => e.topic == topic && e.correct && !e.assisted).Select(e => e.item).ToHashSet();
            var practice = bank.items.Where(i => i.topic == topic && i.id != lastItem && i.level != QuestionLevel.Application).ToList();
            var pool = practice.Where(i => !secured.Contains(i.id)).ToList();
            if (pool.Count == 0) pool = practice;
            var lastMiss = d.evidence.LastOrDefault(e => e.topic == topic);
            var missedLevel = lastMiss != null && !lastMiss.correct ? bank.Get(lastMiss.item)?.level : null;
            return pool.OrderBy(i => missedLevel.HasValue && i.level > missedLevel.Value ? 1 : 0)
                       .ThenBy(i => i.level).ThenBy(i => i.id).FirstOrDefault();
        }

        /// Strong understanding: once a topic is Secure, the next check is one new-situation item instead of more of the same.
        public static QuestionBank.Item Challenge(QuestionBank bank, ProgressData d, string topic) =>
            bank.items.FirstOrDefault(i => i.topic == topic && i.level == QuestionLevel.Application &&
                                           !d.evidence.Any(e => e.item == i.id && e.correct && !e.assisted));

        /// After a first-try correct answer, a further item at the same or a lower level is repetition, so it is skipped.
        public static bool Repetitive(QuestionBank.Item previous, bool firstTryCorrect, QuestionBank.Item next) =>
            previous != null && firstTryCorrect && next.level <= previous.level;

        public static void Record(ProgressData d, QuestionBank.Item item, bool correct, bool assisted)
        {
            d.evidence.Add(new Evidence { item = item.id, topic = item.topic, correct = correct, assisted = assisted });
            if (UnassistedCorrect(d, item.topic) >= SecureThreshold) d.needsPractice.Remove(item.topic);
        }
    }
}
