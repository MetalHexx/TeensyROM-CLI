using System.Reflection;
using TeensyRom.Cli.Commands.Chipsynth;

namespace TeensyRom.Cli.Tests
{
    public class GeneratePatchSettingsTests
    {
        [Fact]
        public void When_SourcePath_Empty_ValidationSucceeds()
        {
            //Arrrange
            var settings = new GeneratePatchesSettings
            {
                SourcePath = string.Empty
            };

            //Act
            var result = settings.Validate();
            
            //Assert
            result.Message.Should().BeNullOrEmpty();
        }

        [Fact]
        public void When_SourcePath_Valid_ValidationSucceeds()
        {
            //Arrrange
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;

            var settings = new GeneratePatchesSettings
            {
                SourcePath = Path.GetDirectoryName(assemblyLocation)!
            };

            //Act
            var result = settings.Validate();

            //Assert
            result.Message.Should().BeNullOrEmpty();
        }

        [Fact]
        public void When_SourcePath_Invalid_ValidationFails()
        {
            //Arrrange
            var settings = new GeneratePatchesSettings
            {
                SourcePath = "invalid"
            };

            //Act
            var result = settings.Validate();

            //Assert
            result.Message.Should().Contain("The source path 'invalid' does not exist.");
        }

        [Fact]
        public void When_TargetPath_Empty_ValidationSucceeds()
        {
            //Arrrange
            var settings = new GeneratePatchesSettings
            {
                TargetPath = string.Empty
            };

            //Act
            var result = settings.Validate();

            //Assert
            result.Message.Should().BeNullOrEmpty();
        }
        [Fact]
        public void When_TargetPath_IsNotRelative_ValidationFails()
        {
            //Arrrange
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;

            var settings = new GeneratePatchesSettings
            {
                SourcePath = Path.GetDirectoryName(assemblyLocation)!,
                TargetPath = Path.GetDirectoryName(assemblyLocation)!
            };

            //Act
            var result = settings.Validate();

            //Assert
            result.Message.Should().Contain($"The target path '{Path.GetDirectoryName(assemblyLocation)}' is not a relative path.");
        }

        [Fact]
        public void When_TargetPath_IsRelative_ValidationSucceeds()
        {
            //Arrrange
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;

            var settings = new GeneratePatchesSettings
            {
                SourcePath = Path.GetDirectoryName(assemblyLocation)!,
                TargetPath = "relative"
            };

            //Act
            var result = settings.Validate();

            //Assert
            result.Message.Should().BeNullOrEmpty();
        }

        [Fact]
        public void When_Clock_Empty_ValidationSucceeds()
        {
            //Arrrange
            var settings = new GeneratePatchesSettings
            {
                Clock = string.Empty
            };

            //Act
            var result = settings.Validate();

            //Assert
            result.Message.Should().BeNullOrEmpty();
        }

        [Fact]
        public void When_Clock_PAL_ValidationSucceeds()
        {
            //Arrrange
            var settings = new GeneratePatchesSettings
            {
                Clock = "PAL"
            };

            //Act
            var result = settings.Validate();

            //Assert
            result.Message.Should().BeNullOrEmpty();
        }

        [Fact]
        public void When_Clock_NTSC_ValidationSucceeds()
        {
            //Arrrange
            var settings = new GeneratePatchesSettings
            {
                Clock = "NTSC"
            };

            //Act
            var result = settings.Validate();

            //Assert
            result.Message.Should().BeNullOrEmpty();
        }

        [Fact]
        public void When_Clock_Invalid_ValidationFails()
        {
            //Arrrange
            var settings = new GeneratePatchesSettings
            {
                Clock = "invalid"
            };

            //Act
            var result = settings.Validate();

            //Assert
            result.Message.Should().Contain("The clock 'invalid' is not valid.  Must be 'PAL' or 'NTSC'.");
        }

        [Fact]
        public void When_Clock_LowerCase_ValidationSucceeds()
        {
            //Arrrange
            var settings = new GeneratePatchesSettings
            {
                Clock = "pal"
            };

            //Act
            var result = settings.Validate();

            //Assert
            result.Message.Should().BeNullOrEmpty();           
        }

        [Fact]
        public void When_Clock_MixedCase_ValidationSucceeds()
        {
            //Arrrange
            var settings = new GeneratePatchesSettings
            {
                Clock = "Pal"
            };

            //Act
            var result = settings.Validate();

            //Assert
            result.Message.Should().BeNullOrEmpty();
        }
    }
}