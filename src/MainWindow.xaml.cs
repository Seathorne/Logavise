using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace Logavise
{
    public partial class MainWindow : Window
    {
        private int previousLineNumber;

        public ObservableCollection<RegexField> RegexFields { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this; // For XAML binding to work

            textEditor.AppendText("00:19:23.763 [1] (null) INFO  Equipment 1 - 00:19:23:759 - Update Printer Receiving Line 1 PandA 1 Enabled False using Tag: SCANNER_30_PRINTER_W_STATUS[1]");

            RegexFields = new ObservableCollection<RegexField>
            {
                new RegexField  // 11:54:58:379
                {
                    Pattern = @"^(\d{2}:\d{2}:\d{2}\.\d{3})",
                    FieldName = "Timestamped Message"
                },

                new RegexField  // 195
                {
                    Pattern = @"\[(\d+)\]",
                    FieldName = "Thread Number"
                },

                new RegexField  // INFO
                {
                    Pattern = @"(INFO|DEBUG|ERROR)",
                    FieldName = "Level"
                },

                new RegexField  // Equipment 5
                {
                    Pattern = @"Equipment (\d+)",
                    FieldName = "Equipment Number"
                },
            };

            textEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
        }

        #region Event Handlers

        private void Caret_PositionChanged(object sender, EventArgs e)
        {
            int lineNumber = textEditor.TextArea.Caret.Line;

            if (lineNumber != previousLineNumber)
            {
                var currentLine = textEditor.Document.GetLineByNumber(lineNumber);
                string lineText = textEditor.Document.GetText(currentLine);

                foreach (var regex in RegexFields)
                {
                    regex.Evaluate(lineText);
                }

                previousLineNumber = lineNumber;
            }
        }

        #endregion
    }
}

// 00:19:23.763 [1] (null) INFO  Equipment 1 - 00:19:23:759 - Update Printer Receiving Line 1 PandA 1 Enabled False using Tag: SCANNER_30_PRINTER_W_STATUS[1]
