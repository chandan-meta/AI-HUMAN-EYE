using System.Text.RegularExpressions;
namespace VisionLab {
    public interface INikoBrain { string Resolve(string question); }
    public class ScriptedNikoBrain : INikoBrain {
        public string Resolve(string question) {
            if (string.IsNullOrWhiteSpace(question)) return null;
            foreach (var part in new[] { "sclera", "cornea", "iris", "pupil" })
                if (Regex.IsMatch(question, @"\b" + part + @"\b", RegexOptions.IgnoreCase)) return part;
            if (Regex.IsMatch(question, @"\b(white|shape|tough)\b", RegexOptions.IgnoreCase)) return "sclera";
            if (Regex.IsMatch(question, @"\b(curved|transparent|bend|bends)\b", RegexOptions.IgnoreCase)) return "cornea";
            if (Regex.IsMatch(question, @"\b(colou?r|colou?red|size)\b", RegexOptions.IgnoreCase)) return "iris";
            if (Regex.IsMatch(question, @"\b(opening|enters|hole)\b", RegexOptions.IgnoreCase)) return "pupil";
            return null;
        }
        public static string Alternative(string part) => part switch {
            "sclera" => " Think of the sclera as a tough protective shell that holds the eye's shape.",
            "cornea" => " Think of the cornea as a clear window that also bends light as it enters.",
            "iris" => " Think of the iris as a curtain that adjusts the pupil's opening.",
            _ => " The pupil is a gap, not a solid black part. Light passes through this opening."
        };
    }
}
