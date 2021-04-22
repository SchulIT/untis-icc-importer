using HtmlAgilityPack;
using SchulIT.IccImport;
using SchulIT.IccImport.Models;
using SchulIT.IccImport.Response;
using SchulIT.UntisExport;
using SchulIT.UntisExport.Model;
using SchulIT.UntisExport.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UntisIccImporter.Gui.Settings;

namespace UntisIccImporter.Gui.Import
{
    public class Importer : IImporter
    {
        #region Services

        private readonly IIccImporter importer;
        private readonly ISettingsManager settingsManager;

        #endregion

        public Importer(IIccImporter importer, ISettingsManager settingsManager)
        {
            this.importer = importer;
            this.settingsManager = settingsManager;
        }

        private void ConfigureImporter()
        {
            importer.BaseUrl = settingsManager.AppSettings.IccEndpoint;
            importer.Token = settingsManager.AppSettings.IccToken;
        }

        public async Task<ImportResult> ImportRoomsAsync(UntisExportResult result)
        {
            ConfigureImporter();

            var rooms = result.Rooms.Select(room =>
            {
                var period = room.Periods.Last();

                return new RoomData
                {
                    Name = period.Name,
                    Description = period.LongName,
                    Capacity = period.Capacity,
                    Id = period.Name
                };
            }).GroupBy(x => x.Name).Select(x => x.First()).ToList();

            var response = await importer.ImportRoomsAsync(rooms);
            return HandleResponse(response);
        }

        public async Task<ImportResult> ImportFreeLessonsAsync(UntisExportResult result, DateTime? startDate, DateTime? endDate)
        {
            ConfigureImporter();

            var free = result.Days.Where(x => startDate == null || endDate == null || (x.Date >= startDate && x.Date <= endDate))
                .SelectMany(day =>
                {
                    var freeLessons = day.FreeLessons.ToArray();
                    var spans = new List<FreeLessonTimespanData>();

                    if (freeLessons.Length > 0)
                    {
                        var currentSpan = new FreeLessonTimespanData
                        {
                            Date = day.Date,
                            Start = freeLessons[0],
                            End = freeLessons[0]
                        };

                        for (int i = 1; i < freeLessons.Length; i++)
                        {
                            var currentLesson = freeLessons[i];
                            if (currentSpan.End == currentLesson - 1) // append current span
                            {
                                currentSpan.End++;
                            }
                            else
                            {
                                spans.Add(currentSpan);
                                currentSpan = new FreeLessonTimespanData
                                {
                                    Date = day.Date,
                                    Start = currentLesson,
                                    End = currentLesson
                                };
                            }
                        }

                        if(currentSpan != null)
                        {
                            spans.Add(currentSpan);
                        }
                    }

                    if(day.Type == DayType.Feiertag || day.Type == DayType.Unterrichtsfrei)
                    {
                        spans.Add(new FreeLessonTimespanData
                        {
                            Date = day.Date,
                            Start = result.Settings.NumberOfFirstLesson,
                            End = result.Settings.NumberOfLessonsPerDay
                        });
                    }

                    return spans;
                });

            var response = await importer.ImportFreeLessonTimespansAsync(free.ToList());
            return HandleResponse(response);
        }

        public async Task<ImportResult> ImportDayTextsAsync(UntisExportResult result, DateTime? startDate, DateTime? endDate)
        {
            ConfigureImporter();

            var infotexts = result.Days.Where(x => startDate == null || endDate == null || (x.Date >= startDate && x.Date <= endDate))
                .SelectMany(day =>
                {
                    var result = new List<InfotextData>();
                    
                    if(!string.IsNullOrEmpty(day.Note))
                    {
                        result.Add(new InfotextData
                        {
                            Date = day.Date,
                            Content = RemoveHtmlTags(day.Note)
                        });
                    }

                    foreach(var text in day.Texts)
                    {
                        var currentDate = new DateTime(text.StartDate.Ticks);

                        while (currentDate <= text.EndDate)
                        {
                            result.Add(new InfotextData
                            {
                                Date = currentDate,
                                Content = RemoveHtmlTags(text.Text)
                            });

                            currentDate = currentDate.AddDays(1);
                        }
                    }

                    return result;
                }).ToList();

            var response = await importer.ImportInfotextsAsync(infotexts);
            return HandleResponse(response);
        }

        public async Task<ImportResult> ImportAbsencesAsync(UntisExportResult result, DateTime? startDate, DateTime? endDate)
        {
            ConfigureImporter();

            var absences = result.Absences.Where(x => !x.IsInternal)
                .SelectMany(absence =>
                {
                    var absences = new List<AbsenceData>();
                    var currentDate = new DateTime(absence.Start.Ticks);

                    while(currentDate <= absence.End)
                    {
                        absences.Add(new AbsenceData
                        {
                            Date = currentDate,
                            LessonStart = currentDate == absence.Start ? absence.LessonStart : result.Settings.NumberOfFirstLesson,
                            LessonEnd = currentDate == absence.End ? absence.LessonEnd : result.Settings.NumberOfLessonsPerDay,
                            Objective = absence.Objective,
                            Type = GetAbsenceType(absence.Type)
                        });
                        currentDate = currentDate.AddDays(1);
                    }

                    return absences.Where(x => startDate == null || endDate == null || (startDate <= x.Date && x.Date <= endDate));
                }).ToList();

            var response = await importer.ImportAbsencesAsync(absences);
            return HandleResponse(response);
        }

        public async Task<ImportResult> ImportSubstitutionsAsync(UntisExportResult result, DateTime? startDate, DateTime? endDate, bool suppressNotifications)
        {
            ConfigureImporter();

            var substitutions = result.Substitutions.Where(x => startDate == null || endDate == null || (startDate <= x.Date && x.Date <= endDate))
                .Select(substitution =>
                {
                    var data = new SubstitutionData
                    {
                        Id = substitution.Number.ToString(),
                        Date = substitution.Date,
                        LessonStart = substitution.Lesson,
                        LessonEnd = substitution.Lesson,
                        StartsBefore = substitution.Type == SubstitutionType.Pausenaufsicht,
                        Subject = substitution.Subject,
                        ReplacementSubject = substitution.ReplacementSubject,
                        Rooms = substitution.Rooms,
                        ReplacementRooms = substitution.ReplacementRooms,
                        Grades = substitution.Grades.Distinct().ToList(),
                        ReplacementGrades = substitution.Grades.Distinct().ToList(),
                        Type = substitution.Type.ToString(),
                        Text = substitution.Text
                    };

                    if(string.IsNullOrEmpty(data.Subject))
                    {
                        data.Grades.Clear();
                    }

                    if(string.IsNullOrEmpty(substitution.ReplacementSubject))
                    {
                        data.ReplacementGrades.Clear();
                    }

                    if(!string.IsNullOrEmpty(substitution.Teacher))
                    {
                        data.Teachers.Add(substitution.Teacher);
                    }

                    if(!string.IsNullOrEmpty(substitution.ReplacementTeacher))
                    {
                        data.ReplacementTeachers.Add(substitution.ReplacementTeacher);
                    }

                    return data;
                }).Collapse().ToList();

            var events = result.Events
                .SelectMany(x =>
                {
                    var data = new List<SubstitutionData>();

                    var currentDate = new DateTime(x.StartDate.Ticks);

                    while(currentDate <= x.EndDate)
                    {
                        if (startDate != null && endDate != null && (startDate > currentDate || endDate < currentDate))
                        {
                            currentDate = currentDate.AddDays(1);
                            continue;
                        }

                        data.Add(new SubstitutionData
                        {
                            Id = $"{x.Number}-{currentDate:yyyymd}",
                            Date = currentDate,
                            Type = "Veranstaltung",
                            StartsBefore = false,
                            LessonStart = currentDate == x.StartDate ? x.StartLesson : result.Settings.NumberOfFirstLesson,
                            LessonEnd = currentDate == x.EndDate ? x.EndLesson : result.Settings.NumberOfLessonsPerDay,
                            Subject = null,
                            ReplacementSubject = null,
                            Teachers = x.Teachers,
                            ReplacementRooms = x.Rooms,
                            Text = x.Text,
                            ReplacementGrades = x.Grades
                        }); 

                        currentDate = currentDate.AddDays(1);
                    }

                    return data;
                }).ToList();

            var response = await importer.ImportSubstitutionsAsync(substitutions.Union(events).ToList(), suppressNotifications);
            return HandleResponse(response);
        }

        private string GetAbsenceType(AbsenceType type)
        {
            switch(type)
            {
                case AbsenceType.Grade:
                    return "study_group";

                case AbsenceType.Room:
                    return "room";

                case AbsenceType.Teacher:
                    return "teacher";
            }

            throw new Exception("Unknown absence type.");
        }

        private ImportResult HandleResponse(IResponse response)
        {
            if (response is ImportResponse)
            {
                var importResponse = response as ImportResponse;
                return new ImportResult(true, $"Import erfolgreich ({importResponse.AddedCount} Einträge hinzugefügt, {importResponse.UpdatedCount} Einträge aktualisiert, {importResponse.RemovedCount} Einträge gelöscht und {importResponse.IgnoredEntities.Count} Einträge ignoriert.", response);
            }
            else if (response is SuccessReponse)
            {
                return new ImportResult(true, "Import erfolgreich.", response);
            }
            else if (response is ErrorResponse)
            {
                return new ImportResult(false, "Import nicht erfolgreich.", response);
            }

            throw new Exception("Unknown response type.");
        }

        public async Task<ImportResult> ImportPeriodsAsync(UntisExportResult result, IEnumerable<Period> periods)
        {
            ConfigureImporter();

            var periodNames = periods.Select(x => x.Name);
            var periodData = new List<TimetablePeriodData>();

            foreach(var period in result.Periods)
            {
                var data = new TimetablePeriodData
                {
                    Id = period.Name,
                    Name = period.LongName,
                    Start = period.Start
                };

                if (period.Parent == null || result.Periods.LastOrDefault() == period)
                {
                    data.End = period.End;
                }

                var last = periodData.LastOrDefault();

                if(last != null && last.End == default)
                {
                    last.End = (new DateTime(period.Start.Ticks).AddDays(-1));
                }

                periodData.Add(data);
            }

            var response = await importer.ImportTimetablePeriodsAsync(periodData.Where(x => periodNames.Contains(x.Id)).ToList());
            return HandleResponse(response);
        }

        public async Task<ImportResult> ImportSupervisionsAsync(UntisExportResult result, Period period)
        {
            ConfigureImporter();

            var supervisions = new List<TimetableSupervisionData>();

            var weeks = WeekResolver.SchoolWeekToCalendarWeek(result);
            var periodWeeks = ComputePeriodWeeks(result, weeks, period);

            foreach (var floor in result.SupervisionFloors)
            {
                foreach(var supervision in floor.Supervisions)
                {
                    var supervisionWeeks = weeks
                        .Where(x => supervision.Weeks.Contains(x.SchoolYearWeek))
                        .Select(x => x.CalendarWeek)
                        .Where(x => periodWeeks.Contains(x))
                        .ToList();

                    if(supervisionWeeks.Any())
                    {
                        var data = new TimetableSupervisionData
                        {
                            Id = Guid.NewGuid().ToString(),
                            Day = supervision.Day,
                            Weeks = supervisionWeeks,
                            IsBefore = true,
                            Lesson = supervision.Lesson,
                            Teacher = supervision.Teacher,
                            Location = floor.Name
                        };

                        supervisions.Add(data);
                    }
                }
            }

            var response = await importer.ImportSupervisionsAsync(period.Name, supervisions);
            return HandleResponse(response);
        }

        public async Task<ImportResult> ImportTimetableAsync(UntisExportResult result, Period period)
        {
            ConfigureImporter();

            var lessons = new List<TimetableLessonData>();
            var subjectReplacementMap = settingsManager.AppSettings.SubjectOverrides.ToDictionary(x => x.UntisSubject, x => x.NewSubject);

            var bereit = result.Tuitions.Where(x => x.Periods.Any(y => y.Subject == "Bereit")).ToList();

            var allowedWeeks = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P" }; // Untis only allows a periodicity of 16

            foreach(var tuition in result.Tuitions)
            {
                foreach(var tuitionPeriod in tuition.Periods)
                {
                    if(tuitionPeriod.PeriodNumber != period.Number)
                    {
                        continue;
                    }

                    // Check if time frame is withing current period
                    if(tuitionPeriod.EndDate < period.Start || tuitionPeriod.StartDate > period.End)
                    {
                        continue;
                    }

                    foreach(var timetable in tuitionPeriod.Timetable)
                    {
                        var weeks = !string.IsNullOrEmpty(timetable.Week) ? new string[] { timetable.Week } : tuitionPeriod.TuitionGroups;

                        foreach (var week in weeks)
                        {
                            if (!allowedWeeks.Contains(week) || timetable.Day == 0 || timetable.Lesson == 0 || string.IsNullOrEmpty(tuitionPeriod.Subject))
                            {
                                // ??!?
                                continue;
                            }

                            var lesson = new TimetableLessonData
                            {
                                Day = timetable.Day,
                                Lesson = timetable.Lesson,
                                IsDoubleLesson = false,
                                Room = timetable.Room,
                                Week = week,
                                Id = Guid.NewGuid().ToString(),
                                Grades = tuitionPeriod.Grades,
                                Subject = tuitionPeriod.Subject
                            };

                            if (!string.IsNullOrEmpty(tuitionPeriod.Teacher))
                            {
                                lesson.Teachers.Add(tuitionPeriod.Teacher);
                            }

                            var last = lessons.LastOrDefault();

                            if (last != null && last.Day == lesson.Day && last.Room == lesson.Room && last.Week == lesson.Week && last.Subject == lesson.Subject && last.IsDoubleLesson == false && last.Lesson == lesson.Lesson - 1)
                            {
                                last.IsDoubleLesson = true;
                                continue;
                            }

                            lessons.Add(lesson);
                        }
                    }
                }
            }

            var splitLessons = new List<TimetableLessonData>();

            foreach (var lesson in lessons)
            {
                if(lesson.Grades.Count == 0)
                {
                    continue;
                }

                if (settingsManager.AppSettings.SplitCourses.Any(x => x.Subject == lesson.Subject && lesson.Grades.Contains(x.Grade)))
                {
                    var firstGrade = lesson.Grades.First();
                    var grades = lesson.Grades.Skip(1).ToList();
                    lesson.Grades = new List<string> { firstGrade };

                    foreach(var grade in grades)
                    {
                        var newLesson = new TimetableLessonData
                        {
                            Day = lesson.Day,
                            Lesson = lesson.Lesson,
                            IsDoubleLesson = lesson.IsDoubleLesson,
                            Room = lesson.Room,
                            Week = lesson.Week,
                            Id = Guid.NewGuid().ToString(),
                            Grades = new List<string> { grade },
                            Subject = lesson.Subject,
                            Teachers = lesson.Teachers
                        };

                        splitLessons.Add(newLesson);
                    }
                }
            }

            lessons.AddRange(splitLessons);

            foreach(var lesson in lessons)
            {
                // Replace subject if necessary
                if (subjectReplacementMap.ContainsKey(lesson.Subject))
                {
                    lesson.Subject = subjectReplacementMap[lesson.Subject];
                }
            }

            var response = await importer.ImportTimetableLessonsAsync(period.Name, lessons);
            return HandleResponse(response);
        }

        public async Task<ImportResult> ImportExamsAsync(UntisExportResult result, DateTime? startDate, DateTime? endDate, bool suppressNotifications)
        {
            ConfigureImporter();

            var exams = result.Exams.Where(x => startDate == null || endDate == null || (startDate <= x.Date && x.Date <= endDate))
                .Select(exam =>
                {
                    var period = result.Periods.Reverse<Period>().FirstOrDefault(p => p.Start <= exam.Date);

                    if(period == null)
                    {
                        return null;
                    }

                    var tuitions = exam.Courses.Select(course =>
                    {
                        return ResolveExamTuition(course, result.Tuitions, period);
                    });

                    var students = new List<string>();

                    if (settingsManager.AppSettings.AlwaysIncludeStudents)
                    {
                        students.AddRange(exam.Students);

                        if (!string.IsNullOrEmpty(settingsManager.AppSettings.ExcludeRegExp) && Regex.IsMatch(exam.Name, settingsManager.AppSettings.ExcludeRegExp))
                        {
                            students.Clear();
                        }
                    }
                    else if (!string.IsNullOrEmpty(settingsManager.AppSettings.ExcludeRegExp) && Regex.IsMatch(exam.Name, settingsManager.AppSettings.ExcludeRegExp))
                    {
                        students.AddRange(exam.Students);
                    }

                    return new ExamData
                    {
                        Id = exam.Number.ToString(),
                        Date = exam.Date,
                        LessonStart = exam.LessonStart,
                        LessonEnd = exam.LessonEnd,
                        Description = exam.Text,
                        Tuitions = tuitions.Where(x => x != null).ToList(),
                        Students = students,
                        Supervisions = exam.Supervisions.ToList(),
                        Rooms = exam.Rooms.ToList()
                    };
                }).Where(x => x != null).ToList();

            var response = await importer.ImportExamsAsync(exams, suppressNotifications);
            return HandleResponse(response);
        }

        private static ExamTuitionData ResolveExamTuition(ExamCourse course, List<Tuition> tuitions, Period period)
        {
            foreach(var tuition in tuitions)
            {
                foreach(var tuitionPeriod in tuition.Periods)
                {
                    if(tuitionPeriod.TuitionNumber == course.TuitionNumber && tuitionPeriod.Subject == course.CourseName && tuitionPeriod.PeriodNumber == period.Number)
                    {
                        return new ExamTuitionData
                        {
                            Grades = tuitionPeriod.Grades.Distinct().ToList(),
                            SubjectOrCourse = tuitionPeriod.Subject,
                            Teachers = new List<string> { tuitionPeriod.Teacher }
                        };
                    }
                }
            }

            return null;
        }

        private static List<int> ComputePeriodWeeks(UntisExportResult result, List<Week> weeks, Period period)
        {
            var calendarWeeks = new List<int>();

            var followupPeriod = result.Periods.FirstOrDefault(x => x.Number == period.Number + 1);
            var startDate = new DateTime(period.Start.Ticks);
            var endDate = new DateTime((followupPeriod != null ? followupPeriod.Start : period.End).Ticks);

            while (startDate < endDate)
            {
                var calendarWeek = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(startDate, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                calendarWeeks.Add(calendarWeek);

                startDate = startDate.AddDays(7);
            }

            return calendarWeeks;
        }

        private static string RemoveHtmlTags(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc.DocumentNode.InnerText;
        }

        
    }
}
