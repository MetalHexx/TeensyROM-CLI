class TeensyromCli < Formula
  desc "TeensyROM CLI tool"
  homepage "https://github.com/MetalHexx/TeensyROM-CLI"
  url "https://github.com/MetalHexx/TeensyROM-CLI/releases/download/1.0.0-alpha.23/tr-cli-1.0.0-alpha.23-osx-x64.zip"
  sha256 "bccd0fc99eef98e1aec71370cfe444243255673ea1ca062d52706e1a535d91a3"
  version "1.0.0-alpha.23"

  def install
    bin.install "TeensyRom.Cli"
  end

  test do
    system "#{bin}/tr-cli", "--version"
  end
end