class TeensyromCli < Formula
  desc "TeensyROM CLI tool"
  homepage "https://github.com/MetalHexx/TeensyROM-CLI"
  url "https://github.com/MetalHexx/TeensyROM-CLI/releases/download/1.0.0-alpha.30/tr-cli-1.0.0-alpha.30-osx-x64.zip"
  sha256 "b867917e9943879682738e6100790b642d2dd7ea05a4d9af7d15959c0fc492b4"
  version "1.0.0-alpha.30"

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