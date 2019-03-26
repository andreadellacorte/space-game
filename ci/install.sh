#!/usr/bin/env bash

set -e -u

brew tap caskroom/cask
brew update >/dev/null
brew cask install spatial

brew install mono
brew cask install dotnet
ln -s /usr/local/share/dotnet/dotnet /usr/local/bin/