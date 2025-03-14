class TeensyromCli < Formula
  desc "TeensyROM CLI tool"
  homepage "https://github.com/MetalHexx/TeensyROM-CLI"
  url "https://github.com/MetalHexx/TeensyROM-CLI/releases/download/1.0.0-alpha.31/tr-cli-1.0.0-alpha.31-osx-x64.zip"
  sha256 "e12306d57449ca26648dab30fc433416f24913c58fe265d408ef8e2de7aabd5e"
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