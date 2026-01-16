namespace client.Classes
{
    public class ProgramShortcut
    {
        public string FilePath { get; set; } = "";
        public bool isWindowsApp { get; set; }

        public string name { get; set; } = "";

        public string Arguments { get; set; } = "";
        public string WorkingDirectory { get; set; } = MainPath.exeString;

        // Store either:
        // - relative path under config/<GroupName>/...  OR
        // - absolute path
        public string CustomIconPath { get; set; } = "";

        public ProgramShortcut() { }
    }
}
