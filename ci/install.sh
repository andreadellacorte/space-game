#!/usr/bin/env bash

set -e -u -x

HOMEBREW_RUBY_PATH=$(which ruby)

echo $HOMEBREW_RUBY_PATH

brew update --debug --verbose
brew config
brew tap caskroom/cask
brew update --debug --verbose
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

brew config