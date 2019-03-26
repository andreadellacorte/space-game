#!/usr/bin/env bash

set -e -u -x

echo "HOMEBREW_FORCE_VENDOR_RUBY is set to:"
echo $HOMEBREW_FORCE_VENDOR_RUBY

brew tap caskroom/cask
rvm $brew_ruby do brew update 1>/dev/null
brew cask install spatial

brew install mono
brew cask install dotnet-sdk
ln -s /usr/local/share/dotnet/dotnet /usr/local/bin/

spatial version
mono --version

if ! dotnet --list-sdks | grep sdk
then
  echo "ERROR: dotnet sdk not found"
  exit 1
fi