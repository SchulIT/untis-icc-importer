using SchulIT.UntisExport;
using SchulIT.UntisExport.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UntisIccImporter.Gui.Import
{
    public interface IImporter
    {
        Task<ImportResult> ImportFreeLessonsAsync(UntisExportResult result, DateTime? startDate, DateTime? endDate);

        Task<ImportResult> ImportDayTextsAsync(UntisExportResult result, DateTime? startDate, DateTime? endDate);

        Task<ImportResult> ImportAbsencesAsync(UntisExportResult result, DateTime? startDate, DateTime? endDate);

        Task<ImportResult> ImportSubstitutionsAsync(UntisExportResult result, DateTime? startDate, DateTime? endDate, bool suppressNotifications);

        Task<ImportResult> ImportRoomsAsync(UntisExportResult result);

        Task<ImportResult> ImportPeriodsAsync(UntisExportResult result, IEnumerable<Period> periods);

        Task<ImportResult> ImportSupervisionsAsync(UntisExportResult result, Period period);

        Task<ImportResult> ImportTimetableAsync(UntisExportResult result, Period period);

        Task<ImportResult> ImportExamsAsync(UntisExportResult result, DateTime? startDate, DateTime? endDate, bool suppressNotifications);
    }
}