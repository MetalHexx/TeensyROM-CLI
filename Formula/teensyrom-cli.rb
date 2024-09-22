class TeensyromCli < Formula
  desc "TeensyROM CLI tool"
  homepage "https://github.com/MetalHexx/TeensyROM-CLI"
  url "https://github.com/MetalHexx/TeensyROM-CLI/releases/download/1.0.0-alpha.23/tr-cli-1.0.0-alpha.23-osx-x64.zip"
  sha256 "e2ca00cf439a87b3277912b4d7741d415a96d753fc340c9a83594fef9b831d83"
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