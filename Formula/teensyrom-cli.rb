class TeensyromCli < Formula
  desc "TeensyROM CLI tool"
  homepage "https://github.com/MetalHexx/TeensyROM-CLI"
  url "https://github.com/MetalHexx/TeensyROM-CLI/releases/download/1.0.0-alpha.31/tr-cli-1.0.0-alpha.31-osx-x64.zip"
  sha256 "61513e22f41f6b51117240d95939a3cd6c8a86b1c427156b41ec6fccbff4cc18"
  version "1.0.0-alpha.31"

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