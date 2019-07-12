#!/usr/bin/env bash

set -e -u -x

brew config
brew tap caskroom/cask
brew update
brew cask install spatial

brew install mono
brew cask install dotnet-sdk

spatial version
mono --version

if ! dotnet --list-sdks | grep sdk
then
  echo "ERROR: dotnet sdk not found"
  exit 1
fi

brew config
