using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    [Serializable]
    public sealed class AcademyTutorialLesson
    {
        public string id, category, title, phase, prompt, goal, criterion, practiceKind, operation;
        public bool required, enabled;
        public string[] evidence = Array.Empty<string>();
    }

    [Serializable]
    public sealed class AcademyTutorialCatalog
    {
        public int version;
        public AcademyTutorialLesson[] lessons = Array.Empty<AcademyTutorialLesson>();
    }

    [Serializable]
    public sealed class AcademyTutorialRecord
    {
        public string id;
        public bool shown, previewed;
        public List<string> evidence = new List<string>();
    }

    // Teaching evidence has its own owner. It never grants rewards or changes the run.
    // Viewing, dismissing and skipping a lesson cannot create operation evidence.
    [Serializable]
    public sealed class AcademyTutorialProgress
    {
        public int version = 20261023;
        public bool enabled = true;
        public int highestPhase;
        public List<AcademyTutorialRecord> records = new List<AcademyTutorialRecord>();
        public List<string> legacyCompleted = new List<string>();
        public string legacyPending = "", legacyAction = "";
        public bool legacyReady;

        public AcademyTutorialRecord Find(string id) => records.FirstOrDefault(r => r.id == id);
        public AcademyTutorialRecord Get(string id)
        {
            var r = Find(id);
            if (r != null) return r;
            r = new AcademyTutorialRecord { id = id }; records.Add(r); return r;
        }
        public bool Observe(string id, string evidence)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(evidence)) return false;
            var record = Get(id);
            if (record.evidence.Contains(evidence)) return false;
            record.evidence.Add(evidence); return true;
        }
        public bool IsComplete(AcademyTutorialLesson lesson)
        {
            if (lesson == null || !lesson.enabled || lesson.evidence == null || lesson.evidence.Length == 0) return false;
            var record = Find(lesson.id);
            return record != null && lesson.evidence.All(record.evidence.Contains);
        }
        public int Completed(IEnumerable<AcademyTutorialLesson> lessons) => lessons.Count(IsComplete);
    }
}
