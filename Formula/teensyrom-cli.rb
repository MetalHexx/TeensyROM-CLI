class TeensyromCli < Formula
  desc "TeensyROM CLI tool"
  homepage "https://github.com/MetalHexx/TeensyROM-CLI"
  url "https://github.com/MetalHexx/TeensyROM-CLI/releases/download/1.0.0-alpha.23/tr-cli-1.0.0-alpha.23-osx-x64.zip"
  sha256 "b32bd9eefee1c3acbe80b5c73f07742000aaaae2fe1b4a67bc32085b890d30d2"
  version "1.0.0-alpha.23"

  def install
    bin.install "TeensyRom.Cli"
  end

  test do
    system "#{bin}/TeensyRom.Cli", "--version"
  end
end