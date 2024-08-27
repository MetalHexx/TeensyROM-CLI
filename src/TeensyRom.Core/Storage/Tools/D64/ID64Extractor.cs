﻿using TeensyRom.Core.Storage.Entities;

namespace TeensyRom.Core.Storage.Tools.D64Extraction
{
    public interface ID64Extractor
    {
        void ClearOutputDirectory();
        ExtractionResult Extract(FileTransferItem d64Item);
    }
}