using Microsoft.Win32;
using LogParser;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Logavise
{
    public partial class MainWindow : Window
    {
        #region Private Fields

        private int previousLineNumber;
        
        private int editorFilesNameIndex = 1;
        private int prevSelectedTabIndex = 0;

        private readonly LogReader logReader = new() { TimeZone = TimeZoneInfo.Local };

        #endregion

        #region Public Properties

        public ICommand NewFileCommand { get; private set; }
        public ICommand OpenFileCommand { get; private set; }
        public ICommand OpenFileAsyncCommand { get; private set; }
        public ICommand SaveFileCommand { get; private set; }
        public ICommand SaveFileAsCommand { get; private set; }
        public ICommand SaveAllFilesCommand { get; private set; }
        public ICommand PreviousTabCommand { get; private set; }
        public ICommand NextTabCommand { get; private set; }
        public ICommand CloseFileCommand { get; private set; }
        public ICommand CloseAllFilesCommand { get; private set; }
        public ICommand ExitCommand { get; private set; }

        public ObservableCollection<TabModel> EditorTabs { get; private set; }

        #endregion

        #region Public Constructors

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this; // For XAML binding to work

            NewFileCommand = new RelayCommand(NewFile);
            OpenFileCommand = new RelayCommand(OpenFile);
            OpenFileAsyncCommand = new AsyncRelayCommand(OpenFileAsync);
            SaveFileCommand = new RelayCommand(SaveFile);
            SaveFileAsCommand = new RelayCommand(SaveFileAs);
            SaveAllFilesCommand = new RelayCommand(SaveAllFiles);
            PreviousTabCommand = new RelayCommand(GoToPreviousTab);
            NextTabCommand = new RelayCommand(GoToNextTab);
            CloseFileCommand = new RelayCommand(() => CloseFile(tabControl.SelectedIndex));
            CloseAllFilesCommand = new RelayCommand(CloseAllFiles);
            ExitCommand = new RelayCommand(Close); // close window

            EditorTabs = new ObservableCollection<TabModel>();
            tabControl.SelectedIndex = 0;

            textEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;

            logReader.Console.OnWriteLine += OnReaderConsoleWriteLine;

            NewFile();
        }

        #endregion

        #region Private Methods

        private void NewFile()
        {
            int newIndex = EditorTabs.Count;
            EditorTabs.Add(new TabModel() { Header = $"new {editorFilesNameIndex}", Index = newIndex, Text = "" });
            tabControl.SelectedIndex = newIndex; // this automatically saves the current file then switches to the new tab
            editorFilesNameIndex++;
        }

        private void OpenFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                using FileStream fs = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.ReadWrite);
                using StreamReader reader = new StreamReader(fs);

                int newIndex = EditorTabs.Count;
                EditorTabs.Add(new TabModel()
                {
                    Header = Path.GetFileName(openFileDialog.FileName),
                    Index = newIndex,
                    Text = reader.ReadToEnd(),
                    FileName = openFileDialog.FileName
                });
                tabControl.SelectedIndex = newIndex; // this automatically saves the current file then opens this file
            }
        }

        private async Task OpenFileAsync()
        {
            string fileName = MainWindow.GetFileName();

            if (fileName == null) return;

            else
            {
                using FileStream stream = new FileStream(fileName, FileMode.Open,
                    FileAccess.ReadWrite, FileShare.ReadWrite, 65536, useAsync: true);
                using StreamReader reader = new StreamReader(stream);

                EditorTabs.Add(new TabModel(fileName, index: EditorTabs.Count));
                tabControl.SelectedIndex = EditorTabs.Count - 1;
                textEditor.Text = await reader.ReadToEndAsync();
            };
        }

        private void SaveFile()
        {
            // TODO: Implement saving a file to a specified filepath
            //       Should automatically run Save As for a new file and Save for an existing file
            var tab = EditorTabs[tabControl.SelectedIndex];
            string filepath;
            if (tab.FileName == null)
            {
                filepath = GetSaveFileName();
                tab.FileName = filepath;
            }
            else
            {
                filepath = tab.FileName;
            }

            EditorTabs[tabControl.SelectedIndex].Text = textEditor.Text;

            if (filepath != null)
            {
                File.WriteAllText(filepath, tab.Text);
                string filename = Path.GetFileName(filepath);
                if (filename != tab.Header)
                {
                    tab.Header = filename;
                }

                EditorTabs[tabControl.SelectedIndex].IsModified = false;
            }
        }

        private void SaveFileAs()
        {
            var tab = EditorTabs[tabControl.SelectedIndex];
            tab.FileName = null;
            
            SaveFile();
        }

        private void SaveAllFiles()
        {
            // TODO: make saving a file take an argument of which index file to save
            int prevIndex = tabControl.SelectedIndex;
            for (int i = 0; i < EditorTabs.Count; i++)
            {
                tabControl.SelectedIndex = i;
                SaveFile();
            }
            tabControl.SelectedIndex = prevIndex;
        }

        private string GetSaveFileName()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = $"Save File {EditorTabs[tabControl.SelectedIndex].Header} As",
                Filter = "Text Documents (*.txt)|*.txt|Log Files (*.log)|*.log|All Files (*.*)|*.*",
                DefaultExt = "txt",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                OverwritePrompt = true,
                AddExtension = true
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                return saveFileDialog.FileName;
            }

            return null;
        }

        private void GoToPreviousTab()
        {
            int index = tabControl.SelectedIndex;
            tabControl.SelectedIndex = (index == 0) ? EditorTabs.Count - 1 : index - 1;
        }

        private void GoToNextTab()
        {
            int index = tabControl.SelectedIndex;
            tabControl.SelectedIndex = (index + 1) % EditorTabs.Count;
        }

        private void CloseFile(int index)
        {
            if (EditorTabs.Count > 1)
            {
                foreach (var tab in EditorTabs)
                {
                    if (tab.Index > index)
                    {
                        tab.Index--;
                    }
                }

                if (tabControl.SelectedIndex == index)
                {
                    StoreFile();
                    LoadFile((index + 1) == EditorTabs.Count ? index - 1 : (index + 1) % EditorTabs.Count);
                }
                if (prevSelectedTabIndex >= index)
                {
                    prevSelectedTabIndex--;
                }
                EditorTabs.RemoveAt(index); // this has to come last because it kicks off tabControl_SelectionChanged.
            }
        }

        private void CloseAllFiles()
        {
            NewFile();
            while (EditorTabs.Count > 1)
            {
                CloseFile(0);
            }
        }

        private void StoreFile()
        {
            if (textEditor.Text != "")
            {
                // store previous tab's text
                EditorTabs[prevSelectedTabIndex].Text = textEditor.Text;
            }
        }

        private void LoadFile(int index)
        {
            textEditor.Text = EditorTabs[index].Text;
        }

        private void SendConsoleLine(string text)
        {
            int lineNumber = consoleEditor.TextArea.Caret.Line;
            var currentLine = consoleEditor.Document.GetLineByNumber(lineNumber);
            string lineText = consoleEditor.Document.GetText(currentLine);

            if (lineText != "")
            {
                consoleEditor.Select(currentLine.Offset, currentLine.EndOffset - currentLine.Offset);
                consoleEditor.Delete();
            }
            consoleEditor.AppendText(text + Environment.NewLine);
            consoleEditor.ScrollToEnd();
        }

        #endregion

        #region Private Static Methods

        private static string GetFileName()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }
            else return null;
        }

        #endregion

        #region Private Event Handlers

        private void Caret_PositionChanged(object sender, EventArgs e)
        {
            int lineNumber = textEditor.TextArea.Caret.Line;

            if (lineNumber != previousLineNumber)
            {
                var currentLine = textEditor.Document.GetLineByNumber(lineNumber);
                string lineText = textEditor.Document.GetText(currentLine);

                // do something ..

                previousLineNumber = lineNumber;
            }
        }

        private void consoleLine_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (consoleLine.Text != "")
                {
                    SendConsoleLine(consoleLine.Text);
                    consoleLine.Clear();
                }
            }
        }

        private void consoleEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                Keyboard.Focus(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? textEditor : consoleLine);
                e.Handled = true;
            }
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            int index = (int)(sender as Button).Tag;
            CloseFile(index);
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int newIndex = (sender as TabControl).SelectedIndex;
            if (newIndex >= 0)
            {
                if (prevSelectedTabIndex >= 0 && newIndex != prevSelectedTabIndex)
                {
                    StoreFile();
                }
                LoadFile(newIndex);
            }

            prevSelectedTabIndex = tabControl.SelectedIndex;
            Keyboard.Focus(textEditor);
        }

        private void textEditor_TextChanged(object sender, EventArgs e)
        {
            var tab = EditorTabs[tabControl.SelectedIndex];
            if (!tab.IsModified && tab.Text != textEditor.Text)
            {
                tab.IsModified = true;
            }
            //else if (tab.IsModified && tab.Text == textEditor.Text)
            //{
            //    tab.IsModified = false;
            //}
        }

        private void OnReaderConsoleWriteLine(object sender, LogReaderEventArgs e)
        {
            consoleEditor.AppendText(e.Line + Environment.NewLine);
            consoleEditor.ScrollToEnd();
        }

        private void OnProcessButtonClicked(object sender, RoutedEventArgs e)
        {
            string fileName = EditorTabs[tabControl.SelectedIndex].FileName;
            if (fileName != null)
            {
                logReader.ProcessFile(fileName);
            }
        }

        #endregion
    }
}

// 00:19:23.763 [1] (null) INFO  Equipment 1 - 00:19:23:759 - Update Printer Receiving Line 1 PandA 1 Enabled False using Tag: SCANNER_30_PRINTER_W_STATUS[1]
