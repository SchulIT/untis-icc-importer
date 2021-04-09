using SchulIT.IccImport.Models;
using System.Collections.Generic;
using System.Linq;

namespace UntisIccImporter.Gui.Import
{
    public static class SubstitutionCollapser
    {
        public static IEnumerable<SubstitutionData> Collapse(this IEnumerable<SubstitutionData> substitutions)
        {
            var result = new List<SubstitutionData>();

            var sortedSubstitutions = substitutions.OrderBy(x => x.Date).ThenBy(x => x.LessonStart).ToList();

            foreach(var substitution in sortedSubstitutions)
            {
                var existingSubstitution = result.FirstOrDefault(x =>
                    x.Date == substitution.Date
                    && x.Subject == substitution.Subject
                    && x.ReplacementSubject == substitution.ReplacementSubject
                    && x.Text == substitution.Text
                    && AreEqual(x.Teachers, substitution.Teachers)
                    && AreEqual(x.ReplacementTeachers, substitution.ReplacementTeachers)
                    && AreEqual(x.Grades, substitution.Grades)
                    && AreEqual(x.ReplacementGrades, substitution.ReplacementGrades)
                    && AreEqual(x.Rooms, substitution.Rooms)
                    && AreEqual(x.ReplacementRooms, substitution.ReplacementRooms)
                );

                if(existingSubstitution != null && existingSubstitution.LessonEnd == substitution.LessonStart - 1)
                {
                    existingSubstitution.LessonEnd = substitution.LessonStart;
                } else
                {
                    result.Add(substitution);
                }
            }

            return result;
        }

        private static bool AreEqual(IEnumerable<string> collectionA, IEnumerable<string> collectionB)
        {
            if(collectionA == null && collectionB == null)
            {
                return true;
            }

            if(collectionA == null && collectionB != null)
            {
                return false;
            }

            if(collectionA != null && collectionB == null)
            {
                return false;
            }

            if(collectionA.Count() != collectionB.Count())
            {
                return false;
            }

            return collectionA.Intersect(collectionB).Count() == collectionA.Count();
        }
    }
}
