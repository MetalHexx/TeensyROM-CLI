using TeensyRom.Core.Storage.Entities;
using TeensyRom.Core.Storage.Services;

namespace TeensyRom.Cli.Services.Player
{
    internal interface IPlayerFileContext
    {
        void Add(ILaunchableItem fileItem);
        void Clear();
        void ClearForwardHistory();
        ILaunchableItem? GetNext(bool wrapAround, params TeensyFileType[] types);
        ILaunchableItem? GetPrevious(bool wrapAround, params TeensyFileType[] types);
        bool CurrentIsNew { get; }
        void Remove(ILaunchableItem fileItem);
        void Load(IEnumerable<ILaunchableItem> items);
        ILaunchableItem? Find(string path);
        int SetCurrentIndex(ILaunchableItem item);
        bool HasCompatibleFiles();
    }
    internal class PlayerFileContext : IPlayerFileContext
    {
        private readonly List<Tuple<int, ILaunchableItem>> _history = [];
        private int _currentIndex = -1;
        public bool CurrentIsNew { get; private set; }

        public void Add(ILaunchableItem fileItem)
        {
            if (_currentIndex < _history.Count - 1)
            {
                ClearForwardHistory();
            }
            _currentIndex = _history.Count;
            _history.Add(new(_currentIndex, fileItem));
            CurrentIsNew = true;
        }

        public void Remove(ILaunchableItem fileItem)
        {
            int index = _history.FindIndex(tuple => tuple.Item2 == fileItem);

            if (index == -1) return;

            _history.RemoveAt(index);

            if (index <= _currentIndex)
            {
                _currentIndex--;
                CurrentIsNew = false;
            }
        }

        public void Load(IEnumerable<ILaunchableItem> items)
        {
            _history.Clear();

            foreach (var item in items)
            {
                Add(item);
            }
        }

        public ILaunchableItem? Find(string path) => _history.FirstOrDefault(tuple => tuple?.Item2?.Path == path)?.Item2 ?? null;
        public int SetCurrentIndex(ILaunchableItem item)
        {
            var index = _history.FindIndex(tuple => tuple.Item2.Path == item.Path);
            _currentIndex = index;
            return _currentIndex;
        }

        public void Clear() => _history.Clear();

        public void ClearForwardHistory() => _history.RemoveRange(_currentIndex + 1, _history.Count - _currentIndex - 1);

        public ILaunchableItem? GetPrevious(bool wrapAround, params TeensyFileType[] types)
        {
            var filteredList = _history;

            if (types.Any())
            {
                filteredList = FilterByTypes(types);
            }
            int currentFilteredIndex = filteredList.FindLastIndex(tuple => tuple.Item1 < _currentIndex);

            if (wrapAround is false && currentFilteredIndex == -1) return null;

            if (wrapAround is true && currentFilteredIndex == -1)
            {
                currentFilteredIndex = filteredList.Count - 1;
            }
            if (currentFilteredIndex == -1) return null;

            _currentIndex = filteredList[currentFilteredIndex].Item1;
            CurrentIsNew = false;
            return filteredList[currentFilteredIndex].Item2;
        }

        public ILaunchableItem? GetNext(bool wrapAround, params TeensyFileType[] types)
        {
            var filteredList = _history;

            if (types.Any())
            {
                filteredList = FilterByTypes(types);
            }
            int currentFilteredIndex = filteredList.FindIndex(tuple => tuple.Item1 > _currentIndex);

            if (wrapAround is false && currentFilteredIndex == -1) return null;

            if (wrapAround is true && currentFilteredIndex == -1)
            {
                currentFilteredIndex = 0;
            }
            if (currentFilteredIndex == -1) return null;

            _currentIndex = filteredList[currentFilteredIndex].Item1;
            CurrentIsNew = false;
            return filteredList[currentFilteredIndex].Item2;
        }

        public bool HasCompatibleFiles() => _history.Any(f => f.Item2.IsCompatible);

        private List<Tuple<int, ILaunchableItem>> FilterByTypes(TeensyFileType[] types) => _history
            .Where(tuple => types.Contains(tuple.Item2.FileType))
            .ToList();
    }
}
