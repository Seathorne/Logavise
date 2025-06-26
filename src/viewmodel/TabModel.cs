using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Logavise
{
    public class TabModel : INotifyPropertyChanged
    {
        private string _header;
        private int _index;
        private string _text;
        private string _filepath = null;
        private bool _isModified = false;

        public TabModel()
        {
        }

        public TabModel(string fileName, int index)
        {
            FileName = fileName;
            Header = Path.GetFileName(fileName);
            Index = index;
        }

        public string Header
        {
            get => _header;
            set
            {
                _header = value;
                OnPropertyChanged();
            }
        }

        public int Index
        {
            get => _index;
            set
            {
                _index = value;
                OnPropertyChanged();
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                OnPropertyChanged();
            }
        }

        public string FileName
        {
            get => _filepath;
            set
            {
                _filepath = value;
                OnPropertyChanged();
            }
        }

        public bool IsModified
        {
            get => _isModified;
            set
            {
                _isModified = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
