class TeensyromCli < Formula
  desc "TeensyROM CLI tool"
  homepage "https://github.com/MetalHexx/TeensyROM-CLI"
  url "https://github.com/MetalHexx/TeensyROM-CLI/releases/download/1.0.1/tr-cli-1.0.1-osx-x64.zip"
  sha256 "a9d692ab8d4b5d63a61c2c0ca9f0984e0e0274c837471adf41ac6878ef389bd8"
  version "1.0.1"

  def install
    libexec.install Dir["*"]

    (bin/"TeensyRom.Cli").write <<~EOS
      #!/bin/zsh
      exec "#{libexec}/TeensyRom.Cli" "$@"
    EOS

    chmod 0755, bin/"TeensyRom.Cli"
  end

  test do
    system "#{bin}/TeensyRom.Cli", "--version"
  end
end