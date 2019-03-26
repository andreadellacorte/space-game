#!/usr/bin/env bash

set -e -u -x

rvm --default use 2.3.7
mkdir -p /usr/local/Homebrew/Library/Homebrew/vendor/portable-ruby/2.3.7/bin/
ln -s $(which ruby) /usr/local/Homebrew/Library/Homebrew/vendor/portable-ruby/2.3.7/bin/ruby

brew config
brew tap caskroom/cask
brew update
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