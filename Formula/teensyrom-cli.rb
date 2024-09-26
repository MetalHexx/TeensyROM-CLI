class TeensyromCli < Formula
  desc "TeensyROM CLI tool"
  homepage "https://github.com/MetalHexx/TeensyROM-CLI"
  url "https://github.com/MetalHexx/TeensyROM-CLI/releases/download/1.0.0-alpha.29/tr-cli-1.0.0-alpha.29-osx-x64.zip"
  sha256 "eff85cf6786d58281a2bf930e3107fbe793da501628698b4db0aa18674452bad"
  version "1.0.0-alpha.29"

  def install
    libexec.install Dir["*"]

    (bin/"TeensyRom.Cli").write <<~EOS
      exec "#{libexec}/TeensyRom.Cli" "$@"
    EOS

    chmod "a+x", bin/"TeensyRom.Cli"
  end

  test do
    system "#{bin}/TeensyRom.Cli", "--version"
  end
end