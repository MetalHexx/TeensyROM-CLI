class TeensyromCli < Formula
  desc "TeensyROM CLI tool"
  homepage "https://github.com/MetalHexx/TeensyROM-CLI"
  url "https://github.com/MetalHexx/TeensyROM-CLI/releases/download/1.0.0-alpha.30/tr-cli-1.0.0-alpha.30-osx-x64.zip"
  sha256 "1086e0e32df923fc63fe6afc2dc98b8078c3a86e493c1ec16deb4c4e9ac89c86"
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