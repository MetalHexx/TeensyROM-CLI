class TeensyromCli < Formula
  desc "TeensyROM CLI tool"
  homepage "https://github.com/MetalHexx/TeensyROM-CLI"
  url "https://github.com/MetalHexx/TeensyROM-CLI/releases/download/v1.0.0-alpha.22/tr-cli-1.0.0-alpha.22-osx-x64.zip"
  sha256 "36faad91219bac4fbb452ea82783124b93e96391e0f5fd1c097228f9c45ae3e8"
  version "1.0.0-alpha.22"

  def install
    bin.install "tr-cli"
  end

  test do
    system "#{bin}/tr-cli", "--version"
  end
end