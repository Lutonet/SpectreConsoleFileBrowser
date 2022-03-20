using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FileBrowser
{
    public class Browser
    {
        public bool DisplayIcons { get; set; } = true;
        public bool IsWindows { get; }
        public int PageSize { get; set; } = 15;
        public bool CanCreateFolder { get; set; } = true;
        public string ActualFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        public string SelectedFile { get; set; }
        public string LevelUpText { get; set; } = "Go to upper level";
        public string ActualFolderText { get; set; } = "Selected Folder";
        public string MoreChoicesText { get; set; } = "Use arrows Up and Down to select";
        public string CreateNewText { get; set; } = "Create new folder";
        public string SelectFileText { get; set; } = "Select File";
        public string SelectFolderText { get; set; } = "Select Folder";
        public string SelectDriveText { get; set; } = "Select Drive";
        public string SelectActualText { get; set; } = "Select Actual Folder";
        public string ConfirmTextSelectedFolder { get; set; } = "Do you want to select this folder?";
        public string ConfirmTextSelectedFile { get; set; } = "Do you want to select this file?";
        public string[] Drives { get; set; }
        public string lastFolder { get; set; }

        public Browser()
        {
            string OS = Environment.OSVersion.Platform.ToString();
            if (OS.Substring(0, 3).ToLower() == "win")
                IsWindows = true;
            lastFolder = ActualFolder;
        }

        public async Task<string> GetPath(string ActualFolder, bool SelectFile)
        {
            string lastFolder = ActualFolder;
            while (true)
            {
                string headerText = SelectFile ? SelectFileText : SelectFolderText;
                string[] directoriesInFolder;
                Directory.SetCurrentDirectory(ActualFolder);

                AnsiConsole.Clear();
                AnsiConsole.WriteLine();
                var rule = new Rule($"[b][green]{headerText}[/][/]").Centered();
                AnsiConsole.Write(rule);

                AnsiConsole.WriteLine();
                AnsiConsole.Markup($"[b][Yellow]{ActualFolderText}: [/][/]");
                var path = new TextPath(ActualFolder.ToString());
                path.RootStyle = new Style(foreground: Color.Green);
                path.SeparatorStyle = new Style(foreground: Color.Green);
                path.StemStyle = new Style(foreground: Color.Blue);
                path.LeafStyle = new Style(foreground: Color.Yellow);
                AnsiConsole.Write(path);
                AnsiConsole.WriteLine();

                Dictionary<string, string> folders = new Dictionary<string, string>();
                // get list of drives

                try
                {
                    directoriesInFolder = Directory.GetDirectories(Directory.GetCurrentDirectory());
                    lastFolder = ActualFolder;
                }
                catch
                {
                    if (ActualFolder == lastFolder) ActualFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    else ActualFolder = lastFolder;
                    Directory.SetCurrentDirectory(ActualFolder);
                    directoriesInFolder = Directory.GetDirectories(Directory.GetCurrentDirectory());
                }

                if (IsWindows)
                {
                    if (DisplayIcons)
                        folders.Add("[green]:computer_disk: " + SelectDriveText + "[/]", "/////");
                    else
                        folders.Add("[green]" + SelectDriveText + "[/]", "/////");
                }
                try
                {
                    if (new DirectoryInfo(ActualFolder).Parent != null)
                    {
                        if (DisplayIcons)
                            folders.Add("[green]:upwards_button: " + LevelUpText + "[/]", new DirectoryInfo(ActualFolder).Parent.FullName);
                        else
                            folders.Add("[green]" + LevelUpText + "[/]", new DirectoryInfo(ActualFolder).Parent.FullName);
                    }
                }
                catch { }
                if (!SelectFile)
                {
                    if (DisplayIcons)
                        folders.Add(":ok_button: [green]" + SelectActualText + "[/]", Directory.GetCurrentDirectory());
                    else
                        folders.Add("[green]" + SelectActualText + "[/]", Directory.GetCurrentDirectory());
                }
                if (CanCreateFolder)
                {
                    if (DisplayIcons)
                        folders.Add("[green]:plus: " + CreateNewText + "[/]", "///new");
                    else
                        folders.Add("[green]" + CreateNewText + "[/]", "///new");
                }
                foreach (var d in directoriesInFolder)
                {
                    int cut = 0;
                    if (new DirectoryInfo(ActualFolder).Parent != null) cut = 1;
                    string FolderName = d.Substring((ActualFolder.Length) + cut);
                    string FolderPath = d;
                    if (DisplayIcons) folders.Add(":file_folder: " + FolderName, FolderPath);
                    else folders.Add(FolderName, FolderPath);
                }

                if (SelectFile)
                {
                    var fileList = Directory.GetFiles(ActualFolder);
                    foreach (string file in fileList)
                    {
                        string fileName;
                        string result = Path.GetFileName(file);
                        if (DisplayIcons) folders.Add(":abacus: " + result, file);
                        else folders.Add(result, file);
                    }
                }
                // We got two sets of lists list files and list folders
                string title = SelectFile ? SelectFileText : SelectFolderText;
                var selected = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[green]{title}:[/]")
                    .PageSize(PageSize)
                    .MoreChoicesText($"[grey]{MoreChoicesText}[/]")
                    .AddChoices(folders.Keys)
                );
                lastFolder = ActualFolder;
                var record = folders.Where(s => s.Key == selected).Select(s => s.Value).FirstOrDefault();
                if (record == "/////")
                {
                    record = SelectDrive();
                    ActualFolder = record;
                }
                if (record == "///new")
                {
                    string folderName = AnsiConsole.Ask<string>("[blue]" + CreateNewText + ": [/]");
                    if (folderName != null)
                    {
                        try
                        {
                            Directory.CreateDirectory(folderName);
                            string newFolder = Path.Combine(ActualFolder, folderName);
                            record = newFolder;
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.WriteLine("[red]Error: [/]" + ex.Message);
                        }
                    }
                }
                string responseType;
                if (Directory.Exists(record)) responseType = "Directory";
                else responseType = "File";

                if (record == Directory.GetCurrentDirectory())
                    return ActualFolder;
                if (responseType == "Directory")
                    try
                    {
                        ActualFolder = record;
                    }
                    catch
                    {
                        AnsiConsole.WriteLine("[red]You have no access to this folder[/]");
                    }
                else
                    return record;
            }
        }

        public async Task<string> GetFilePath(string ActualFolder)
        {
            return await GetPath(ActualFolder, true);
        }

        public async Task<string> GetFilePath()
        {
            return await GetPath(ActualFolder, true);
        }

        public async Task<string> GetFolderPath(string ActualFolder)
        {
            return await
                GetPath(ActualFolder, false);
        }

        public async Task<string> GetFolderPath()
        {
            return await GetPath(ActualFolder, false);
        }

        private string SelectDrive()
        {
            Drives = Directory.GetLogicalDrives();
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (string drive in Drives)
            {
                if (DisplayIcons)
                    result.Add(":computer_disk: " + drive, drive);
                else
                    result.Add(drive, drive);
            }
            AnsiConsole.Clear();
            AnsiConsole.WriteLine();
            var rule = new Rule($"[b][green]{SelectDriveText}[/][/]").Centered();
            AnsiConsole.Write(rule);

            AnsiConsole.WriteLine();
            string title = SelectDriveText;
            var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[green]{title}:[/]")
                .PageSize(PageSize)
                .MoreChoicesText($"[grey]{MoreChoicesText}[/]")
                .AddChoices(result.Keys)
            );
            var record = result.Where(s => s.Key == selected).Select(s => s.Value).FirstOrDefault();
            return record;
        }
    }
}