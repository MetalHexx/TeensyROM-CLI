class TeensyromCli < Formula
  desc "TeensyROM CLI tool"
  homepage "https://github.com/MetalHexx/TeensyROM-CLI"
  url "https://github.com/MetalHexx/TeensyROM-CLI/releases/download/..."
  sha256 "YOUR_SHA256_HASH_HERE"
  version "1.0.0"

  def install
    bin.install "tr-cli"
  end

  test do
    system "#{bin}/tr-cli", "--version"
  end
end