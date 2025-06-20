using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection.Metadata;
using System.Windows;
using System.Windows.Input;

namespace Logavise
{
    public partial class MainWindow : Window
    {
        private int previousLineNumber;
        
        public ICommand OpenFileCommand { get; private set; }


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this; // For XAML binding to work

            OpenFileCommand = new RelayCommand(OpenFile);

            textEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
        }

        #region Private Methods

        private void OpenFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                using FileStream fs = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.ReadWrite);
                using StreamReader reader = new StreamReader(fs);
                textEditor.Text = reader.ReadToEnd();

            }
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

        #region Event Handlers

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

        #endregion

        private void consoleEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                Keyboard.Focus(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? textEditor : consoleLine);
                e.Handled = true;
            }
        }
    }
}

// 00:19:23.763 [1] (null) INFO  Equipment 1 - 00:19:23:759 - Update Printer Receiving Line 1 PandA 1 Enabled False using Tag: SCANNER_30_PRINTER_W_STATUS[1]
