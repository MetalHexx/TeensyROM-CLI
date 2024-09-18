using AutoFixture;
using TeensyRom.Core.Common;
using TeensyRom.Core.Storage.Entities;
using TeensyRom.Core.Storage.Services;
using FluentAssertions;

namespace TeensyRom.Core.Tests
{
    public class PlayHistoryTests
    {
        private IFixture _fixture = new Fixture();
        private ILaunchHistory _history = new LaunchHistory();

        [Fact]
        public void Given_FilesLoaded_When_FindExecuted_FileFound()
        {
            //Arrange
            var expectedFile = CreateFile<SongItem>("/path/to/file2");

            List<ILaunchableItem> items = 
            [
                CreateFile<SongItem>("/path/to/file1"),
                expectedFile,
                CreateFile<SongItem>("/path/to/file3")
            ];
            _history.Load(items);

            //Act
            var result = _history.Find("/path/to/file2");

            //Assert
            result.Should().BeEquivalentTo(expectedFile);
        }

        [Fact]
        public void Given_FilesLoaded_When_FindExecuted_FileNotFound()
        {
            //Arrange
            List<ILaunchableItem> items =
            [
                CreateFile<SongItem>("/path/to/file1"),
                CreateFile<SongItem>("/path/to/file2"),
                CreateFile<SongItem>("/path/to/file3")
            ];
            _history.Load(items);

            //Act
            var result = _history.Find("/path/to/file4");

            //Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Given_FilesLoaded_When_SetAsCurrentExecuted_CurrentIndexSet()
        {
            //Arrange
            List<ILaunchableItem> items =
            [
                CreateFile<SongItem>("/path/to/file1"),
                CreateFile<SongItem>("/path/to/file2"),
                CreateFile<SongItem>("/path/to/file3")
            ];
            _history.Load(items);

            //Act
            var index = _history.SetAsCurrent(items[1]);

            //Assert
            index.Should().Be(1);
        }

        [Fact]
        public void Given_FilesLoaded_When_GetPreviousExecuted_PreviousReturned()
        {
            //Arrange
            List<ILaunchableItem> items =
            [
                CreateFile<SongItem>("/path/to/file1"),
                CreateFile<SongItem>("/path/to/file2"),
                CreateFile<SongItem>("/path/to/file3")
            ];
            _history.Load(items);
            _history.SetAsCurrent(items[1]);

            //Act
            var previous = _history.GetPrevious(false);

            //Assert
            previous.Should().BeEquivalentTo(items[0]);
        }

        [Fact]
        public void Given_FilesLoaded_When_GetPreviousWithFilterExecuted_PreviousReturned()
        {
            //Arrange
            List<ILaunchableItem> items =
            [
                CreateFile<SongItem>("/path/to/file1.sid"),
                CreateFile<GameItem>("/path/to/file2.crt"),
                CreateFile<GameItem>("/path/to/file3.crt"),
                CreateFile<GameItem>("/path/to/file4.crt"),
                CreateFile<GameItem>("/path/to/file5.crt"),
                CreateFile<GameItem>("/path/to/file6.sid")
            ];
            _history.Load(items);
            _history.SetAsCurrent(items[5]);

            //Act
            var previous = _history.GetPrevious(false, [TeensyFileType.Sid]);

            //Assert
            previous.Should().BeEquivalentTo(items[0]);
        }

        [Fact]
        public void Given_FilesLoaded_AndFirstItemIsIndex_When_GetPreviousWithFilterExecuted_ReturnsNull()
        {
            //Arrange
            List<ILaunchableItem> items =
            [
                CreateFile<SongItem>("/path/to/file1.sid"),
                CreateFile<GameItem>("/path/to/file2.crt"),
                CreateFile<GameItem>("/path/to/file3.crt"),
                CreateFile<GameItem>("/path/to/file4.crt"),
                CreateFile<GameItem>("/path/to/file5.crt"),
                CreateFile<GameItem>("/path/to/file6.sid")
            ];
            _history.Load(items);
            _history.SetAsCurrent(items[0]);

            //Act
            var previous = _history.GetPrevious(false, [TeensyFileType.Sid]);

            //Assert
            previous.Should().BeNull();
        }

        [Fact]
        public void Given_FilesLoaded_AndFirstItemIsIndex_When_GetPreviousWithFilterExecuted_AndWrapAroundIsTrue_ReturnsLast()
        {
            //Arrange
            List<ILaunchableItem> items =
            [
                CreateFile<SongItem>("/path/to/file1.sid"),
                CreateFile<GameItem>("/path/to/file2.crt"),
                CreateFile<GameItem>("/path/to/file3.crt"),
                CreateFile<GameItem>("/path/to/file4.crt"),
                CreateFile<GameItem>("/path/to/file5.crt"),
                CreateFile<GameItem>("/path/to/file6.sid")
            ];
            _history.Load(items);
            _history.SetAsCurrent(items[0]);

            //Act
            var previous = _history.GetPrevious(true, [TeensyFileType.Sid]);

            //Assert
            previous.Should().BeEquivalentTo(items[5]);
        }

        [Fact]
        public void Given_FilesLoaded_When_GetNextExecuted_NextReturned()
        {
            //Arrange
            List<ILaunchableItem> items =
            [
                CreateFile<SongItem>("/path/to/file1"),
                CreateFile<SongItem>("/path/to/file2"),
                CreateFile<SongItem>("/path/to/file3")
            ];
            _history.Load(items);
            _history.SetAsCurrent(items[1]);

            //Act
            var next = _history.GetNext(false);

            //Assert
            next.Should().BeEquivalentTo(items[2]);
        }


        [Fact]
        public void Given_FilesLoaded_When_GetNextWithFilterExecuted_NextReturned()
        {
            //Arrange
            List<ILaunchableItem> items =
            [
                CreateFile<SongItem>("/path/to/file1.sid"),
                CreateFile<GameItem>("/path/to/file2.crt"),
                CreateFile<GameItem>("/path/to/file3.crt"),
                CreateFile<GameItem>("/path/to/file4.crt"),
                CreateFile<GameItem>("/path/to/file5.crt"),
                CreateFile<GameItem>("/path/to/file6.sid")
            ];
            _history.Load(items);
            _history.SetAsCurrent(items[0]);

            //Act
            var previous = _history.GetNext(false, [TeensyFileType.Sid]);

            //Assert
            previous.Should().BeEquivalentTo(items[5]);
        }

        [Fact]
        public void Given_FilesLoaded_AndLastItemIsIndex_When_GetNextWithFilterExecuted_ReturnsNull()
        {
            //Arrange
            List<ILaunchableItem> items =
            [
                CreateFile<SongItem>("/path/to/file1.sid"),
                CreateFile<GameItem>("/path/to/file2.crt"),
                CreateFile<GameItem>("/path/to/file3.crt"),
                CreateFile<GameItem>("/path/to/file4.crt"),
                CreateFile<GameItem>("/path/to/file5.crt"),
                CreateFile<GameItem>("/path/to/file6.sid")
            ];
            _history.Load(items);
            _history.SetAsCurrent(items[5]);

            //Act
            var previous = _history.GetNext(false, [TeensyFileType.Sid]);

            //Assert
            previous.Should().BeNull();
        }

        [Fact]
        public void Given_FilesLoaded_AndLastItemIsIndex_When_GetNextWithFilterExecuted_AndWrapAroundIsTrue_ReturnsFirst()
        {
            //Arrange
            List<ILaunchableItem> items =
            [
                CreateFile<SongItem>("/path/to/file1.sid"),
                CreateFile<GameItem>("/path/to/file2.crt"),
                CreateFile<GameItem>("/path/to/file3.crt"),
                CreateFile<GameItem>("/path/to/file4.crt"),
                CreateFile<GameItem>("/path/to/file5.crt"),
                CreateFile<GameItem>("/path/to/file6.sid")
            ];
            _history.Load(items);
            _history.SetAsCurrent(items[5]);

            //Act
            var previous = _history.GetPrevious(true, [TeensyFileType.Sid]);

            //Assert
            previous.Should().BeEquivalentTo(items[0]);
        }

        [Fact]
        public void Given_OnlyOneFileLoaded_When_GetNextExecuted_WithWrapTrue_ReturnsFile() 
        {
            //Arrange
            var expectedFile = CreateFile<SongItem>("/path/to/file1");
            _history.Load([expectedFile]);
            _history.SetAsCurrent(expectedFile);

            //Act
            var next = _history.GetNext(true);

            //Assert
            next.Should().BeEquivalentTo(expectedFile);
        }

        [Fact]
        public void Given_OnlyOneFileLoaded_When_GetNextExecuted_WithWrapFalse_ReturnsNull()
        {
            //Arrange
            var file = CreateFile<SongItem>("/path/to/file1");
            _history.Load([file]);
            _history.SetAsCurrent(file);

            //Act
            var next = _history.GetNext(false);

            //Assert
            next.Should().BeNull();
        }



        private T CreateFile<T>(string path) where T : ILaunchableItem
        {
            var name = path.GetFileNameFromPath();
            return _fixture.Build<T>()
                .With(s => s.Name, $"{name}")
                .With(s => s.Path, $"{path}")
                .With(s => s.IsCompatible, true)
                .Create();
        }
    }
}