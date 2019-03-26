#!/usr/bin/env bash

set -e -u -x

brew doctor
brew update --debug --verbose
brew doctor
brew tap caskroom/cask
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