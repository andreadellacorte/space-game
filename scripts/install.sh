#!/usr/bin/env bash

set -e
set -u

brew tap caskroom/cask
brew tap improbable-io/spatialos
brew update
brew cask install spatial

brew install mono