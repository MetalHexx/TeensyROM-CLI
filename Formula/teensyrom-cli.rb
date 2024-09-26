class TeensyromCli < Formula
  desc "TeensyROM CLI tool"
  homepage "https://github.com/MetalHexx/TeensyROM-CLI"
  url "https://github.com/MetalHexx/TeensyROM-CLI/releases/download/1.0.0-alpha.27/tr-cli-1.0.0-alpha.27-osx-x64.zip"
  sha256 "eade6b518ae53cd7a8e7b6964bf84f15e22b95a354d42c445327298a3919c5d8"
  version "1.0.0-alpha.27"

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