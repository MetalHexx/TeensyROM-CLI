class TeensyromCli < Formula
  desc "TeensyROM CLI tool"
  homepage "https://github.com/MetalHexx/TeensyROM-CLI"
  url "https://github.com/MetalHexx/TeensyROM-CLI/releases/download/1.0.0-alpha.26/tr-cli-1.0.0-alpha.26-osx-x64.zip"
  sha256 "a36a4bd8efc52dbf081a448ca830f8cf8c931ec5526ce3fa4d052ec2990c400c"
  version "1.0.0-alpha.26"

  def install
    libexec.install Dir["*"]

    (bin/"TeensyRom.Cli").write <<~EOS
      exec "#{libexec}/TeensyRom.Cli" "$@"
    EOS

    chmod "+x", bin/"TeensyRom.Cli"
  end

  test do
    system "#{bin}/TeensyRom.Cli", "--version"
  end
end