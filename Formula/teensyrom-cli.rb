class TeensyromCli < Formula
  desc "TeensyROM CLI tool"
  homepage "https://github.com/MetalHexx/TeensyROM-CLI"
  url "https://github.com/MetalHexx/TeensyROM-CLI/releases/download/1.0.0-alpha.28/tr-cli-1.0.0-alpha.28-osx-x64.zip"
  sha256 "13a8ceb8ade2307f400fecaed9b3dbac5a84393d1985d30ce441dcfe5116bb65"
  version "1.0.0-alpha.28"

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