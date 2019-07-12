#!/usr/bin/env bash

# This script builds the full project by running all other provided shell scripts in sequence

pushd "$( dirname "${BASH_SOURCE[0]}" )"

source ../SpatialOS/scripts/utils.sh

FOLDER="$( dirname "${BASH_SOURCE[0]}" )/.."

function n() {

  if [ "$PLATFORM_NAME" = "win32" ]; then
    $FOLDER/Utils/balloon.bat "$1"
  fi

  if [ "$PLATFORM_NAME" = "macOS64" ]; then
    osascript -e "display notification \"$1\" with title \"SpatialOS\""
  fi
}

function b() { # Build Project
  if [ -z $1 ]; then
    WORKER='empty'
  fi

  if [ "$PLATFORM_NAME" = "win32" ]; then
    $FOLDER/SpatialOS/scripts/build_project.sh $WORKER "Windows64"

    if [ $WORKER = "empty" ]; then
      n "Project Build Complete"
    else
      n "$WORKER Build Complete"
    fi
  fi

  if [ "$PLATFORM_NAME" = "macOS64" ]; then
    start=`gdate +%s%N`
    $FOLDER/SpatialOS/scripts/build_project.sh $WORKER "macOS64"
    end=`gdate +%s%N`
    build_time=$( echo "scale=2;($end - $start)/1000000000" | bc -l )

    if [ $WORKER = "empty" ]; then
      n "Project Build Complete in $build_time seconds"
    else
      n "$(basename $1) Build Complete in $build_time seconds"
    fi
  fi
}

function ks() { # Kill Server
  if [ "$PLATFORM_NAME" = "win32" ]; then
    netstat -ano | findStr "8080"
  fi

  if [ "$PLATFORM_NAME" = "macOS64" ]; then
    lsof -ti tcp:5301 | xargs kill -9
  fi

  n 'Local Server Stopped'
}

alias s="n 'Workers Started' && $FOLDER/SpatialOS/scripts/run_server.sh" # Server Start
alias c="n 'Client Started' && $FOLDER/client/run.sh" # Client Start
alias kw="ps -ef | grep \"127.0.0.1 22000\" | grep -v grep | awk '{print \$2}' | xargs kill -9 && n 'Workers Killed'" # Kill Workers
alias kc="ps -ef | grep Client.exe | grep -v grep | awk '{print \$2}' | xargs kill -9 && n 'Client Killed'" # Kill Client
alias sd="ks && kc" # Shutdown
alias bsw="b $FOLDER/PlanetWorker && ks && s" # Build Workers & Start Server
alias brw="b $FOLDER/PlanetWorker && kw" # Build & Restart Workers
alias brc="b $FOLDER/client && kc && c" # Build & Restart Client
alias bs="b && ks && s" # Build Project & Start Server

popd
