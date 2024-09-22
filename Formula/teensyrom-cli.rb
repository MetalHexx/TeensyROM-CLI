class TeensyromCli < Formula
  desc "TeensyROM CLI tool"
  homepage "https://github.com/MetalHexx/TeensyROM-CLI"
  url "https://github.com/MetalHexx/TeensyROM-CLI/releases/download/v1.0.0-alpha.22/tr-cli-1.0.0-alpha.22-osx-x64.zip"
  sha256 "d4a535d7be52dc8280442f78f5f6f5c9a41347eb35ad2967870ee4cf63060c72"
  version "1.0.0-alpha.22"

  def install
    bin.install "tr-cli"
  end

  test do
    system "#{bin}/tr-cli", "--version"
  end
end