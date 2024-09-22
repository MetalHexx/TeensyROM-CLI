class TeensyromCli < Formula
  desc "TeensyROM CLI tool"
  homepage "https://github.com/MetalHexx/TeensyROM-CLI"
  url "https://github.com/MetalHexx/TeensyROM-CLI/releases/download/v1.0.0-alpha.23/tr-cli-1.0.0-alpha.23-osx-x64.zip"
  sha256 "d2f816f902c64ad6d84c7e8fd522cf90754812d16bd0fbb5aefe39a356fa2d8a"
  version "1.0.0-alpha.23"

  def install
    bin.install "tr-cli"
  end

  test do
    system "#{bin}/tr-cli", "--version"
  end
end