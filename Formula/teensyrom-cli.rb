class TeensyromCli < Formula
  desc "TeensyROM CLI tool"
  homepage "https://github.com/MetalHexx/TeensyROM-CLI"
  url "https://github.com/MetalHexx/TeensyROM-CLI/releases/download/1.0.0-alpha.24/tr-cli-1.0.0-alpha.24-osx-x64.zip"
  sha256 "fc69ed5a1f8742420324551f25528d0d6c68a366314b3922e68b3204bd54c470"
  version "1.0.0-alpha.24"

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