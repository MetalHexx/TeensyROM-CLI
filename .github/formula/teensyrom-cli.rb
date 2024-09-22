class TeensyromCli < Formula
  desc "TeensyROM CLI tool"
  homepage "https://github.com/MetalHexx/TeensyROM-CLI"
  url "https://github.com/MetalHexx/TeensyROM-CLI/releases/download/v1.0.0-alpha.21/tr-cli-1.0.0-alpha.21-osx-x64.zip"
  sha256 "3a50b9f85f053faa50cd9da0e8f700768cc9f6c1ef5a41e4d653e4731b46e8ba"
  version "1.0.0-alpha.21"

  def install
    bin.install "tr-cli"
  end

  test do
    system "#{bin}/tr-cli", "--version"
  end
end