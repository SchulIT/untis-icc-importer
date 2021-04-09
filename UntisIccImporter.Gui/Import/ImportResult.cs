using SchulIT.IccImport.Response;

namespace UntisIccImporter.Gui.Import
{
    public class ImportResult
    {
        public bool WasSuccessful { get; private set; }

        public string Text { get; private set; }

        public IResponse Response { get; private set; }

        public ImportResult(bool wasSuccessful, string text, IResponse response)
        {
            WasSuccessful = wasSuccessful;
            Text = text;
            Response = response;
        }
    }
}
