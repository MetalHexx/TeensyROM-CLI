class TeensyromCli < Formula
  desc "TeensyROM CLI tool"
  homepage "https://github.com/MetalHexx/TeensyROM-CLI"
  url "https://github.com/MetalHexx/TeensyROM-CLI/releases/download/1.0.0-alpha.23/tr-cli-1.0.0-alpha.23-osx-x64.zip"
  sha256 "d4ad73399993036fbc6314eab3165458b1faffe0d1e6962d7241dc29a8b43390"
  version "1.0.0-alpha.23"

  def install
    libexec.install Dir["*"]

    (bin/"TeensyRom.Cli").write <<~EOS
      exec "#{libexec}/TeensyRom.Cli" "$@"
    EOS
  end

  test do
    system "#{bin}/TeensyRom.Cli", "--version"
  end
end